using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

public class AAEWindow : EditorWindow {

	static string defaultSavePath = "Assets/Music";
	static string userSavePath = defaultSavePath;
	static string saveName = "AAE File";
	static string oldSavePath = userSavePath;
	//window update handling
//	Rect musicWindow;
//	static float musicWindowWidth;
//	static float musicWindowHeight;

	int newPos;

	//music settings
	public static bool snapToBars;
	public static bool snapToBeats;
	public static bool loop;
	private bool isLooping;
	public static bool loopCalled = false;
	float clipLength;//in seconds
	float clipLengthSamples;//in samples
	float beatsPerSec;
	float beatsInClip;
	int samplesPerSec;
	//Music rect
	Rect musicRect;
	Rect previewLine;
	float previewPoint;
	bool postExitSelected = false;
	bool preEntrySelected = false;
	bool isPaused;

	Color mfygBlue = new Color (0, 0.71f, 0.97f);
	Color mfygOrange = new Color (0.96f, 0.58f, 0.117f);

	public static AAEFile currentFile = null;

	[MenuItem ("MUSIC FOR YOUR GAME/Advanced Audio Editor")]//put it in the [Window] tab
	static void Init(){
		//get existing open window, or if none, make a new window
		AAEWindow window = (AAEWindow)EditorWindow.GetWindow(typeof (AAEWindow));
		window.Show ();
		window.minSize = new Vector2 (400, 510);
		//window
		currentFile = null;
		loopCalled = false;
		EditorApplication.update += update;
	}

	private static Texture2D texOrange;
	private static Texture2D texBlue;

	void OnGUI(){
		//	MFYG ORANGE BACKGROUND
		texOrange = new Texture2D(1, 1, TextureFormat.RGBA32, false);
		texOrange.SetPixel(0, 0, mfygOrange);
		texOrange.Apply();
		GUI.DrawTexture(new Rect(0, 0, position.width, position.height), texOrange, ScaleMode.StretchToFill);

		//  MFYG BLUE BACKGROUND
		if (currentFile != null) {
			texBlue = new Texture2D (1, 1, TextureFormat.RGBA32, false);
			texBlue.SetPixel (0, 0, mfygBlue);
			texBlue.Apply ();
			GUI.DrawTexture (new Rect (0, 110, position.width, 113), texBlue, ScaleMode.StretchToFill);
		}

		GUIStyle headline = GUI.skin.GetStyle ("Label");
		headline.fontSize = 16;
		headline.alignment = TextAnchor.UpperCenter;
		headline.fontStyle = FontStyle.Bold;
		headline.normal.textColor = Color.black;

		GUILayout.Label ("Advanced Audio Editor (NGJ16 Edition)", headline);
		EditorGUILayout.Space();

		if (currentFile != null) {
			clipLength = currentFile.clip.length;//in seconds
			samplesPerSec = currentFile.clip.frequency;
			clipLengthSamples = currentFile.clip.samples;
			beatsPerSec = (currentFile.BPM / 60f);
			beatsInClip = clipLength/beatsPerSec* currentFile.Base;
			if (currentFile.preview != null) {
				GeneralSettings ();
				EditorGUILayout.BeginHorizontal ();

				GUI.backgroundColor = new Color(1,0.5f,0.5f);
				GUI.contentColor = Color.white;

				if(currentFile != null){
					if (GUILayout.Button (new GUIContent("Unload music", "Press this button to unload the current clip and return to the drag & drop screen."))) {
						currentFile = null;
					}//
				}

				GUI.backgroundColor = Color.white;
				GUI.contentColor = Color.white;

				Saving ();
				EditorGUILayout.EndHorizontal ();
				GUILayout.Space (20);
				MusicDraw ();
				DrawGrid (beatsInClip);
//				EditCues ();
				GUILayout.Space (10);
				TransportControls ();
//				Saving ();
				GUILayout.Space (15);
				EditCues ();
			}
			if (currentFile != null) {
				previewPoint = previewLine.x / position.width * clipLengthSamples;
				if (isLooping && !loopCalled && previewPoint >= currentFile.postExit - currentFile.preEntry) {
					PlayClip (currentFile.clip);
					loopCalled = true;
				} else {
					loopCalled = false;
				}
			}

		} else {
			DropAreaGUI ();
		}
		if(currentFile != null && IsClipPlaying(currentFile.clip)){
			Repaint();
		}
	}
	void GeneralSettings(){
		if (currentFile != null) {
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.BeginVertical ();
			EditorGUILayout.BeginHorizontal ();
			EditorGUIUtility.labelWidth = 20f;
			EditorGUILayout.LabelField (new GUIContent("Save Path:", "The destination at which the edited clip will be saved."));
			userSavePath = EditorGUILayout.TextField (userSavePath, GUILayout.MinWidth (position.width / 2));
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent("Save Name:", "The name that the saved file will receive."));
			saveName = EditorGUILayout.TextField (saveName, GUILayout.MinWidth (position.width / 2));
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.EndVertical ();
			EditorGUILayout.EndHorizontal ();
		}
	}
	void Saving(){
		if (currentFile != null) {
			GUI.backgroundColor = new Color(0.5f,1f,0.5f);
			GUI.contentColor = Color.white;

			if (GUILayout.Button (new GUIContent("SAVE TO '" + userSavePath + "'", "Press this button to save the edited clip to the chosen destination."))) {
				if (userSavePath [userSavePath.Length - 1].ToString() == "/") {
					string newSavePath = "";
					oldSavePath = userSavePath;
					for(int i=0; i<userSavePath.Length-1; i++){
						newSavePath += userSavePath [i];
					}
					userSavePath = newSavePath;
				}
				if (AssetDatabase.IsValidFolder (userSavePath)) {
					Save ();
				} else {
					string[] folders = userSavePath.Split ('/');
					for (int i = 1; i < folders.Length; i++) {
						string folderPath = "Assets";
						for (int j = 1; j <= i; j++) {
							folderPath += "/" + folders [j];
						}
						if (!AssetDatabase.IsValidFolder (folderPath)) {
							//create folder
							folderPath = "";
							for (int j = 0; j <= i - 1; j++) {
								if (j > 0) {
									folderPath += "/" + folders [j];
								} else {
									folderPath += folders [j];
								}
							}
							Debug.Log (folderPath);
							AssetDatabase.CreateFolder (folderPath, folders [i]);
							Debug.Log ("Created folder in '" + folders [i - 1] + "', called '" + folders [i] + "'");

						}
					}
					//now folders are made, so we can create the asset
					Save ();

				}
			}
			GUI.backgroundColor = Color.white;
			GUI.contentColor = Color.white;
		}
	}

	void Save(){
		GameObject go = new GameObject(saveName);
		AAEClip a = go.AddComponent<AAEClip>();
		a.clip = currentFile.clip;
		a.preEntry = currentFile.preEntry;
		a.postExit = currentFile.postExit;
		a.preview = currentFile.preview;
		a.BPM = currentFile.BPM;
		a.Step = currentFile.Step;
		a.Base = currentFile.Base;
		UnityEngine.Object g = PrefabUtility.CreatePrefab (userSavePath + "/" + saveName + ".prefab", go);
		AssetDatabase.SaveAssets ();
		AssetDatabase.Refresh ();
		DestroyImmediate (go);
		userSavePath = oldSavePath;
	}
		

	void TransportControls(){
		if (currentFile != null) {
			GUILayout.Label (new GUIContent("Transport", "These are your Transport Controls with which you can play, pause and stop the clip, as well as test if it loops correctly with itself."));
			GUILayout.BeginHorizontal ();
			GUILayout.Space (position.width / 4);

			GUI.backgroundColor = mfygBlue;
			GUI.contentColor = Color.white;

			if (GUILayout.Button (new GUIContent(AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/PLAY.png"), "PLAY"))) {//PLAY
				if (!IsClipPlaying (currentFile.clip)) {
					PlayClip (currentFile.clip);

				} else if (isPaused) {
					ResumeClip (currentFile.clip);
					isPaused = false;
				}
			}
			if(IsClipPlaying (currentFile.clip) && !isPaused){
				drawOutline ();
			}
			if (GUILayout.Button (new GUIContent(AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/PAUSE.png"), "PAUSE"))) {//PAUSE
				if (IsClipPlaying (currentFile.clip)) {
					PauseClip (currentFile.clip);
					isPaused = true;
				}
			}
			if (isPaused) {
				drawOutline ();
			}
			//GUILayout.Space (10);
			if (GUILayout.Button (new GUIContent(AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/STOP.png"), "STOP"))) {//STOP
				StopAllClips ();
				if (isPaused) {
					isPaused = false;
				}
			}

			if(GUILayout.Button(new GUIContent(AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/LOOPSMALL.png"), "LOOP"))){
				if (loop) {
					loop = false;
				} else if (!loop) {
					loop = true;
				}
			}
			if (loop) {
				drawOutline ();
			}

			GUI.backgroundColor = Color.white;
			GUI.contentColor = Color.white;

			if (loop && !isLooping) {
				LoopClip (currentFile.clip, true);
				isLooping = true;
			} else if (isLooping && !loop) {
				LoopClip (currentFile.clip, false);
				isLooping = false;
			}
			GUILayout.Space (position.width / 4);
			GUILayout.EndHorizontal ();
			if (!isLooping) {
				EditorGUILayout.HelpBox ("Remember to check the LOOP button if you want to test how the music piece loops!", MessageType.Warning);
			} else {
				GUILayout.Space (43);
			}

			//Draw preview line
			previewLine = new Rect (0, musicRect.y - 10, 1, currentFile.preview.height + 10);
			Rect previewLineTop = new Rect (0, musicRect.y - 10, 10, 10);
			previewLine.x = position.width * GetClipSamplePosition (currentFile.clip) / clipLengthSamples;
			previewLineTop.x = -4 + position.width * GetClipSamplePosition (currentFile.clip) / clipLengthSamples;
			EditorGUI.DrawRect (previewLine, Color.yellow);
			EditorGUI.DrawRect (previewLineTop, Color.yellow);

			if (loop) {
				Rect loopPoint = new Rect (0, musicRect.y - 5, 1, currentFile.preview.height + 5);
				loopPoint.x = position.width * (currentFile.postExit - currentFile.preEntry) / clipLengthSamples;
				Rect loopPointArm = new Rect (loopPoint.x, musicRect.y - 5, 3, 1);
				Rect loopLabelPos = new Rect (loopPoint.x + 2, musicRect.y - 16, 60, 30);
				EditorGUI.LabelField (loopLabelPos, "loop point", EditorStyles.whiteLabel);
				EditorGUI.DrawRect (loopPoint, Color.white);
				EditorGUI.DrawRect (loopPointArm, Color.white);
			}

			//CLICKING ON TIMELINE
			Event e = Event.current;
			if (e.type == EventType.mouseDown && musicRect.Contains (e.mousePosition)) {
				//Debug.Log ("CLICKED ON MUSIC TIMELINE!");
				if (loop && ((e.mousePosition.x / position.width) * clipLengthSamples) < currentFile.postExit - currentFile.preEntry) {
					newPos = (int)((e.mousePosition.x / position.width) * clipLengthSamples);
					SetClipSamplePosition (currentFile.clip, newPos);
				} else if (!loop) {
					newPos = (int)((e.mousePosition.x / position.width) * clipLengthSamples);
					SetClipSamplePosition (currentFile.clip, newPos);
				}
			}
		}
	}

	void drawOutline(){
		Rect b = GUILayoutUtility.GetLastRect ();
		EditorGUI.DrawRect(new Rect(b.x-3, b.y-3, b.width+6, 3), Color.yellow);
		EditorGUI.DrawRect(new Rect(b.x-3, b.y+b.height, b.width+6, 3), Color.yellow);
		EditorGUI.DrawRect(new Rect(b.x-3, b.y, 3, b.height+3), Color.yellow);
		EditorGUI.DrawRect(new Rect(b.x+b.width, b.y, 3, b.height+3), Color.yellow);
	}

	private void MusicDraw(){
		if (currentFile != null) {
			musicRect = new Rect (0, position.height - currentFile.preview.height, position.width, currentFile.preview.height);
		
			//Debug.Log("length in seconds: "+clipLength+", bps: "+beatsPerSec+", amount of beats in clip: "+ clipLength/beatsPerSec);//
			//centered label
			GUIStyle centeredLabel = GUI.skin.GetStyle ("Label");
			centeredLabel.fontSize = 14;
			centeredLabel.alignment = TextAnchor.UpperCenter;//

			GUILayout.Label ("Now editing '" + currentFile.clip.name + "'", centeredLabel);
			//PROPERTIES
			//BPM SLIDER
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent("BPM", "The BPM of the clip. Useful to adjust for proper snapping to bars and beats."), EditorStyles.boldLabel);
			currentFile.BPM = EditorGUILayout.IntSlider (currentFile.BPM, 10, 500, GUILayout.ExpandWidth (true));
			EditorGUILayout.Space ();
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.BeginVertical ();
			GUILayout.Space (20);
			EditorGUILayout.LabelField ("SNAP TO");
			snapToBars = EditorGUILayout.ToggleLeft ("Bars", snapToBars);
			snapToBeats = EditorGUILayout.ToggleLeft ("Beats", snapToBeats);
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ();
			GUILayout.Space (20);
			EditorGUILayout.LabelField ("TIME SIGNATURE");
			currentFile.Step = EditorGUILayout.IntField (currentFile.Step, GUILayout.MaxWidth(30));
			currentFile.Base = EditorGUILayout.IntField (currentFile.Base, GUILayout.MaxWidth(30));
			EditorGUILayout.EndVertical ();

			EditorGUILayout.EndHorizontal ();	

			if (currentFile.preview != null) {
				GUILayoutUtility.GetRect (0, position.height - currentFile.preview.height * 4, GUILayout.ExpandWidth (true));// 
			}

			//background box
			if (currentFile.preview != null) {
				EditorGUI.DrawRect (musicRect, Color.black + (Color.white * 0.2f));
			}
			//GUI.DrawTextureWithTexCoords (new Rect (0, position.height-100, position.width, 100), currentFile.preview, new Rect (0, 0, 1, 1), true);
			currentFile.preview = AssetPreview.GetAssetPreview (currentFile.clip);
			if (currentFile.preview != null) {
				GUI.DrawTexture (musicRect, currentFile.preview);
			}

		}
	}

	void EditCues(){
		if (currentFile != null) {
			GUILayout.Label (new GUIContent("Entry & Exit Cues", "Here you can edit Entry & Exit markers for the current clip. The Entry marker should be where the 'body' of the clip starts, while the Exit marker is where the 'body' of the clip ends."));
			EditorGUILayout.MinMaxSlider (ref currentFile.preEntry, ref currentFile.postExit, 0f, clipLengthSamples);
			GUI.backgroundColor = Color.white;

			if (!snapToBeats && !snapToBars) {
				//... not much
			} else if (snapToBeats) {
				float newPre = (clipLengthSamples / beatsInClip) * Mathf.RoundToInt (((currentFile.preEntry / clipLengthSamples) * beatsInClip));
				float newPost = (clipLengthSamples / beatsInClip) * Mathf.RoundToInt (((currentFile.postExit / clipLengthSamples) * beatsInClip));
				currentFile.preEntry = newPre;
				currentFile.postExit = newPost;
			} else if (snapToBars) {//only called if !snapToBeats
				currentFile.preEntry = (clipLengthSamples / (beatsInClip / currentFile.Step)) * Mathf.RoundToInt (((currentFile.preEntry / clipLengthSamples) * (beatsInClip / currentFile.Step)));
				currentFile.postExit = (clipLengthSamples / (beatsInClip / currentFile.Step)) * Mathf.RoundToInt (((currentFile.postExit / clipLengthSamples) * (beatsInClip / currentFile.Step)));
			}
		}
	}

	void DrawGrid(float bars){
		if (currentFile != null) {
			//bars
			for (int i = 0; i < bars / 4f; i++) {
				EditorGUI.DrawRect (new Rect (position.width / (bars / (float)currentFile.Step) * i, musicRect.y, 1, currentFile.preview.height), new Color (255, 255, 255, 0.6f));
			}
			//beats
			for (int i = 0; i < bars; i++) {
				EditorGUI.DrawRect (new Rect (position.width / bars * i, musicRect.y, 1, currentFile.preview.height), new Color (255, 255, 255, 0.3f));
			}

			//PRE ENTRY & POST EXIT
			DrawPreEntry ();
			DrawPostExit ();
		}
	}

	void DrawPreEntry(){
		//pre entry
		Rect top = new Rect(position.width/clipLengthSamples*currentFile.preEntry-5, musicRect.y-5, 5, 5);
		Rect line = new Rect (position.width / clipLengthSamples * currentFile.preEntry, musicRect.y - 5, 1, currentFile.preview.height + 5);
		EditorGUI.DrawRect(line, new Color(0f, 1f, 0f, 1f));//line
		EditorGUI.DrawRect(top, new Color(0f, 1f, 0f, 1f));//top bit
		EditorGUI.DrawRect(new Rect(0, musicRect.y, (position.width/clipLengthSamples)*currentFile.preEntry, currentFile.preview.height), new Color(0f, 1f, 0f, 0.3f));//green alpha box
		//DRAG FUNCTIONALITY
//		var e = Event.current;
//		if (e.type == EventType.MouseDown) {
//			//Debug.Log ("mouse down");
//			if (top.Contains (e.mousePosition) || line.Contains(e.mousePosition)) {
//				Debug.Log ("clicked pre entry");
//				preEntrySelected = true;
//				Event.current.Use ();
//			}
//
//		}
//		if (e.type == EventType.MouseUp) {
//			preEntrySelected = false;
//		}
//
//		e = Event.current;
//		if (preEntrySelected) {
//			//Debug.Log ("full: "+position.width+", mouse: "+e.mousePosition.x+", :"+e.mousePosition.x/position.width);
//			float newPreEntry = (e.mousePosition.x/position.width)*clipLengthSamples; 
//			currentFile.preEntry = newPreEntry;
//		}
	}

	void DrawPostExit(){
		//post exit
		Rect top = new Rect(position.width/clipLengthSamples*currentFile.postExit, musicRect.y-5, 5, 5);
		Rect line = new Rect (position.width / clipLengthSamples * currentFile.postExit, musicRect.y - 5, 1, currentFile.preview.height + 5);
		EditorGUI.DrawRect(line, new Color(1f, 0f, 0f, 1f));
		EditorGUI.DrawRect(top, new Color(1f, 0f, 0f, 1f));//top bit
		EditorGUI.DrawRect(new Rect(position.width/clipLengthSamples*currentFile.postExit, musicRect.y, position.xMax-(position.width/clipLengthSamples)*currentFile.postExit, currentFile.preview.height), new Color(1f, 0f, 0f, 0.3f));//red alpha box
		//DRAG FUNCTIONALITY
//		var e = Event.current;
//		if (e.type == EventType.MouseDown) {
//			//Debug.Log ("mouse down");
//			if (top.Contains (e.mousePosition) || line.Contains(e.mousePosition)) {
//				//Debug.Log ("clicked post exit");
//				postExitSelected = true;
//				Event.current.Use ();
//			}
//
//		}
//		if (e.type == EventType.MouseUp) {
//			postExitSelected = false;
//		}
//
//		e = Event.current;
//		if (postExitSelected) {
//			Debug.Log ("full: "+position.width+", mouse: "+e.mousePosition.x+", :"+e.mousePosition.x/position.width);
//			float newPostExit = (e.mousePosition.x/position.width)*clipLengthSamples;
//			currentFile.postExit = newPostExit;
//		}

	}

	private void DropAreaGUI(){
		Repaint ();
		var e = Event.current;
		var dropArea = GUILayoutUtility.GetRect (0.0f, 50.0f, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight(true));
		//GUI.Box (dropArea, "DROP AUDIO HERE", EditorStyles.centeredGreyMiniLabel);
		GUI.DrawTexture (new Rect (position.width / 4, position.height / 10, position.width/2, Mathf.Clamp(position.width/2, 0, 200)), AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/DROP.png"), ScaleMode.ScaleToFit);

		//LOGO
		Texture logo = AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/MFYGSticker.png");
//		float xPlace = position.width/2 - logo.width / 2;
		Rect logoRect = new Rect((position.width/6)*1.5f, (position.height/3)*1.7f, position.width/2, 200);
		GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit);

		if (Event.current.type == EventType.mouseDown && logoRect.Contains (Event.current.mousePosition)) {
			Application.OpenURL ("http://musicforyourgame.com");
		}


		GUIStyle link = GUI.skin.GetStyle ("LabelField");
		link.normal.textColor = Color.blue;
		link.fontSize = 12;
		link.fontStyle = FontStyle.Bold;
		Rect feedbackRect = new Rect (position.width - 100, position.height - 20, 100, 50); 
		EditorGUI.LabelField (feedbackRect, "GIVE FEEDBACK", link);

		if (feedbackRect.Contains(Event.current.mousePosition)) {
			EditorGUI.DrawRect (new Rect (feedbackRect.x, feedbackRect.y + 15, feedbackRect.width-3, 1), Color.blue);
		}

		if (Event.current.type == EventType.mouseDown && feedbackRect.Contains (Event.current.mousePosition)) {
			Application.OpenURL ("http://mfyg.dk/ngj16"); //LINK TO FEEDBACK DOCUMENT GOES HERE
		}

		//handle all the drag & drop events
		switch (e.type) {
		case EventType.dragUpdated:
		case EventType.dragPerform:
			if (!dropArea.Contains (e.mousePosition)) {//check if the mouse is in the drop area
				break;
			}

			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

			if (e.type == EventType.dragPerform) {
				DragAndDrop.AcceptDrag ();
				loop = false;
				if (DragAndDrop.objectReferences [0].GetType () == typeof(AudioClip)) {//AUDIO CLIP FOUND!
					
					foreach (AudioClip draggedObject in DragAndDrop.objectReferences) {
						//This is where you add the audioclip!
						string objPath = AssetDatabase.GetAssetPath (draggedObject);
						currentFile = new AAEFile ();
						currentFile.clip = (AudioClip)AssetDatabase.LoadAssetAtPath<AudioClip> (objPath);
						currentFile.postExit = currentFile.clip.samples;
						currentFile.preEntry = 0;
						////AssetPreview.SetPreviewTextureCacheSize (2);
						while (currentFile.preview == null) {
							currentFile.preview = AssetPreview.GetAssetPreview (currentFile.clip);
							System.Threading.Thread.Sleep (15);
						}
						if (currentFile.preview != null) {
							currentFile.preview.filterMode = FilterMode.Point;//
						}
						saveName = currentFile.clip.name + "_AAE";
					}

				}else if (DragAndDrop.objectReferences [0].GetType () == typeof(GameObject)) {//GAMEOBJECT FOUND
					foreach (GameObject draggedObject in DragAndDrop.objectReferences) {
						if (draggedObject.GetComponent<AAEClip> () != null) {
							AAEClip a = draggedObject.GetComponent<AAEClip>();
							string objPath = AssetDatabase.GetAssetPath (draggedObject);
							currentFile = new AAEFile ();
							currentFile.clip = a.clip;
							currentFile.postExit = a.postExit;
							currentFile.preEntry = a.preEntry;
							currentFile.BPM = a.BPM;
							currentFile.Step = a.Step;
							currentFile.Base = a.Base;
							while (currentFile.preview == null) {
								currentFile.preview = AssetPreview.GetAssetPreview (currentFile.clip);
								System.Threading.Thread.Sleep (15);
							}
							if (currentFile.preview != null) {
								currentFile.preview.filterMode = FilterMode.Point;//
							}
							saveName = currentFile.clip.name + "_AAE";

						} else {
							Debug.LogError ("AAE error: Please drop a previously saved AAE Clip or an AudioClip!");
						}
					}
				}
				
				DragAndDrop.activeControlID = 0;
			}
			Event.current.Use ();
			break;
		}
	}

	void PlayClip(AudioClip clip){
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip) }, null);
		method.Invoke(null, new object[] { clip });
	}

	void LoopClip(AudioClip clip, bool on)
	{
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("LoopClip", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip), typeof(bool) }, null);
		method.Invoke(null, new object[] { clip, on });
	}

	void PauseClip(AudioClip clip) {
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("PauseClip", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] {typeof(AudioClip)}, null);
		method.Invoke(null, new object[] {clip});
	}

	void ResumeClip(AudioClip clip) {
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("ResumeClip", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] {typeof(AudioClip)}, null);
		method.Invoke(null, new object[] {clip});
	}

	void StopAllClips(){
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("StopAllClips", BindingFlags.Static | BindingFlags.Public);
		method.Invoke(null, null);
	}

	void StopClip(AudioClip clip) {
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("StopClip", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] {typeof(AudioClip)}, null);
		method.Invoke(null, new object[] {clip});
	}

	float GetClipPosition(AudioClip clip){
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("GetClipPosition", BindingFlags.Static | BindingFlags.Public);
		float position = (float)method.Invoke(null, new object[] { clip });
		return position;
	}

	public static int GetClipSamplePosition(AudioClip clip) {
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("GetClipSamplePosition", BindingFlags.Static | BindingFlags.Public);

		int position = (int)method.Invoke(null,new object[] {clip});
		return position;
	}

	public static void SetClipSamplePosition(AudioClip clip , int iSamplePosition) {
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("SetClipSamplePosition", BindingFlags.Static | BindingFlags.Public);

		method.Invoke(null, new object[] {clip, iSamplePosition});
	}

	public static int GetSampleCount(AudioClip clip) {
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("GetSampleCount", BindingFlags.Static | BindingFlags.Public);

		int samples = (int)method.Invoke(null, new object[] {clip});
		return samples;
	}

	bool IsClipPlaying(AudioClip clip){
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
		MethodInfo method = audioUtilClass.GetMethod("IsClipPlaying", BindingFlags.Static | BindingFlags.Public);
		bool playing = (bool)method.Invoke(null, new object[] { clip, });
		return playing;
	}

	public class AAEFile{
		public AudioClip clip = null;
		public Texture2D preview = null;
		public float preEntry = 0;
		public float postExit = 0;
		public int BPM = 120;
		public int Step = 4; //4
		public int Base = 4; //4ths 
	}	

	public static void update(){
//		if (currentFile != null) {
//			Debug.Log ("pre: " + currentFile.preEntry);
//		}
	}

}
