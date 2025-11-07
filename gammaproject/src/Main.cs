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
        public static readonly Vector3 VECTOR3_DEFAULT_UPWARD_CAMERA_OFFSET = new Vector3(0, 1.246f, 0);
        public static readonly Transform3D DEFAULT_SLINK_CHEST_POSE = new Transform3D(new Basis(new Vector3(0.99487317f, 0.0031584306f, 0.10108134f), new Vector3(-0.0003058136f, 0.99960166f, -0.028224066f), new Vector3(-0.10113021f, 0.02804844f, 0.99447775f)), new Vector3(3.9579175E-09f, 1.3612776f, 0.02173036f));
        public AudioStream metalDinkSFX = GD.Load<AudioStream>("res://assets/sound/metalslam.wav");
        public AudioStream metalSlamSFX = GD.Load<AudioStream>("res://assets/sound/metalslam1.wav");
        public AudioStream footStepMetalSFX = GD.Load<AudioStream>("res://assets/sound/metal-footstep.wav");
        public AudioStream teleportSFX = GD.Load<AudioStream>("res://assets/sound/teleport.mp3");
        public Interactable[] interactables;
        public PendingSceneChange pendingSceneChange;
        public DialogueBox dialogueBox;
        public Player player;
        public PlayerCamera playerCamera;
        public PrisonSpotLight prisonSpotlight;
        public Node previousFrameScene;
        public Node entitiesNode;
        public PhysicsMaterial globalPhysicsMaterial;
        public Vector2 inputDirection;
        public double globalDelta;
        public float timeSinceSceneLoad;
        public int physicsFramesSinceSceneLoad;
        public string CurrentScenePath;
        public bool shouldSpawnMothman = false;
        public bool outOfTime = false;
        public bool isSceneLoaded;
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
            isSceneLoaded = false;
            GetTree().ChangeSceneToFile(pendingSceneChange.scenePath);
            pendingSceneChange.shouldChangeScene = false;
            pendingSceneChange.scenePath = "";
        }
        public void InitializeScene() {
            GD.Print("Initializing scene");
            timeSinceSceneLoad = 0;
            physicsFramesSinceSceneLoad = 0;
            player = new Player();
            playerCamera = new PlayerCamera();
            interactables = new Interactable[DEFAULT_INTERACTABLES_SIZE];
            prisonSpotlight = new PrisonSpotLight();
            entitiesNode = GetTree().CurrentScene.GetNode<Node>("Entities");
            GD.Print("entities children: " + entitiesNode.GetChildCount());
            for (int i = 0; i < entitiesNode.GetChildCount(); i++) {
                Node3D child = entitiesNode.GetChild<Node3D>(i);
                GD.Print("entity: " + child.Name);
                if (child.Name == "Player") {
                    PlayerInitialize((CharacterBody3D)child);
                    continue;
                }
                if (child.Name == "PlayerCamera") {
                    PlayerCameraInitialize((Camera3D)child);
                    continue;
                }
                if (child.Name == "DungeonExit") {
                    InteractablesAdd((Node3D)child, InteractableLookup.ExitDungeon);
                    continue;
                }
                if (child.Name == "DungeonEntrance") {
                    InteractablesAdd((Node3D)child, InteractableLookup.EnterDungeon);
                    continue;
                }
                if (child.Name == "SlinkSink") {
                    InteractablesAdd((Node3D)child, InteractableLookup.SlinkSinkDialogueStart);
                    continue;
                }
                if (child.Name == "PrisonSpotlight") {
                    prisonSpotlight = new PrisonSpotLight();
                    prisonSpotlight.node = child;
                    prisonSpotlight.speed = 0.5f;
                    continue;
                }
                GD.Print("unknown entity: " + child.Name);
            }
            DialogueBoxInitialize(GetTree().CurrentScene.GetNode<Node>("UI").GetNode<Control>("DialogueBox"));
            Audio3DInitialize(DEFAULT_AUDIO_POOL_SIZE);
        }
        public override void _Ready() {
            GD.Print("Ready");
            Engine.MaxFps = 240;
            globalPhysicsMaterial = new PhysicsMaterial();
            globalPhysicsMaterial.Friction = 0.2f;
            globalPhysicsMaterial.Bounce = 0f;
        }
        public override void _Process(double delta) {
            string currentPlayerAnimationName = player.animationState.GetCurrentNode();
            float currentFrame = (float)Math.Round(player.animationState.GetCurrentPlayPosition(), 2);
            switch (currentPlayerAnimationName) {
                case "Idle":
                    break;
                case "Walk":
                    switch (currentFrame) {
                        case 0.33f or 1.54f:
                            if (player.isOnGround) {
                                PlayAudio3D(footStepMetalSFX, player.node.GlobalPosition, 0.1f, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), true);
                            }
                            break;
                    }
                    break;
            }
        }
        public override void _PhysicsProcess(double delta) {
            globalDelta = delta;
            timeSinceSceneLoad += (float)delta;
            physicsFramesSinceSceneLoad++;
            if (!shouldSpawnMothman && timeSinceSceneLoad >= 2f) {
                shouldSpawnMothman = true;
                GD.Print("Mothman should spawn");
            }
            if (!outOfTime && timeSinceSceneLoad >= 600f) {
                outOfTime = true;
                GD.Print("Out of time");
            }
            inputDirection = Input.GetVector("moveLeft", "moveRight", "moveUp", "moveBack");
            if (GetTree().CurrentScene != previousFrameScene) {
                isSceneLoaded = false;
                previousFrameScene = GetTree().CurrentScene;
                InitializeScene();
            }
            if (Input.IsActionJustPressed("interact")) {
                Interact();
            }
            PlayerUpdate();
            PlayerCameraUpdate(ref playerCamera);
            DialogueUpdate();
            if (prisonSpotlight.node != null) { PrisonSpotlightUpdate(ref prisonSpotlight); }
            ProcessPendingSceneChange();
        }
    }
}
