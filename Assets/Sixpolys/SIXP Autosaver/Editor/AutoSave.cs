using UnityEngine;
using UnityEditor;
using System.Timers;
using System;
using System.Diagnostics;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LOG = UnityEngine.Debug;

[InitializeOnLoad]
public class AutoSave : EditorWindow
{
	public static AutoSave Instance = null;
	public static Texture2D Logo = null;
	protected static Timer Timer = null;
	protected static int HierarchyChangeCount = 0;
	protected static string LogoPath = "Assets/Sixpolys/SIXP Autosaver/Editor/SixpolysLogo.png";
	protected static bool SaveNow = false;
	protected static bool SavedBeforePlay = false;
	protected static bool SaveAfterPlay = false;
	protected static Stopwatch Stw1 = null;

	[MenuItem("Window/Autosave Settings")]
	public static void ShowWindow ()
	{
		var window = EditorWindow.GetWindow<AutoSave> ();
		window.maxSize = new Vector2 (window.maxSize.x, 50);
		window.minSize = new Vector2 (0, 50);
	}

	public static void LoadPreferences ()
	{
		if (AutoSavePreferences.autosaveEnabled) {
			if (Timer == null) {
				Timer = new System.Timers.Timer ();
				Timer.Interval = AutoSavePreferences.saveInterval;
				Timer.Elapsed += new  ElapsedEventHandler (TimerFired);
				Timer.Start ();
			} else {
				if (Math.Abs(Timer.Interval - AutoSavePreferences.saveInterval) > 0) {
					Timer.Interval = AutoSavePreferences.saveInterval;
				}
			}
		} else {
			if (Timer != null) {
				Timer.Stop ();
				Timer.Dispose ();
				Timer = null;
			}
		}
		EditorApplication.hierarchyChanged -= HierarchyChanged;
		EditorApplication.playModeStateChanged -= PlayModeChanged;
		EditorApplication.hierarchyChanged += HierarchyChanged;
		EditorApplication.playModeStateChanged += PlayModeChanged;

		if (Instance != null) {
			Instance.Repaint ();
		}

	}

    public static void PlayModeChanged (PlayModeStateChange obj)
	{
		if (AutoSavePreferences.saveBeforeRun && EditorApplication.isPlayingOrWillChangePlaymode && !SavedBeforePlay) {
			SavedBeforePlay = true;
			ExecuteSave ();
		} else if (!EditorApplication.isPaused && !EditorApplication.isPlaying) {
			if (SaveAfterPlay) {
				ExecuteSave ();
			}
		}
	}

	public static void HierarchyChanged ()
	{
		if (AutoSavePreferences.saveOnHierarchyChanges && !EditorApplication.isPlaying) {
			HierarchyChangeCount++;
			if (HierarchyChangeCount >= AutoSavePreferences.hierarchyChangeCountTrigger) {
				HierarchyChangeCount = 0;
				ExecuteSave ();
			}
		}
	}

	public static void TimerFired (object sender, ElapsedEventArgs args)
	{
		if (!SaveNow) {
			SaveNow = true;
		}
	}
	
	public static void ExecuteSave ()
	{
		Stw1.Stop ();
		Stw1.Reset ();

		if (EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer) {
			return;
		}

		// don't save during running game
		if (EditorApplication.isPlaying || EditorApplication.isPaused) {
			SaveAfterPlay = true;
			Stw1.Start ();
			return;
		}
		SaveAfterPlay = false;

		// save untitled scene?

        Scene activeScene = SceneManager.GetActiveScene();
        string sceneName = activeScene.IsValid() ? activeScene.path : "";

		if ((sceneName == "" || sceneName.StartsWith ("Untitled")) && !AutoSavePreferences.saveUnnamedNewScene) {
			Stw1.Start ();
			return;
		}


		if (AutoSavePreferences.logSaveEvent) {
			LOG.Log ("Autosave");
		}

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "", false);

		if (AutoSavePreferences.saveAssets) {
			AssetDatabase.SaveAssets ();
			AssetDatabase.SaveAssets ();
		}
		if (Instance != null) {
			Instance.Repaint ();
		}
		Stw1.Start ();
	}

	[InitializeOnLoadMethod]
	public static void InitAutosave ()
	{
		Stw1 = new Stopwatch ();
		Stw1.Start ();
		Logo = (Texture2D)AssetDatabase.LoadAssetAtPath (LogoPath, typeof(Texture2D));
		EditorApplication.update += EditorUpdate;
		AutoSavePreferences.LoadPreferences ();
		LoadPreferences ();
	}

	public static void EditorUpdate ()
	{
		if (SaveNow) {
			SaveNow = false;
			ExecuteSave ();
		}
		if (Instance != null) {
			Instance.Repaint ();
		}
	}

	public void OnEnable ()
	{
		Instance = this;
	}

	void OnGUI ()
	{
		EditorGUILayout.BeginHorizontal ();
		if (Logo != null) {
			GUILayout.Label (Logo, GUILayout.Width (50));
		}
		EditorGUILayout.BeginVertical ();
		bool autosaveEnabled = GUILayout.Toggle (AutoSavePreferences.autosaveEnabled, "Autosave", GUILayout.ExpandWidth (true));

		EditorGUILayout.LabelField ("Last saved: " + Math.Floor (Stw1.Elapsed.TotalMinutes) + " minutes ago");
		EditorGUILayout.EndVertical ();
		EditorGUILayout.EndHorizontal ();

		if (GUI.changed) {
			AutoSavePreferences.autosaveEnabled = autosaveEnabled;
			AutoSavePreferences.SavePreferences ();

		}
	}	
}
