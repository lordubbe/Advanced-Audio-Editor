using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

public class AAEWindow : EditorWindow {

	//saving & save path variables
	static string defaultSavePath = "Assets/Music";
	static string userSavePath = defaultSavePath;
	static string saveName = "AAE File";
	static string oldSavePath = userSavePath;

	//this x position is used for placing the playback marker according to current playback point and where the user clicks on the music timeline
	int newPos;

	//music settings and playback logic
	public static bool snapToBars;
	public static bool snapToBeats;
	public static bool loop;
	private bool isLooping;
	public static bool loopCalled = false;
//	bool postExitSelected = false; //these were used for dragging the handles
//	bool preEntrySelected = false;
	bool isPaused;

	//current clip variables
	float clipLength;//in seconds
	float clipLengthSamples;//in samples... duh
	float beatsPerSec;
	float beatsInClip;

	//Music rect (used for drawing the music timeline)
	Rect musicRect;
	Rect previewLine;
	float previewPoint;

	//store some specific colors for easier reference later on
	Color mfygBlue = new Color (0, 0.71f, 0.97f);
	Color mfygOrange = new Color (0.96f, 0.58f, 0.117f);

	//the current file that has been dragged into the window
	public static AAEFile currentFile = null;

	[MenuItem ("MUSIC FOR YOUR GAME/Advanced Audio Editor")]//make a new tab in the top menu and place it as a subitem!

	static void Init(){ //This function is used for initialisation
		AAEWindow window = (AAEWindow)EditorWindow.GetWindow(typeof (AAEWindow)); //get existing open window, or if none, make a new window
		window.Show ();
		window.minSize = new Vector2 (400, 510); //set minimum size to prevent the interface fucking up
		currentFile = null; //Initialise the window without a loaded file
		loopCalled = false;
		EditorApplication.update += update; //subscribe the update function in this script to the update function that runs every frame (even outside play mode). This allows us to redraw changes more smoothly 
	}

	private static Texture2D texOrange;
	private static Texture2D texBlue;

	void OnGUI(){
		//	MFYG ORANGE BACKGROUND
		texOrange = new Texture2D(1, 1, TextureFormat.RGBA32, false); //convert to 1x1 pixel texture
		texOrange.SetPixel(0, 0, mfygOrange);//set the pixel color
		texOrange.Apply();//apply the color change
		GUI.DrawTexture(new Rect(0, 0, position.width, position.height), texOrange, ScaleMode.StretchToFill);//make the texture fill the entire screen

		//  MFYG BLUE BACKGROUND
		if (currentFile != null) {
			texBlue = new Texture2D (1, 1, TextureFormat.RGBA32, false);
			texBlue.SetPixel (0, 0, mfygBlue);
			texBlue.Apply ();
			GUI.DrawTexture (new Rect (0, 110, position.width, 113), texBlue, ScaleMode.StretchToFill); //this should only color a certain portion of the screen
		}

		// configure the headline style
		GUIStyle headline = GUI.skin.GetStyle ("Label");
		headline.fontSize = 16;
		headline.alignment = TextAnchor.UpperCenter;
		headline.fontStyle = FontStyle.Bold;
		headline.normal.textColor = Color.black;
		// write headline and make some space beneath it
		GUILayout.Label ("Advanced Audio Editor", headline);
		EditorGUILayout.Space();

		if (currentFile != null) { //basically if something has been dragged into the window and a file is succesfully loaded
			clipLength = currentFile.clip.length;//in seconds
			clipLengthSamples = currentFile.clip.samples;
			beatsPerSec = (currentFile.BPM / 60f);
			beatsInClip = clipLength/beatsPerSec* currentFile.Base;

			if (currentFile.preview != null) { //if the preview is succesfully loaded
				GeneralSettings (); //draw the general settings

				EditorGUILayout.BeginHorizontal (); //start a horizontal section
				//configure settings for the red 'unload' button
				GUI.backgroundColor = new Color(1,0.5f,0.5f); 
				GUI.contentColor = Color.white;
				//draw the button and include a tooltip
				if (GUILayout.Button (new GUIContent("Unload music", "Press this button to unload the current clip and return to the drag & drop screen."))) {
					currentFile = null;
				}
				//reset the colors to not affect further things drawn
				GUI.backgroundColor = Color.white;
				GUI.contentColor = Color.white;

				Saving (); //draw the saving button and run all the saving code
				EditorGUILayout.EndHorizontal (); //end that section

				GUILayout.Space (20); //add some space
				MusicDraw (); //draw the music section
				DrawGrid (beatsInClip); //draw grid on top of the musicsection (yeah this is perhaps not the best way to lay it out
//				EditCues ();
				GUILayout.Space (10); //more space. Yay :)
				TransportControls (); //draw the transport controls
				GUILayout.Space (15); //wow, I can't comprehend all this space
				EditCues (); //run this function to edit the exit and entry cues
			}

			//control the looping. If the playback marker is apast the loop point and the loop has not yet been called, then the clip is played again.
			if (currentFile != null) {
				previewPoint = previewLine.x / position.width * clipLengthSamples;
				if (isLooping && !loopCalled && previewPoint >= currentFile.postExit - currentFile.preEntry) {
					PlayClip (currentFile.clip);
					loopCalled = true;
				} else {
					loopCalled = false;
				}
			}
		} else { //If the user has yet to drag in an AAE Clip or audio file
			DropAreaGUI (); //This function takes care of that
		}
		// Repaint / force gui to update if the current clip is playing (this is necessary to draw the progress of the playback marker more smoothly)
		if(currentFile != null && IsClipPlaying(currentFile.clip)){
			Repaint();
		}
	}
	void GeneralSettings(){ //this section basically just draws the saving section
		if (currentFile != null) {//there's nullchecks quite often just to make the program more stable
			EditorGUILayout.BeginHorizontal ();//basically just structuring the sections
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
	void Saving(){ //this is the section that takes care of saving
		if (currentFile != null) { //obviously you can't save an AAE Clip if there is no audio file loaded!
			GUI.backgroundColor = new Color(0.5f,1f,0.5f); //prepare the backgroundcolor of the GUI to be green (for the 'save' button)
			GUI.contentColor = Color.white;

			if (GUILayout.Button (new GUIContent("SAVE TO '" + userSavePath + "'", "Press this button to save the edited clip to the chosen destination."))) {//draw the button
				//handle saving of the file upon press of the button
				if (userSavePath [userSavePath.Length - 1].ToString() == "/") {//if the last character of the path entered by the user is '/' we remove it from the save path
					string newSavePath = "";
					oldSavePath = userSavePath;
					for(int i=0; i<userSavePath.Length-1; i++){
						newSavePath += userSavePath [i];
					}
					userSavePath = newSavePath;
				}
				if (AssetDatabase.IsValidFolder (userSavePath)) { 	// if the path is valid, ie the path already exists, then we just save
					Save ();
				} else { 												// otherwise we create the path ourselves!
					string[] folders = userSavePath.Split ('/');		// first split the string at slashes and save each folder name as an element in an array
					for (int i = 1; i < folders.Length; i++) {			// run through the length of this array
						string folderPath = "Assets";					// Initialise the new path with 'Assets' so the path will originate from this folder
						for (int j = 1; j <= i; j++) {					// add the rest of the path bit by bit
							folderPath += "/" + folders [j];
						}
						if (!AssetDatabase.IsValidFolder (folderPath)) {// if this path is not valid then we start from scratch again and build a new path
							//create folder
							folderPath = "";
							for (int j = 0; j <= i - 1; j++) {
								if (j > 0) {
									folderPath += "/" + folders [j];
								} else {
									folderPath += folders [j];
								}
							}
							AssetDatabase.CreateFolder (folderPath, folders [i]); //finally create the folders one by one until the path will be as specified
							Debug.Log ("Created folder in '" + folders [i - 1] + "', called '" + folders [i] + "'");

						}
					}
					//now folders are made, so we can create the asset
					Save ();
				}
			}
			//reset the GUI colors to white in order to not 
			GUI.backgroundColor = Color.white;
			GUI.contentColor = Color.white;
		}
	}

	void Save(){
		GameObject go = new GameObject(saveName); 	//create a new gameobject with the chosen name
		AAEClip a = go.AddComponent<AAEClip>(); 	//add the AAE Clip component
		a.clip = currentFile.clip;					//from here we just save the values as the user has edited them in the editor window
		a.preEntry = currentFile.preEntry;
		a.postExit = currentFile.postExit;
		a.preview = currentFile.preview;
		a.BPM = currentFile.BPM;
		a.Step = currentFile.Step;
		a.Base = currentFile.Base;
		PrefabUtility.CreatePrefab (userSavePath + "/" + saveName + ".prefab", go); //this line creates the prefab 
		AssetDatabase.SaveAssets (); 	// actually save what we've created
		AssetDatabase.Refresh (); 		// refresh to update the project view
		DestroyImmediate (go);			//destroy the temporary object so it's not added to the current scene
		userSavePath = oldSavePath;		//revert the save path
	}
		

	void TransportControls(){
		if (currentFile != null) {
			GUILayout.Label (new GUIContent("Transport", "These are your Transport Controls with which you can play, pause and stop the clip, as well as test if it loops correctly with itself.")); //just creates the headline
			GUILayout.BeginHorizontal (); 
			GUILayout.Space (position.width / 4);

			GUI.backgroundColor = mfygBlue; //make the buttons nice and blue
			GUI.contentColor = Color.white; 

			//now draw the buttons and put the icons on them so it's like people are used to instead of just text
			if (GUILayout.Button (new GUIContent(AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/PLAY.png"), "PLAY"))) {//PLAY
				if (!IsClipPlaying (currentFile.clip)) { //if it's not already playing then play it
					PlayClip (currentFile.clip);

				} else if (isPaused) {	//if it's paused then just resume playing from where it left off
					ResumeClip (currentFile.clip);
					isPaused = false;
				}
			}
			if(IsClipPlaying (currentFile.clip) && !isPaused){ //if the clip is playing and it's not paused then draw outline
				drawOutline ();
			}
			if (GUILayout.Button (new GUIContent(AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/PAUSE.png"), "PAUSE"))) {//PAUSE
				if (IsClipPlaying (currentFile.clip)) { //only pause if the clip is actually playing
					PauseClip (currentFile.clip);
					isPaused = true;
				}
			}
			if (isPaused) { //if it's paused then draw outline 
				drawOutline ();
			}
			//GUILayout.Space (10);
			if (GUILayout.Button (new GUIContent(AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/STOP.png"), "STOP"))) {//STOP
				StopAllClips ();
				if (isPaused) { //if it's paused and STOP is pressed then unpause as well
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
			if (!isLooping) { //if the player has not enabled looping then the track will (obviously) not loop, and as such, the biggest features of this plugin can not be previewed
				EditorGUILayout.HelpBox ("Remember to check the LOOP button if you want to test how the music piece loops!", MessageType.Warning); //...so we remind the user to enable it
			} else {
				GUILayout.Space (43); //if looping is enabled then we don't want to display the helpbox, but we also don't want to fuck up the formatting so instead we add some space to keep it consistent
			}

			//Draw preview line
			previewLine = new Rect (0, musicRect.y - 10, 1, currentFile.preview.height + 10); 				//configure the dimension of the preview line (yellow line with top)
			Rect previewLineTop = new Rect (0, musicRect.y - 10, 10, 10); 									//this is the top part
			previewLine.x = position.width * GetClipSamplePosition (currentFile.clip) / clipLengthSamples;	//we set the x position to correspond to the current sample that the track is playing, relative to the music preview
			previewLineTop.x = -4 + position.width * GetClipSamplePosition (currentFile.clip) / clipLengthSamples;
			EditorGUI.DrawRect (previewLine, Color.yellow);	//now actually draw it :b
			EditorGUI.DrawRect (previewLineTop, Color.yellow);

			if (loop) {	//if looping is enabled we will also draw the loop point 
				Rect loopPoint = new Rect (0, musicRect.y - 5, 1, currentFile.preview.height + 5);
				loopPoint.x = position.width * (currentFile.postExit - currentFile.preEntry) / clipLengthSamples;
				Rect loopPointArm = new Rect (loopPoint.x, musicRect.y - 5, 3, 1);
				Rect loopLabelPos = new Rect (loopPoint.x + 2, musicRect.y - 16, 60, 30);
				EditorGUI.LabelField (loopLabelPos, "loop point", EditorStyles.whiteLabel);
				EditorGUI.DrawRect (loopPoint, Color.white);
				EditorGUI.DrawRect (loopPointArm, Color.white);
			}

			//CLICKING ON TIMELINE
			Event e = Event.current; //store the current event 
			if (e.type == EventType.mouseDown && musicRect.Contains (e.mousePosition)) { //if this event is 'mouse down' and it's within the music rect then we want to move the playback to another sample
				if (loop && ((e.mousePosition.x / position.width) * clipLengthSamples) < currentFile.postExit - currentFile.preEntry) { //don't allow clicks beyond the loop point
					newPos = (int)((e.mousePosition.x / position.width) * clipLengthSamples); 
					SetClipSamplePosition (currentFile.clip, newPos);
				} else if (!loop) { //if looping is off then we just change the positions
					newPos = (int)((e.mousePosition.x / position.width) * clipLengthSamples); 
					SetClipSamplePosition (currentFile.clip, newPos);
				}
			}
		}
	}

	void drawOutline(){ //draw outline just gets the last rect drawn (which is why it doesn't need input arguments to draw the correct places
		Rect b = GUILayoutUtility.GetLastRect ();
		EditorGUI.DrawRect(new Rect(b.x-3, b.y-3, b.width+6, 3), Color.yellow); //we just make the outline a thickness of 3 OUTSIDE the last rect
		EditorGUI.DrawRect(new Rect(b.x-3, b.y+b.height, b.width+6, 3), Color.yellow);
		EditorGUI.DrawRect(new Rect(b.x-3, b.y, 3, b.height+3), Color.yellow);
		EditorGUI.DrawRect(new Rect(b.x+b.width, b.y, 3, b.height+3), Color.yellow);
	}

	private void MusicDraw(){ //this one draws the music properties that the user should enter, as well as the preview in the bottom of the screen
		if (currentFile != null) {
			musicRect = new Rect (0, position.height - currentFile.preview.height, position.width, currentFile.preview.height); //decide where the music should be drawn
		
			//centered label
			GUIStyle centeredLabel = GUI.skin.GetStyle ("Label"); //configure the style for a centered label that is to display the name of the track being edited
			centeredLabel.fontSize = 14;
			centeredLabel.alignment = TextAnchor.UpperCenter;//

			GUILayout.Label ("Now editing '" + currentFile.clip.name + "'", centeredLabel); //display it to the user if they should be in doubt
			//PROPERTIES
			//BPM SLIDER
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent("BPM", "The BPM of the clip. Useful to adjust for proper snapping to bars and beats."), EditorStyles.boldLabel);
			currentFile.BPM = EditorGUILayout.IntSlider (currentFile.BPM, 10, 500, GUILayout.ExpandWidth (true));
			EditorGUILayout.Space ();
			EditorGUILayout.EndHorizontal ();
			//SNAPPING OPTIONS
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.BeginVertical ();
			GUILayout.Space (20);
			EditorGUILayout.LabelField ("SNAP TO");
			snapToBars = EditorGUILayout.ToggleLeft ("Bars", snapToBars);
			snapToBeats = EditorGUILayout.ToggleLeft ("Beats", snapToBeats);
			EditorGUILayout.EndVertical ();
			//TIME SIGNATURE SETTINGS
			EditorGUILayout.BeginVertical ();
			GUILayout.Space (20);
			EditorGUILayout.LabelField ("TIME SIGNATURE");
			currentFile.Step = EditorGUILayout.IntField (currentFile.Step, GUILayout.MaxWidth(30));
			currentFile.Base = EditorGUILayout.IntField (currentFile.Base, GUILayout.MaxWidth(30));
			EditorGUILayout.EndVertical ();

			EditorGUILayout.EndHorizontal ();	

			if (currentFile.preview != null) { //get the rect for the music preview (timeline)
				GUILayoutUtility.GetRect (0, position.height - currentFile.preview.height * 4, GUILayout.ExpandWidth (true));// 
			}

			//background box
			if (currentFile.preview != null) { //make a background that is dark grey
				EditorGUI.DrawRect (musicRect, Color.black + (Color.white * 0.2f));
			}
			//GUI.DrawTextureWithTexCoords (new Rect (0, position.height-100, position.width, 100), currentFile.preview, new Rect (0, 0, 1, 1), true); //failed experiment
			currentFile.preview = AssetPreview.GetAssetPreview (currentFile.clip); //this gets the asset preview made by Unity for the audio clip 
			if (currentFile.preview != null) {
				GUI.DrawTexture (musicRect, currentFile.preview);
			}

		}
	}

	void EditCues(){ //this section takes care of editing the pre and post exits
		if (currentFile != null) {
			GUILayout.Label (new GUIContent("Entry & Exit Cues", "Here you can edit Entry & Exit markers for the current clip. The Entry marker should be where the 'body' of the clip starts, while the Exit marker is where the 'body' of the clip ends."));
			EditorGUILayout.MinMaxSlider (ref currentFile.preEntry, ref currentFile.postExit, 0f, clipLengthSamples);
			GUI.backgroundColor = Color.white;

			if (!snapToBeats && !snapToBars) {
				//... not much
			} else if (snapToBeats) { //if snapping to beats is enabled then we round the values for the slider, which makes the snapping occur
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

	void DrawGrid(float bars){ //this draws the grid according to the set time signature and tempo
		if (currentFile != null) {
			//bars
			for (int i = 0; i < bars / 4f; i++) { //these are 'stronger', or, more white than the beats. This is familiar to what's seen in most audio editing software
				EditorGUI.DrawRect (new Rect (position.width / (bars / (float)currentFile.Step) * i, musicRect.y, 1, currentFile.preview.height), new Color (255, 255, 255, 0.6f));
			}
			//beats
			for (int i = 0; i < bars; i++) {
				EditorGUI.DrawRect (new Rect (position.width / bars * i, musicRect.y, 1, currentFile.preview.height), new Color (255, 255, 255, 0.3f));
			}

			//PRE ENTRY & POST EXIT
			DrawPreEntry (); //these functions take care of drawing them according to where these are configured by the user
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

	private void DropAreaGUI(){ //this is the 'opening' screen that is displayed when the user has not yet dropped an audio or AAE clip into the window
		Repaint ();
		var e = Event.current; //I used var here because this part was the first that I made, and I took inspiration from a tutorial i found online, and cared more about getting something to work rather than having full code consistensy
		var dropArea = GUILayoutUtility.GetRect (0.0f, 50.0f, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight(true)); //define the area that the user can drop stuff in
		//GUI.Box (dropArea, "DROP AUDIO HERE", EditorStyles.centeredGreyMiniLabel);
		GUI.DrawTexture (new Rect (position.width / 4, position.height / 10, position.width/2, Mathf.Clamp(position.width/2, 0, 200)), AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/DROP.png"), ScaleMode.ScaleToFit); //load and display the graphics I made

		//LOGO
		Texture logo = AssetDatabase.LoadAssetAtPath<Texture> ("Assets/AAE/MFYGSticker.png"); //this is my logo! Wuhu!
//		float xPlace = position.width/2 - logo.width / 2;
		Rect logoRect = new Rect((position.width/6)*1.5f, (position.height/3)*1.7f, position.width/2, 200); //where the logo will be placed
		GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit);

		if (Event.current.type == EventType.mouseDown && logoRect.Contains (Event.current.mousePosition)) { //if the logo is pressed I'll let the sweet user have a look at my website :b
			Application.OpenURL ("http://musicforyourgame.com"); //this just opens their default browser and directs it to the webpage
		}
			
		GUIStyle link = GUI.skin.GetStyle ("Label"); //make a GIVE FEEDBACK link because if people use it I'd like to have some feedback if they have any to share!
		link.normal.textColor = Color.blue;
		link.fontSize = 12;
		link.fontStyle = FontStyle.Bold;
		Rect feedbackRect = new Rect (position.width - 100, position.height - 20, 100, 50); 
		EditorGUI.LabelField (feedbackRect, "GIVE FEEDBACK", link);

		if (feedbackRect.Contains(Event.current.mousePosition)) {
			EditorGUI.DrawRect (new Rect (feedbackRect.x, feedbackRect.y + 15, feedbackRect.width-3, 1), Color.blue);
		}

		if (Event.current.type == EventType.mouseDown && feedbackRect.Contains (Event.current.mousePosition)) {
			Application.OpenURL ("http://goo.gl/forms/KZH7Lq9ibQExSUbq1"); //LINK TO FEEDBACK DOCUMENT GOES HERE
		}

		//handle all the drag & drop events
		switch (e.type) { //a switch between the type of drag actions
		case EventType.dragUpdated:
		case EventType.dragPerform: //if the user is holding the object over the drag zone
			if (!dropArea.Contains (e.mousePosition)) {//check if the mouse is not in the drop area
				break; //in that case do nothing
			}

			DragAndDrop.visualMode = DragAndDropVisualMode.Copy; //change the cursor to the one with the plus to indicate that they can actually drop something

			if (e.type == EventType.dragPerform) { //so if they release
				DragAndDrop.AcceptDrag ();
				loop = false;
				if (DragAndDrop.objectReferences [0].GetType () == typeof(AudioClip)) {//AUDIO CLIP FOUND!
					
					foreach (AudioClip draggedObject in DragAndDrop.objectReferences) {
						//This is where you add the audioclip!
						string objPath = AssetDatabase.GetAssetPath (draggedObject);
						currentFile = new AAEFile (); //make a new AAEFile as the temporary file
						currentFile.clip = (AudioClip)AssetDatabase.LoadAssetAtPath<AudioClip> (objPath);//do a lot of initialisation from here
						currentFile.postExit = currentFile.clip.samples;
						currentFile.preEntry = 0;
						////AssetPreview.SetPreviewTextureCacheSize (2);
						while (currentFile.preview == null) { //attempt loading the preview for a while as it can fail the first time without giving any warning
							currentFile.preview = AssetPreview.GetAssetPreview (currentFile.clip);
							System.Threading.Thread.Sleep (15);
						}
						if (currentFile.preview != null) { //if it's successfully loaded then change the filtering mode since we don't want a blurry ass image
							currentFile.preview.filterMode = FilterMode.Point;//
						}
						saveName = currentFile.clip.name + "_AAE";
					}

				}else if (DragAndDrop.objectReferences [0].GetType () == typeof(GameObject)) {//GAMEOBJECT FOUND
					foreach (GameObject draggedObject in DragAndDrop.objectReferences) {
						if (draggedObject.GetComponent<AAEClip> () != null) { //check if the gameobject contains an AAE Clip component (which means it was previously saved)
							AAEClip a = draggedObject.GetComponent<AAEClip>(); //if it does, then initialise the temporary AAE File with the saved information
							currentFile = new AAEFile ();
							currentFile.clip = a.clip;
							currentFile.postExit = a.postExit;
							currentFile.preEntry = a.preEntry;
							currentFile.BPM = a.BPM;
							currentFile.Step = a.Step;
							currentFile.Base = a.Base;
							while (currentFile.preview == null) { //and load the according preview
								currentFile.preview = AssetPreview.GetAssetPreview (currentFile.clip);
								System.Threading.Thread.Sleep (15);
							}
							if (currentFile.preview != null) {
								currentFile.preview.filterMode = FilterMode.Point; //pls no blur
							}
							saveName = currentFile.clip.name + "_AAE";

						} else {
							Debug.LogError ("AAE error: Please drop a previously saved AAE Clip or an AudioClip!"); //throw a logerror if the user has dropped something that is not an audioclip or contains an AAE Clip component
						}
					}
				}
				
				DragAndDrop.activeControlID = 0; 
			}
			Event.current.Use (); //actually use the event so we don't do the same thing a lot of times 
			break;
		}
	}

	//THIS FOLLOWING SECTION CONTAINS A LOT OF METHODS THAT INVOKE METHODS IN THE AudioUtil CLASS OF UNITY

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

	//This is the AAE File class that acts as a temporary object that can store the information that will be saved to the AAE Clip upon saving
	public class AAEFile{
		public AudioClip clip = null;
		public Texture2D preview = null;
		public float preEntry = 0;
		public float postExit = 0;
		public int BPM = 120;
		public int Step = 4; //4
		public int Base = 4; //4ths 
	}	

	public static void update(){ //this just allows per-frame code
//		if (currentFile != null) {
//			Debug.Log ("pre: " + currentFile.preEntry);
//		}
	}
}
