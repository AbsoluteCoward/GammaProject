using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct TeleportEntity {
            public CharacterBody3D node;
            public RayCast3D topRayCast;
            public Node3D teleportShadowMesh;
        }
        public struct Player {
            public Vector3 wishDirection;
            public CharacterBody3D node;
            public Node3D gunBarrel;
            public AnimationPlayer animationPlayer;
            public AnimationTree animationTree;
            public AnimationNodeStateMachinePlayback animationState;
            public Skeleton3D skeleton;
            public TeleportOrb orb;
            public Node3D[] targets;
            public PlaybackPositionData[] animationPlaybackBlocks;
            public string previousAnimationName;
            public float moveSpeed;
            public float turnAnticipation;
            public float maxTeleportDistance;
            public int targetCount;
            public bool isOnGround;
            public bool shouldBeAsleep;
            public bool hasPlayerModelCopy;
            public bool isTeleporting;
            public static float maxDistance = float.MaxValue;
            public static int meatCount = 0;
            public static int chestBoneIndex;
            public static int headBoneIndex;
            public static int miscObjectBoneIndex;
        }
        public struct PlayerCamera {
            public Vector3 targetPosition;
            public Camera3D node;
            public RayCast3D WallRayCast;
            public SpotLight3D SpotLight;
            public float offsetDistance;
            public float offsetHeight;
            public float targetAngle;
            public float angle;
            public float maxLerpDistance;
            public float rotationLerpSpeed;
        }
        public void PlayerInitialize(CharacterBody3D inputPlayerNode) {
            player = new Player();
            player.wishDirection = Vector3.Zero;
            player.node = inputPlayerNode;
            player.gunBarrel = player.node.GetNode<MeshInstance3D>("Slink/Skeleton3D/GunBone/Trigger");
            player.animationPlayer = inputPlayerNode.GetChild<Node3D>(2).GetChild<AnimationPlayer>(1);
            player.animationTree = inputPlayerNode.GetChild<AnimationTree>(3);
            player.animationTree.Active = true;
            player.animationTree.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
            player.animationPlayer.Active = false;
            player.animationState = (AnimationNodeStateMachinePlayback)player.animationTree.Get("parameters/playback");
            player.skeleton = inputPlayerNode.GetChild<Node3D>(2).GetChild<Skeleton3D>(0);
            for (int i = 0; i < player.skeleton.GetBoneCount(); i++) {
                if (player.skeleton.GetBoneName(i) == "Abdomen") { Player.chestBoneIndex = i; }
                if (player.skeleton.GetBoneName(i) == "HeadBone") { Player.headBoneIndex = i; }
                if (player.skeleton.GetBoneName(i) == "MiscObject") { Player.miscObjectBoneIndex = i; }
            }
            if (Player.chestBoneIndex  == 0) { GD.PrintErr("Couldn't find chest bone!"); }
            if (Player.headBoneIndex == 0) { GD.PrintErr("Couldn't find head bone!"); }
            if (Player.miscObjectBoneIndex == 0) { GD.PrintErr("Couldn't find misc object bone!"); }
            OrbInitialize(inputPlayerNode.GetNode<Node3D>("TeleportOrb"));
            player.targets = new Node3D[16];
            player.animationPlaybackBlocks = new PlaybackPositionData[4];
            player.targetCount = 0;
            player.moveSpeed = 8.0f;
            player.turnAnticipation = 0f;
            player.animationPlaybackBlocks[0].previousPlaybackPosition = 0f;
            player.previousAnimationName = "";
            player.node.FloorSnapLength = 0.8f;
            player.hasPlayerModelCopy = false;
            player.isTeleporting = false;
            GD.Print("Player Initialized");
        }
        public void PlayerCameraInitialize(Camera3D inputCamera) {
            currentCamera = inputCamera;
            playerCamera.node = inputCamera;
            playerCamera.WallRayCast = inputCamera.GetChild<RayCast3D>(0);
            playerCamera.SpotLight = inputCamera.GetChild<SpotLight3D>(1);
            playerCamera.WallRayCast.TopLevel = true;
            playerCamera.WallRayCast.AddException(player.node);
            playerCamera.WallRayCast.CollisionMask = 2;
            playerCamera.SpotLight.SpotRange = 120;
            playerCamera.SpotLight.SpotAngle = 120;
            playerCamera.SpotLight.LightEnergy = GetTree().CurrentScene.Name == "Prison" ? 0f : 0f;
            playerCamera.offsetDistance = DEFAULT_CAMERA_DISTANCE;
            playerCamera.offsetHeight = DEFAULT_CAMERA_HEIGHT;
            playerCamera.node.Fov = 64;
            playerCamera.node.Far = cameraFarSetting;
            bool sceneHasFog = worldEnvironment.Environment.FogEnabled;
            if (sceneHasFog) {
                playerCamera.node.Far = worldEnvironment.Environment.FogDepthEnd > cameraFarSetting ?
                    worldEnvironment.Environment.FogDepthEnd :
                    cameraFarSetting;
            } else {
                worldEnvironment.Environment.FogEnabled = true;
                worldEnvironment.Environment.FogLightColor = Colors.Black;
                worldEnvironment.Environment.FogMode = Godot.Environment.FogModeEnum.Depth;
                worldEnvironment.Environment.FogDepthBegin = cameraFarSetting * 0.8f;
                worldEnvironment.Environment.FogDepthEnd = cameraFarSetting;
            }
            GD.Print("fog begin" + worldEnvironment.Environment.FogDepthBegin);
            GD.Print("fog end" + worldEnvironment.Environment.FogDepthEnd);
            playerCamera.maxLerpDistance = 200f;
            playerCamera.rotationLerpSpeed = 0.1f;
            GD.Print("Player Camera Initialized");
        }
        public void PlayerApplyDynamicBoneTransformations(float inputChestTwist, float inputChestRoll, float inputHeadTwist) {
            if (player.skeleton == null) { return; }
            float chestRotation = player.turnAnticipation * 0.6f;
            Transform3D chestPose = player.skeleton.GetBoneGlobalPose(Player.chestBoneIndex);
            Vector3 chestYawAxis = player.skeleton.GetBoneGlobalRest(Player.chestBoneIndex).Basis.Y;
            Vector3 chestRollAxis = player.skeleton.GetBoneGlobalRest(Player.chestBoneIndex).Basis.Z;
            Quaternion chestTwist = new Quaternion(chestYawAxis, chestRotation * inputChestTwist);
            Quaternion chestRoll = new Quaternion(chestRollAxis, chestRotation * inputChestRoll);
            chestPose.Basis = chestPose.Basis * new Basis(chestTwist * chestRoll);
            player.skeleton.SetBoneGlobalPose(Player.chestBoneIndex, chestPose);
            Vector3 headRotationAxis = player.skeleton.GetBoneGlobalRest(Player.headBoneIndex).Basis.Y.Normalized();
            Quaternion headTwist = new Quaternion(headRotationAxis, chestRotation * inputHeadTwist);
            Transform3D headPose = player.skeleton.GetBoneGlobalPose(Player.headBoneIndex);
            headPose.Basis = headPose.Basis * new Basis(headTwist);
            player.skeleton.SetBoneGlobalPose(Player.headBoneIndex, headPose);
        }
        public void PlayerTeleportTo(Vector3 inputPosition, PlayerCamera inputCamera) {
            StartSound3D(teleportSFX, inputPosition, 0.01f, 1.0f, false);
            player.node.GlobalPosition = inputPosition;
            PlayerCameraUpdate(ref inputCamera);
        }
        public void PlayerUpdate() {
            player.animationTree.Advance((float)globalPhysicsDelta);
            Transform3D whatever = player.orb.node.Visible ?
                player.skeleton.GetBoneGlobalPose(Player.miscObjectBoneIndex) : 
                player.skeleton.GetBoneGlobalPose(Player.chestBoneIndex);
            Vector3 global_whatever = player.skeleton.ToGlobal(whatever.Origin);
            player.orb.node.GlobalPosition = player.orb.node.TopLevel ? player.orb.node.GlobalPosition : global_whatever;
            if (sceneState.physicsFramesSinceSceneLoad % (int)GD.RandRange(20f, 100f) == 0) {
                Vector3 playerPosition = player.node.GlobalPosition;
                if (Mathf.Abs(playerPosition.X) > Player.maxDistance ||
                    Mathf.Abs(playerPosition.Y) > Player.maxDistance ||
                    Mathf.Abs(playerPosition.Z) > Player.maxDistance) {
                    player.node.GlobalPosition = Vector3.Zero;
                }
            }
            player.isOnGround = player.node.IsOnFloor();
            Vector3 playerForward = -player.node.Transform.Basis.Z.Normalized();
            player.wishDirection =
                (currentCamera.GlobalTransform.Basis.Z.Normalized() * inputDirection.Y + currentCamera.GlobalTransform.Basis.X.Normalized() * inputDirection.X) *
                Y_FLAT;
            Vector3 airControlDirection = player.wishDirection.Length() > 0.1f ? player.wishDirection : player.node.Velocity.Normalized();
            Vector3 rootPosition = player.animationTree.GetRootMotionPosition();
            Vector3 rootVelocity = (player.node.Transform.Basis * rootPosition) / (float)globalPhysicsDelta;
            string targetAnimation = player.animationState.GetCurrentNode();
            bool isAnimationSameAsPrevious = player.animationState.GetCurrentNode() == player.previousAnimationName;
            bool action3JustPressed = Input.IsActionJustPressed("action3") && !inputState.action3.isConsumed;
            bool action3Pressed = Input.IsActionPressed("action3");
            bool action3JustReleased = Input.IsActionJustReleased("action3");
            bool hasMovementInput = player.wishDirection.Length() > 0.1f;
            if (player.animationState.GetCurrentNode() == "") { return; }
            AnimationNodeStateMachine stateMachine = (AnimationNodeStateMachine)player.animationTree.TreeRoot;
            bool isCurrentAnimationNodeABlendTree = ((AnimationNodeStateMachine)player.animationTree.TreeRoot).GetNode(player.animationState.GetCurrentNode()).GetType() == typeof(AnimationNodeBlendTree);
            if (!isCurrentAnimationNodeABlendTree) {
                player.animationPlaybackBlocks[0].currentPlaybackPosition = player.animationState.GetCurrentPlayPosition();
            } else {
                AnimationNodeBlendTree currentBlendTree = (AnimationNodeBlendTree)stateMachine.GetNode(player.animationState.GetCurrentNode());
                Godot.Collections.Array<Godot.StringName> childNodes = currentBlendTree.GetNodeList();
                int blockIndex = 0;
                for (int i = 0; i < childNodes.Count; i++) {
                    AnimationNode childNode = currentBlendTree.GetNode(childNodes[i]);
                    if (childNode.GetType() != typeof(AnimationNodeAnimation)) { continue; }
                    string path = "parameters/" + player.animationState.GetCurrentNode() + "/" + childNodes[i] + "/current_position";
                    player.animationPlaybackBlocks[blockIndex].currentPlaybackPosition = (float)player.animationTree.Get(path);
                    player.animationPlaybackBlocks[blockIndex].parameterName = childNodes[i];
                    blockIndex++;
                }
            }
            bool isIdleAndStill = (float)player.animationTree.Get("parameters/Walk/WalkBlend/blend_amount") <= ALMOST_ZERO && !hasMovementInput;
            switch (player.animationState.GetCurrentNode()) {
                case "Walk":
                    if (!isAnimationSameAsPrevious) {
                        player.animationTree.Set("parameters/Walk/TeleportStartupBlend/blend_amount", 0.0f);
                        player.animationTree.Set("parameters/Walk/WalkBlend/blend_amount", 0.0f);
                    }
                    bool shouldWalkBlend = hasMovementInput || player.isTeleporting;
                    float walkBlendAmount = Mathf.MoveToward((float)player.animationTree.Get("parameters/Walk/WalkBlend/blend_amount"), shouldWalkBlend ? 1f : 0f, 0.1f);
                    player.animationTree.Set("parameters/Walk/WalkBlend/blend_amount", walkBlendAmount);
                    bool shouldTeleportBlend = Input.IsActionPressed("action2");
                    float teleportBlendAmount = Mathf.MoveToward((float)player.animationTree.Get("parameters/Walk/TeleportStartupBlend/blend_amount"), shouldTeleportBlend ? 1f : 0f, 0.1f);
                    player.animationTree.Set("parameters/Walk/TeleportStartupBlend/blend_amount", teleportBlendAmount);
                    if ((float)player.animationTree.Get("parameters/Walk/TeleportStartupBlend/blend_amount") <= ALMOST_ZERO && (float)player.animationTree.Get("parameters/Walk/TeleportStartup/current_position") > 0.0f) {
                        player.animationTree.Set("parameters/Walk/TeleportStartupSeek/seek_request", 0.0f);
                    }
                    if ((float)player.animationTree.Get("parameters/Walk/TeleportStartupBlend/blend_amount") <= ALMOST_ZERO && !player.orb.node.TopLevel) {
                        player.orb.node.Visible = false;
                    } else {
                        player.orb.node.Visible = true;
                    }
                    bool shouldShootFromWalk = Input.IsActionJustReleased("action2") && (float)player.animationTree.Get("parameters/Walk/TeleportStartup/current_position") > 0.8f;
                    if (shouldShootFromWalk) { 
                        targetAnimation = "TeleportShoot";
                    }
                    bool shouldFallFromWalk = !player.isOnGround;
                    if (shouldFallFromWalk) { 
                        targetAnimation = "Fall"; 
                    }
                    bool shouldJumpFromWalk = action3JustPressed && isIdleAndStill;
                    if (shouldJumpFromWalk) {
                        targetAnimation = "Jump";
                        player.animationTree.Set("parameters/Jump/JumpSeek/seek_request", 0.0f);
                        inputState.action3.isConsumed = true;
                    }
                    bool shouldRunFromWalk = action3Pressed && hasMovementInput;
                    if (shouldRunFromWalk) {
                        targetAnimation = "Run";
                    }
                    int walkBlockIndex = GetPlaybackBlockIndex("Walk");
                    if (
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[walkBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[walkBlockIndex].currentPlaybackPosition, 0.33f)
                    ) {
                        if (player.isOnGround) {
                            StartSound3D(footStepMetalSFX, player.node.GlobalPosition, 0.1f * walkBlendAmount, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), true);
                            GD.Print("Right footstep sound");
                        }
                    }
                    if (
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[walkBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[walkBlockIndex].currentPlaybackPosition, 1.54f)
                    ) {
                        if (player.isOnGround) {
                            StartSound3D(footStepMetalSFX, player.node.GlobalPosition, 0.1f * walkBlendAmount, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), true);
                            GD.Print("Left footstep sound");
                        }
                    }
                    Vector3 direction = player.isTeleporting || !hasMovementInput ? playerForward : player.wishDirection;
                    float targetAngle = Mathf.Atan2(playerForward.Cross(direction).Y, playerForward.Dot(direction));
                    targetAngle = Mathf.Clamp(targetAngle, -0.8f, 0.8f);
                    player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, targetAngle, 0.2f);
                    RotateTowards(direction, player.node, 0.2f);
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    player.node.Velocity += Vector3.Down * 9.8f * (float)globalPhysicsDelta;
                    PlayerApplyDynamicBoneTransformations(1, 0.15f, 0.5f);
                    break;
                case "Run":
                    if (!action3Pressed || !hasMovementInput || player.node.Velocity.Length() < 0.2f) {
                        targetAnimation = "Walk";
                    }
                    if (
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[0].previousPlaybackPosition, player.animationPlaybackBlocks[0].currentPlaybackPosition, 0.14f) ||
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[0].previousPlaybackPosition, player.animationPlaybackBlocks[0].currentPlaybackPosition, 0.68f)
                    ) {
                        if (player.isOnGround) {
                            StartSound3D(footStepMetalSFX, player.node.GlobalPosition, 0.1f, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), true);
                        } else {
                            targetAnimation = "Fall";
                        }
                    }
                    float targetAngleRun = Mathf.Atan2(playerForward.Cross(player.wishDirection).Y, playerForward.Dot(player.wishDirection));
                    targetAngle = Mathf.Clamp(targetAngleRun, -0.8f, 0.8f);
                    player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, targetAngle, 0.2f);
                    RotateTowards(player.wishDirection, player.node, 0.2f);
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    player.node.Velocity += Vector3.Down * 1.8f * (float)globalPhysicsDelta;
                    PlayerApplyDynamicBoneTransformations(1.5f, 0.8f, 0.5f);
                    break;
                case "Jump":
                    if (!isAnimationSameAsPrevious) {
                        if (player.previousAnimationName == "TeleportShoot") {
                            player.animationTree.Set("parameters/Jump/JumpSeek/seek_request", 0.4f);
                        } else {
                            player.animationTree.Set("parameters/Jump/JumpSeek/seek_request", 0.0f);
                        }
                        break;
                    }
                    bool shouldJump =
                        HasCrossedPlaybackPosition(
                            inputPreviousPosition: player.animationPlaybackBlocks[0].previousPlaybackPosition,
                            inputCurrentPosition: player.animationPlaybackBlocks[0].currentPlaybackPosition,
                            inputEventPosition: 0.5f
                        ) &&
                        player.isOnGround &&
                        isAnimationSameAsPrevious;
                    if (shouldJump) {
                        player.node.Velocity += Vector3.Up * 12f;
                        player.node.Velocity += player.wishDirection.Length() > 0.1f ? player.wishDirection * 2f : player.node.Transform.Basis.Z.Normalized() * 2f;
                    }
                    if (
                        player.animationPlaybackBlocks[0].currentPlaybackPosition > 0.67f && 
                        player.node.GetLastSlideCollision() != null
                    ) {
                        player.animationState.Start("Fall", true);
                        targetAnimation = "Fall";
                    }
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Jump/Jump/current_length") && isAnimationSameAsPrevious) {
                        targetAnimation = "Fall";
                    }
                    player.node.Velocity += Vector3.Down * 9.8f * (float)globalPhysicsDelta;
                    if (!player.isOnGround) {
                        player.node.Velocity = new Vector3(
                            Mathf.Lerp(player.node.Velocity.X, airControlDirection.X * player.moveSpeed, 0.01f),
                            player.node.Velocity.Y,
                            Mathf.Lerp(player.node.Velocity.Z, airControlDirection.Z * player.moveSpeed, 0.01f)
                        );
                    }
                    break;
                case "Fall":
                    if (player.isOnGround) { player.animationState.Start("FallToIdle", true); }
                    player.node.Velocity += Vector3.Down * 9.8f * (float)globalPhysicsDelta;
                    player.node.Velocity = new Vector3(
                        Mathf.Lerp(player.node.Velocity.X, airControlDirection.X * player.moveSpeed, 0.02f),
                        player.node.Velocity.Y,
                        Mathf.Lerp(player.node.Velocity.Z, airControlDirection.Z * player.moveSpeed, 0.02f)
                    );
                    break;
                case "FallToIdle":
                    if (!player.isOnGround) { targetAnimation = "Fall"; }
                    if (
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[0].previousPlaybackPosition, player.animationPlaybackBlocks[0].currentPlaybackPosition, 0.2f) &&
                        isAnimationSameAsPrevious
                    ) {
                        if (player.isOnGround) {
                            GD.Print("Landing sound at positions " + player.animationPlaybackBlocks[0].previousPlaybackPosition + " and " + player.animationPlaybackBlocks[0].currentPlaybackPosition);
                            StartSound3D(footStepMetalSFX, player.node.GlobalPosition, 0.4f, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), true);
                        }
                    }
                    player.node.Velocity = player.node.Velocity.Lerp(Vector3.Zero, 0.08f);
                    player.node.Velocity += Vector3.Down * 9.8f * (float)globalPhysicsDelta;
                    break;
                case "TeleportShoot":
                    if (!isAnimationSameAsPrevious) {
                        RotateTowards(-player.wishDirection, player.node, 1f);
                        break;
                    }
                    if (!player.isOnGround) {
                        player.node.Velocity = Vector3.Up * 5f;
                        player.animationState.Start("Fall", true);
                        targetAnimation = "Fall";
                        OrbReturn(true);
                    }
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition > 0.8f && action3JustPressed) {
                        targetAnimation = "Jump";
                        player.node.Velocity = Vector3.Up * 12f;
                        player.node.GlobalPosition += Vector3.Up * 0.5f;
                        OrbReturn(true);
                    }
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition >= player.animationPlayer.GetAnimation(player.animationState.GetCurrentNode()).Length) {
                        targetAnimation = "OrbIdle";
                    }
                    if (HasCrossedPlaybackPosition(player.animationPlaybackBlocks[0].previousPlaybackPosition, player.animationPlaybackBlocks[0].currentPlaybackPosition, 1f)) {
                        if (player.wishDirection != Vector3.Zero) {
                            RotateTowards(player.wishDirection, player.node, 1f);
                        }
                        GD.Print("Emitting orb on position " + player.animationPlaybackBlocks[0].currentPlaybackPosition);
                        OrbShoot();
                    }
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    if (targetAnimation != "TeleportShoot" && targetAnimation != "OrbIdle") {
                        OrbReturn(false);
                    } else {
                        player.orb.node.Visible = true;
                    }
                    break;
                    case "OrbIdle":
                        if (!isAnimationSameAsPrevious) {
                            break;
                        }
                        if (!player.orb.node.TopLevel) {
                            targetAnimation = "Walk";
                        }
                    break;
            }
            if (!isCurrentAnimationNodeABlendTree) {
                player.animationPlaybackBlocks[0].previousPlaybackPosition = player.animationPlaybackBlocks[0].currentPlaybackPosition;
            } else {
                for (int i = 0; i < player.animationPlaybackBlocks.Length; i++) {
                    player.animationPlaybackBlocks[i].previousPlaybackPosition = player.animationPlaybackBlocks[i].currentPlaybackPosition;
                }
            }
            player.previousAnimationName = player.animationState.GetCurrentNode();
            bool shouldChangeAnimation = player.animationState.GetCurrentNode() != targetAnimation;
            bool isInTransition = player.animationState.GetTravelPath().Count > 0;
            if (shouldChangeAnimation && !isInTransition) {
                GD.Print("changing animation to " + targetAnimation + " from " + player.animationState.GetCurrentNode());
                for (int i = 0; i < player.animationPlaybackBlocks.Length; i++) {
                    player.animationPlaybackBlocks[i].currentPlaybackPosition = 0f;
                }
                switch (player.animationState.GetCurrentNode()) {
                    case "Walk":
                        GD.Print("Resetting walk blend amounts");
                        if (!player.orb.node.TopLevel) {
                            player.orb.node.Visible = false;
                        }
                        break;
                }
                player.animationState.Travel(targetAnimation);
            }
            if (Input.IsActionPressed("attack")) {
                if (Input.IsActionJustPressed("attack")) {
                    for (int i = 0; i < player.targets.Length; i++) { player.targets[i] = null; }
                    player.targetCount = 0;
                }
                for (int i = 0; i < enemyCount; i++) {
                    Node3D potentialTarget = enemies[i].node;
                    bool isTargetInvalid = potentialTarget == player.node || potentialTarget.GetType() == typeof(AudioStreamPlayer3D);
                    if (isTargetInvalid) { continue; }
                    Vector3 toTarget = potentialTarget.GlobalPosition - player.gunBarrel.GlobalPosition;
                    Vector3 toTargetFlat = (toTarget * Y_FLAT).Normalized();
                    float dotProduct = playerForward.Dot(toTargetFlat);
                    float angleToTarget = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f));
                    float angleInDegrees = Mathf.RadToDeg(angleToTarget);
                    if (angleInDegrees <= TARGETTING_ANGLE) {
                        bool alreadyTargeted = false;
                        for (int j = 0; j < player.targetCount; j++) {
                            if (player.targets[j] == potentialTarget) {
                                alreadyTargeted = true;
                                break;
                            }
                        }
                        if (!alreadyTargeted && player.targetCount < player.targets.Length) {
                            player.targets[player.targetCount] = potentialTarget;
                            targetReticles[player.targetCount].node.Visible = true;
                            player.targetCount++;
                        }
                    }
                }
            } else if (Input.IsActionJustReleased("attack")) {
                Vector3 gunPosition = player.gunBarrel.GlobalPosition;
                StartSound3D(shootSFX, gunPosition, 0.1f, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), false);
                for (int i = 0; i < player.targets.Length; i++) {
                    if (player.targets[i] == null) { continue; }
                    GD.Print("Firing at target " + player.targets[i].Name);
                    ProjectilesCreate(
                        inputStartPosition: gunPosition,
                        inputTarget: player.targets[i],
                        inputDirection: -player.node.GlobalTransform.Basis.Z,
                        inputSpeed: 15f
                    );
                }
                for (int i = 0; i < player.targets.Length; i++) { player.targets[i] = null; }
                player.targetCount = 0;
            }
            player.node.MoveAndSlide();
        }
        public void PlayerCameraUpdate(ref PlayerCamera inputCamera) {
            if (inputCamera.node == null) { return; }
            currentCamera = inputCamera.node;
            if (Input.IsActionJustPressed("cameraRight")) {
                inputCamera.targetAngle -= 90f;
            } else if (Input.IsActionJustPressed("cameraLeft")) {
                inputCamera.targetAngle += 90f;
            }
            float targetHeight = DEFAULT_CAMERA_HEIGHT + (player.isOnGround ? 0f : 3f);
            inputCamera.offsetHeight = Mathf.Lerp(inputCamera.offsetHeight, targetHeight, 0.05f);
            Vector3 medianPosition = inputCamera.WallRayCast.GlobalPosition.Lerp(player.orb.node.GlobalPosition, 0.1f);
            inputCamera.targetAngle = Mathf.PosMod(inputCamera.targetAngle, 360f);
            float angleDifference = Mathf.PosMod(inputCamera.targetAngle - inputCamera.angle + 180f, 360f) - 180f;
            inputCamera.angle += angleDifference * inputCamera.rotationLerpSpeed;
            float cameraAngleRadians = Mathf.DegToRad(inputCamera.angle);
            Vector3 offsetDirection = new Vector3(Mathf.Sin(cameraAngleRadians), 0, Mathf.Cos(cameraAngleRadians));
            inputCamera.WallRayCast.TargetPosition = inputCamera.WallRayCast.ToLocal(
                medianPosition +
                (offsetDirection * inputCamera.offsetDistance) +
                new Vector3(0, inputCamera.offsetHeight, 0)
            );
            inputCamera.WallRayCast.GlobalPosition =
                inputCamera.WallRayCast.GlobalPosition.Lerp(player.orb.node.GlobalPosition, 0.3f);
                //(player.node.GlobalPosition + player.skeleton.GetBoneGlobalPose(Player.chestBoneIndex).Origin).Lerp(player.orb.node.GlobalPosition, 0.5f);
                //player.node.GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild<MeshInstance3D>(0).GlobalPosition +
            inputCamera.node.GlobalPosition = inputCamera.WallRayCast.IsColliding() ?
                inputCamera.WallRayCast.GetCollisionPoint() + inputCamera.WallRayCast.GetCollisionNormal() * 0.1f :
                inputCamera.WallRayCast.ToGlobal(inputCamera.WallRayCast.TargetPosition);
            inputCamera.targetPosition = medianPosition;
            inputCamera.node.LookAt(inputCamera.targetPosition);
        }
    }
}