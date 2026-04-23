using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct FadeRect {
            public ColorRect node;
            public float fadeMagnitude;
        }
        public struct PendingSceneChange {
            public string scenePath;
            public bool shouldChangeScene;
        }
        public const int DIALOGUE_PORTRAIT_SIZE = 192;
        public const int DEFAULT_INTERACTABLES_SIZE = 16;
        public const int DEFAULT_AUDIO_POOL_SIZE = 8;
        public const int DEFAULT_PROJECTILES_SIZE = 16;
        public const int DEFAULT_EXPLOSIONS_SIZE = 16;
        public const int DEFAULT_ENEMIES_SIZE = 16;
        public const int DEFAULT_TARGET_RETICLES_SIZE = 16;
        public const float DEFAULT_CAMERA_DISTANCE = 3.0f;
        public const float DEFAULT_CAMERA_HEIGHT = DEFAULT_CAMERA_DISTANCE * 0.5f;
        public const float ALMOST_ZERO = 0.00001f;
        public const float GRAVITY = 9.81f;
        public const float MAX_PROJECTILE_DISTANCE = 1000f;
        public const float MAX_PROJECTILE_LIFETIME = 10f;
        public const float TARGETTING_ANGLE = 12f;
        public const float TELEPORTENTITY_SPEED_MODIFIER = 8.0f;
        public const float TELEPORTENTITY_CLIMB_MINIMUM_HEIGHT_DIFFERENCE = 0.1f;
        public const float TELEPORTENTITY_CLIMB_MAXIMUM_HEIGHT_DIFFERENCE = 2f;
        public const float TELEPORTENTITY_CLIMB_SURFACE_NORMAL_THRESHOLD = 0.7f;
        public static readonly Color NULL_COLOR = new Color(0f, 0f, 0f, 0f);
        public static readonly Vector3 TELEPORT_VERTICAL_OFFSET = new Vector3(0, 0.1f, 0);
        public static readonly Vector3 DEFAULT_UPWARD_CAMERA_OFFSET = new Vector3(0, 1.246f, 0);
        public static readonly Vector3 Y_FLAT = new Vector3(1, 0, 1);
        public static readonly Transform3D DEFAULT_SLINK_WALK_CHEST_POSE = new Transform3D(
            new Vector3(-1f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 0f, -1f),
            new Vector3(-0.0055462457f, 1.320011f, 0.016058354f)
        );
        public static readonly Transform3D DEFAULT_SLINK_IDLE_CHEST_POSE = new Transform3D(
            new Vector3(-0.9452619f, 1.0294444E-07f, -0.32631275f),
            new Vector3(0.005352374f, 0.9998655f, -0.015504442f),
            new Vector3(0.32626888f, -0.016402349f, -0.9451347f),
            new Vector3(-0.0055462415f, 1.320011f, 0.01605832f)
        );
        public struct SceneState {
            public Node currentScene;
            public float timeSinceSceneLoad;
            public int physicsFramesSinceSceneLoad;
            public string CurrentScenePath;
            public bool isSceneLoaded;
        }
        public SceneState sceneState;
        public Player player;
        public PlayerCamera playerCamera;
        public VideoPlayer videoPlayer;
        public FadeRect fadeRect;
        public static AudioStream metalDinkSFX = GD.Load<AudioStream>("res://assets/sound/metalslam.wav");
        public static AudioStream metalSlamSFX = GD.Load<AudioStream>("res://assets/sound/metalslam1.wav");
        public static AudioStream footStepMetalSFX = GD.Load<AudioStream>("res://assets/sound/metal-footstep.wav");
        public static AudioStream teleportSFX = GD.Load<AudioStream>("res://assets/sound/teleport.mp3");
        public static AudioStream shootSFX = GD.Load<AudioStream>("res://assets/sound/rocket-launcher-shoot.wav");
        public Texture2D efxFire01 = GD.Load<Texture2D>("res://assets/textures/EFX_FIRE01.jpg");
        public PackedScene rocketScene = GD.Load<PackedScene>("res://scenes/entities/slink_rocket.tscn");
        public PackedScene targetReticleScene = GD.Load<PackedScene>("res://scenes/entities/target_reticle.tscn");
        public Interactable[] interactables;
        public Projectile[] projectiles;
        public Explosion[] explosions;
        public PendingSceneChange pendingSceneChange;
        public PrisonSpotLight prisonSpotlight;
        public WorldEnvironment worldEnvironment;
        public Node environmentNode;
        public Node entitiesNode;
        public Node uiNode;
        public Camera3D currentCamera;
        public PhysicsMaterial globalPhysicsMaterial;
        public float cameraFarSetting = 100;
        public double globalPhysicsDelta;
        public double globalProcessDelta;
        public void RotateTowards(Vector3 lookDirection, Node3D inputNode, float rotationSpeed) {
            if (lookDirection.LengthSquared() <= ALMOST_ZERO) { return; }
            float targetRotation = (float)Math.Atan2(-lookDirection.X, -lookDirection.Z);
            inputNode.Rotation = new Vector3(inputNode.Rotation.X, Mathf.LerpAngle(inputNode.Rotation.Y, targetRotation, rotationSpeed), inputNode.Rotation.Z);
        }
        bool HasCrossedPlaybackPosition(float inputPreviousPosition, float inputCurrentPosition, float inputEventPosition) {
            if (inputCurrentPosition >= inputPreviousPosition) { return inputPreviousPosition < inputEventPosition && inputEventPosition <= inputCurrentPosition; }
            return inputPreviousPosition < inputEventPosition || inputEventPosition <= inputCurrentPosition;
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
        public void StartFade(float inputFadeMagnitude) {
            float startAlpha = inputFadeMagnitude > 0f ? 0f : 1f;
            fadeRect.node.Color = new Color(0, 0, 0, startAlpha);
            fadeRect.fadeMagnitude = inputFadeMagnitude;
        }
        public void ChangeScene(string scenePath) {
            pendingSceneChange.shouldChangeScene = true;
            pendingSceneChange.scenePath = scenePath;
            StartFade(0.3f);
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
        }
        public void InitializeScene() {
            GD.Print("Initializing scene");
            sceneState.timeSinceSceneLoad = 0;
            sceneState.physicsFramesSinceSceneLoad = 0;
            player = new Player();
            playerCamera = new PlayerCamera();
            enemies = new Enemy[DEFAULT_ENEMIES_SIZE];
            enemyCount = 0;
            interactables = new Interactable[DEFAULT_INTERACTABLES_SIZE];
            projectiles = new Projectile[DEFAULT_PROJECTILES_SIZE];
            explosions = new Explosion[DEFAULT_EXPLOSIONS_SIZE];
            environmentNode = GetTree().CurrentScene.GetNode<Node>("Environment");
            worldEnvironment = environmentNode.GetNode<WorldEnvironment>("WorldEnvironment");
            entitiesNode = GetTree().CurrentScene.GetNode<Node>("Entities");
            uiNode = GetTree().CurrentScene.GetNode<Node>("UI");
            fadeRect.node = uiNode.GetNode<ColorRect>("FadeRect");
            videoPlayer.node = uiNode.GetNode<VideoStreamPlayer>("VideoStreamPlayer");
            GD.Print("entities children: " + entitiesNode.GetChildCount());
            int typelessEntityCount = 0;
            for (int i = 0; i < entitiesNode.GetChildCount(); i++) {
                Node3D child = entitiesNode.GetChild<Node3D>(i);
                if (child.HasMeta("Type") == false) {
                    GD.PrintErr("Entity " + child.Name + " has no type metadata.");
                    typelessEntityCount++;
                    continue;
                }
                string entityType = (string)child.GetMeta("Type");
                GD.Print("entity: " + child.GetMeta("Type"));
                switch (entityType) {
                    case "Bear":
                        EnemyInitialize((CharacterBody3D)child);
                        break;
                    case "Player":
                        PlayerInitialize((CharacterBody3D)child);
                        break;
                    case "PlayerCamera":
                        PlayerCameraInitialize((Camera3D)child);
                        break;
                    case "DungeonExit":
                        InteractablesInitialize((Node3D)child, InteractableLookup.ExitDungeon);
                        break;
                    case "DungeonEntrance":
                        InteractablesInitialize((Node3D)child, InteractableLookup.EnterDungeon);
                        break;
                    case "SlinkSink":
                        InteractablesInitialize((Node3D)child, InteractableLookup.SlinkSinkDialogueStart);
                        break;
                    case "Pot":
                        InteractablesInitialize((Node3D)child, InteractableLookup.PotOpen);
                        break;
                    case "PrisonSpotlight":
                        prisonSpotlight = new PrisonSpotLight();
                        prisonSpotlight.node = child;
                        prisonSpotlight.speed = 0.5f;
                        break;
                    case "VideoTest":
                        InteractablesInitialize((Node3D)child, InteractableLookup.VideoTest);
                        break;
                    case "TestDialogue":
                        InteractablesInitialize((Node3D)child, InteractableLookup.TestDialogue);
                        break;
                    case "ChangeLevel":
                        InteractablesInitialize((Node3D)child, InteractableLookup.ChangeLevel);
                        if (!(bool)child.GetMeta("isVisible")) {
                            for (int j = 0; j < child.GetChildCount(); j++) {
                                child.GetChild(j).QueueFree();
                            }
                        }
                        break;
                    default:
                        GD.PrintErr("Unknown entity type: " + entityType + "\n");
                        break;
                }
            }
            if (typelessEntityCount > 0) {
                GD.PushWarning(
                    $"There were {typelessEntityCount} typeless entities in the scene.\n" +
                    "Entity types must be defined using the 'Type' metadata field on each local root node of the entity."
                );
            }
            TargetReticlesInitialize();
            DialogueBoxInitialize(uiNode.GetNode<Control>("DialogueBox"));
            SubtitlesInitialize(uiNode.GetNode<VBoxContainer>("SubtitleBox"));
            Audio3DInitialize(DEFAULT_AUDIO_POOL_SIZE);
            AudioUIInitialize(DEFAULT_AUDIO_POOL_SIZE);
            StartFade(-0.3f);
            sceneState.isSceneLoaded = true;
        }
        public override void _Ready() {
            GD.Print("Setting up game...");
            ProcessMode = ProcessModeEnum.Always;
            Engine.MaxFps = 999;
            targetReticleMaterial = new StandardMaterial3D {
                AlbedoColor = new Color(1, 0, 0),
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                NoDepthTest = true
            };
            globalPhysicsMaterial = new PhysicsMaterial {
                Friction = 0.2f,
                Bounce = 0f
            };
            GD.Print("Setup complete");
        }
        public override void _PhysicsProcess(double delta) {
            if (sceneState.isSceneLoaded == false) { InitializeScene(); }
            globalPhysicsDelta = delta;
            sceneState.timeSinceSceneLoad += (float)delta;
            sceneState.physicsFramesSinceSceneLoad++;
            inputDirection = Input.GetVector("moveLeft", "moveRight", "moveUp", "moveBack");
            UpdateVideo(ref videoPlayer);
            UpdateFadeInOut();
            //if (GetTree().CurrentScene.Name == "Level") { entitiesNode.GetParent().GetChild(0).GetChild<DirectionalLight3D>(1).RotationDegrees += new Vector3(0f, 20f, 0f); }
            ProjectilesUpdate();
            PlayerUpdate();
            PlayerCameraUpdate(ref playerCamera);
            EnemyUpdate();
            if (Input.IsActionJustPressed("interact")) { Interact(); }
            DialogueUpdate();
            SubtitlesUpdate();
            UpdateExplosions();
            TargetReticlesUpdate();
            if (prisonSpotlight.node != null) { PrisonSpotlightUpdate(ref prisonSpotlight); }
            inputState.interact.isConsumed = false;
            inputState.action1.isConsumed = false;
            inputState.action2.isConsumed = false;
            inputState.action3.isConsumed = false;
        }
        public override void _Process(double delta) {
            globalProcessDelta = delta;
            ProcessPendingSceneChange();
        }
    }
}
