using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Presentation.Behaviors;
using System;
using System.IO;

namespace Unity.Presentation.Utils
{
	public static class EditorUtils
	{
		public static void BuildPresentation(SlideDeck deck)
		{
			string path = EditorUtility.SaveFolderPanel("Choose Location of the Presentation build", "", "");
			if (string.IsNullOrEmpty(path)) return;

			updateLoaderScene(deck);
			deck.BakeSlides();

			var scenes = SceneUtils.GetBuildScenes(deck, SlideDeck.PlayModeType.PlayMode, SlideDeck.VisibilityType.Visible);
			scenes.Insert(0, new EditorBuildSettingsScene(SceneUtils.LoaderScenePath, true));

			var options = BuildOptions.ShowBuiltPlayer;
			if (EditorUserBuildSettings.development) options |= BuildOptions.Development;
			if (EditorUserBuildSettings.connectProfiler) options |= BuildOptions.ConnectWithProfiler;

			try 
			{
				BuildPipeline.BuildPlayer(scenes.ToArray(), Path.Combine(path, "Presentation"), EditorUserBuildSettings.activeBuildTarget, options);
			} catch (Exception e)
			{
				Debug.Log(e.Message);
			}

		}

		private static void updateLoaderScene(SlideDeck deck)
		{
			var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
			var scene = EditorSceneManager.OpenScene(SceneUtils.LoaderScenePath, OpenSceneMode.Single);
			var loader = GameObject.FindObjectOfType<Loader>() as Loader;
			if (loader == null)
			{
				Debug.LogError("Failed to update Loader scene. Can't find Loader script");
				return;
			}

			var so = new SerializedObject(loader);
			so.Update();

			var prop = so.FindProperty("Properties");
			prop.objectReferenceValue = Properties.Instance;
			var d = so.FindProperty("Deck");
			d.objectReferenceValue = deck;

			so.ApplyModifiedProperties();

			EditorSceneManager.SaveScene(scene);
			EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
		}
	}
}