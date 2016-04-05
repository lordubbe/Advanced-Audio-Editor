using UnityEngine;
using System.Collections;

public class AAEInstance : MonoBehaviour {

	public event AAEMusicLooper.AAE_MusicEvent OnExitMarker;

	public GameObject file;
	[HideInInspector]
	public AAEMusicLooper looper;
	public AAEClip clip;
	private string origName;
	[HideInInspector]
	public AudioSource audioSource;
	bool subClipCreated = false;

	public bool independentVolume;
	public float volume = 1;
	public bool continueLoop = true;
	bool metronomeStarted = false;
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
		looper.BPM = clip.BPM;
	}


	float beatLength;
	float barLength;
	bool exitEventCalled;
	bool loopPointEventCalled;

	void Update(){
		//SET VOLUME
		if (!independentVolume) {
			audioSource.volume = looper.Volume;
		} else if (independentVolume) {
			audioSource.volume = volume;
		}
		if (!metronomeStarted && audioSource.timeSamples >= clip.preEntry) {
			looper.StartMetronome ();
			metronomeStarted = true; 
		}

		//PRE ENTRY
		if (!subClipCreated ) {
			if (looper.nextClip.GetComponent<AAEClip> ().playPreEntry && audioSource.timeSamples >= clip.postExit - looper.nextClip.GetComponent<AAEClip> ().preEntry) {
				if (continueLoop) {
					looper.InstantiateClip ();
				}
				if (OnExitMarker != null) {
					print ("next clip: " + looper.nextClip.GetComponent<AAEClip> ().clip.name);
					OnExitMarker ();
				}
				//looper.gameObject.SendMessage ("OnCurrentClipExitCue");
				subClipCreated = true;
			} else if (!looper.nextClip.GetComponent<AAEClip> ().playPreEntry && audioSource.timeSamples >= clip.postExit) {
				if (continueLoop) {
					looper.InstantiateClip ();
				}
				subClipCreated = true;
				if (OnExitMarker != null) {
					OnExitMarker ();
				}
			}
		} 
		//POST EXIT
		if (subClipCreated || !continueLoop) {
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

	void OnDestroy(){
		OnExitMarker = null;
	}
}
