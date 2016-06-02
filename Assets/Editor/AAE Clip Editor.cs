using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(AAEClip))]
public class AAEClipEditor : Editor {

	public override void OnInspectorGUI(){
		AAEClip clip = (AAEClip)target;
		clip.clip = (AudioClip)EditorGUILayout.ObjectField ("Clip", clip.clip, typeof(AudioClip), true);
		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("BPM: ", EditorStyles.boldLabel, GUILayout.MaxWidth(Screen.width/2));
		EditorGUILayout.LabelField (clip.BPM.ToString (), GUILayout.MaxWidth(50));
		EditorGUILayout.EndHorizontal ();

		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("Time Signature: ", EditorStyles.boldLabel, GUILayout.MaxWidth (Screen.width/2));
		EditorGUILayout.LabelField (clip.Step.ToString ()+"/"+clip.Base.ToString(), GUILayout.MaxWidth (100));
		EditorGUILayout.EndHorizontal ();

		clip.playPreEntry = EditorGUILayout.Toggle ("Play Pre-Entry", clip.playPreEntry);
		clip.playPostExit = EditorGUILayout.Toggle ("Play Post-Exit", clip.playPostExit);


	}

	public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height){
		Debug.Log ("UPDATED THE PREVIEW!");
		Texture2D staticPrev = AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/MFYGSticker.png") as Texture2D;
		staticPrev.Apply ();
		return staticPrev;
	}

}
