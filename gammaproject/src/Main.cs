using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
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
        public AudioStream shootSFX = GD.Load<AudioStream>("res://assets/sound/rocket-launcher-shoot.wav");
        public Texture2D efxFire01 = GD.Load<Texture2D>("res://assets/textures/EFX_FIRE01.jpg");
        public PackedScene rocketScene = GD.Load<PackedScene>("res://scenes/entities/slink_rocket.tscn");
        public PackedScene targetReticleScene = GD.Load<PackedScene>("res://scenes/entities/target_reticle.tscn");
        public Interactable[] interactables;
        public Projectile[] projectiles;
        public Explosion[] explosions;
        public PendingSceneChange pendingSceneChange;
        public PrisonSpotLight prisonSpotlight;
        public WorldEnvironment worldEnvironment;
        public VideoPlayback videoPlayback;
        public Node entitiesNode;
        public Node uiNode;
        public PhysicsMaterial globalPhysicsMaterial;
        public float cameraFarSetting = 100;
        public double globalDelta;
        public bool shouldSpawnMothman = false;
        public bool outOfTime = false;
        public void RotateTowards(Vector3 lookDirection, Node3D inputNode, float rotationSpeed) {
            if (lookDirection.LengthSquared() <= ALMOST_ZERO) { return; }
            float targetRotation = (float)Math.Atan2(-lookDirection.X, -lookDirection.Z);
            inputNode.Rotation = new Vector3(inputNode.Rotation.X, Mathf.LerpAngle(inputNode.Rotation.Y, targetRotation, rotationSpeed), inputNode.Rotation.Z);
        }
        bool HasCrossedPlaybackPosition(float inputPreviousPosition, float inputCurrentPosition, float inputEventPosition) {
            if (inputCurrentPosition >= inputPreviousPosition) return inputPreviousPosition < inputEventPosition && inputEventPosition <= inputCurrentPosition;
            return inputPreviousPosition < inputEventPosition || inputEventPosition <= inputCurrentPosition;
        }
        public void ChangeScene(string scenePath) {
            pendingSceneChange.shouldChangeScene = true;
            pendingSceneChange.scenePath = scenePath;
        }
        public void ProcessPendingSceneChange() {
            if (!pendingSceneChange.shouldChangeScene) { return; }
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
            worldEnvironment = GetTree().CurrentScene.GetNode<WorldEnvironment>("Environment/WorldEnvironment");
            entitiesNode = GetTree().CurrentScene.GetNode<Node>("Entities");
            uiNode = GetTree().CurrentScene.GetNode<Node>("UI");
            videoPlayback.node = uiNode.GetNode<VideoStreamPlayer>("VideoStreamPlayer");
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
        }
        public override void _Ready() {
            ProcessMode = ProcessModeEnum.Always;
            GD.Print("Setting up game");
            Engine.MaxFps = 999;
            targetReticleMaterial = new StandardMaterial3D();
            targetReticleMaterial.AlbedoColor = new Color(1, 0, 0);
            targetReticleMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            targetReticleMaterial.NoDepthTest = true;
            globalPhysicsMaterial = new PhysicsMaterial();
            globalPhysicsMaterial.Friction = 0.2f;
            globalPhysicsMaterial.Bounce = 0f;
            GD.Print("Setup complete");
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
            UpdateVideo(ref videoPlayback);
            if (!shouldSpawnMothman && sceneState.timeSinceSceneLoad >= 300f) { shouldSpawnMothman = true; }
            if (!outOfTime && sceneState.timeSinceSceneLoad >= 600f) { outOfTime = true; }
            if (Input.IsActionJustPressed("debug")) {
                Video testVideoData = new Video {
                    videoPath = "res://assets/videos/testvideo.ogv",
                    onVideoStart = null,
                    onVideoComplete = null,
                };
                StartVideo(videoPlayback, testVideoData);
                string randomText = "";
                int textLength = GD.RandRange(5, 20);
                for (int i = 0; i < textLength; i++) {
                    randomText += (char)GD.RandRange(65, 90);
                }
                float randomLifetime = (float)GD.RandRange(2f, 10f);
                SubtitleData RandomSubtitle = new SubtitleData {
                    text = "Random subtitle " + randomText,
                    textColor = new Color(
                        (float)GD.RandRange(0f, 1f),
                        (float)GD.RandRange(0f, 1f),
                        (float)GD.RandRange(0f, 1f),
                        1f
                    ),
                    totalLifeTime = randomLifetime,
                    currentLifeTime = randomLifetime,
                };
                SubtitlesAdd(RandomSubtitle);
            }
            ProjectilesUpdate();
            PlayerCameraUpdate(ref playerCamera);
            PlayerUpdate();
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
            ProcessPendingSceneChange();
        }
    }
}
