using UnityEngine;
using System.Collections;

public class AAEInstance : MonoBehaviour {

	public GameObject file;
	[HideInInspector]
	public AAEMusicLooper looper;
	private AAEClip clip;
	private string origName;
	[HideInInspector]
	public AudioSource audioSource;
	bool subClipCreated = false;

	//coroutines
	bool isBeatRoutineRunning;
	bool isBarRoutineRunning;
	int beatCount;


	// Use this for initialization
	void Start () {
		origName = transform.name;
		audioSource = gameObject.GetComponent<AudioSource> ();
		if (audioSource == null) {
			audioSource = gameObject.AddComponent<AudioSource> ();
		}
			
		if (file == null) {
			Debug.LogWarning ("AAEInstance: ERROR! No file! ARRGGHHHH!");
		} else {
			clip = file.GetComponent<AAEClip> ();
		}
//		audioSource.playOnAwake = true;
		audioSource.clip = clip.clip;
		if (clip.playPreEntry) {
			audioSource.Play ();
		} else {
			audioSource.timeSamples = (int)clip.preEntry;
			audioSource.Play ();
		}
	}


	float beatLength;
	float barLength;
	bool exitEventCalled;
	bool loopPointEventCalled;

	void Update(){
		//SET VOLUME
		audioSource.volume = looper.Volume;
		//print ("beatlength: "+beatLength+", out of: "+clip.clip.samples);
		//print(audioSource.timeSamples);
		//FROM AAE Window
//		clipLength = currentFile.clip.length;//in seconds
//		samplesPerSec = currentFile.clip.frequency;
//		clipLengthSamples = currentFile.clip.samples;
//		beatsPerSec = (currentFile.BPM / 60f);
//		beatsInClip = clipLength/beatsPerSec* 4;

//		for (int i = 0; i < bars / 4f; i++) {
//			EditorGUI.DrawRect (new Rect (position.width / (bars / 4f) * i, musicRect.y, 1, currentFile.preview.height), new Color (255, 255, 255, 0.6f));
//		}
//		//beats
//		for (int i = 0; i < bars; i++) {
//			EditorGUI.DrawRect (new Rect (position.width / bars * i, musicRect.y, 1, currentFile.preview.height), new Color (255, 255, 255, 0.3f));
//		}
//		

//		//handle events
//		if(audioSource.timeSamples % beatLength < 100){
//			if (OnBeat != null) {
//				OnBeat ();
//			}
//			print ("BEAT");
//		}
//		if (audioSource.timeSamples % barLength == 0) {
//			if (OnBar != null) {
//				OnBar ();
//			}
//			print ("BAR");
//		}

//		if (audioSource.timeSamples >= clip.postExit && !exitEventCalled) {
//			if (OnExitMarker != null) {
//				OnExitMarker ();
//				exitEventCalled = true;
//			}
//		}
//		if (audioSource.timeSamples >= clip.postExit - looper.nextClip.GetComponent<AAEClip> ().preEntry && !loopPointEventCalled) {
//			if (OnLoopPoint != null) {
//				OnLoopPoint ();
//				loopPointEventCalled = true;
//			}
//		}


		//PRE ENTRY
		if (!subClipCreated) {
			if (looper.nextClip.GetComponent<AAEClip> ().playPreEntry && audioSource.timeSamples >= clip.postExit - looper.nextClip.GetComponent<AAEClip> ().preEntry) {
				looper.InstantiateClip ();
				//looper.gameObject.SendMessage ("OnCurrentClipExitCue");
				subClipCreated = true;
			} else if (!looper.nextClip.GetComponent<AAEClip> ().playPreEntry && audioSource.timeSamples >= clip.postExit) {
				looper.InstantiateClip ();
				subClipCreated = true;
			}
		} 
		//POST EXIT
		if (subClipCreated) {
			if (clip.playPostExit && (audioSource.timeSamples == 0 || audioSource.timeSamples == clip.clip.samples)) {
				Destroy (gameObject); //destroy when done
			} else if (!clip.playPostExit && audioSource.timeSamples >= clip.postExit) {
				Destroy (gameObject);
			}
		}


		//UPDATE NAMES IN EDITOR HIERARCHY
		if (audioSource.isPlaying) {
			if (audioSource.timeSamples < clip.preEntry) {
				transform.name = origName + " (Playing Pre-Entry)";
			} else if (audioSource.timeSamples > clip.postExit) {
				transform.name = origName + " (Playing Post-Exit)";
			} else {
				transform.name = origName + " (Playing)";
			}
		}
	}
}
