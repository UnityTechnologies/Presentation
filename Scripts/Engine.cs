using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Presentation.Behaviors;
using Unity.Presentation.Utils;

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

		private const string PRESENTATION_SCENE = "Scenes/Empty.unity";

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

		private Ticker gameViewTicker;

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
			EditorApplication.update += updateHandler;
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
			{
				destroySceneHelper();
				clearPresentationState();
				restoreEditorState();
			}
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
			gameViewTicker = new Ticker(0.5f, () => {
				var cam = Camera.main;
				if (cam == null) return; // fails when going to Play Mode
				cam.transform.Translate(0.1f, 0, 0);
				cam.transform.Translate(-0.1f, 0, 0);
			});

			if (isPresenting) 
			{
				EditorApplication.update += updateHandler;
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
						SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Single);
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
			// Need to fetch all scenes since visibility and play mode can be switched in play mode
			SceneUtils.UpdateBuildScenes(deck, SlideDeck.PlayModeType.All, SlideDeck.VisibilityType.All);
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

		private void openEmptyScene()
		{
			// Need to open an empty scene first because the editor will try to return to the scene from which we started playmode
			EditorSceneManager.OpenScene(Path.Combine(PresentationUtils.PackageRoot, PRESENTATION_SCENE), OpenSceneMode.Single);
		}

		private void createSceneHelper()
		{
			var go = new GameObject();
			go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;// | HideFlags.HideInHierarchy;

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

		private void clearPresentationState()
		{
			EditorApplication.playmodeStateChanged -= playmodeChangeHandler;
			EditorApplication.update -= updateHandler;
		}

		#endregion

		#region Event handlers

#if UNITY_EDITOR

		private void updateHandler()
		{
			if (!Application.isPlaying)
			{
				gameViewTicker.Tick();
			}
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
					clearPresentationState();
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

#endif

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
				var scene = SceneManager.GetSceneAt(0);

				if (scene.isLoaded)
				{
					state = PresentationState.Default;
					destroySceneHelper();
					createSceneHelper();
				}
			}
		}

		#endregion

		private class Ticker
		{
			private float interval;
			private double nextTime;
			private Action action;

			public Ticker(float interval, Action action)
			{
				this.interval = interval;
				this.action = action;

				reset();
			}

			public void Tick()
			{
				var delta = nextTime - EditorApplication.timeSinceStartup;
				if (delta <= 0)
				{
					action();
					reset();
				}
			}

			private void reset()
			{
				nextTime = EditorApplication.timeSinceStartup + interval;
			}
		}

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
