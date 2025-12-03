using Godot;
using System;
using System.ComponentModel;

namespace Gamma {
    public partial class Main : Node {
        public enum InteractableLookup : byte { ExitDungeon, EnterDungeon, SlinkSinkDialogueStart }
        public struct PendingSceneChange {
            public string scenePath;
            public bool shouldChangeScene;
        }
        public const int DEFAULT_INTERACTABLES_SIZE = 16;
        public const int DEFAULT_AUDIO_POOL_SIZE = 8;
        public const float ALMOST_ZERO = 0.00001f;
        public const float GRAVITY = 9.81f;
        public static readonly Color NULL_COLOR = new Color(1f, 1f, 1f, 1f);
        public static readonly Vector3 TELEPORT_VERTICAL_OFFSET = new Vector3(0, 0.1f, 0);
        public static readonly Vector3 DEFAULT_UPWARD_CAMERA_OFFSET = new Vector3(0, 1.246f, 0);
        public static readonly Vector3 Y_FLAT = new Vector3(1, 0, 1);
        public static readonly Transform3D DEFAULT_SLINK_CHEST_POSE = new Transform3D(
            new Vector3(-1f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 0f, -1f),
            new Vector3(-0.0055462457f, 1.320011f, 0.016058354f)
        );
        public struct SceneState {
            public Node previousFrameScene;
            public Node currentScene;
            public float timeSinceSceneLoad;
            public int physicsFramesSinceSceneLoad;
            public string CurrentScenePath;
            public bool isSceneLoaded;
        }
        public SceneState sceneState;
        public AudioStream metalDinkSFX = GD.Load<AudioStream>("res://assets/sound/metalslam.wav");
        public AudioStream metalSlamSFX = GD.Load<AudioStream>("res://assets/sound/metalslam1.wav");
        public AudioStream footStepMetalSFX = GD.Load<AudioStream>("res://assets/sound/metal-footstep.wav");
        public AudioStream teleportSFX = GD.Load<AudioStream>("res://assets/sound/teleport.mp3");
        public Interactable[] interactables;
        public PendingSceneChange pendingSceneChange;
        public DialogueBox dialogueBox;
        public SubtitleBox subtitleBox;
        public Player player;
        public PlayerCamera playerCamera;
        public PrisonSpotLight prisonSpotlight;
        public Node entitiesNode;
        public PhysicsMaterial globalPhysicsMaterial;
        public Vector2 inputDirection;
        public double globalDelta;
        public bool shouldSpawnMothman = false;
        public bool outOfTime = false;
        public void RotateTowards(Vector3 lookDirection, Node3D inputNode, float rotationSpeed) {
            if (lookDirection.LengthSquared() <= ALMOST_ZERO) { return; }
            float targetRotation = (float)Math.Atan2(-lookDirection.X, -lookDirection.Z);
            inputNode.Rotation = new Vector3(inputNode.Rotation.X, Mathf.LerpAngle(inputNode.Rotation.Y, targetRotation, rotationSpeed), inputNode.Rotation.Z);
        }
        public void InteractablesAdd(Node3D inputNode, InteractableLookup inputInteraction) {
            for (int i = 0; i < interactables.Length; i++) {
                if (interactables[i].node == null) {
                    interactables[i].node = inputNode;
                    interactables[i].interaction = inputInteraction;
                    GD.Print($"Added interactable {inputNode.Name} at index {i}");
                    return;
                }
            }
            GD.PrintErr("No space to add new interactable!");
        }
        public void ChangeScene(string scenePath) {
            pendingSceneChange.shouldChangeScene = true;
            pendingSceneChange.scenePath = scenePath;
        }
        public void ProcessPendingSceneChange() {
            if (!pendingSceneChange.shouldChangeScene) { return; }
            sceneState.isSceneLoaded = false;
            GetTree().ChangeSceneToFile(pendingSceneChange.scenePath);
            pendingSceneChange.shouldChangeScene = false;
            pendingSceneChange.scenePath = "";
        }
        public void InitializeScene() {
            GD.Print("Initializing scene");
            sceneState.timeSinceSceneLoad = 0;
            sceneState.physicsFramesSinceSceneLoad = 0;
            player = new Player();
            playerCamera = new PlayerCamera();
            interactables = new Interactable[DEFAULT_INTERACTABLES_SIZE];
            prisonSpotlight = new PrisonSpotLight();
            entitiesNode = GetTree().CurrentScene.GetNode<Node>("Entities");
            GD.Print("entities children: " + entitiesNode.GetChildCount());
            for (int i = 0; i < entitiesNode.GetChildCount(); i++) {
                Node3D child = entitiesNode.GetChild<Node3D>(i);
                GD.Print("entity: " + child.Name);
                if (child.Name == "Player") { PlayerInitialize((CharacterBody3D)child); continue; }
                if (child.Name == "PlayerCamera") { PlayerCameraInitialize((Camera3D)child); continue; }
                if (child.Name == "DungeonExit") { InteractablesAdd((Node3D)child, InteractableLookup.ExitDungeon); continue; }
                if (child.Name == "DungeonEntrance") { InteractablesAdd((Node3D)child, InteractableLookup.EnterDungeon); continue; }
                if (child.Name == "SlinkSink") { InteractablesAdd((Node3D)child, InteractableLookup.SlinkSinkDialogueStart); continue; }
                if (child.Name == "PrisonSpotlight") {
                    prisonSpotlight = new PrisonSpotLight();
                    prisonSpotlight.node = child;
                    prisonSpotlight.speed = 0.5f;
                    continue;
                }
                GD.Print("unknown entity: " + child.Name);
            }
            DialogueBoxInitialize(GetTree().CurrentScene.GetNode<Node>("UI").GetNode<Control>("DialogueBox"));
            SubtitlesInitialize(GetTree().CurrentScene.GetNode<Node>("UI").GetNode<VBoxContainer>("SubtitleBox"));
            Audio3DInitialize(DEFAULT_AUDIO_POOL_SIZE);
        }
        public override void _Ready() {
            GD.Print("Ready");
            Engine.MaxFps = 240;
            globalPhysicsMaterial = new PhysicsMaterial();
            globalPhysicsMaterial.Friction = 0.2f;
            globalPhysicsMaterial.Bounce = 0f;
        }
        private bool HasCrossedFrame(float previousFrame, float currentFrame, float targetFrame) {
            if (currentFrame < previousFrame) { return targetFrame >= previousFrame || targetFrame <= currentFrame; }
            return previousFrame < targetFrame && targetFrame <= currentFrame;
        }
        public override void _PhysicsProcess(double delta) {
            if (GetTree().CurrentScene != sceneState.previousFrameScene) {
                sceneState.isSceneLoaded = false;
                sceneState.previousFrameScene = GetTree().CurrentScene;
                InitializeScene();
            }
            globalDelta = delta;
            sceneState.timeSinceSceneLoad += (float)delta;
            sceneState.physicsFramesSinceSceneLoad++;
            inputDirection = Input.GetVector("moveLeft", "moveRight", "moveUp", "moveBack");
            if (!shouldSpawnMothman && sceneState.timeSinceSceneLoad >= 300f) { shouldSpawnMothman = true; }
            if (!outOfTime && sceneState.timeSinceSceneLoad >= 600f) { outOfTime = true; }

            if (Input.IsActionJustPressed("abort")) {
                Color randomColor = new Color(GD.Randf(), GD.Randf(), GD.Randf(), 1f);
                SubtitleData test1 = new SubtitleData {
                    text = "Hello, world!",
                    textColor = randomColor,
                    totalLifeTime = 3f,
                    currentLifeTime = 3f,
                    onSubtitleStart = new Action<Main>[] {
                        (main) => GD.Print("Test 1 started!"),
                        (main) => {player.node.GlobalPosition += new Vector3(0, 5f, 0); GD.Print("Player teleported up!"); }
                    },
                    onSubtitleComplete = new Action<Main>[] {
                        (main) => GD.Print("Test 1 completed!"),
                        (main) => {player.node.GlobalPosition += new Vector3(0, 5f, 0); GD.Print("Player teleported up!"); }
                    }
                };
                SubtitlesAdd(test1);
             }
            if (Input.IsActionJustPressed("interact")) { Interact(); }
            PlayerUpdate();
            PlayerCameraUpdate(ref playerCamera);
            DialogueUpdate();
            SubtitlesUpdate();
            if (prisonSpotlight.node != null) { PrisonSpotlightUpdate(ref prisonSpotlight); }
            ProcessPendingSceneChange();
        }
    }
}
