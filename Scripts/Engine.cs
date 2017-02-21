using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;

namespace Unity.Presentation 
{
	public class Engine : ScriptableObject 
	{

		#region Consts

		public delegate void SlideEventHandler(object sender, SlideEventArgs e);

		private enum PlayModeChange
		{
			// User clicked Play button
			User,

			// Need to exit Play Mode when starting
			ExitBeforeStart,

			// Slide wants to change Play Mode
			SlideChangedPlayMode,

			// Need to exit Play Mode when stopping
			ExitBeforeStop
		}

		private enum PresentationState
		{
			Default,

			LoadingScene
		}

		private const string PRESENTATION_SCENE = "Scenes/PresentationLoader.unity";

		#endregion

		#region Static properties

		public static Engine Instance
		{
			get 
			{
				if (instance == null)
				{
					var objs = Resources.FindObjectsOfTypeAll<Engine>();
					if (objs.Length > 0) instance = objs[0];
					else
					{
						instance = CreateInstance<Engine>();
						instance.hideFlags = HideFlags.HideAndDontSave;
					}
				}
				return instance;
			}
		}

		#endregion

		#region Events

		public event SlideEventHandler SlideChanged;

		#endregion

		#region Public properties

		public SlideDeck SlideDeck
		{
			get 
			{
				if (!deck) NewDeck();
				return deck;
			}
		}

		public bool IsBusy
		{
			get { return state != PresentationState.Default || playModeChangeReason != PlayModeChange.User; }
		}

		public bool IsPresenting
		{
			get { return isPresenting; }
		}

		public int CurrentSlideId
		{
			get { return currentSlideId; }
		}

		public PresentationSlide CurrentSlide
		{
			get { return SlideDeck.Slides[currentSlideId]; }
		}

		#endregion

		#region Private variables

		static private Engine instance;

		[SerializeField]
		private SlideDeck deck;
		[SerializeField]
		private bool isPresenting = false;
		[SerializeField]
		private int currentSlideId = 0;
		[SerializeField]
		private SceneSetup[] defaultSceneSetup;

		private Properties props;

		private int startFrom = 0;
		private PresentationState state = PresentationState.Default;
		private PlayModeChange playModeChangeReason = PlayModeChange.User;
		private PresentationHelper helper;
		private int newSceneIndex;

		#endregion

		#region Public API

		public SlideDeck NewDeck()
		{
			deck = CreateInstance<SlideDeck>();
			deck.hideFlags = HideFlags.HideAndDontSave;
			return deck;
		}

		public void LoadDeck(SlideDeck deck = null)
		{
			if (deck == null) 
			{
				var path = EditorUtility.OpenFilePanel("Open Slide Deck", Application.dataPath, "asset");
				if (string.IsNullOrEmpty(path)) return;
				path = Path.Combine("Assets", path.Substring(Application.dataPath.Length + 1));
				var newDeck = AssetDatabase.LoadAssetAtPath<SlideDeck>(path);
				if (newDeck == null)
				{
					EditorUtility.DisplayDialog("Error!", "Couldn't load the presentation.", "OK");
					return;
				}
				this.deck = newDeck;
			}
			else
			{
				this.deck = deck;
			}
		}

		public void StartPresentation(int slide = 0)
		{
#if UNITY_EDITOR
			EditorApplication.playmodeStateChanged += playmodeChangeHandler;

			// Exit Play mode first
			if (EditorApplication.isPlaying)
			{
				startFrom = slide;
				changePlayMode(false, PlayModeChange.ExitBeforeStart);
			}
			else
#endif
			{
				startPresentation(slide);
			}
		}

		public void StopPresentation()
		{
			isPresenting = false;
			if (EditorApplication.isPlaying)
				changePlayMode(false, PlayModeChange.ExitBeforeStop);
			else
				restoreEditorState();
		}

		public void NextSlide()
		{
			GotoSlide(currentSlideId + 1);
		}

		public void PreviousSlide()
		{
			GotoSlide(currentSlideId - 1);
		}

		public void GotoSlide(int i)
		{
			if (IsBusy) return;
			gotoSlide(i);
		}

		#endregion

		#region Unity methods

		private void OnEnable()
		{
			props = Properties.Instance;

			if (isPresenting) 
			{
				EditorApplication.playmodeStateChanged += playmodeChangeHandler;
			}
		}

		#endregion

		#region Private functions

		private void changePlayMode(bool value, PlayModeChange reason)
		{
			playModeChangeReason = reason;
			EditorApplication.isPlaying = value;
		}

		private void startPresentation(int slide)
		{
			isPresenting = true;
			currentSlideId = -1;

#if UNITY_EDITOR
			// Save scene setup to return to it after Stop()
			saveEditorState();
			// Add all needed scenes to build settings
			fixScenes();
#endif
			gotoSlide(slide);
		}

		private void gotoSlide(int i)
		{
			if (!isPresenting) 
			{
				Debug.LogWarningFormat("Can't go to slide {0}. Need to start presentation first.", i);
				return;
			}

			if (i < 0 || i >= deck.Slides.Count || i == currentSlideId) return;

			var newSlide = deck.Slides[i];
			if (!newSlide.Visible || string.IsNullOrEmpty(newSlide.ScenePath))
			{
				if (i > currentSlideId) gotoSlide(i + 1);
				else gotoSlide(i - 1);
				return;
			}

			currentSlideId = i;

			var wasInPlayMode = EditorApplication.isPlaying;
			if (newSlide.StartInPlayMode)
			{
				if (wasInPlayMode) changeSlide(); 
				else 
				{
					openEmptyScene();
					changePlayMode(true, PlayModeChange.SlideChangedPlayMode);
				}
			} 
			else 
			{
				if (wasInPlayMode) 
				{
					changePlayMode(false, PlayModeChange.SlideChangedPlayMode);
				} 
				else changeSlide();
			}
		}

		private void changeSlide()
		{
			var newSlide = deck.Slides[currentSlideId];
			var newScene = newSlide.ScenePath;
			if (!string.IsNullOrEmpty(newScene))
			{
				if (EditorApplication.isPlaying)
				{
					try 
					{
						state = PresentationState.LoadingScene;
						newSceneIndex = SceneManager.sceneCount;
						SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);
					} 
					catch
					{
						state = PresentationState.Default;
					}
				} 
				else 
				{
					destroySceneHelper();
					EditorSceneManager.OpenScene(newScene, OpenSceneMode.Single);
					createSceneHelper();
				}

				if (SlideChanged != null) SlideChanged(this, new SlideEventArgs(currentSlideId));
			}
		}

		private void fixScenes()
		{
#if UNITY_EDITOR
			var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
			var hash = new HashSet<string>();
			var count = scenes.Count;
			for (var i = 0; i < count; i++) hash.Add(scenes[i].path);

			var modified = false;
			for (var i = 0; i < deck.Slides.Count; i++)
			{
				var path = deck.Slides[i].ScenePath;
				if (string.IsNullOrEmpty(path)) continue;
				if (hash.Contains(path)) continue;
				scenes.Add(new EditorBuildSettingsScene(path, true));
				modified = true;
			}

			if (modified) EditorBuildSettings.scenes = scenes.ToArray();
#endif
		}

		private void saveEditorState()
		{
#if UNITY_EDITOR
			defaultSceneSetup = EditorSceneManager.GetSceneManagerSetup();
#endif
		}

		private void restoreEditorState()
		{
			if (defaultSceneSetup != null && defaultSceneSetup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(defaultSceneSetup);
		}

		private void playmodeChangeHandler()
		{
			if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
			{
				// Went out of Play Mode
				destroySceneHelper();
				switch (playModeChangeReason)
				{
					case PlayModeChange.ExitBeforeStart:
						startPresentation(startFrom);
						break;
					case PlayModeChange.SlideChangedPlayMode:
						changeSlide();
						break;
					case PlayModeChange.ExitBeforeStop:
						restoreEditorState();
						break;
					case PlayModeChange.User:
						var newScene = deck.Slides[currentSlideId].ScenePath;
						if (string.IsNullOrEmpty(newScene)) restoreEditorState();
						else EditorSceneManager.OpenScene(newScene, OpenSceneMode.Single);
						createSceneHelper();
						break;
				}
				playModeChangeReason = PlayModeChange.User;
			} 
			else if (EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
			{
				// Went into Play Mode
				if (playModeChangeReason == PlayModeChange.SlideChangedPlayMode)
				{
					createSceneHelper();
					changeSlide();
				}
				playModeChangeReason = PlayModeChange.User;
			}
			else if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
			{
				// Going into Play Mode
				destroySceneHelper();
				if (playModeChangeReason == PlayModeChange.User)
				{
					openEmptyScene();
					changePlayMode(true, PlayModeChange.SlideChangedPlayMode);
				}
			}
		}

		private void openEmptyScene()
		{
			// Need to open an empty scene first because the editor will try to return to the scene from which we started playmode
			EditorSceneManager.OpenScene(Path.Combine(Utils.PackageRoot, PRESENTATION_SCENE), OpenSceneMode.Single);
		}

		private void createSceneHelper()
		{
			var go = new GameObject();
			go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;

			helper = go.AddComponent<PresentationHelper>();
			helper.Frame += frameHandler;
			helper.NextSlide = props.NextSlide;
			helper.PreviousSlide = props.PreviousSlide;
			helper.Previous += previousSlideHandler;
			helper.Next += nextSlideHandler;
		}

		private void destroySceneHelper()
		{
			if (helper == null) return;

			if (Application.isPlaying) 
			{
				Destroy(helper.gameObject);
			} else 
			{
				DestroyImmediate(helper.gameObject);
			}
			helper = null;
		}

		#endregion

		#region Event handlers

		private void previousSlideHandler(object sender, EventArgs e)
		{
			gotoSlide(currentSlideId-1);
		}

		private void nextSlideHandler(object sender, EventArgs e)
		{
			gotoSlide(currentSlideId+1);
		}

		private void frameHandler(object sender, EventArgs e)
		{
			if (state == PresentationState.LoadingScene)
			{
				var scene = SceneManager.GetSceneAt(newSceneIndex);
				if (scene.isLoaded)
				{
					state = PresentationState.Default;

					SceneManager.SetActiveScene(scene);
					destroySceneHelper();
					for (var i = 0; i < newSceneIndex ; i++) 
					{
						var s = SceneManager.GetSceneAt(0);
						SceneManager.UnloadSceneAsync(s);
					}

					createSceneHelper();
				}
			}
		}

		#endregion

	}

	public class SlideEventArgs : EventArgs
	{
		public int Index;

		public SlideEventArgs(int index)
		{
			Index = index;
		}
	}

}
