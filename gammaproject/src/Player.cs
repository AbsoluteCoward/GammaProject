using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct Player {
            public TeleportOrb orb;
            public CharacterBody3D node;
            public MeshInstance3D rocketIndicator;
            public Area3D detectionArea;
            public Node3D model;
            public Node3D gunBarrel;
            public AnimationPlayer animationPlayer;
            public AnimationTree animationTree;
            public AnimationNodeStateMachinePlayback animationState;
            public Skeleton3D skeleton;
            public Node3D[] targets;
            public PlaybackPositionData[] animationPlaybackBlocks;
            public RayCast3D groundRay;
            public ShapeCast3D ledgeShapeCast;
            public Vector3 wishDirection;
            public Vector3 targetPosition;
            public Vector3 targetCorrection;
            public string currentAnimationName;
            public float turnAnticipation;
            public int targetCount;
            public int maxTargetCount;
            public int currentTargetIndex;
            public bool isOnGround;
            public static float maxDistance = 1000f;
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
        public void PlayerInitialize(CharacterBody3D inputPlayerNode) {
            player.node = inputPlayerNode;
            player.model = player.node.GetNode<Node3D>("Slink");
            player.rocketIndicator = player.node.GetNode<MeshInstance3D>("RocketIndicator");
            player.rocketIndicator.TopLevel = true;
            player.detectionArea = player.node.GetNode<Area3D>("DetectionArea");
            player.gunBarrel = player.node.GetNode<Node3D>("Slink/Skeleton3D/GunBone/GunBarrel");
            player.animationPlayer = inputPlayerNode.GetNode<AnimationPlayer>("Slink/AnimationPlayer");
            player.animationTree = inputPlayerNode.GetNode<AnimationTree>("AnimationTree");
            player.animationTree.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
            player.animationState = (AnimationNodeStateMachinePlayback)player.animationTree.Get("parameters/playback");
            player.ledgeShapeCast = player.node.GetNode<ShapeCast3D>("LedgeShapeCast");
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
            player.animationPlaybackBlocks = new PlaybackPositionData[DEFAULT_MISCELLANEOUS_SIZE/2];
            player.wishDirection = Vector3.Zero;
            player.targetCount = 0;
            player.maxTargetCount = DEFAULT_PLAYER_MAX_TARGET_COUNT;
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
            playerCamera.node = inputCamera;
            playerCamera.WallRayCast = inputCamera.GetChild<RayCast3D>(0);
            playerCamera.WallRayCast.TopLevel = true;
            playerCamera.WallRayCast.AddException(player.node);
            playerCamera.WallRayCast.CollisionMask = 2;
            playerCamera.offsetDistance = DEFAULT_CAMERA_DISTANCE;
            playerCamera.offsetHeight = DEFAULT_CAMERA_HEIGHT;
            playerCamera.node.Fov = 64;
            playerCamera.node.Far = cameraFarSetting;
            if (currentCamera == null) {
                currentCamera = inputCamera;
                inputCamera.Current = true;
            }
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
            GD.Print("Shooting " + inputRocketCount + " rockets");
            bool didEverHaveTarget = player.currentTargetIndex < player.targetCount;
            int previousTarget = player.currentTargetIndex;
            for (int i = 0; i < inputRocketCount; i++) {
                while (player.currentTargetIndex < player.targetCount && player.targets[player.currentTargetIndex] == null) { 
                    player.currentTargetIndex++; 
                }
                bool hasTarget = player.currentTargetIndex < player.targetCount;
                PlaySoundUI(shootSFX, 0.1f, globalSlightPitchVaration, false);
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
                    inputSpeed: PLAYER_ROCKET_SPEED
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
        public bool PlayerCanLeap(ref Vector3 outHitPosition, float maxDistance) {
            if (!player.ledgeShapeCast.IsColliding()) { return false; }
            Vector3 collisionPosition =
                player.ledgeShapeCast.GetCollisionPoint(0) * Y_FLAT -
                player.ledgeShapeCast.GetCollisionNormal(0) * 0.15f * Y_FLAT +
                player.node.GlobalPosition * XZ_FLAT;
            Vector3 rayStart = collisionPosition + Vector3.Up * 3.1f;
            Vector3 rayEnd = collisionPosition + Vector3.Up * 0.7f;
            //DebugSpawnLine(rayStart, rayEnd, 0.1f, entitiesNode);
            switch (maxDistance) {
                case < 0:
                    GD.PrintErr("PlayerCanLeap: max distance too low");
                    break;
                case > 0:
                    if ((collisionPosition - player.node.GlobalPosition).Length() > maxDistance) {
                        GD.Print(maxDistance);
                        GD.Print("PlayerCanLeap: recorded distance: " + (collisionPosition - player.node.GlobalPosition).Length());
                        return false; 
                    }
                    break;
            }
            RaycastWorldHitInfo findLedge = new RaycastWorldHitInfo();
            if (RaycastWorld(globalWorld3D, player.node, rayStart, rayEnd, out findLedge)) {
                outHitPosition = findLedge.Position;
                return true;
            }
            return false;
        }
        public string PlayerMatchLeapAnimation(Vector3 inputLeapToPosition, string currentAnimation) {
            RotateTowards(-player.ledgeShapeCast.GetCollisionNormal(0), player.node, 1f);
            player.targetPosition = inputLeapToPosition;
            float heightDistanceToTarget = inputLeapToPosition.Y - player.node.GlobalPosition.Y;
            float lateralDistanceToTarget = (inputLeapToPosition * XZ_FLAT - player.node.GlobalPosition* XZ_FLAT).Length();
            string targetAnimation = currentAnimation;
            switch (heightDistanceToTarget) {
                    case < 1.5f:
                        targetAnimation = "Climb01";
                        break;
                    case < 2.5f:
                        targetAnimation = "Climb02";
                        break;
                    case < 3.2f:
                        targetAnimation = "Climb03";
                        break;
                    default:
                        GD.PrintErr("Climb distance too far");
                        return null;
            }
            if (targetAnimation == currentAnimation) {
                targetAnimation = "Climb00";
            }
            return targetAnimation;
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
        public void PlayerRocketIndicatorUpdate(bool inputShouldShow, Vector3 inputDirection) {
            player.rocketIndicator.Visible = inputShouldShow;
            Vector3 velocity = inputDirection * PLAYER_ROCKET_SPEED;
            Vector3 position = player.gunBarrel.GlobalPosition;
            Vector3 end = player.gunBarrel.GlobalPosition;
            float time = globalPhysicsDeltaFloat * 12f;
            bool curveHit = false;
            for (int i = 0; i < 24; i++) {
                Vector3 next = position + velocity * time;
                velocity += GRAVITY_VECTOR * time;
                RaycastWorldHitInfo hit;
                if (RaycastWorld(globalWorld3D, player.node, position, next, out hit)) {
                    end = hit.Position;
                    curveHit = true;
                    break;
                }
                end = next;
                position = next;
            }
            if (!curveHit) {
                RaycastWorldHitInfo hit;
                if (RaycastWorld(globalWorld3D, player.node, position, end + Vector3.Down * 500, out hit)) {
                    end = hit.Position;
                    curveHit = true;
                } else {
                    player.rocketIndicator.Visible = false;
                }
            }
            player.rocketIndicator.GlobalPosition = end;
        }
        public void PlayerUpdate() {
            AnimationNodeStateMachine stateMachine = (AnimationNodeStateMachine)player.animationTree.TreeRoot;
            string previousAnimationName = player.animationState.GetCurrentNode();
            float previousPlaybackPosition = player.animationPlaybackBlocks[0].previousPlaybackPosition;
            player.animationTree.Advance(globalPhysicsDeltaFloat);
            stateMachine = (AnimationNodeStateMachine)player.animationTree.TreeRoot;
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
            string currentAnimation = player.animationState.GetCurrentNode();
            float currentPlaybackPosition = player.animationPlaybackBlocks[0].currentPlaybackPosition;
            Vector3 playerForward = -player.node.Transform.Basis.Z.Normalized();
            Vector3 playerRight = player.node.Transform.Basis.X.Normalized();
            Vector3 currentCameraForward = -currentCamera.Transform.Basis.Z.Normalized();
            Vector3 currentCameraRight = currentCamera.Transform.Basis.X.Normalized();
            player.wishDirection = (-currentCameraForward * inputDirection.Y + currentCameraRight * inputDirection.X) * Y_FLAT;
            bool hasMovementInput = player.wishDirection.Length() > 0.1f;
            Vector3 movementDirection = !hasMovementInput ? playerForward : player.wishDirection;
            float turnAnticipationTargetAngle = Mathf.Atan2(playerForward.Cross(movementDirection).Y, playerForward.Dot(movementDirection));
            Vector3 airControlDirection = player.wishDirection.Length() > 0.1f ? player.wishDirection : player.node.Velocity.Normalized();
            bool action3Pressed = Input.IsActionPressed("action3");
            bool action3JustReleased = Input.IsActionJustReleased("action3");
            bool shouldMoveAndSlide = true;
            Vector3 ledgeShapeCastDirection = player.wishDirection.LengthSquared() > ALMOST_ZERO ? player.wishDirection : playerForward;
            ledgeShapeCastDirection *= Y_FLAT;
            player.ledgeShapeCast.LookAt(player.ledgeShapeCast.GlobalPosition + ledgeShapeCastDirection, Vector3.Up);
            Transform3D orbTarget = player.orb.node.Visible ?
                player.skeleton.GetBoneGlobalPose(Player.miscObjectBoneIndex) :
                player.skeleton.GetBoneGlobalPose(Player.headBoneIndex);
            Vector3 global_whatever = player.skeleton.ToGlobal(orbTarget.Origin);
            player.orb.node.GlobalPosition = player.orb.node.TopLevel ? player.orb.node.GlobalPosition : global_whatever;
            bool shouldShowRocketIndicator = false;
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
            Vector3 rootVelocity = player.node.Transform.Basis * rootPosition / globalPhysicsDeltaFloat;
            string targetAnimation = player.animationState.GetCurrentNode();
            bool isAnimationSameAsPrevious = currentAnimation == previousAnimationName;
            bool isInTransition = player.animationState.GetTravelPath().Count > 0;
            switch (player.animationState.GetCurrentNode()) {
                case "Walk": {
                    if (!isAnimationSameAsPrevious) {
                        player.animationTree.Set("parameters/Walk/WalkBlend/blend_amount", 0.0f);
                        player.animationTree.Set("parameters/Walk/TeleportStartupBlend/blend_amount", 0.0f);
                        player.animationTree.Set("parameters/Walk/GunBlend/blend_amount", 0.0f);
                        player.animationTree.Set("parameters/Walk/FireWalkOneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Abort);
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
                    if (Input.IsActionJustPressed("attack")) {
                        for (int i = 0; i < player.targets.Length; i++) { player.targets[i] = null; }
                        player.targetCount = 0;
                        player.currentTargetIndex = 0;
                    }
                    //GD.Print((bool)player.animationTree.Get("parameters/Walk/FireWalkOneShot/active"));
                    if (!isShooting && (Input.IsActionJustReleased("attack") || (player.targetCount > 0 && !Input.IsActionPressed("attack")))) {
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
                            if (hitSomething && potentialTargetHitInfo.Collider != potentialTarget) { continue; }
                            if (!alreadyTargeted && player.targetCount < player.targets.Length) {
                                player.targets[player.targetCount] = potentialTarget;
                                targetReticles[player.targetCount].node.Visible = true;
                                player.targetCount++;
                            }
                        }
                    }
                    if (gunBlendAmount > ALMOST_ONE || isShooting) {
                        shouldShowRocketIndicator = true;
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
                                Vector3 toLedge = Vector3.Zero;
                                if (PlayerCanLeap(ref toLedge, 0)) {
                                    string climbAnimation = PlayerMatchLeapAnimation(toLedge, currentAnimation);
                                    targetAnimation = climbAnimation;
                                    break;
                                } else {
                                    RotateTowards(player.wishDirection, player.node, 0.1f);
                                    player.animationState.Start("RunJump01", true);
                                    targetAnimation = "RunJump01";
                                }
                            }
                        } else if (player.targetCount > 0 || IsInputPressedEx(ref inputState.attack, true, true)) {
                            targetAnimation = "Fire01";
                        } else {
                            targetAnimation = "Jump";
                            player.animationTree.Set("parameters/Jump/JumpSeek/seek_request", 0.0f);
                        }
                    }
                    if (player.node.IsOnWall() && walkBlendAmount > 0.8f) {
                        Vector3 toLedge = Vector3.Zero;
                        if (PlayerCanLeap(ref toLedge, 1)) {
                            string climbAnimation = PlayerMatchLeapAnimation(toLedge, targetAnimation);
                            player.animationState.Start(climbAnimation, true);
                            break;
                        }
                    }
                    int walkBlockIndex = GetPlaybackBlockIndex("Walk");
                    if (
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[walkBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[walkBlockIndex].currentPlaybackPosition, 0.33f) ||
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[walkBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[walkBlockIndex].currentPlaybackPosition, 1.54f)
                    ) {
                        if (player.isOnGround) {
                            PlaySoundUI(footStepMetalSFX, 0.2f * walkBlendAmount, globalSlightPitchVaration, true);
                        }
                    }
                    int fireWalkBlockIndex = GetPlaybackBlockIndex("FireWalk");
                    if (HasCrossedPlaybackPosition(player.animationPlaybackBlocks[fireWalkBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[fireWalkBlockIndex].currentPlaybackPosition, 0.1f) && (float)player.animationTree.Get("parameters/Walk/FireWalk/current_position") > 0.0f) {
                        Shoot(1);
                    }
                    turnAnticipationTargetAngle = Mathf.Clamp(turnAnticipationTargetAngle, -0.8f, 0.8f);
                    player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, turnAnticipationTargetAngle, 0.2f);
                    RotateTowards(movementDirection, player.node, 0.2f);
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    PlayerApplyDynamicBoneTransformations(1, 0.15f, 0.5f);
                    break;
                }
                case "Run": {
                    if (!isAnimationSameAsPrevious) { break; }
                    if (!IsInputPressed(ref inputState.action3) || !hasMovementInput) {
                        targetAnimation = "Walk";
                    }
                    if (!player.isOnGround) {
                        Vector3 toLedge = Vector3.Zero;
                        if (PlayerCanLeap(ref toLedge, 6)) {
                            string climbAnimation = PlayerMatchLeapAnimation(toLedge, targetAnimation);
                            player.animationState.Start(climbAnimation, true);
                            break;
                        } else {
                            targetAnimation = "RunJump01";
                        }
                    }
                    float leapMaxDistance = 2 * player.node.Velocity.Normalized().Dot(player.wishDirection);
                    if (leapMaxDistance > 0f) {
                        Vector3 toLedge = Vector3.Zero;
                        if (PlayerCanLeap(ref toLedge, leapMaxDistance)) {
                            string climbAnimation = PlayerMatchLeapAnimation(toLedge, targetAnimation);
                            player.animationState.Start(climbAnimation, true);
                            break;
                        }
                    }
                    if (
                        HasCrossedPlaybackPosition(previousPlaybackPosition, currentPlaybackPosition, 0.14f) ||
                        HasCrossedPlaybackPosition(previousPlaybackPosition, currentPlaybackPosition, 0.68f)
                    ) {
                        PlaySoundUI(footStepMetalSFX, 0.2f, globalSlightPitchVaration, true);
                    }
                    float targetAngleRun = Mathf.Atan2(playerForward.Cross(player.wishDirection).Y, playerForward.Dot(player.wishDirection));
                    turnAnticipationTargetAngle = Mathf.Clamp(targetAngleRun, -0.8f, 0.8f);
                    player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, turnAnticipationTargetAngle, 0.2f);
                    RotateTowards(player.wishDirection, player.node, 0.2f);
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    PlayerApplyDynamicBoneTransformations(1.5f, 0.8f, 0.5f);
                    break;
                }
                case "Fire01": {
                    if (!isAnimationSameAsPrevious) { break; }
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Fire01/current_length")) {
                        targetAnimation = "Walk";
                    }
                    if (HasCrossedPlaybackPosition(previousPlaybackPosition, currentPlaybackPosition, 0.66f)) {
                        Shoot(3);
                    }
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    break;
                }
                case "Climb03":
                case "Climb02":
                case "Climb01":
                case "Climb00": {
                    float distanceUp = 0f;
                    float distanceForward = 0f;
                    float landingTimeStamp = 0f;
                    switch (player.animationState.GetCurrentNode()) {
                        case "Climb03":
                            distanceUp = 2f;
                            distanceForward = 0.3f;
                            landingTimeStamp = 0.55f;
                            break;
                        case "Climb02":
                            distanceUp = 1f;
                            distanceForward = 0.3f;
                            landingTimeStamp = 0.55f;
                            break;
                        case "Climb01":
                        case "Climb00":
                            distanceUp = 0.5f;
                            distanceForward = 0f;
                            landingTimeStamp = 0.33f;
                            break;
                    }
                    if (distanceUp == 0f && distanceForward == 0f && landingTimeStamp == 0f) { GD.PrintErr("Leap case: How"); }
                    float animationLength = (float)player.animationTree.Get("parameters/" + player.animationState.GetCurrentNode() + "/current_length");
                    if (!isAnimationSameAsPrevious) {
                        player.targetCorrection = player.targetPosition - player.node.GlobalPosition;
                        player.targetCorrection.Y -= distanceUp;
                        PlayerSetCollision(false);
                        player.node.Velocity = Vector3.Zero;
                        break;
                    }
                    if (HasCrossedPlaybackPosition(previousPlaybackPosition, currentPlaybackPosition, landingTimeStamp)) {                   
                        PlayerSetCollision(true);
                    }
                    if (currentPlaybackPosition < landingTimeStamp && player.targetCorrection.LengthSquared() > ALMOST_ZERO) {
                        float timeRemaining = Mathf.Max(
                            (landingTimeStamp - currentPlaybackPosition) * animationLength,
                            globalPhysicsDeltaFloat
                        );
                        Vector3 step = player.targetCorrection * (globalPhysicsDeltaFloat / timeRemaining);
                        player.node.GlobalPosition += step;
                        player.targetCorrection -= step;
                        RotateTowards(player.wishDirection, player.node, 0.1f);
                        player.node.Velocity = new Vector3(rootVelocity.X, rootVelocity.Y, rootVelocity.Z);
                        break;
                    }
                    bool isAnimationComplete = currentPlaybackPosition >= animationLength;
                    if (!isAnimationComplete) {
                        if (IsInputJustPressed(ref inputState.action3)) {
                            Vector3 toLedge = Vector3.Zero;
                            if (PlayerCanLeap(ref toLedge, 6)) {
                                string climbAnimation = PlayerMatchLeapAnimation(toLedge, targetAnimation);
                                player.animationState.Start(climbAnimation, true);
                            } else { 
                                RotateTowards(player.wishDirection, player.node, 1f); 
                                player.node.Velocity = Vector3.Up + (player.wishDirection * PLAYER_RUN_SPEED); 
                                player.animationState.Start("RunJump01", true); 
                                targetAnimation = "RunJump01"; }
                        }
                        break;
                    }
                    const float MAX_DISTANCE_TO_GROUND = 1.5f;
                    if (distanceToGround > MAX_DISTANCE_TO_GROUND) {
                        if (!IsInputPressed(ref inputState.action3)) { targetAnimation = "Fall"; break; }
                        Vector3 toLedge = Vector3.Zero;
                        if (PlayerCanLeap(ref toLedge, 3)) {
                            string climbAnimation = PlayerMatchLeapAnimation(toLedge, targetAnimation);
                            player.animationState.Start(climbAnimation, true);
                        } else { 
                            RotateTowards(player.wishDirection, player.node, 1f); 
                            player.node.Velocity = Vector3.Up + (player.wishDirection * PLAYER_RUN_SPEED); 
                            player.animationState.Start("RunJump01", true); 
                            targetAnimation = "RunJump01";
                        }
                        break;
                    }
                    if (action3Pressed) {
                        Vector3 toLedge = Vector3.Zero;
                        if (PlayerCanLeap(ref toLedge, 3)) {
                            string climbAnimation = PlayerMatchLeapAnimation(toLedge, targetAnimation);
                            player.animationState.Start(climbAnimation, true);
                        } else {
                            targetAnimation = "Run";
                        }
                    } else {
                        targetAnimation = "Walk";
                    }
                    player.node.Velocity *= Y_FLAT;
                    break;
                }
                case "Jump": {
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
                            inputPreviousPosition: previousPlaybackPosition,
                            inputCurrentPosition: currentPlaybackPosition,
                            inputEventPosition: JUMP_PLAYBACK_POSITION
                        );
                    if (shouldJump) {
                        player.node.Velocity += Vector3.Up * 12f;
                        player.node.Velocity += player.wishDirection.Length() > 0.1f ? player.wishDirection * 2f : player.node.Transform.Basis.Z.Normalized() * 2f;
                    }
                    if (
                        currentPlaybackPosition >= JUMP_PLAYBACK_POSITION &&
                        player.node.IsOnWall()
                    ) {
                        targetAnimation = "WallGrab";
                        RotateTowards(-player.node.GetSlideCollision(0).GetNormal(), player.node, 1);
                    }
                    if (
                        currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Jump/Jump/current_length") ||
                        player.node.IsOnCeiling()
                    ) {
                        targetAnimation = "Fall";
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    if (!player.isOnGround) {
                        player.node.Velocity = new Vector3(
                            Mathf.Lerp(player.node.Velocity.X, airControlDirection.X * PLAYER_AIR_SPEED, 0.01f),
                            player.node.Velocity.Y,
                            Mathf.Lerp(player.node.Velocity.Z, airControlDirection.Z * PLAYER_AIR_SPEED, 0.01f)
                        );
                    }
                    break;
                }
                case "WallGrab": {
                    if (!isAnimationSameAsPrevious) { 
                        PlaySoundUI(GD.Load<AudioStream>("res://assets/sound/grab01.wav"), 0.05f, globalSlightPitchVaration, false);
                        break; 
                    }
                    if (
                        currentPlaybackPosition > 0.266f &&
                        hasMovementInput && 
                        IsInputPressed(ref inputState.action3)
                    ) {
                        RotateTowards(player.wishDirection, player.node, 1f);
                        targetAnimation = "RunJump02";
                        player.node.Velocity = player.wishDirection * 12f + Vector3.Up * 4f;
                        break;
                    }
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/WallGrab/current_length")) {
                        targetAnimation = "Fall";
                    }
                    player.node.Velocity = Vector3.Zero;
                    break;
                }
                case "RunJump01": {
                    if (!isAnimationSameAsPrevious) {
                        player.node.Velocity = Vector3.Up * 5f + (player.wishDirection * PLAYER_RUN_SPEED);
                        break;
                    }
                    RotateTowards(player.node.Velocity, player.node, 0.2f);
                    if (player.node.IsOnWall() && distanceToGround > 2f) {
                        targetAnimation = "WallGrab";
                        RotateTowards(-player.node.GetSlideCollision(0).GetNormal(), player.node, 1);
                    }
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/RunJump01/current_length")) {
                         targetAnimation = "Fall";
                    }
                    if (player.isOnGround) {
                        string animationToPlay = playerForward.Dot(player.wishDirection) > -0.6f ? "Roll" : "FallToIdle";
                        player.animationState.Start(animationToPlay, true);
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    break;
                }
                case "RunJump02": {
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
                }
                case "Roll": {
                    if (!isAnimationSameAsPrevious) {
                        PlaySoundUI(rollSFX, 0.05f, globalSlightPitchVaration, true);
                        break;
                    }
                    if (currentPlaybackPosition < 0.5f) {
                        RotateTowards(player.wishDirection, player.node, 0.1f);
                        player.node.Velocity = playerForward * 8f;
                    }
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Roll/current_length") - 0.4f) {
                        if (inputDirection != Vector2.Zero && action3Pressed) { 
                            targetAnimation = "Run";
                        }
                    }
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Roll/current_length") && isAnimationSameAsPrevious) {
                        if (inputDirection != Vector2.Zero && action3Pressed) { 
                            targetAnimation = "Run";
                        } else {
                            targetAnimation = "Walk";
                        }
                    }
                    if (!player.isOnGround) { targetAnimation = "Fall"; }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    break;
                }
                case "Fall": {
                    if (player.isOnGround) {
                        if ((player.node.Velocity * Y_FLAT).Length() > 6f) {
                            player.animationState.Start("Roll", true);
                        } else {
                            float impactSpeed = -player.node.Velocity.Y;
                            float animationScale = Mathf.Clamp(impactSpeed / 12.0f, 0.5f, 2.0f);
                            float animationSpeed = Mathf.Clamp(2.0f - (impactSpeed / 10.0f), 0.5f, 2f);
                            GD.Print("impactSpeed: " + impactSpeed + " animationScale: " + animationScale + " animationSpeed: " + animationSpeed);
                            player.animationTree.Set("parameters/FallToIdle/FallToIdleTimeSeek/seek_request", 0.0f);
                            player.animationTree.Set("parameters/FallToIdle/FallToIdleBlend/blend_amount", animationScale);
                            player.animationTree.Set("parameters/FallToIdle/FallToIdleTimeScale/scale", animationSpeed);
                            player.animationState.Start("FallToIdle", true);
                        }
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    player.node.Velocity = new Vector3(
                        Mathf.Lerp(player.node.Velocity.X, airControlDirection.X * PLAYER_AIR_SPEED, 0.02f),
                        player.node.Velocity.Y,
                        Mathf.Lerp(player.node.Velocity.Z, airControlDirection.Z * PLAYER_AIR_SPEED, 0.02f)
                    );
                    break;
                }
                case "FallToIdle": {
                    if (!isAnimationSameAsPrevious) {
                        break;
                    }
                    int FallToIdleBlockIndex = GetPlaybackBlockIndex("FallToIdle");
                    if (HasCrossedPlaybackPosition(player.animationPlaybackBlocks[FallToIdleBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[FallToIdleBlockIndex].currentPlaybackPosition, 0.2f)) {
                        if (player.isOnGround) { PlaySoundUI(footStepMetalSFX, 0.4f, globalSlightPitchVaration, true); }
                    }
                    if (player.animationPlaybackBlocks[FallToIdleBlockIndex].currentPlaybackPosition >= (float)player.animationTree.Get("parameters/FallToIdle/FallToIdle/current_length")) {
                        targetAnimation = "Walk";
                    }
                    player.node.Velocity = player.node.Velocity.Lerp(Vector3.Zero, 0.08f);
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    if (!player.isOnGround) { 
                        player.node.GlobalPosition -= player.node.Velocity * 1.2f * globalPhysicsDeltaFloat;
                        player.node.Velocity = Vector3.Zero;
                    }
                    break;
                }
                case "TeleportShoot": {
                    if (!isAnimationSameAsPrevious) {
                        RotateTowards(-player.wishDirection, player.node, 1f);
                        player.orb.node.Visible = true;
                        break;
                    }
                    if (!player.isOnGround) {
                        player.node.Velocity += Vector3.Up * 5f;
                        player.animationState.Start("Fall", true);
                        OrbReturn(false);
                    }
                    if (currentPlaybackPosition > 0.8f && currentPlaybackPosition < 1f && IsInputJustPressed(ref inputState.action3)) {
                        targetAnimation = "Jump";
                        player.node.Velocity = Vector3.Up * 12f;
                        player.node.GlobalPosition += Vector3.Up * 0.5f;
                        OrbReturn(true);
                    }
                    if (HasCrossedPlaybackPosition(previousPlaybackPosition, currentPlaybackPosition, 1f)) {
                        if (player.wishDirection != Vector3.Zero) {
                            RotateTowards(player.wishDirection, player.node, 1f);
                        }
                        OrbShoot();
                    }
                    if (currentPlaybackPosition >= player.animationPlayer.GetAnimation(player.animationState.GetCurrentNode()).Length) {
                        targetAnimation = "OrbIdle";
                    }
                    player.node.Velocity = new Vector3(rootVelocity.X, rootVelocity.Y, rootVelocity.Z);
                    if (targetAnimation != "TeleportShoot" && targetAnimation != "OrbIdle") {
                        OrbReturn(false);
                    }
                    break;
                }
                case "OrbIdle": {
                    if (!isAnimationSameAsPrevious) {
                        break;
                    }
                    if (!player.orb.node.TopLevel) {
                        targetAnimation = player.isOnGround ? "Walk" : "Fall";
                    }
                    break;
                }
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
                player.currentAnimationName = targetAnimation;
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
            //PlayerRocketIndicatorUpdate(shouldShowRocketIndicator, playerForward + player.node.GlobalTransform.Basis.Y.Normalized() * 0.2f);
            CharacterBody3D target = null;
            if (player.detectionArea.GetOverlappingBodies().Count > 0) {
                for (int i = 0; i < player.detectionArea.GetOverlappingBodies().Count; i++) {
                    //get closest enemy
                    Node3D body = player.detectionArea.GetOverlappingBodies()[i];
                    if (!(body.GetType() == typeof(CharacterBody3D))) {
                        continue;
                    }
                    GD.Print(body.Name);
                    if ((string)body.GetMeta("Type") != "EnemyGeneric") {
                        continue;
                    }
                    CharacterBody3D enemy = (CharacterBody3D)body;
                    if (target == null) {
                        target = enemy;
                    } else {
                        if ((target.GlobalPosition - player.node.GlobalPosition).Length() > (enemy.GlobalPosition - player.node.GlobalPosition).Length()) {
                            target = enemy;
                        }
                    }
                }
            }
            player.rocketIndicator.GlobalPosition = target == null ? Vector3.Zero : target.GlobalPosition;
            //GD.Print(player.rocketIndicator.GlobalPosition);
        }
        public void PlayerCameraUpdate(ref PlayerCamera inputCamera) {
            if (inputCamera.node == null) { return; }
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