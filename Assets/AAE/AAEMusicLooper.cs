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

	public enum ChangeMode{
		Instant, NextBeat, NextBar, ExitMarker
	};

	struct MusicChange{
		public LoopMode loopMode;
		public ChangeMode changeMode;
		public GameObject music;
	}

	/// <summary>
	/// Cue a music change from what's currently playing to <param name="newMusic">newMusic</param> (either AAE Clip or AAE Music Playlist). Music changes with the supplied 'mode' parameter.
	/// </summary>
	/// <param name="newMusic">New music.</param>
	/// <param name="mode">Mode.</param>
	public void ChangeMusic(GameObject newMusic, ChangeMode changeMode){
		MusicChange change;

		if (newMusic.GetComponent<AAEClip> () != null) {//CLIP
			AAEClip clip = newMusic.GetComponent<AAEClip> ();//

			if (mode == LoopMode.SingleClip) {
				
			} else if (mode == LoopMode.Playlist) {
			
			}

		} else if (newMusic.GetComponent<AAEMusicPlaylist> () != null) {//PLAYLIST
			AAEMusicPlaylist playlist = newMusic.GetComponent<AAEMusicPlaylist> ();

			if (mode == LoopMode.SingleClip) {

			} else if (mode == LoopMode.Playlist) {

			}

		} else {
			Debug.LogError ("AAE Music Looper: [void ChangeMusic()]: Invalid GameObject passed as 'newMusic' parameter! Must contain either 'AAE Clip' or 'AAE Music Playlist' component!");
			return;
		}

		switch (changeMode) {
		case ChangeMode.Instant:
			//
			break;
		case ChangeMode.NextBeat:
			//
			break;
		case ChangeMode.NextBar:
			//
			break;
		case ChangeMode.ExitMarker:
			//
			break;
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
	}
//TEMP DEV CONTROLS END

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
		} else if (mode == LoopMode.Playlist) {
			GameObject go = new GameObject (playlist.currentClip.name);
			AAEInstance a = go.AddComponent<AAEInstance> ();
			go.transform.parent = this.transform;
			a.file = playlist.currentClip;
			a.looper = this;
			playlist.Advance ();
			nextClip = playlist.currentClip;
			activeClips.Add (go);
		}
	}
}
