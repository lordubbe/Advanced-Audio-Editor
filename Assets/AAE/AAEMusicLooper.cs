using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

public class AAEMusicLooper : MonoBehaviour {
	//EVENTS
	public delegate void AAE_MusicEvent();
//	public static event AAE_MusicEvent OnExitMarker;
//	public static event AAE_MusicEvent OnBeat;
//	public static event AAE_MusicEvent OnBar;

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
			ChangeMusic (Playlist, ChangeMode.Instant, ChangeType.Crossfade);
		}
	}
//TEMP DEV CONTROLS END

	public enum ChangeMode{
		Instant, NextBeat, ExitMarker
	};
	public enum ChangeType{
		None, Crossfade, TransitionClip
	}

	public void ChangeMusic(GameObject newMusic, ChangeMode changeMode, ChangeType type){
		if (newMusic.GetComponent<AAEClip> () != null) {//has AAE Clip
			mode = LoopMode.SingleClip;
			AAE_Clip = newMusic;
		} else if (newMusic.GetComponent<AAEMusicPlaylist> () != null) {//has AAE Music Playlist
			mode = LoopMode.Playlist;
			Playlist = newMusic;
		} else {
			Debug.LogError ("AAE Music Looper: [void ChangeMusic()]: Invalid GameObject passed! Does not contain 'AAE Clip' or 'AAE Music Playlist' component!");
		}


		switch (type) {
		case ChangeType.None:
			//
			break;
		case ChangeType.Crossfade:
			//
			currentClip.GetComponent<AAEInstance>().continueLoop = false;
			FadeOutClip (currentClip, 3f);
			Initialise ();
			InstantiateClip ();
			FadeInClip (currentClip, 3f);
			break;
		case ChangeType.TransitionClip:
			//
			break;
		}

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
			//Destroy (clip.gameObject);
			break;
		}
		print ("coroutine should have stopped now...");
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
}
