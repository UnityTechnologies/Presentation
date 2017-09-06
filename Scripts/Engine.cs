/* This is the heart of the Presentation framework. It holds current state and handles assembly reloads.
 * 
 * Engine references the following parts of the system:
 * 
 * 	1. Properties -- global settings for the presentation.
 * 	2. Slide Deck -- presentation data.
 * 	3. PresentationHelper -- a MonoBehaviour in the scene providing frame and button events from the Game View.
 * 
 * ** API **
 * 
 * 	- NewDeck() -- creates a new slide deck.
 * 	- LoadDeck() -- Loads a slide deck or opens a dialog to select a slide deck from disk.
 * 	- SaveDeck() -- Saves current slide deck.
 * 	- StartPresentation() -- Starts the presentation at a specific slide.
 * 	- StopPresentation() -- Stops the presentation.
 * 	- NextSlide() -- Switches to the next slide.
 * 	- PreviousSlide() -- Switches to the previous slide.
 * 	- GotoSlide() -- Jumps to the specific slide.
 * 
 * ** Surviving Play Mode Changes **
 * 
 * This object survives assembly reloading when going into Play Mode and when scripts are recompiled.
 * Detecting Play Mode change is not very clear in Unity, so the code follows these conventions:
 * 
 * 	1. The Engine subscribes to `EditorApplication.playmodeStateChanged` in `OnEnable`.
 * 	2. All Play Mode changes initiated from code go through `changePlayMode(bool value, PlayModeChange reason)`
 * 	   method where the reason Play Mode was changed is explicitly specified.
 *  3. The code `playmodeChangeHandler()` figures out if we are going in or out of Play Mode:
 * 		a. (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) -- just went out of Play Mode
 * 		b. (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode) -- going into Play Mode (before assembly reload)
 * 		c. (EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode) -- Just went into Play Mode
 * 	4. And based on `reason` decides how to handle this situation:
 * 		a. User -- User clicked Play button when presentation was running (default).
 * 		b. ExitBeforeStart -- Need to exit Play Mode when starting.
 * 		c. SlideChangedPlayMode -- Slide wants to change Play Mode.
 * 		d. ExitBeforeStop -- Need to exit Play Mode when stopping.
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using Unity.Presentation.Behaviors;
using Unity.Presentation.Utils;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Presentation.EditorOnly;
#endif

namespace Unity.Presentation
{
    /// <summary>
    /// Main Presentation controller.
    /// Holds current state of the Presentation.
    /// </summary>
    public class Engine : ScriptableObject
    {

#region Consts

        /// <summary>
        /// Public version.
        /// </summary>
        public readonly Version VERSION = new Version(1, 0);

        /// <summary>
        /// Slide state events.
        /// </summary>
		public delegate void SlideEventHandler(object sender, SlideEventArgs e);

        /// <summary>
        /// The reason why Play Mode state was changed.
        /// </summary>
        private enum PlayModeChange
        {
            /// <summary>
            /// User pressed Play button.
            /// </summary>
            User,

            /// <summary>
            /// Need to exit Play Mode when starting.
            /// </summary>
            ExitBeforeStart,

            /// <summary>
            /// Slide wants to change Play Mode.
            /// </summary>
            SlideChangedPlayMode,

            /// <summary>
            /// Need to exit Play Mode when stopping.
            /// </summary>
            ExitBeforeStop
        }

        /// <summary>
        /// Current state of the presentation.
        /// </summary>
        private enum PresentationState
        {
            /// <summary>
            /// General state.
            /// </summary>
            Default,

            /// <summary>
            /// A scene is being loaded.
            /// </summary>
            LoadingScene
        }

#endregion

#region Static properties

        /// <summary>
        /// The instance of the singleton.
        /// </summary>
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

        /// <summary>
        /// Slide change event.
        /// </summary>
        public event SlideEventHandler SlideChanged;

#endregion

#region Public properties

        /// <summary>
        /// Returns current slide deck.
        /// </summary>
        public SlideDeck SlideDeck
        {
            get
            {
                if (!deck) NewDeck();
                return deck;
            }
        }

        /// <summary>
        /// Indicates that the Engine is busy, so controlling interface should be inactive.
        /// </summary>
        public bool IsBusy
        {
            get { return state != PresentationState.Default || playModeChangeReason != PlayModeChange.User; }
        }

        /// <summary>
        /// Indicates if the Engine is in presenting mode.
        /// </summary>
        public bool IsPresenting
        {
            get { return isPresenting; }
        }

        /// <summary>
        /// Returns current slide id.
        /// </summary>
        public int CurrentSlideId
        {
            get { return currentSlideId; }
        }

        /// <summary>
        /// Returns the current slide object.
        /// </summary>
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

        private Properties props;

        private PresentationState state = PresentationState.Default;
        private PlayModeChange playModeChangeReason = PlayModeChange.User;
        private PresentationHelper helper;

#if UNITY_EDITOR
        private int startFrom = 0;
        private Ticker gameViewTicker;
        [SerializeField]
        private SceneSetup[] defaultSceneSetup;
#endif

#endregion

#region Public API

        /// <summary>
        /// Creates a new Slide Deck.
        /// </summary>
        /// <returns>The deck.</returns>
        public SlideDeck NewDeck()
        {
            deck = CreateInstance<SlideDeck>();
            deck.hideFlags = HideFlags.HideAndDontSave;
            return deck;
        }

        /// <summary>
        /// Loads a slide deck or opens a dialog to select a slide deck from disk.
        /// </summary>
        /// <param name="deck">The deck to load. If <c>null</c>, this method shows open file dialog.</param>
        public void LoadDeck(SlideDeck deck = null)
        {
            if (deck == null)
            {
#if UNITY_EDITOR
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
#endif
            }
            else
            {
                this.deck = deck;
            }
        }

        /// <summary>
        /// Saves current slide deck.
        /// </summary>
        public void SaveDeck()
        {
            if (deck == null) return;
#if UNITY_EDITOR
            deck.Save(true);
#endif
        }

        /// <summary>
        /// Starts the presentation at a specific slide.
        /// </summary>
        /// <param name="slide">The slide to start from.</param>
        public void StartPresentation(int slide = 0)
        {
#if UNITY_EDITOR
            subscribeToEvents();

            // Exit Play mode first
            if (EditorApplication.isPlaying)
            {
                startFrom = slide;
                changePlayMode(false, PlayModeChange.ExitBeforeStart);
            }
            else
            {
                startPresentation(slide);
            }
#else
			createSceneHelper();
			startPresentation(slide);
#endif
        }

        /// <summary>
        /// Stops the presentation.
        /// </summary>
        public void StopPresentation()
        {
            isPresenting = false;
#if UNITY_EDITOR
            if (EditorApplication.isPlaying) changePlayMode(false, PlayModeChange.ExitBeforeStop);
            else
            {
                destroySceneHelper();
                unsubscribeFromEvents();
                restoreEditorState();
            }
#endif
        }

        /// <summary>
        /// Switches to the next slide.
        /// </summary>
        public void NextSlide()
        {
            GotoSlide(currentSlideId + 1);
        }

        /// <summary>
        /// Switches to the previous slide.
        /// </summary>
        public void PreviousSlide()
        {
            GotoSlide(currentSlideId - 1);
        }

        /// <summary>
        /// Jumps to the specific slide.
        /// </summary>
        /// <param name="i">The slide index.</param>
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

#if UNITY_EDITOR
            // This is a hack to be able to capture input from the selected Game View's OnGUI __out of Play Mode__.
            // This is needed to handle previous/next slide keys when Game View is selected.
            // This moves the main camera 0.1 units and back every half a second, so the main Game View is updated.
            gameViewTicker = new Ticker(0.5f, () =>
            {
                var cam = Camera.main;
                if (cam == null) return; // fails when going to Play Mode
                cam.transform.Translate(0.1f, 0, 0);
                cam.transform.Translate(-0.1f, 0, 0);
            });

            if (isPresenting) subscribeToEvents();
#endif
        }

#endregion

#region Private functions

        /// <summary>
        /// Initiates Play Mode change with the specific reason
        /// </summary>
        /// <param name="value"><c>true</c> — go into Play Mode, <c>false</c> — go out of Play Mode.</param>
        /// <param name="reason">The reason why Play Mode was changed.</param>
        private void changePlayMode(bool value, PlayModeChange reason)
        {
#if UNITY_EDITOR
            playModeChangeReason = reason;
            EditorApplication.isPlaying = value;
#endif
        }

        /// <summary>
        /// Starts the presentation from the slide.
        /// </summary>
        /// <param name="slide">Slide index.</param>
        private void startPresentation(int slide)
        {
            isPresenting = true;
            currentSlideId = -1;

#if UNITY_EDITOR
            // Save scene setup to return to it after Stop()
            saveEditorState();
            // Add all needed scenes to build settings
            addScenesToBuildSettings();
#endif
            gotoSlide(slide);
        }

        /// <summary>
        /// Changes the current slide.
        /// </summary>
        /// <param name="i">The slide index.</param>
        private void gotoSlide(int i)
        {
            if (!isPresenting)
            {
                Debug.LogWarningFormat("Can't go to slide {0}. Need to start presentation first.", i);
                return;
            }

            if (i < 0 || i >= deck.Slides.Count || i == currentSlideId) return;

            var newSlide = deck.Slides[i];
            if (!newSlide.Visible || string.IsNullOrEmpty(newSlide.ScenePath)
#if !UNITY_EDITOR
				// In standalone mode we skip all scenes which are not designed to run in Play Mode.
				|| !newSlide.StartInPlayMode
#endif
            )
            {
                // Go to the next slide until it is a valid slide.
                if (i > currentSlideId) gotoSlide(i + 1);
                else gotoSlide(i - 1);
                return;
            }

            currentSlideId = i;

#if UNITY_EDITOR
            var wasInPlayMode = EditorApplication.isPlaying;
            if (newSlide.StartInPlayMode)
            {
                // Play Mode slide.
                if (wasInPlayMode) 
					// We are already in Play Mode, just change the slide.
					changeSlide();
                else
                {
                    // We are out of Play Mode and need to go into Play Mode.
                    // First, we need to open an empty scene.
                    openEmptyScene();
                    // Go into Play Mode and continue setting up the slide when we are there.
                    changePlayMode(true, PlayModeChange.SlideChangedPlayMode);
                }
            }
            else
            {
                // Out of Play Mode slide.
                if (wasInPlayMode)
                {
                    // Go out of Play Mode and continue setting up the slide when we are there.
                    changePlayMode(false, PlayModeChange.SlideChangedPlayMode);
                }
                else
					// If we are out of Play Mode, we can just go to the next slide.
					changeSlide();
            }
#else
			// In Standalone mode we are always in Play Mode.
			changeSlide(); 
#endif
        }

        /// <summary>
        /// Actually displays the new slide.
        /// </summary>
        private void changeSlide()
        {
            var newSlide = deck.Slides[currentSlideId];
            var newScene = newSlide.ScenePath;
            if (!string.IsNullOrEmpty(newScene))
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlaying)
                {
#endif
                    // In Play Mode.
                    try
                    {
                        // Load the new scene async.
                        state = PresentationState.LoadingScene;
                        SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Single);
                    }
                    catch
                    {
                        state = PresentationState.Default;
                    }
#if UNITY_EDITOR
                }
                else
                {
                    // Out of Play Mode.
                    // Destroy scene helper in the old scene.
                    destroySceneHelper();
                    // Open the new scene.
                    EditorSceneManager.OpenScene(newScene, OpenSceneMode.Single);
                    // Create a new scene helper in the new scene.
                    createSceneHelper();
                }
#endif

                if (SlideChanged != null) SlideChanged(this, new SlideEventArgs(currentSlideId));
            }
        }

#if UNITY_EDITOR

        /// <summary>
        /// Adds all needed scenes to Build Settings in the editor.
        /// </summary>
        private void addScenesToBuildSettings()
        {
            // Need to fetch all scenes since visibility and play mode can be switched in play mode
            SceneUtils.UpdateBuildScenes(deck, SlideDeck.PlayModeType.All, SlideDeck.VisibilityType.All);
        }

        /// <summary>
        /// Saves current editor scene setup before going to Play Mode.
        /// </summary>
        private void saveEditorState()
        {
            defaultSceneSetup = EditorSceneManager.GetSceneManagerSetup();
        }

        /// <summary>
        /// Restores saved editor scene setup.
        /// </summary>
        private void restoreEditorState()
        {
            if (defaultSceneSetup != null && defaultSceneSetup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(defaultSceneSetup);
        }

        /// <summary>
        /// Opens the Empty.scene.
        /// </summary>
        private void openEmptyScene()
        {
            // Need to open an empty scene first because the editor will try to return to the scene from which we started playmode
            EditorSceneManager.OpenScene(SceneUtils.EmptyScenePath, OpenSceneMode.Single);
        }

        /// <summary>
        /// Subscribes to editor events.
        /// </summary>
        private void subscribeToEvents()
        {
            EditorApplication.playmodeStateChanged += playmodeChangeHandler;
            EditorApplication.update += updateHandler;
        }

        /// <summary>
        /// Clears callbacks when stopping the presentation
        /// </summary>
        private void unsubscribeFromEvents()
        {
            EditorApplication.playmodeStateChanged -= playmodeChangeHandler;
            EditorApplication.update -= updateHandler;
        }

#endif

        /// <summary>
        /// Creates scene helper
        /// </summary>
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

        /// <summary>
        /// Destroys current scene helper
        /// </summary>
        private void destroySceneHelper()
        {
            if (helper == null) return;

            if (Application.isPlaying)
            {
                Destroy(helper.gameObject);
            }
            else
            {
                DestroyImmediate(helper.gameObject);
            }
            helper = null;
        }

#endregion

#region Event handlers

#if UNITY_EDITOR

        /// <summary>
        /// Handler for the editor update event.
        /// </summary>
        private void updateHandler()
        {
            if (!Application.isPlaying)
            {
                gameViewTicker.Tick();
            }
        }

        /// <summary>
        /// Handler for the playmode change event.
        /// </summary>
        private void playmodeChangeHandler()
        {
            if (EditorApplication.isPaused)
            {
                // User paused the game
                GameView.Instance.SetNormal();
            }
            else if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Went out of Play Mode
                destroySceneHelper();
                GameView.Instance.SetNormal();

                switch (playModeChangeReason)
                {
                    case PlayModeChange.ExitBeforeStart:
					// We manually exited Play Mode to start current presentation.
					// Continue startup.
                        startPresentation(startFrom);
                        break;
                    case PlayModeChange.SlideChangedPlayMode:
					// Current slide initiated going out of Play Mode.
					// Continue showing the current slide.
                        changeSlide();
                        break;
                    case PlayModeChange.ExitBeforeStop:
					// We are stopping the presentation and need to leave Play Mode.
                        restoreEditorState();
                        unsubscribeFromEvents();
                        break;
                    case PlayModeChange.User:
					// User clicked Play button to exit Play Mode.
					// We need to open the current running scene because the one open before going to Play Mode will be reopened otherwise.
                        var newScene = deck.Slides[currentSlideId].ScenePath;
                        if (string.IsNullOrEmpty(newScene)) restoreEditorState();
                        else EditorSceneManager.OpenScene(newScene, OpenSceneMode.Single);
                        createSceneHelper();
                        break;
                }
                playModeChangeReason = PlayModeChange.User;
            }
            else
            if (EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Went into Play Mode
                if (playModeChangeReason == PlayModeChange.SlideChangedPlayMode)
                {
                    // Current slide initiated going into Play Mode.
                    // Continue showing the current slide.
                    createSceneHelper();
                    changeSlide();
                }
                playModeChangeReason = PlayModeChange.User;
            }
            else
            if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Going into Play Mode
                // Need to destroy the scene helper because it will be recreated in the new scene.
                destroySceneHelper();
                if (playModeChangeReason == PlayModeChange.User)
                {
                    // User clicked Play button to go into Play Mode.
                    // Need to open the Empty.scene and set the reason to `SlideChangedPlayMode` for the next callback logic.
                    openEmptyScene();
                    changePlayMode(true, PlayModeChange.SlideChangedPlayMode);
                }
            }
        }

#endif

        /// <summary>
        /// Handler for the PreviousSlide event of the PresentationHelper.
        /// </summary>
        private void previousSlideHandler(object sender, EventArgs e)
        {
            gotoSlide(currentSlideId - 1);
        }

        /// <summary>
        /// Handler for the NextSlide event of the PresentationHelper.
        /// </summary>
        private void nextSlideHandler(object sender, EventArgs e)
        {
            gotoSlide(currentSlideId + 1);
        }

        /// <summary>
        /// Handler for the Frame event of the PresentationHelper.
        /// This gives the Engine script Update events.
        /// </summary>
        private void frameHandler(object sender, EventArgs e)
        {
            if (state == PresentationState.LoadingScene)
            {
                // If we are loading a scene, check its progress and recreate PresentationHelper in the new scene.
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

#if UNITY_EDITOR
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
#endif

    }

    /// <summary>
    /// Slide event arguments.
    /// </summary>
    public class SlideEventArgs : EventArgs
    {
        /// <summary>
        /// Slide index.
        /// </summary>
        public int Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Unity.Presentation.SlideEventArgs"/> class.
        /// </summary>
        /// <param name="index">Slide index.</param>
        public SlideEventArgs(int index)
        {
            Index = index;
        }
    }
}
