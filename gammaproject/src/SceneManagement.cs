using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct LoadingScreen {
            public Control node;
            public Sprite2D icon;
            public static int currentFrame;
            public static int totalFrames;
            public static int speed = 2;
        }
        public struct FadeRect {
            public ColorRect node;
            public float fadeMagnitude;
        }
        public struct PendingSceneChange {
            public string scenePath;
            public bool shouldChangeScene;
        }
        public struct SceneState {
            public Node currentScene;
            public float timeSinceSceneLoad;
            public int physicsFramesSinceSceneLoad;
            public string CurrentScenePath;
            public bool isSceneLoaded;
        }
        public void InitializeLoadingScreen(Control inputLoadingScreen) {
            loadingScreen.node = inputLoadingScreen;
            loadingScreen.icon = loadingScreen.node.GetNode<Sprite2D>("LoadingSpriteControl/LoadingSprite");
            DirAccess directory = DirAccess.Open("res://assets/textures/loadingicons/");
            if (directory == null) {
                GD.PrintErr("InitializeLoadingScreen: Could not open loading icons directory!");
                return;
            }
            string[] files = directory.GetFiles();
            int count = 0;
            for (int i = 0; i < files.Length; i++) {
                if (files[i].EndsWith(".png.import") || files[i].EndsWith(".png")) { count++; }
            }
            if (count == 0) {
                GD.PrintErr("InitializeLoadingScreen: No PNG files found in loading icons directory!");
                return;
            }
            string[] pngFiles = new string[count];
            int index = 0;
            for (int i = 0; i < files.Length; i++) {
                if (files[i].EndsWith(".png.import")) { 
                    pngFiles[index++] = "res://assets/textures/loadingicons/" + files[i].Replace(".import", "");
                } else if (files[i].EndsWith(".png")) {
                    pngFiles[index++] = "res://assets/textures/loadingicons/" + files[i];
                }
            }
            int chosenIndex = (int)GD.RandRange(0, count - 1);
            loadingScreen.icon.Texture = GD.Load<Texture2D>(pngFiles[chosenIndex]);
            GD.Print("loadingScreen.icon.Texture: " + loadingScreen.icon.Texture);
            LoadingScreen.totalFrames =
                loadingScreen.icon.Hframes *
                loadingScreen.icon.Vframes;
            LoadingScreen.currentFrame = 0;
            LoadingScreen.speed = 4;
        }
        public void StartFade(float inputFadeMagnitude) {
            float startAlpha = inputFadeMagnitude > 0f ? 0f : 0.99f;
            fadeRect.node.Color = new Color(0, 0, 0, startAlpha);
            fadeRect.fadeMagnitude = inputFadeMagnitude;
        }
        public void ChangeScene(string scenePath) {
            pendingSceneChange.shouldChangeScene = true;
            pendingSceneChange.scenePath = scenePath;
            StartFade(0.6f);
        }
        public void ProcessPendingSceneChange() {
            if (!pendingSceneChange.shouldChangeScene) { return; }
            if (fadeRect.node.Color.A < 1f) { return; }
            GD.Print("Changing scene");
            ClearScene();
            sceneState.isSceneLoaded = false;
            GetTree().ChangeSceneToFile(pendingSceneChange.scenePath);
            pendingSceneChange.shouldChangeScene = false;
            pendingSceneChange.scenePath = "";
        }
        public void ClearScene() {
            GD.Print("Clearing scene");
            prisonSpotlight.node = null;
            sceneState.physicsFramesSinceSceneLoad = 0;
            sceneState.timeSinceSceneLoad = 0f;
        }
        public void UpdateFadeInOut() {
            if (fadeRect.node == null) { return; }
            float newAlpha = fadeRect.node.Color.A + (fadeRect.fadeMagnitude * (float)globalPhysicsDelta);
            fadeRect.node.Color = new Color(
                fadeRect.node.Color.R,
                fadeRect.node.Color.G,
                fadeRect.node.Color.B,
                Mathf.Clamp(newAlpha, 0f, 1)
            );
        }
        public void UpdateLoadingScreen() {
            if (loadingScreen.node == null || loadingScreen.node.Modulate == NULL_COLOR || loadingScreen.icon == null) { return; }
            LoadingScreen.currentFrame++;
            int period = 2 * (LoadingScreen.totalFrames - 1);
            int position = LoadingScreen.currentFrame % period;
            loadingScreen.icon.Frame = position < LoadingScreen.totalFrames ? position : period - position;
        }
    }
}
