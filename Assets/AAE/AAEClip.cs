using UnityEngine;
using System.Collections;

public class AAEClip : MonoBehaviour {

	public AudioClip clip = null;
	//[HideInInspector]
	public int BPM;

	[HideInInspector]
	public Texture2D preview = null;
	[HideInInspector]
	public float preEntry = 0;
	[HideInInspector]
	public float postExit = 0;

	public int Step;
	public int Base;

	public bool playPreEntry = true;
	public bool playPostExit = true;
}
