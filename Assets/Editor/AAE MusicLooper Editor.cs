using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(AAEMusicLooper))]
public class AAEMusicLooperEditor : Editor {

	public override void OnInspectorGUI(){
		AAEMusicLooper looper = (AAEMusicLooper)target;
//	//STUPID COLORS BEGIN
//		EditorGUILayout.LabelField ("AAE MUSIC LOOPER");
//		texOrange = new Texture2D(1, 1, TextureFormat.RGBA32, false);
//		texOrange.SetPixel(0, 0, mfygOrange);
//		texOrange.Apply();
//		Rect lastRect = GUILayoutUtility.GetLastRect ();
//		lastRect.x -= 10;
//		lastRect.height = 90;
//		lastRect.width += 10;
//		GUI.DrawTexture(lastRect, texOrange, ScaleMode.StretchToFill);
//	//STUPID COLORS END
		looper.mode = (AAEMusicLooper.LoopMode)EditorGUILayout.EnumPopup ("Looping Mode", looper.mode);
		looper.startOnPlay = EditorGUILayout.Toggle ("Start On Play", looper.startOnPlay);
		looper.Volume = EditorGUILayout.Slider ("Volume", looper.Volume, 0, 1);

		if (looper.mode == AAEMusicLooper.LoopMode.Playlist) {//PLAYLIST
			looper.Playlist = (GameObject)EditorGUILayout.ObjectField("Playlist", looper.Playlist, typeof(GameObject), true);
			if (looper.Playlist == null) {
				EditorGUILayout.HelpBox ("Remember to assign a GameObject with an 'AAE Music Playlist' component to the 'Playlist' field!", MessageType.Warning);
			}else if (looper.Playlist.GetComponent<AAEMusicPlaylist> () == null) {
				EditorGUILayout.HelpBox ("ERROR! '"+looper.Playlist.name+"' doesn't have an 'AAE Music Playlist' component!", MessageType.Error);
			}
		} else if (looper.mode == AAEMusicLooper.LoopMode.SingleClip) {//SINGLE CLIP
			looper.AAE_Clip = (GameObject)EditorGUILayout.ObjectField("Clip", looper.AAE_Clip, typeof(GameObject), true);
			if (looper.AAE_Clip == null) {
				EditorGUILayout.HelpBox ("Remember to assign a GameObject with an 'AAE Clip' component to the 'Clip' field!", MessageType.Warning);
			} else if (looper.AAE_Clip.GetComponent<AAEClip> () == null) {
				EditorGUILayout.HelpBox ("ERROR! '"+looper.AAE_Clip.name+"' doesn't have an 'AAE Clip' component!", MessageType.Error); 
			}
		}
	}
}
