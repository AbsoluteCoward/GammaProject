using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct RaycastWorldHitInfo {
            public Vector3 Position;
            public Vector3 Normal;
            public object Collider;
            public Godot.Collections.Dictionary RawResult;
        }
        public const int AUDIO_POOL_SIZE = 8;
        public const int ARRAY_GROWTH_FACTOR = 2;
        public const int DEFAULT_PLAYER_MAX_TARGET_COUNT = 4;
        public const int DEFAULT_MISCELLANEOUS_SIZE = 16;
        public const int DIALOGUE_PORTRAIT_SIZE = 192;
        public const int DEFAULT_INTERACTABLES_SIZE = 16;
        public const int DEFAULT_PROJECTILES_SIZE = 16;
        public const int DEFAULT_EXPLOSIONS_SIZE = 16;
        public const int DEFAULT_ENEMIES_SIZE = 16;
        public const int DEFAULT_TARGET_RETICLES_SIZE = 16;
        public const float DEFAULT_LOAD_DELAY = 4.0f;
        public const float DEFAULT_CAMERA_DISTANCE = 3.0f;
        public const float DEFAULT_CAMERA_HEIGHT = DEFAULT_CAMERA_DISTANCE * 0.4f;
        public const float ALMOST_ZERO = 0.00001f;
        public const float GRAVITY_STRENGTH = 9.81f;
        public const float MAX_PROJECTILE_DISTANCE = 1000f;
        public const float MAX_PROJECTILE_LIFETIME = 10f;
        public const float TARGETTING_ANGLE = 12f;
        public const float PLAYER_LEG_LENGTH = 1;
        public static readonly Color NULL_COLOR = new Color(0f, 0f, 0f, 0f);
        public static readonly Vector3 TELEPORT_VERTICAL_OFFSET = new Vector3(0, 0.1f, 0);
        public static readonly Vector3 DEFAULT_UPWARD_CAMERA_OFFSET = new Vector3(0, 1.246f, 0);
        public static readonly Vector3 Y_FLAT = new Vector3(1, 0, 1);
        public static readonly Vector3 GRAVITY_VECTOR = new Vector3(0, -GRAVITY_STRENGTH, 0);
        public static AudioStream metalDinkSFX = GD.Load<AudioStream>("res://assets/sound/metalslam.wav");
        public static AudioStream metalSlamSFX = GD.Load<AudioStream>("res://assets/sound/metalslam1.wav");
        public static AudioStream footStepMetalSFX = GD.Load<AudioStream>("res://assets/sound/metal-footstep.wav");
        public static AudioStream teleportSFX = GD.Load<AudioStream>("res://assets/sound/teleport.mp3");
        public static AudioStream shootSFX = GD.Load<AudioStream>("res://assets/sound/rocket-launcher-shoot.wav");
        public static AudioStream sloshSFX = GD.Load<AudioStream>("res://assets/sound/slosh.wav");
        public Texture2D efxFire01 = GD.Load<Texture2D>("res://assets/textures/EFX_FIRE01.jpg");
        public PackedScene rocketScene = GD.Load<PackedScene>("res://scenes/entities/slink_rocket.tscn");
        public PackedScene targetReticleScene = GD.Load<PackedScene>("res://scenes/entities/target_reticle.tscn");
        public PackedScene rewardObjectScene = GD.Load<PackedScene>("res://scenes/entities/reward.tscn");
        public SceneState sceneState;
        public PendingSceneChange pendingSceneChange;
        public FadeRect fadeRect;
        public LoadingScreen loadingScreen;
        public SubtitleBox subtitleBox;
        public DialogueBox dialogueBox;
        public VideoPlayer videoPlayer;
        public Player player;
        public PlayerCamera playerCamera;
        public Enemy[] enemies;
        public int enemyCount = 0;
        public Interactable[] interactables;
        public TargetReticle[] targetReticles;
        public Projectile[] projectiles;
        public Explosion[] explosions;
        public Trail[] trails;
        public Reward[] rewards;
        public int rewardsCount = 0;
        public Sound3D[] sounds3D;
        public int sounds3DCount = 0;
        public SoundUI[] soundsUI;
        public int soundsUICount = 0;
        public PrisonSpotLight prisonSpotlight;
        public WorldEnvironment worldEnvironment;
        public Node environmentNode;
        public Node entitiesNode;
        public Node uiNode;
        public Camera3D currentCamera;
        public PhysicsMaterial globalPhysicsMaterial;
        public World3D globalWorld3D;
        public float cameraFarSetting = 100;
        public float loadDelay = DEFAULT_LOAD_DELAY;
        public double globalPhysicsDelta;
        public double globalProcessDelta;
        public static bool RaycastWorld(World3D relativeWorld, CollisionObject3D exceptions, Vector3 start, Vector3 end, out RaycastWorldHitInfo hitInfo) {
            hitInfo = new RaycastWorldHitInfo();
            var rayResult = relativeWorld.DirectSpaceState.IntersectRay(
                PhysicsRayQueryParameters3D.Create(start, end, 1, new Godot.Collections.Array<Rid> { exceptions.GetRid() })
            );
            if (rayResult.Count <= 0) { return false; }
            hitInfo.RawResult = rayResult;
            hitInfo.Position = (Vector3)rayResult["position"];
            hitInfo.Normal = (Vector3)rayResult["normal"];
            hitInfo.Collider = rayResult["collider"];
            return true;
        }
        public void RotateTowards(Vector3 lookDirection, Node3D inputNode, float rotationSpeed) {
            if (lookDirection.LengthSquared() <= ALMOST_ZERO) { return; }
            float targetRotation = (float)Math.Atan2(-lookDirection.X, -lookDirection.Z);
            inputNode.Rotation = new Vector3(inputNode.Rotation.X, Mathf.LerpAngle(inputNode.Rotation.Y, targetRotation, rotationSpeed), inputNode.Rotation.Z);
        }
        public void InitializeScene() {
            GD.Print("Initializing scene");
            loadDelay = DEFAULT_LOAD_DELAY;
            enemyCount = 0;
            rewardsCount = 0;
            sounds3DCount = 0;
            soundsUICount = 0;
            player = new Player();
            playerCamera = new PlayerCamera();
            enemies = new Enemy[DEFAULT_ENEMIES_SIZE];
            interactables = new Interactable[DEFAULT_INTERACTABLES_SIZE];
            targetReticles = new TargetReticle[DEFAULT_TARGET_RETICLES_SIZE];
            projectiles = new Projectile[DEFAULT_PROJECTILES_SIZE];
            explosions = new Explosion[DEFAULT_EXPLOSIONS_SIZE];
            trails = new Trail[DEFAULT_MISCELLANEOUS_SIZE];
            environmentNode = GetTree().CurrentScene.GetNode<Node>("Environment");
            worldEnvironment = environmentNode.GetNode<WorldEnvironment>("WorldEnvironment");
            entitiesNode = GetTree().CurrentScene.GetNode<Node>("Entities");
            uiNode = GetTree().CurrentScene.GetNode<Node>("UI");
            fadeRect.node = uiNode.GetNode<ColorRect>("FadeRect");
            videoPlayer.node = uiNode.GetNode<VideoStreamPlayer>("VideoStreamPlayer");
            InitializeLoadingScreen(uiNode.GetNode<Control>("LoadingScreen"));
            globalWorld3D = entitiesNode.GetChild<Node3D>(0).GetWorld3D();
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
                    case "EnemyGeneric":
                        EnemyInitialize((CharacterBody3D)child);
                        break;
                    case "EnemyCrab01":
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
                    case "OilCandle":
                        child.GetChild<Node3D>(0).GetChild<AnimationPlayer>(2).Play("Fire");
                        InteractablesInitialize((Node3D)child, InteractableLookup.OilCandle);
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
                        string labelName = (string)child.GetMeta("LevelPath");
                        int lastSlashIndex = labelName.LastIndexOf('/');
                        labelName = labelName.Substring(lastSlashIndex + 1);
                        child.GetChild<Label3D>(0).Text = labelName;
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
            RewardsInitialize(DEFAULT_MISCELLANEOUS_SIZE);
            Audio3DInitialize(AUDIO_POOL_SIZE);
            AudioUIInitialize(AUDIO_POOL_SIZE);
            StartFade(-0.3f);
            sceneState.isSceneLoaded = true;
        }
        public override void _Ready() {
            GD.Print("Setting up game...");
            ProcessMode = ProcessModeEnum.Always;
            Engine.MaxFps = 999;
            globalPhysicsMaterial = new PhysicsMaterial {
                Friction = 0.2f,
                Bounce = 0f
            };
            GD.Print("Setup complete");
        }
        public override void _PhysicsProcess(double delta) {
            if (sceneState.isSceneLoaded == false) { 
                InitializeScene(); 
                return; 
            }
            globalPhysicsDelta = delta;
            sceneState.timeSinceSceneLoad += (float)delta;
            sceneState.physicsFramesSinceSceneLoad++;
            if (sceneState.physicsFramesSinceSceneLoad % LoadingScreen.speed == 0) {
                UpdateLoadingScreen();
            }
            if (loadDelay < 2f) {
                float t = Mathf.Clamp((loadDelay) / 2f, 0f, 1f);
                loadingScreen.node.Modulate = new Color(1f, 1f, 1f, Mathf.Pow(t, 1.5f));
            }
            if (loadDelay > 0f) {
                loadDelay -= (float)delta;
                return;
            }
            if (loadDelay <= 0f) {
                loadingScreen.node.Visible = false;
                loadingScreen.icon.Texture = null;
            }
            inputDirection = Input.GetVector("moveLeft", "moveRight", "moveUp", "moveBack");
            if (UpdateVideo(ref videoPlayer)) { return; }
            UpdateFadeInOut();
            //if (GetTree().CurrentScene.Name == "Level") { entitiesNode.GetParent().GetChild(0).GetChild<DirectionalLight3D>(1).RotationDegrees += new Vector3(0f, 20f, 0f); }
            ProjectilesUpdate();
            PlayerUpdate();
            PlayerCameraUpdate(ref playerCamera);
            OrbUpdate();
            EnemyUpdate();
            if (Input.IsActionJustPressed("interact")) { Interact(); }
            DialogueUpdate();
            RewardsUpdate();
            SubtitlesUpdate();
            UpdateExplosions();
            TargetReticlesUpdate();
            TrailUpdate(trails, 0.1f);
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
