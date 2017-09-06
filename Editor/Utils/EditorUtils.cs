using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Presentation.Behaviors;
using System;
using System.IO;

namespace Unity.Presentation.Utils
{
    /// <summary>
    /// Editor-only utility methods.
    /// </summary>
    public static class EditorUtils
    {
        /// <summary>
        /// Builds presentation deck as a standalone application.
        /// </summary>
        /// <param name="deck">Slide Deck to build.</param>
        public static void BuildPresentation(SlideDeck deck)
        {
            string path = EditorUtility.SaveFolderPanel("Choose Location of the Presentation build", "", "");
            if (string.IsNullOrEmpty(path)) return;

            updateLoaderScene(deck);
            deck.PrepareSlidesForBuild();

            var scenes = SceneUtils.GetBuildScenes(deck, SlideDeck.PlayModeType.PlayMode, SlideDeck.VisibilityType.Visible);
            scenes.Insert(0, new EditorBuildSettingsScene(SceneUtils.LoaderScenePath, true));

            var options = BuildOptions.ShowBuiltPlayer;
            if (EditorUserBuildSettings.development) options |= BuildOptions.Development;
            if (EditorUserBuildSettings.connectProfiler) options |= BuildOptions.ConnectWithProfiler;
            if (EditorUserBuildSettings.allowDebugging) options |= BuildOptions.AllowDebugging;

            try
            {
                string name;
                switch (EditorUserBuildSettings.activeBuildTarget)
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                        name = "Presentation.exe";
                        break;
                    default:
                        name = "Presentation";
                        break;
                }
                BuildPipeline.BuildPlayer(scenes.ToArray(), Path.Combine(path, name), EditorUserBuildSettings.activeBuildTarget, options);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }

        /// <summary>
        /// Updates Loader scene parameters to run the deck on start.
        /// </summary>
        /// <param name="deck">Slide Deck to use.</param>
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

            // Set properties.
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