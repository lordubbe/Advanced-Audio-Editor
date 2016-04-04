using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

public class AAEMusicLooper : MonoBehaviour {
	//EVENTS & BARS/BEATS SYNC STUFF
	public delegate void AAE_MusicEvent();
	public static event AAE_MusicEvent OnBeat;
	public static event AAE_MusicEvent OnBar;

	int Base = 4;
	int Step = 4;
	public float BPM;
	int CurrentStep = 1;
	int CurrentMeasure;
	float interval;
	float nextTime;

	public enum LoopMode{
		SingleClip, Playlist 
	};
	public LoopMode mode;

	public bool startOnPlay = false;

	[Range(0,1)]
	public float Volume = 1;

	public GameObject AAE_Clip;
	public GameObject Playlist;

	AAEClip clip;
	AAEMusicPlaylist playlist;

	private bool loopCalled;
	private bool isFadingOut;
	private float oldVolume;
	public List<GameObject> activeClips = new List<GameObject>();

	public GameObject nextClip;
	public GameObject currentClip;
	string origName;
	bool changeCalled;
	// Use this for initialization
	void OnEnable () {
		Initialise ();
	}

	void Initialise(){
		if (mode == LoopMode.SingleClip) {
			clip = AAE_Clip.GetComponent<AAEClip> ();
			if (clip == null) {
				Debug.LogError ("AAE MusicLooper: ERROR! It seems you've forgotten to drag in your clip of choice in the 'AAE_File' slot!");
			}
		} else if (mode == LoopMode.Playlist) {
			origName = Playlist.name;
			Playlist = (GameObject)Instantiate (Playlist, transform.position, Quaternion.identity);
			Playlist.name = origName;
			Playlist.transform.parent = this.transform;
			playlist = Playlist.GetComponent<AAEMusicPlaylist> (); 
			if (playlist == null) {
				Debug.LogError ("AAE MusicLooper: ERROR! It seems you've forgotten to drag in your playlist of choice in the 'Playlist' slot!");
			}
		}
		oldVolume = 1;
		if (startOnPlay) {
			Play ();
		}
	}

	/// <summary>
	/// Start the AAE Music Looper. If it is already playing, this function will do nothing.
	/// </summary>
	public void Play(){
		if (!isPlaying ()) {
			if ((mode == LoopMode.Playlist && playlist != null) || (mode == LoopMode.SingleClip && clip != null)) {
				Volume = oldVolume;
				InstantiateClip ();
			} else {
				Debug.LogError ("AAE MusicLooper: ERROR! No AAE Clip or AAE Playlist to play or bad GameObject in MusicLooper 'Clip' or 'Playlist' field!");
			}
		}
	}
	/// <summary>
	/// Is the AAE Music Looper currently playing?
	/// </summary>
	/// <returns><c>true</c>, if currently playing, <c>false</c> otherwise.</returns>
	public bool isPlaying(){
		if (activeClips.Count > 0) {
			return true;
		} else {
			return false;
		}
	}

	/// <summary>
	/// Stop the AAE Music Looper. If it is not playing, this function will do nothing.
	/// </summary>
	public void Stop(){
		if (isPlaying ()) {
			List<int> indeces = new List<int> ();
			for (int i = 0; i < activeClips.Count; i++) {
				if (activeClips [i] != null) {
					if (activeClips [i].GetComponent<AAEMusicPlaylist> () == null && activeClips [i].GetComponent<AAEInstance> () != null) {//if it doesn't have a playlist, but has an AAEInstance
						Destroy (activeClips [i]);
						indeces.Add (i);
					}
				} else {
					indeces.Add (i);
				}
			}
			indeces.Sort ();
			for (int i = indeces.Count - 1; i >= 0; i--) {
				activeClips.RemoveAt (indeces [i]);
			}
			if (mode == LoopMode.Playlist) {
				playlist.Reset ();
			}
		}
	}

		

	/// <summary>
	/// Fade out whatever is playing over <param name="time">'time'</param> seconds.
	/// </summary>
	/// <param name="time">Time.</param>
	public void FadeoutAndStop(float time){
		if (!isFadingOut) {
			StartCoroutine ("FadeAndStop", time);
		}
	}

	private IEnumerator FadeAndStop(float s){
		isFadingOut = true;
		oldVolume = Volume;
		float i = Volume;
		for (float t = 0; t < 1; t += Time.deltaTime / s) {
			Volume = Mathf.Lerp (i, 0, t);
			yield return null;
		}
		Stop ();
		isFadingOut = false;
	}

//TEMP DEV CONTROLS START
	void Update(){
		if (Input.GetKeyDown (KeyCode.P)) {
			if (isPlaying()) {
				FadeoutAndStop (2.0f);
			} else {
				Play ();
			}
		}
		if (Input.GetKeyDown (KeyCode.C)) {
			ChangeMusic (Playlist, ChangeMode.ExitMarker, ChangeType.Crossfade);
		}
	}
//TEMP DEV CONTROLS END

	public enum ChangeMode{
		Instant, NextBeat, NextBar, ExitMarker
	};
	public enum ChangeType{
		None, Crossfade//, TransitionClip
	}

	public void ChangeMusic(GameObject newMusic, ChangeMode changeMode, ChangeType type){
		if (!changeCalled) {
			switch (changeMode) {
			case ChangeMode.Instant:
				if (newMusic.GetComponent<AAEClip> () != null) {//has AAE Clip
					mode = LoopMode.SingleClip;
					AAE_Clip = newMusic;
				} else if (newMusic.GetComponent<AAEMusicPlaylist> () != null) {//has AAE Music Playlist
					mode = LoopMode.Playlist;
					Playlist = newMusic;
				} else {
					Debug.LogError ("AAE Music Looper: [void ChangeMusic()]: Invalid GameObject passed! Does not contain 'AAE Clip' or 'AAE Music Playlist' component!");
					return;
				}
				AAEInstance c = currentClip.GetComponent<AAEInstance> ();
				c.continueLoop = false;
				switch (type) {
				case ChangeType.None:
					c.independentVolume = true;
					c.volume = 0;
					Initialise ();
					InstantiateClip ();
					break;
				case ChangeType.Crossfade:
				//
					FadeOutClip (currentClip, 3f);
					Initialise ();
					InstantiateClip ();
					FadeInClip (currentClip, 3f);
					break;
//			case ChangeType.TransitionClip:
//				//
//				break;
				}
				break;
			case ChangeMode.NextBeat:
			//
				changeCalled = true;
				changeMusic = newMusic;
				changeType = type;
				OnBeat += nextBeatChange;
				break;
			case ChangeMode.NextBar:
			//
				changeCalled = true;
				changeMusic = newMusic;
				changeType = type;
				OnBar += nextBarChange;
				break;
			case ChangeMode.ExitMarker:
			//
				if (newMusic.GetComponent<AAEMusicPlaylist> () != null) {
					nextClip = newMusic.GetComponent<AAEMusicPlaylist> ().playlist [0];
					currentClip.GetComponent<AAEInstance> ().continueLoop = false;
				} else if (newMusic.GetComponent<AAEClip> () != null) {
					nextClip = newMusic;
					currentClip.GetComponent<AAEInstance> ().continueLoop = false;
				}
				changeCalled = true;
				changeMusic = newMusic;
				changeType = type;
				currentClip.GetComponent<AAEInstance> ().OnExitMarker += ExitMarkerChange;
				break;
			}
		}
	}

	GameObject changeMusic; 
	ChangeType changeType;

	void nextBeatChange(){
		OnBeat -= nextBeatChange;
		if (changeMusic != null && changeType != null) {
			if (changeMusic.GetComponent<AAEClip> () != null) {//has AAE Clip
				mode = LoopMode.SingleClip;
				AAE_Clip = changeMusic;
			} else if (changeMusic.GetComponent<AAEMusicPlaylist> () != null) {//has AAE Music Playlist
				mode = LoopMode.Playlist;
				Playlist = changeMusic;
			} else {
				Debug.LogError ("AAE Music Looper: [void ChangeMusic()]: Invalid GameObject passed! Does not contain 'AAE Clip' or 'AAE Music Playlist' component!");
				return;
			}
			AAEInstance c = currentClip.GetComponent<AAEInstance> ();
			switch (changeType) {
			case ChangeType.None:
				c.continueLoop = false;
				c.independentVolume = true;
				c.volume = 0;
				Initialise ();
				InstantiateClip ();
				break;
			case ChangeType.Crossfade:
				c.continueLoop = false;
				FadeOutClip (currentClip, 3f);
				Initialise ();
				InstantiateClip ();
				FadeInClip (currentClip, 3f);
				break;
//			case ChangeType.TransitionClip:
//			//
//				break;
			}
		}
		changeCalled = false;
	}
	void nextBarChange(){
		OnBar -= nextBarChange;
		if (changeMusic != null && changeType != null) {
			if (changeMusic.GetComponent<AAEClip> () != null) {//has AAE Clip
				mode = LoopMode.SingleClip;
				AAE_Clip = changeMusic;
			} else if (changeMusic.GetComponent<AAEMusicPlaylist> () != null) {//has AAE Music Playlist
				mode = LoopMode.Playlist;
				Playlist = changeMusic;
			} else {
				Debug.LogError ("AAE Music Looper: [void ChangeMusic()]: Invalid GameObject passed! Does not contain 'AAE Clip' or 'AAE Music Playlist' component!");
				return;
			}
			AAEInstance c = currentClip.GetComponent<AAEInstance> ();
			switch (changeType) {
			case ChangeType.None:
				c.continueLoop = false;
				c.independentVolume = true;
				c.volume = 0;
				Initialise ();
				InstantiateClip ();
				break;
			case ChangeType.Crossfade:
				c.continueLoop = false;
				FadeOutClip (currentClip, 3f);
				Initialise ();
				InstantiateClip ();
				FadeInClip (currentClip, 3f);
				break;
//			case ChangeType.TransitionClip:
//				//
//				break;
			}
		}
		changeCalled = false;
	}

	void ExitMarkerChange(){
		currentClip.GetComponent<AAEInstance>().OnExitMarker -= ExitMarkerChange;
		if (changeMusic != null && changeType != null) {
			if (changeMusic.GetComponent<AAEClip> () != null) {//has AAE Clip
				mode = LoopMode.SingleClip;
				AAE_Clip = changeMusic;
			} else if (changeMusic.GetComponent<AAEMusicPlaylist> () != null) {//has AAE Music Playlist
				mode = LoopMode.Playlist;
				Playlist = changeMusic;
			} else {
				Debug.LogError ("AAE Music Looper: [void ChangeMusic()]: Invalid GameObject passed! Does not contain 'AAE Clip' or 'AAE Music Playlist' component!");
				return;
			}
			AAEInstance c = currentClip.GetComponent<AAEInstance> ();
			switch (changeType) {
			case ChangeType.None:
				c.continueLoop = false;
				c.independentVolume = true;
				c.volume = 0;
				Initialise ();
				InstantiateClip ();
				break;
			case ChangeType.Crossfade:
				c.continueLoop = false;
				FadeOutClip (currentClip, 3f);
				Initialise ();
				InstantiateClip ();
				FadeInClip (currentClip, 3f);
				break;
//			case ChangeType.TransitionClip:
//				//
//				break;
			}
		}
		changeCalled = false;
	}

	void FadeOutClip(GameObject clip, float s){
		StartCoroutine(FadeClip(clip, FadeType.Out, s));
	}

	void FadeInClip(GameObject clip, float s){
		StartCoroutine (FadeClip (clip, FadeType.In, s));
	}

	public enum FadeType{
		In, Out
	}

	private IEnumerator FadeClip(GameObject go, FadeType type, float s){
		AAEInstance clip = go.GetComponent<AAEInstance> ();
		clip.independentVolume = true;
		float i;
		switch (type) {
		case FadeType.Out:
			i = clip.volume;
			for (float t = 0; t < 1; t += Time.deltaTime / s) {
				clip.volume = Mathf.Lerp (i, 0, t);
				yield return null;
			}
			break;
		case FadeType.In:
			i = 0;
			for (float t = 0; t < 1; t += Time.deltaTime / s) {
				clip.volume = Mathf.Lerp (i, 1, t);
				yield return null;
			}
			break;
		}
	}


	public void InstantiateClip(){
		if (mode == LoopMode.SingleClip) {
			GameObject go = new GameObject (AAE_Clip.name);
			AAEInstance a = go.AddComponent<AAEInstance> ();
			go.transform.parent = this.transform;
			a.file = AAE_Clip;
			//a.clip = file.clip;
			a.looper = this;
			nextClip = AAE_Clip;
			activeClips.Add (go);
			currentClip = go;
		} else if (mode == LoopMode.Playlist) {
			GameObject go = new GameObject (playlist.currentClip.name);
			AAEInstance a = go.AddComponent<AAEInstance> ();
			go.transform.parent = this.transform;
			a.file = playlist.currentClip;
			a.looper = this;
			playlist.Advance ();
			nextClip = playlist.currentClip;
			activeClips.Add (go);
			currentClip = go;
		}
	}

	public void StartMetronome()
	{
		StopCoroutine("DoTick");
		Base = currentClip.GetComponent<AAEInstance>().clip.Base;
		Step = currentClip.GetComponent<AAEInstance>().clip.Step;
		CurrentStep = 1;
		var multiplier = Base / 4f;
		var tmpInterval = 60f / BPM;
		interval = tmpInterval / multiplier;
		nextTime = Time.time;
		StartCoroutine("DoTick");
	}

	IEnumerator DoTick()
	{
		for (; ; )//lel crying man
		{
			if (CurrentStep == 1 && OnBar != null) {
				OnBar ();
			}
			if (OnBeat != null) {
				OnBeat ();
			}
			nextTime += interval;
			yield return new WaitForSeconds(nextTime - Time.time);
			CurrentStep++;
			if (CurrentStep > Step)
			{
				CurrentStep = 1;
				CurrentMeasure++;
			}
		}
	}
}
