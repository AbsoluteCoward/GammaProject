using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct Player {
            public TeleportOrb orb;
            public CharacterBody3D node;
            public Node3D model;
            public Node3D gunBarrel;
            public AnimationPlayer animationPlayer;
            public AnimationTree animationTree;
            public AnimationNodeStateMachinePlayback animationState;
            public Skeleton3D skeleton;
            public Node3D[] targets;
            public PlaybackPositionData[] animationPlaybackBlocks;
            public RayCast3D groundRay;
            public RayCast3D ledgeRay;
            public Vector3 wishDirection;
            public Vector3 targetPosition;
            public Vector3 targetCorrection;
            public float moveSpeed;
            public float turnAnticipation;
            public float maxTeleportDistance;
            public int targetCount;
            public int maxTargetCount;
            public int currentTargetIndex;
            public bool isOnGround;
            public static float maxDistance = 1000f;
            public static int meatCount = 0;
            public static int chestBoneIndex;
            public static int headBoneIndex;
            public static int miscObjectBoneIndex;
        }
        public struct PlayerCamera {
            public Vector3 targetPosition;
            public Camera3D node;
            public RayCast3D WallRayCast;
            public float offsetDistance;
            public float offsetHeight;
            public float targetAngle;
            public float angle;
            public float maxLerpDistance;
            public float rotationLerpSpeed;
        }
        public struct ClimbData {
            public float climbHeight;
            public float climbLateralDistance;
            public float footOnGoundTimeStamp;
        }
        public void PlayerInitialize(CharacterBody3D inputPlayerNode) {
            player.node = inputPlayerNode;
            player.model = player.node.GetNode<Node3D>("Slink");
            player.gunBarrel = player.node.GetNode<Node3D>("Slink/Skeleton3D/GunBone/GunBarrel");
            player.animationPlayer = inputPlayerNode.GetNode<AnimationPlayer>("Slink/AnimationPlayer");
            player.animationTree = inputPlayerNode.GetNode<AnimationTree>("AnimationTree");
            player.animationTree.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
            player.animationState = (AnimationNodeStateMachinePlayback)player.animationTree.Get("parameters/playback");
            player.ledgeRay = player.node.GetNode<RayCast3D>("LedgeRay");
            player.groundRay = player.node.GetNode<RayCast3D>("GroundRay");
            player.skeleton = inputPlayerNode.GetNode<Skeleton3D>("Slink/Skeleton3D");
            for (int i = 0; i < player.skeleton.GetBoneCount(); i++) {
                if (player.skeleton.GetBoneName(i) == "Abdomen") { Player.chestBoneIndex = i; }
                if (player.skeleton.GetBoneName(i) == "HeadBone") { Player.headBoneIndex = i; }
                if (player.skeleton.GetBoneName(i) == "MiscObject") { Player.miscObjectBoneIndex = i; }
            }
            if (Player.chestBoneIndex == 0) { GD.PrintErr("Couldn't find chest bone!"); }
            if (Player.headBoneIndex == 0) { GD.PrintErr("Couldn't find head bone!"); }
            if (Player.miscObjectBoneIndex == 0) { GD.PrintErr("Couldn't find misc object bone!"); }
            OrbInitialize(inputPlayerNode.GetNode<Node3D>("TeleportOrb"));
            player.targets = new Node3D[DEFAULT_PLAYER_MAX_TARGET_COUNT];
            player.animationPlaybackBlocks = new PlaybackPositionData[DEFAULT_MISCELLANEOUS_SIZE];
            player.wishDirection = Vector3.Zero;
            player.targetCount = 0;
            player.maxTargetCount = DEFAULT_PLAYER_MAX_TARGET_COUNT;
            player.moveSpeed = 8.0f;
            player.turnAnticipation = 0f;
            for (int i = 0; i < player.animationPlaybackBlocks.Length; i++) {
                player.animationPlaybackBlocks[i].previousPlaybackPosition = 0f;
                player.animationPlaybackBlocks[i].currentPlaybackPosition = 0f;
            }
            AnimationNodeOneShot fireShotNode = (AnimationNodeOneShot)((AnimationNodeBlendTree)((AnimationNodeStateMachine)player.animationTree.TreeRoot).GetNode("Walk")).GetNode("FireWalkOneShot");
            SetOneShotFilters(true, "shoulder.R", player.skeleton, player.node, fireShotNode); // Godot forces us to do this for some reason or else it will literally forget the filters we set for the oneshot in the editor

            GD.Print("Player Initialized");
        }
        public void PlayerCameraInitialize(Camera3D inputCamera) {
            currentCamera = inputCamera;
            playerCamera.node = inputCamera;
            playerCamera.WallRayCast = inputCamera.GetChild<RayCast3D>(0);
            playerCamera.WallRayCast.TopLevel = true;
            playerCamera.WallRayCast.AddException(player.node);
            playerCamera.WallRayCast.CollisionMask = 2;
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
            GD.Print("Fog begin" + worldEnvironment.Environment.FogDepthBegin);
            GD.Print("Fog end" + worldEnvironment.Environment.FogDepthEnd);
            playerCamera.maxLerpDistance = 200f;
            playerCamera.rotationLerpSpeed = 0.1f;
            GD.Print("Player Camera Initialized");
        }
        public void Shoot(int inputRocketCount) {
            bool didEverHaveTarget = player.currentTargetIndex < player.targetCount;
            int previousTarget = player.currentTargetIndex;
            for (int i = 0; i < inputRocketCount; i++) {
                while (player.currentTargetIndex < player.targetCount && player.targets[player.currentTargetIndex] == null) { 
                    player.currentTargetIndex++; 
                }
                bool hasTarget = player.currentTargetIndex < player.targetCount;
                PlaySoundUI(shootSFX, 0.1f, (float)GD.Randfn(1.0, 0.05f), false);
                Vector3 shootDirection = -player.node.GlobalTransform.Basis.Z;
                if (!hasTarget && i != 0) {
                    Vector3 right = player.node.GlobalTransform.Basis.X.Normalized();
                    float offset = (float)GD.Randfn(0, 0.4f);
                    shootDirection = (shootDirection + right * offset).Normalized();
                }
                if (!didEverHaveTarget) {
                    Vector3 up = player.node.GlobalTransform.Basis.Y.Normalized();
                    shootDirection = (shootDirection + up * 0.2f).Normalized();
                }
                ProjectilesCreate(
                    inputStartPosition: player.gunBarrel.GlobalPosition,
                    inputTarget: didEverHaveTarget ? player.targets[player.currentTargetIndex] : null,
                    inputDirection: shootDirection,
                    inputSpeed: 20f
                );
                if (!hasTarget) {
                    player.currentTargetIndex = 0;
                    player.targetCount = 0;
                    continue; 
                }
                player.targets[player.currentTargetIndex] = null;
                player.currentTargetIndex++;
                if (player.currentTargetIndex >= player.targetCount) {
                    for (int j = 0; j < player.targets.Length; j++) { player.targets[j] = null; }
                    player.targetCount = 0;
                    player.currentTargetIndex = 0;
                } 
            }
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
        public bool PlayerClimb(ref string inputTargetAnimation, ref bool inputShouldMoveAndSlide) {
            RaycastWorldHitInfo findLedge = new RaycastWorldHitInfo();
            Vector3 collisionPosition = 
                (player.node.GetSlideCollision(0).GetPosition() * Y_FLAT - 
                player.node.GetSlideCollision(0).GetNormal() * 0.2f * Y_FLAT)
                + player.node.GlobalPosition * XZ_FLAT;
            Vector3 rayStart = collisionPosition + Vector3.Up * 3f;
            Vector3 rayEnd = collisionPosition + Vector3.Up * 0.7f;
            if (RaycastWorld(globalWorld3D, player.node, rayStart, rayEnd, out findLedge)) {
                player.targetPosition = new Vector3(
                    findLedge.Position.X,
                    findLedge.Position.Y,
                    findLedge.Position.Z
                );
                RotateTowards(-player.node.GetSlideCollision(0).GetNormal(), player.node, 0.6f);
                float distanceToTarget = (player.targetPosition - player.node.GlobalPosition).Length();
                GD.Print("Distance to target: " + distanceToTarget);
                switch (distanceToTarget) {
                    case > 2f:
                        inputTargetAnimation = "Climb02";
                        break;
                    case < 2f:
                        inputTargetAnimation = "Climb01";
                        break;
                }
                inputShouldMoveAndSlide = false;
                return true;
            }
            return false;
        }
        public void PlayerTeleportTo(Vector3 inputPosition, PlayerCamera inputCamera) {
            player.node.GlobalPosition = inputPosition;
            player.node.Velocity = Vector3.Zero;
            PlayerCameraUpdate(ref inputCamera);
        }
        public void PlayerSetCollision(bool inputEnabled) {
            player.node.CollisionLayer = (uint)(inputEnabled ? 1 : 0);
            player.node.CollisionMask = (uint)(inputEnabled ? 1 : 0);
        }
        public void PlayerUpdate() {
            string previousAnimationName = player.animationState.GetCurrentNode();
            player.animationTree.Advance(globalPhysicsDeltaFloat);
            Vector3 playerForward = -player.node.Transform.Basis.Z.Normalized();
            player.wishDirection = 
                (
                    currentCamera.GlobalTransform.Basis.Z.Normalized() * 
                    inputDirection.Y + 
                    currentCamera.GlobalTransform.Basis.X.Normalized() * 
                    inputDirection.X
                ) *
                Y_FLAT;
            Vector3 airControlDirection = player.wishDirection.Length() > 0.1f ? player.wishDirection : player.node.Velocity.Normalized();
            bool action3Pressed = Input.IsActionPressed("action3");
            bool action3JustReleased = Input.IsActionJustReleased("action3");
            bool hasMovementInput = player.wishDirection.Length() > 0.1f;
            bool shouldMoveAndSlide = true;
            Transform3D orbTarget = player.orb.node.Visible ?
                player.skeleton.GetBoneGlobalPose(Player.miscObjectBoneIndex) : 
                player.skeleton.GetBoneGlobalPose(Player.chestBoneIndex);
            Vector3 global_whatever = player.skeleton.ToGlobal(orbTarget.Origin);
            player.orb.node.GlobalPosition = player.orb.node.TopLevel ? player.orb.node.GlobalPosition : global_whatever;
            float distanceToGround = player.groundRay.IsColliding() ? 
                player.groundRay.GlobalPosition.Y - player.groundRay.GetCollisionPoint().Y  : 
                float.MaxValue;
            bool shouldSnapToGround = distanceToGround <= PLAYER_LEG_LENGTH * 1.5f;
            bool isRayCollidingAtPlayerFeet = distanceToGround <= PLAYER_LEG_LENGTH;
            player.isOnGround = (player.node.IsOnFloor() || shouldSnapToGround) && player.node.Velocity.Y <= 0f;
            Vector3 TargetPosition = new Vector3(player.node.GlobalPosition.X, player.groundRay.GetCollisionPoint().Y, player.node.GlobalPosition.Z);
            if (player.isOnGround && player.node.CollisionMask > 0) {
                player.node.GlobalPosition = player.node.GlobalPosition.Lerp(TargetPosition, 0.1f + (player.node.Velocity.Length() * globalPhysicsDeltaFloat));
            }
            Vector3 rootPosition = player.animationTree.GetRootMotionPosition();
            Vector3 rootVelocity = (player.node.Transform.Basis * rootPosition) / globalPhysicsDeltaFloat;
            string targetAnimation = player.animationState.GetCurrentNode();
            bool isAnimationSameAsPrevious = player.animationState.GetCurrentNode() == previousAnimationName;
            bool isInTransition = player.animationState.GetTravelPath().Count > 0;
            if (player.animationState.GetCurrentNode() == "") { return; }
            AnimationNodeStateMachine stateMachine = (AnimationNodeStateMachine)player.animationTree.TreeRoot;
            bool isCurrentAnimationNodeABlendTree = stateMachine.GetNode(player.animationState.GetCurrentNode()).GetType() == typeof(AnimationNodeBlendTree);
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
            switch (player.animationState.GetCurrentNode()) {
                case "Walk":
                    if (!isAnimationSameAsPrevious) {
                        player.animationTree.Set("parameters/Walk/WalkBlend/blend_amount", 0.0f);
                        player.animationTree.Set("parameters/Walk/TeleportStartupBlend/blend_amount", 0.0f);
                        player.animationTree.Set("parameters/Walk/GunBlend/blend_amount", 0.0f);
                        break;
                    }
                    bool shouldWalkBlend = hasMovementInput;
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
                    bool shouldFireLeviathan = Input.IsActionJustReleased("action2") && (float)player.animationTree.Get("parameters/Walk/TeleportStartup/current_position") > 0.8f;
                    if (shouldFireLeviathan) {
                        targetAnimation = "TeleportShoot";
                    }
                    bool isShooting = 
                        (bool)player.animationTree.Get("parameters/Walk/FireWalkOneShot/active") &&
                        (float)player.animationTree.Get("parameters/Walk/FireWalk/current_position") < 0.36f;
                    bool shouldGunBlend = !isShooting && (Input.IsActionPressed("attack") || player.targetCount > 0);
                    float gunBlendAmount = Mathf.MoveToward((float)player.animationTree.Get("parameters/Walk/GunBlend/blend_amount"), shouldGunBlend ? 1f : 0f, 0.1f);
                    player.animationTree.Set("parameters/Walk/GunBlend/blend_amount", gunBlendAmount);
                    if (Input.IsActionJustPressed("attack") && player.targetCount > 0) {
                        for (int i = 0; i < player.targets.Length; i++) { player.targets[i] = null; }
                        player.targetCount = 0;
                        player.currentTargetIndex = 0;
                    }
                    if (!isShooting && (Input.IsActionJustReleased("attack") || (player.targetCount > 0 && !Input.IsActionPressed("attack")))) {
                        GD.Print("Firing");
                        GD.Print("Target count: " + player.targetCount);
                        player.animationTree.Set("parameters/Walk/FireWalkOneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
                        gunBlendAmount = 0f;
                    }
                    if (gunBlendAmount == 1) {
                        for (int i = 0; i < enemyCount; i++) {
                            Node3D potentialTarget = enemies[i].node;
                            bool isTargetInvalid = potentialTarget == player.node || potentialTarget.GetType() == typeof(AudioStreamPlayer3D);
                            if (isTargetInvalid) { continue; }
                            Vector3 toTarget = potentialTarget.GlobalPosition - player.gunBarrel.GlobalPosition;
                            Vector3 toTargetFlat = (toTarget * Y_FLAT).Normalized();
                            float dotProduct = playerForward.Dot(toTargetFlat);
                            float angleToTarget = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f));
                            float angleInDegrees = Mathf.RadToDeg(angleToTarget);
                            if (angleInDegrees > TARGETTING_ANGLE) { continue; }
                            bool alreadyTargeted = false;
                            for (int j = 0; j < player.targetCount; j++) {
                                if (player.targets[j] == potentialTarget) {
                                    alreadyTargeted = true;
                                    break;
                                }
                            }
                            RaycastWorldHitInfo potentialTargetHitInfo;
                            bool hitSomething = RaycastWorld(globalWorld3D, player.node, player.node.GlobalPosition + Vector3.Up, potentialTarget.GlobalPosition + Vector3.Up, out potentialTargetHitInfo);
                            GD.Print("Collider: " + potentialTargetHitInfo.Collider + "\nPotential target: " + potentialTarget);
                            if (hitSomething && potentialTargetHitInfo.Collider != potentialTarget) { continue; }
                            if (!alreadyTargeted && player.targetCount < player.targets.Length) {
                                player.targets[player.targetCount] = potentialTarget;
                                targetReticles[player.targetCount].node.Visible = true;
                                player.targetCount++;
                            }
                        }
                    }
                    bool shouldFallFromWalk = !player.isOnGround;
                    if (shouldFallFromWalk) { 
                        targetAnimation = "Fall";
                        player.node.Velocity = Vector3.Down * 2 + playerForward;
                        break;
                    }
                    bool shouldRunFromWalk = action3Pressed && hasMovementInput;
                    if (shouldRunFromWalk) {
                        targetAnimation = "Run";
                    }
                    if (IsInputJustPressed(ref inputState.action3)) {
                        if (hasMovementInput) {
                            if (player.animationState.GetFadingFromNode() == "Run") {
                                GD.Print("Run to Walk transition jump");
                                RotateTowards(player.wishDirection, player.node, 0.1f);
                                player.node.Velocity = Vector3.Up + (player.wishDirection * 8);
                                player.animationState.Start("RunJump01", true);
                                targetAnimation = "RunJump01";
                            }
                        } else if (player.targetCount > 0 || IsInputPressedEx(ref inputState.attack, true, true)) {
                            targetAnimation = "Fire01";
                        } else {
                            targetAnimation = "Jump";
                            player.animationTree.Set("parameters/Jump/JumpSeek/seek_request", 0.0f);
                        }
                    }
                    if (player.node.IsOnWall() && walkBlendAmount > 0.9f) {
                        if (PlayerClimb(ref targetAnimation, ref shouldMoveAndSlide)) { break; }
                    }
                    int walkBlockIndex = GetPlaybackBlockIndex("Walk");
                    if (
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[walkBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[walkBlockIndex].currentPlaybackPosition, 0.33f) ||
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[walkBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[walkBlockIndex].currentPlaybackPosition, 1.54f)
                    ) {
                        if (player.isOnGround) {
                            PlaySoundUI(footStepMetalSFX, 0.1f * walkBlendAmount, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), true);
                        }
                    }
                    int fireWalkBlockIndex = GetPlaybackBlockIndex("FireWalk");
                    if (HasCrossedPlaybackPosition(player.animationPlaybackBlocks[fireWalkBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[fireWalkBlockIndex].currentPlaybackPosition, 0.1f) && (float)player.animationTree.Get("parameters/Walk/FireWalk/current_position") > 0.0f) {
                        Shoot(1);
                    }
                    Vector3 direction = !hasMovementInput ? playerForward : player.wishDirection;
                    float targetAngle = Mathf.Atan2(playerForward.Cross(direction).Y, playerForward.Dot(direction));
                    targetAngle = Mathf.Clamp(targetAngle, -0.8f, 0.8f);
                    player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, targetAngle, 0.2f);
                    RotateTowards(direction, player.node, 0.2f);
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    PlayerApplyDynamicBoneTransformations(1, 0.15f, 0.5f);
                    break;
                case "Run":
                    if (!isAnimationSameAsPrevious) { break; }
                    if (!IsInputPressed(ref inputState.action3) || !hasMovementInput) {
                        targetAnimation = "Walk";
                    }
                    if (!player.isOnGround) {
                        targetAnimation = "RunJump01";
                    }
                    if (player.node.IsOnWall()) {
                        if (PlayerClimb(ref targetAnimation, ref shouldMoveAndSlide)) { break; }
                    }
                    if (
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[0].previousPlaybackPosition, player.animationPlaybackBlocks[0].currentPlaybackPosition, 0.14f) ||
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[0].previousPlaybackPosition, player.animationPlaybackBlocks[0].currentPlaybackPosition, 0.68f)
                    ) {
                        PlaySoundUI(footStepMetalSFX, 0.2f, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), true);
                    }
                    float targetAngleRun = Mathf.Atan2(playerForward.Cross(player.wishDirection).Y, playerForward.Dot(player.wishDirection));
                    targetAngle = Mathf.Clamp(targetAngleRun, -0.8f, 0.8f);
                    player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, targetAngle, 0.2f);
                    RotateTowards(player.wishDirection, player.node, 0.2f);
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    PlayerApplyDynamicBoneTransformations(1.5f, 0.8f, 0.5f);
                    break;
                case "Fire01":
                    if (!isAnimationSameAsPrevious) { break; }
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Fire01/current_length")) {
                        targetAnimation = "Walk";
                    }
                    if (HasCrossedPlaybackPosition(player.animationPlaybackBlocks[0].previousPlaybackPosition, player.animationPlaybackBlocks[0].currentPlaybackPosition, 0.66f)) {
                        Shoot(3);
                    }
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    break;
                case "Climb02":
                case "Climb01":
                    float distanceUp = 0f;
                    float distanceForward = 0f;
                    float landingTimeStamp = 0f;
                    switch (player.animationState.GetCurrentNode()) {
                        case "Climb02":
                            distanceUp = 2f;
                            distanceForward = 0.3f;
                            landingTimeStamp = 0.6f;
                            break;
                        case "Climb01":
                            distanceUp = 1f;
                            distanceForward = 0.3f;
                            landingTimeStamp = 0.55f;
                            break;
                    }
                    if (distanceUp == 0f || distanceForward == 0f || landingTimeStamp == 0f) { GD.PrintErr("Leap case: How"); }
                    float animationLength = (float)player.animationTree.Get("parameters/" + player.animationState.GetCurrentNode() + "/current_length");
                    if (!isAnimationSameAsPrevious) {
                        PlayerSetCollision(false);
                        player.targetCorrection = player.targetPosition - player.node.GlobalPosition;
                        player.targetCorrection.Y -= distanceUp;
                        player.targetCorrection = new Vector3(
                            player.targetCorrection.Normalized().X * distanceForward,
                            player.targetCorrection.Y,
                            player.targetCorrection.Normalized().Z * distanceForward
                        );
                        break;
                    }
                    float maxDistanceToGround = 1.5f;
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition < landingTimeStamp && player.targetCorrection.LengthSquared() > ALMOST_ZERO) {
                        Vector3 previous = player.targetCorrection;
                        player.targetCorrection = player.targetCorrection.Lerp(Vector3.Zero, 4f * globalPhysicsDeltaFloat);
                        player.node.GlobalPosition += previous - player.targetCorrection;
                        if (player.targetCorrection.LengthSquared() < 0.001f) { player.targetCorrection = Vector3.Zero; }
                        RotateTowards(player.wishDirection, player.node, 0.1f);
                        player.node.Velocity = new Vector3(rootVelocity.X, rootVelocity.Y, rootVelocity.Z);
                        break;
                    }
                    if (distanceToGround > maxDistanceToGround) {
                        if (IsInputPressed(ref inputState.action3)) {
                            GD.Print("Jump from Leap");
                            player.node.Velocity = Vector3.Up + (playerForward * 8);
                            player.animationState.Start("RunJump01", true);
                            targetAnimation = "RunJump01";
                        } else {
                            targetAnimation = "Fall";
                            player.node.Velocity += Vector3.Down;
                        }
                        PlayerSetCollision(true);
                    }
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition >= animationLength) {
                        if (distanceToGround < maxDistanceToGround) {
                            GD.Print("Snap to ground from Leap");
                            player.node.GlobalPosition = player.groundRay.GetCollisionPoint();
                            targetAnimation = action3Pressed ? "Run" : "Walk";
                            player.node.Velocity = Vector3.Zero;
                        } else if (IsInputPressed(ref inputState.action3)) {
                                GD.Print("Jump from Climb01");
                                player.node.Velocity = Vector3.Up + (playerForward * 8);
                                player.animationState.Start("RunJump01", true);
                                targetAnimation = "RunJump01";
                        } else {
                            targetAnimation = "Fall";
                        }
                        PlayerSetCollision(true);
                        break;
                    }
                    break;
                case "Jump":
                    if (!isAnimationSameAsPrevious) {
                        if (previousAnimationName == "TeleportShoot") {
                            GD.Print("Jump from TeleportShoot");
                            player.animationTree.Set("parameters/Jump/JumpSeek/seek_request", 0.4f);
                        } else {
                            player.animationTree.Set("parameters/Jump/JumpSeek/seek_request", 0.0f);
                        }
                        break;
                    }
                    const float JUMP_PLAYBACK_POSITION = 0.5f;
                    bool shouldJump =
                        HasCrossedPlaybackPosition(
                            inputPreviousPosition: player.animationPlaybackBlocks[0].previousPlaybackPosition,
                            inputCurrentPosition: player.animationPlaybackBlocks[0].currentPlaybackPosition,
                            inputEventPosition: JUMP_PLAYBACK_POSITION
                        );
                    if (shouldJump) {
                        player.node.Velocity += Vector3.Up * 12f;
                        player.node.Velocity += player.wishDirection.Length() > 0.1f ? player.wishDirection * 2f : player.node.Transform.Basis.Z.Normalized() * 2f;
                    }
                    if (
                        player.animationPlaybackBlocks[0].currentPlaybackPosition >= JUMP_PLAYBACK_POSITION &&
                        player.node.IsOnWall()
                    ) {
                        targetAnimation = "WallGrab";
                        RotateTowards(-player.node.GetSlideCollision(0).GetNormal(), player.node, 1);
                    }
                    if (
                        player.animationPlaybackBlocks[0].currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Jump/Jump/current_length") ||
                        player.node.IsOnCeiling()
                    ) {
                        targetAnimation = "Fall";
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    if (!player.isOnGround) {
                        player.node.Velocity = new Vector3(
                            Mathf.Lerp(player.node.Velocity.X, airControlDirection.X * player.moveSpeed, 0.01f),
                            player.node.Velocity.Y,
                            Mathf.Lerp(player.node.Velocity.Z, airControlDirection.Z * player.moveSpeed, 0.01f)
                        );
                    }
                    break;
                case "WallGrab":
                    if (!isAnimationSameAsPrevious) { 
                        PlaySoundUI(GD.Load<AudioStream>("res://assets/sound/grab01.wav"), 0.05f, (float)GD.Randfn(1f, 0.01f), false);
                        break; 
                    }
                    if (
                        player.animationPlaybackBlocks[0].currentPlaybackPosition > 0.266f &&
                        hasMovementInput && 
                        IsInputPressed(ref inputState.action3)
                    ) {
                        RotateTowards(player.wishDirection, player.node, 1f);
                        targetAnimation = "RunJump02";
                        player.node.Velocity = player.wishDirection * 12f + Vector3.Up * 4f;
                        break;
                    }
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition >= (float)player.animationTree.Get("parameters/WallGrab/current_length")) {
                        targetAnimation = "Fall";
                    }
                    player.node.Velocity = Vector3.Zero;
                    break;
                case "RunJump01":
                    if (!isAnimationSameAsPrevious) {
                        player.node.Velocity += Vector3.Up * 5f;
                        break;
                    }
                    RotateTowards(player.node.Velocity, player.node, 0.2f);
                    if (player.node.IsOnWall() && distanceToGround > 2f) {
                        targetAnimation = "WallGrab";
                        RotateTowards(-player.node.GetSlideCollision(0).GetNormal(), player.node, 1);
                    }
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition >= (float)player.animationTree.Get("parameters/RunJump01/current_length") && isAnimationSameAsPrevious) {
                         targetAnimation = "Fall";
                    }
                    if (player.isOnGround) {
                        string animationToPlay = playerForward.Dot(player.wishDirection) > -0.8f ? "Roll" : "FallToIdle";
                        player.animationState.Start(animationToPlay, true);
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    break;
                case "RunJump02":
                    if (!isAnimationSameAsPrevious) {
                        player.node.Velocity += Vector3.Up * 4f;
                        break;
                    }
                    RotateTowards(player.node.Velocity, player.node, 0.2f);
                    if (player.isOnGround) { 
                        player.animationState.Start("Roll", true);
                        GD.Print("forcing animation from RunJump02 to Roll");
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    break;
                case "Roll":
                    if (!isAnimationSameAsPrevious) {
                        PlaySoundUI(rollSFX, 0.05f, 1f, true);
                        break;
                    }
                    if (!player.isOnGround) { player.animationState.Start("Fall", true); }
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition < 0.5f) {
                        RotateTowards(player.wishDirection, player.node, 0.1f);
                        player.node.Velocity = playerForward * 8f;
                    }
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Roll/current_length") - 0.4f) {
                        if (inputDirection != Vector2.Zero && action3Pressed) { 
                            targetAnimation = "Run";
                        }
                    }
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Roll/current_length") && isAnimationSameAsPrevious) {
                        if (inputDirection != Vector2.Zero && action3Pressed) { 
                            targetAnimation = "Run";
                        } else {
                            targetAnimation = "Walk";
                        }
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    break;
                case "Fall":
                    if (player.isOnGround) {
                        if ((player.node.Velocity * Y_FLAT).Length() > 6f) {
                            player.animationState.Start("Roll", true);
                        } else {
                            player.animationState.Start("FallToIdle", true);
                            float impactSpeed = -player.node.Velocity.Y;
                            float animationScale = Mathf.Clamp(impactSpeed / 12.0f, 0.5f, 2.0f);
                            float animationSpeed = Mathf.Clamp(2.0f - (impactSpeed / 10.0f), 0.5f, 2f);
                            GD.Print("impactSpeed: " + impactSpeed + " animationScale: " + animationScale + " animationSpeed: " + animationSpeed);
                            player.animationTree.Set("parameters/FallToIdle/FallToIdleTimeSeek/seek_request", 0.0f);
                            player.animationTree.Set("parameters/FallToIdle/FallToIdleBlend/blend_amount", animationScale);
                            player.animationTree.Set("parameters/FallToIdle/FallToIdleTimeScale/scale", animationSpeed);
                        }
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    player.node.Velocity = new Vector3(
                        Mathf.Lerp(player.node.Velocity.X, airControlDirection.X * player.moveSpeed, 0.02f),
                        player.node.Velocity.Y,
                        Mathf.Lerp(player.node.Velocity.Z, airControlDirection.Z * player.moveSpeed, 0.02f)
                    );
                    break;
                case "FallToIdle":
                    if (!isAnimationSameAsPrevious) {
                        break;
                    }
                    int FallToIdleBlockIndex = GetPlaybackBlockIndex("FallToIdle");
                    if (HasCrossedPlaybackPosition(player.animationPlaybackBlocks[FallToIdleBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[FallToIdleBlockIndex].currentPlaybackPosition, 0.2f)) {
                        if (player.isOnGround) { PlaySoundUI(footStepMetalSFX, 0.4f, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), true); }
                    }
                    if (player.animationPlaybackBlocks[FallToIdleBlockIndex].currentPlaybackPosition >= (float)player.animationTree.Get("parameters/FallToIdle/FallToIdle/current_length")) {
                        targetAnimation = "Walk";
                    }
                    player.node.Velocity = player.node.Velocity.Lerp(Vector3.Zero, 0.08f);
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    if (!player.isOnGround) { 
                        player.node.GlobalPosition -= playerForward;
                        player.node.Velocity = Vector3.Zero;
                    }
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
                    if (player.animationPlaybackBlocks[0].currentPlaybackPosition > 0.8f && IsInputJustPressed(ref inputState.action3)) {
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
                            targetAnimation = player.isOnGround ? "Walk" : "Fall";
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
            bool shouldChangeAnimation = player.animationState.GetCurrentNode() != targetAnimation;
            if (shouldChangeAnimation && !isInTransition) {
                GD.Print("changing animation to " + targetAnimation + " from " + player.animationState.GetCurrentNode());
                for (int i = 0; i < player.animationPlaybackBlocks.Length; i++) {
                    player.animationPlaybackBlocks[i].currentPlaybackPosition = 0f;
                }
                switch (player.animationState.GetCurrentNode()) {
                    case "Walk":
                        if (!player.orb.node.TopLevel) { player.orb.node.Visible = false; }
                        break;
                }
                player.animationState.Travel(targetAnimation);
            }
            if (player.isOnGround && player.node.Velocity.Y < 0f) {
                player.node.Velocity = new Vector3(player.node.Velocity.X, 0f, player.node.Velocity.Z);
            }
            if (shouldMoveAndSlide) { 
                player.node.MoveAndSlide(); 
            } else { 
                player.node.Velocity = Vector3.Zero; 
            }
            if (sceneState.physicsFramesSinceSceneLoad % (int)GD.RandRange(20f, 100f) == 0) {
                Vector3 playerPosition = player.node.GlobalPosition;
                if (Mathf.Abs(playerPosition.X) > Player.maxDistance ||
                    Mathf.Abs(playerPosition.Y) > Player.maxDistance ||
                    Mathf.Abs(playerPosition.Z) > Player.maxDistance) {
                    player.node.GlobalPosition = Vector3.Zero;
                }
            }
        }
        public void PlayerCameraUpdate(ref PlayerCamera inputCamera) {
            if (inputCamera.node == null) { return; }
            currentCamera = inputCamera.node;
            if (Input.IsActionJustPressed("cameraRight")) {
                inputCamera.targetAngle -= 90f;
            } else if (Input.IsActionJustPressed("cameraLeft")) {
                inputCamera.targetAngle += 90f;
            }
            float targetHeight = 
                DEFAULT_CAMERA_HEIGHT + 
                (player.groundRay.IsColliding() ? 
                    Mathf.Max(0f, player.node.GlobalPosition.Y - player.groundRay.GetCollisionPoint().Y) * 0.5f : 
                    8f
                );
            inputCamera.offsetHeight = Mathf.Lerp(inputCamera.offsetHeight, targetHeight, 0.05f);
            Vector3 medianPosition = inputCamera.WallRayCast.GlobalPosition.Lerp(player.orb.node.GlobalPosition, 0.1f);
            inputCamera.targetAngle = Mathf.PosMod(inputCamera.targetAngle, 360f);
            float angleDifference = Mathf.PosMod(inputCamera.targetAngle - inputCamera.angle + 180f, 360f) - 180f;
            inputCamera.angle += angleDifference * inputCamera.rotationLerpSpeed;
            float cameraAngleRadians = Mathf.DegToRad(inputCamera.angle);
            Vector3 offsetDirection = new Vector3(Mathf.Sin(cameraAngleRadians), 0, Mathf.Cos(cameraAngleRadians));
            float breathe = Mathf.Sin(sceneState.timeSinceSceneLoad * 0.8f) * 0.04f;
            float speed = (player.node.Velocity * Y_FLAT).LengthSquared();
            inputCamera.WallRayCast.TargetPosition = inputCamera.WallRayCast.ToLocal(
                medianPosition +
                (offsetDirection * (inputCamera.offsetDistance)) +
                new Vector3(0, inputCamera.offsetHeight + breathe, 0)
            );
            inputCamera.WallRayCast.GlobalPosition =
                inputCamera.WallRayCast.GlobalPosition.Lerp(player.orb.node.GlobalPosition, 0.3f);
            inputCamera.node.GlobalPosition = inputCamera.WallRayCast.IsColliding() ?
                inputCamera.WallRayCast.GetCollisionPoint() + inputCamera.WallRayCast.GetCollisionNormal() * 0.1f :
                inputCamera.WallRayCast.ToGlobal(inputCamera.WallRayCast.TargetPosition);
            inputCamera.targetPosition = medianPosition;
            inputCamera.node.LookAt(inputCamera.targetPosition);
            inputCamera.node.Fov = Mathf.Lerp(inputCamera.node.Fov, 50 + Mathf.Min(player.node.Velocity.LengthSquared() * 0.2f, 30f), 0.02f);
        }
    }
}