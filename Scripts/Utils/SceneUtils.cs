using System.Collections.Generic;
using Unity.Presentation;
using System.IO;
using Unity.Presentation.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Static scene utils.
public static class SceneUtils
{

	// The path to the Empty.scene in the project.
	public static string EmptyScenePath
	{
		get
		{
			return Path.Combine(PresentationUtils.PackageRoot, "Scenes/Empty.unity");
		}
	}

	// The path to the Loader.scene in the project.
	public static string LoaderScenePath
	{
		get
		{
			return Path.Combine(PresentationUtils.PackageRoot, "Scenes/Loader.unity");
		}
	}

#if UNITY_EDITOR
	// Returns all scenes in build settings which do not belong to the deck.
	// SlideDeck deck -- current deck.
	public static List<EditorBuildSettingsScene> GetNonDeckScenes(SlideDeck deck)
	{
		var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
		var slides = deck.GetSlides(SlideDeck.PlayModeType.All, SlideDeck.VisibilityType.All);

		var nonDeckScenes = new List<EditorBuildSettingsScene>();
		var hash = new HashSet<string>();
		var count = slides.Count;
		for (var i = 0; i < count; i++) hash.Add(slides[i].ScenePath);

		foreach (var scene in scenes)
		{
			if (hash.Contains(scene.path)) continue;
			nonDeckScenes.Add(scene);
		}

		return nonDeckScenes;
	}

	// Updates scenes in build settings.
	// SlideDeck deck -- deck to use.
	// SlideDeck.PlayModeType playmode -- Play Mode filter.
	// SlideDeck.VisibilityType visibility -- visibility filter.
	public static void UpdateBuildScenes(SlideDeck deck, SlideDeck.PlayModeType playmode, SlideDeck.VisibilityType visibility)
	{
		EditorBuildSettings.scenes = GetBuildScenes(deck, playmode, visibility).ToArray();
	}

	// Returns slide list for builds settings.
	// SlideDeck deck -- deck to use.
	// SlideDeck.PlayModeType playmode -- Play Mode filter.
	// SlideDeck.VisibilityType visibility -- visibility filter.
	public static List<EditorBuildSettingsScene> GetBuildScenes(SlideDeck deck, SlideDeck.PlayModeType playmode, SlideDeck.VisibilityType visibility)
	{
		var nonDeckScenes = SceneUtils.GetNonDeckScenes(deck);

		var slides = deck.GetSlides(playmode, visibility);
		var scenes = new List<EditorBuildSettingsScene>(slides.Count + nonDeckScenes.Count);

		foreach (var slide in slides)
		{
			var path = slide.ScenePath;
			if (string.IsNullOrEmpty(path)) continue;
			scenes.Add(new EditorBuildSettingsScene(path, true));
		}

		scenes.AddRange(nonDeckScenes);
		return scenes;
	}
#endif
}
