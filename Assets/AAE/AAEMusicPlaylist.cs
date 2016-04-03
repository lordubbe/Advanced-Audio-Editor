using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class AAEMusicPlaylist : MonoBehaviour {

	public enum PlayListMode{
		Random, Sequence
	};

	public PlayListMode mode;


	public List<GameObject> playlist;

	[HideInInspector]
	public GameObject currentClip; 
	private int clipIndex;

	// Use this for initialization
	void Awake () {
		if (playlist.Count == 0) {
			Debug.LogError ("AAE Playlist: WARNING! No AAE Clips in playlist!");
		}else if(playlist.Count > 0){
			AAEClip clip;
			foreach (GameObject go in playlist) {
				if (go.GetComponent<AAEClip> () == null) {
					Debug.LogError ("AAE Playlist: ERROR! Bad element in playlist ("+go.name+")! Please only include GameObjects that have an 'AAE Clip' component!");
				}
			}
			currentClip = playlist [0];
			clipIndex = 0;

		}
	}

	public void Reset(){
		currentClip = playlist [0];
		clipIndex = 0;
	}

	public void Advance(){
		if (mode == PlayListMode.Sequence) {//SEQUENCE MODE
			clipIndex++;
			if (clipIndex > playlist.Count - 1) {
				clipIndex = 0;
			}
		} else if (mode == PlayListMode.Random) {//RANDOM MODE
			clipIndex = UnityEngine.Random.Range (0, playlist.Count);
		}
		currentClip = playlist [clipIndex];
	}


}
