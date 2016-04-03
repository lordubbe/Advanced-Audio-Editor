using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(AAEClip))]
public class AAEClipEditor : Editor {

	public override void OnInspectorGUI(){
		AAEClip clip = (AAEClip)target;

		clip.clip = (AudioClip)EditorGUILayout.ObjectField ("Clip", clip.clip, typeof(AudioClip), true);
		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("BPM: ", EditorStyles.boldLabel, GUILayout.MaxWidth(50));
		EditorGUILayout.LabelField (clip.BPM.ToString ());
		EditorGUILayout.EndHorizontal ();
		clip.playPreEntry = EditorGUILayout.Toggle ("Play Pre-Entry", clip.playPreEntry);
		clip.playPostExit = EditorGUILayout.Toggle ("Play Post-Exit", clip.playPostExit);


	}

}
