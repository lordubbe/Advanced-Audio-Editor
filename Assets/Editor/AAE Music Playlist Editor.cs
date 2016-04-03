using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(AAEMusicPlaylist))]
public class AAEMusicPlaylistEditor : Editor {

	private ReorderableList list;

	void OnEnable(){
		list = new ReorderableList (serializedObject, serializedObject.FindProperty ("playlist"), true, true, true, true);

		list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
			var element = list.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;
			EditorGUI.PropertyField(
				new Rect(rect.x, rect.y, (Screen.width/3)*2, EditorGUIUtility.singleLineHeight),
				element, GUIContent.none);
		};

		list.drawHeaderCallback = (Rect rect) => {  
			EditorGUI.LabelField(rect, "Clips in Playlist");
		};
	}

	public override void OnInspectorGUI(){
		AAEMusicPlaylist playlist = (AAEMusicPlaylist)target;
		playlist.mode = (AAEMusicPlaylist.PlayListMode)EditorGUILayout.EnumPopup ("Playlist Mode", playlist.mode);

		serializedObject.Update ();
		list.DoLayoutList ();
		serializedObject.ApplyModifiedProperties ();
//		private ReorderableList list;
//
//		list = new ReorderableList(property.serializedObject, property.FindPropertyRelative("tagList"), true, true, true, true);
//		list.drawHeaderCallback += rect => GUI.Label(rect, label);
//		list.drawElementCallback += (rect, index, active, focused) =>
//		{
//			rect.height = 16;
//			rect.y += 2;
//			EditorGUI.PropertyField(rect, 
//				list.serializedProperty.GetArrayElementAtIndex(index), 
//				GUIContent.none);
//		};
//		list.DoList(position);

	}

}
