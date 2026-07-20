using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct Player {
            public TeleportOrb orb;
            public CharacterBody3D node;
            public Node3D targetIndicator;
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
            public targetAnimationData targetAnimation;
            public float turnAnticipation;
            public int targetCount;
            public int maxTargetCount;
            public int currentTargetIndex;
            public bool isOnGround;
            public bool hasShotDuringThisAnimation;
            public static int chestBoneIndex;
            public static int headBoneIndex;
            public static int miscObjectBoneIndex;
        }
        public struct PlayerCamera {
            public Vector3 targetPosition;
            public Camera3D node;
            public float offsetDistance;
            public float offsetHeight;
            public float targetAngle;
            public float angle;
            public float maxLerpDistance;
            public float rotationLerpSpeed;
            public float shakeAmount;
        }
        public struct targetAnimationData { // maybe better name: AnimStateCheck
            public string name;
            public bool shouldChangeImmediately;
            public bool shouldSkipStartup;
        }
        public void PlayerInitialize(CharacterBody3D inputPlayerNode) {
            player.node = inputPlayerNode;
            player.model = player.node.GetNode<Node3D>("Slink");
            player.targetIndicator = player.node.GetNode<Node3D>("TargetIndicator");
            player.targetIndicator.TopLevel = true;
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
            player.turnAnticipation = 0f;
            player.targetCount = 0;
            player.maxTargetCount = DEFAULT_PLAYER_MAX_TARGET_COUNT;
            player.currentTargetIndex = 0;
            player.isOnGround = false;
            player.hasShotDuringThisAnimation = false;
            for (int i = 0; i < player.animationPlaybackBlocks.Length; i++) {
                player.animationPlaybackBlocks[i].previousPlaybackPosition = 0f;
                player.animationPlaybackBlocks[i].currentPlaybackPosition = 0f;
            }
            AnimationNodeOneShot fireShotNode = (AnimationNodeOneShot)((AnimationNodeBlendTree)((AnimationNodeStateMachine)player.animationTree.TreeRoot).GetNode("Move")).GetNode("FireWalkOneShot");
            SetOneShotFilters(true, "shoulder.R", player.skeleton, player.node, fireShotNode); // Godot forces us to do this for some reason or else it will literally forget the filters we set for the oneshot in the editor
            GD.Print("Player Initialized");
        }
        public void PlayerCameraInitialize(Camera3D inputCamera) {
            playerCamera.node = inputCamera;
            playerCamera.offsetDistance = DEFAULT_CAMERA_DISTANCE;
            playerCamera.offsetHeight = DEFAULT_CAMERA_HEIGHT;
            playerCamera.node.Fov = 64;
            playerCamera.node.Far = cameraFarSetting;
            playerCamera.targetAngle = Mathf.Round(inputCamera.Rotation.Y / 90f) * 90f;
            bool sceneHasFog = worldEnvironment.Environment.FogEnabled;
            if (sceneHasFog) {
                playerCamera.node.Far = worldEnvironment.Environment.FogDepthEnd > cameraFarSetting ?
                    worldEnvironment.Environment.FogDepthEnd :
                    cameraFarSetting;
            } 
            // else {
            //     worldEnvironment.Environment.FogEnabled = true;
            //     worldEnvironment.Environment.FogLightColor = Colors.Black;
            //     worldEnvironment.Environment.FogMode = Godot.Environment.FogModeEnum.Depth;
            //     worldEnvironment.Environment.FogDepthBegin = cameraFarSetting * 0.8f;
            //     worldEnvironment.Environment.FogDepthEnd = cameraFarSetting;
            // }
            playerCamera.maxLerpDistance = 200f;
            playerCamera.rotationLerpSpeed = 0.1f;
            playerCamera.shakeAmount = 0.0f;
            GD.Print("Player Camera Initialized");
        }
        public void PlayerShoot(int inputRocketCount) {
            player.hasShotDuringThisAnimation = true;
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
                if (!hasTarget) {
                    Node3D maybeTarget = PlayerTargetIndicatorUpdate();
                    if (maybeTarget != null) {
                        player.targets[player.currentTargetIndex] = maybeTarget;
                        hasTarget = true;
                        didEverHaveTarget = true;
                        player.targets[player.currentTargetIndex] = maybeTarget; 
                        GD.Print("Found target");
                    } else {
                        GD.Print("Couldn't find target");
                    }
                }
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
        public static targetAnimationData ChangeTargetAnimation(string newAnimation, bool shouldChangeImmediately, bool shouldSkipStartup) {
            targetAnimationData targetAnimationData = new targetAnimationData {
                name = newAnimation,
                shouldChangeImmediately = shouldChangeImmediately,
                shouldSkipStartup = shouldSkipStartup
            };
            return targetAnimationData;
        }
        public void PlayerChangeAnimationEx(string newAnimation, bool shouldChangeImmediately, bool shouldSkipStartup) {
            player.targetAnimation = ChangeTargetAnimation(newAnimation, shouldChangeImmediately, shouldSkipStartup);
        }
        public void PlayerChangeAnimation(string newAnimation) {
            player.targetAnimation = ChangeTargetAnimation(newAnimation, false, false);
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
            if (RayCast(rayStart, rayEnd, LAYER_WORLD_STATIC)){
                outHitPosition = globalHitInfo.Position; 
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
        public Node3D PlayerTargetIndicatorUpdate() {
            player.node.GetNode<Sprite3D>("TargetIndicator/Sprite3D").Frame = (player.node.GetNode<Sprite3D>("TargetIndicator/Sprite3D").Frame + 1) % 24;
            CharacterBody3D indicatorTarget = null;
            float best = float.MaxValue;
            Godot.Collections.Array<Node3D> bodies = player.detectionArea.GetOverlappingBodies();
            if (bodies.Count > 0) {
                for (int i = 0; i < bodies.Count; i++) {
                    CharacterBody3D enemy = (CharacterBody3D)bodies[i];
                    bool hitSomething = RayCast(player.node.GlobalPosition + Vector3.Up, enemy.GlobalPosition + Vector3.Up, LAYER_WORLD_STATIC);
                    if (hitSomething) { continue; }
                    Vector3 toEnemy = enemy.GlobalPosition - player.node.GlobalPosition;
                    float distance = toEnemy.Length();
                    float angleToPlayer = Mathf.Acos(Mathf.Clamp((-player.node.GlobalTransform.Basis.Z).Dot(toEnemy.Normalized()), -1f, 1f));
                    float angleToCamera = Mathf.Acos(Mathf.Clamp(-currentCamera.GlobalTransform.Basis.Z.Dot(toEnemy.Normalized()), -1f, 1f));
                    float score = distance + angleToPlayer * 10f;
                    if (score < best) {
                        best = score;
                        indicatorTarget = enemy;
                    }
                }
            }
            if (!(indicatorTarget == null)) {
                player.targetIndicator.Visible = true;
                player.targetIndicator.GlobalPosition = player.targetIndicator.GlobalPosition.Lerp(indicatorTarget.GlobalPosition, 0.1f);
                return indicatorTarget;
            } else {
                player.targetIndicator.Visible = false;
                return null;
            }
        }
        public void PlayerRocketIndicatorManualCurveUpdate(bool inputShouldShow, Vector3 inputDirection) {
            player.targetIndicator.Visible = inputShouldShow;
            Vector3 velocity = inputDirection * PLAYER_ROCKET_SPEED;
            Vector3 position = player.gunBarrel.GlobalPosition;
            Vector3 end = player.gunBarrel.GlobalPosition;
            float time = globalPhysicsDeltaFloat * 12f;
            bool curveHit = false;
            for (int i = 0; i < 24; i++) {
                Vector3 next = position + velocity * time;
                velocity += GRAVITY_VECTOR * time;
                if (RayCast(position, next, LAYER_WORLD_STATIC)) {
                    end = globalHitInfo.Position;
                    curveHit = true;
                    break;
                }
                end = next;
                position = next;
            }
            if (!curveHit) {
                if (RayCast(position, end + Vector3.Down * 500, LAYER_WORLD_STATIC)) {
                    end = globalHitInfo.Position;
                    curveHit = true;
                } else {
                    player.targetIndicator.Visible = false;
                }
            }
            player.targetIndicator.GlobalPosition = end;
        }
        public void PlayerShakeCamera(float amount) {
            playerCamera.shakeAmount = amount;
        }
        public void PlayerUpdate() {
            PlayerTargetIndicatorUpdate();
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
            float velocityLength = player.node.Velocity.Length();
            float velocityLengthFlat = (velocityLength * Y_FLAT).Length();
            Vector3 currentCameraForward = -currentCamera.Transform.Basis.Z.Normalized();
            Vector3 currentCameraRight = currentCamera.Transform.Basis.X.Normalized();
            player.wishDirection = (-currentCameraForward * inputDirection.Y + currentCameraRight * inputDirection.X) * Y_FLAT;
            Vector3 wishDirectionOrVelocity = player.wishDirection.Length() > ALMOST_ZERO ? player.wishDirection : player.node.Velocity.Normalized();
            Vector3 wishDirectionOrForward = player.wishDirection.Length() > ALMOST_ZERO ? player.wishDirection : playerForward;
            bool hasTarget = player.targets[0] != null;
            Vector3 directionToTarget = hasTarget ?
                (player.targets[0].GlobalPosition - player.node.GlobalPosition).Normalized() :
                playerForward;
            bool hasMovementInput = player.wishDirection.Length() > ALMOST_ZERO;
            Vector3 movementDirection = !hasMovementInput ? playerForward : player.wishDirection;
            float turnAnticipationTargetAngle = Mathf.Atan2(playerForward.Cross(movementDirection).Y, playerForward.Dot(movementDirection));
            Vector3 ledgeShapeCastDirection = player.wishDirection.LengthSquared() > ALMOST_ZERO ? player.wishDirection : playerForward;
            ledgeShapeCastDirection *= Y_FLAT;
            player.ledgeShapeCast.LookAt(player.ledgeShapeCast.GlobalPosition + ledgeShapeCastDirection, Vector3.Up);
            Transform3D orbTarget = player.orb.node.Visible ?
                player.skeleton.GetBoneGlobalPose(Player.miscObjectBoneIndex) :
                player.skeleton.GetBoneGlobalPose(Player.headBoneIndex);
            Vector3 global_whatever = player.skeleton.ToGlobal(orbTarget.Origin);
            player.orb.node.GlobalPosition = player.orb.node.TopLevel ? player.orb.node.GlobalPosition : global_whatever;
            float distanceToGround = player.groundRay.IsColliding() ?
                player.groundRay.GlobalPosition.Y - player.groundRay.GetCollisionPoint().Y  :
                float.MaxValue;
            bool shouldSnapToGround = distanceToGround <= PLAYER_LEG_LENGTH * 1.5f;
            bool isRayCollidingAtPlayerFeet = distanceToGround <= PLAYER_LEG_LENGTH;
            player.isOnGround = (shouldSnapToGround) && player.node.Velocity.Y <= 0f;
            Vector3 collisionPointPosition = new Vector3(player.node.GlobalPosition.X, player.groundRay.GetCollisionPoint().Y, player.node.GlobalPosition.Z);
            if (player.isOnGround && player.node.CollisionMask > 0) {
                player.node.GlobalPosition = player.node.GlobalPosition.Lerp(collisionPointPosition, 0.1f + (player.node.Velocity.Length() * globalPhysicsDeltaFloat));
            }
            Vector3 rootPosition = player.animationTree.GetRootMotionPosition();
            Vector3 rootVelocity = player.node.Transform.Basis * rootPosition / globalPhysicsDeltaFloat;
            Vector3 rootVelocityFlat = rootVelocity * Y_FLAT;
            Vector3 rootVelocityXZ = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
            bool shouldMoveAndSlide = true;
            player.targetAnimation.name = player.animationState.GetCurrentNode();
            player.targetAnimation.shouldChangeImmediately = false;
            bool isAnimationSameAsPrevious = currentAnimation == previousAnimationName;
            bool isInTransition = player.animationState.GetTravelPath().Count > 0;
            switch (player.animationState.GetCurrentNode()) {
                case "Move": {
                    if (!isAnimationSameAsPrevious) {
                        player.animationTree.Set("parameters/Move/WalkBlend/blend_amount", 0.0f);
                        player.animationTree.Set("parameters/Move/TeleportStartupBlend/blend_amount", 0.0f);
                        player.animationTree.Set("parameters/Move/GunBlend/blend_amount", 0.0f);
                        player.animationTree.Set("parameters/Move/FireWalkOneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Abort);
                        break;
                    }
                    bool hyper = Input.IsActionPressed("action3");
                    float isHyper = hyper ? 1.3f : 1f;
                    bool isShooting = 
                        (bool)player.animationTree.Get("parameters/Move/FireWalkOneShot/active") &&
                        (float)player.animationTree.Get("parameters/Move/FireWalk/current_position") < 0.36f;
                    //bool shouldGunBlend = !isShooting && (Input.IsActionPressed("attack") || player.targetCount > 0);
                    //float gunBlendAmount = Mathf.MoveToward((float)player.animationTree.Get("parameters/Move/GunBlend/blend_amount"), shouldGunBlend ? 1f : 0f, 0.1f);
                    //player.animationTree.Set("parameters/Move/GunBlend/blend_amount", gunBlendAmount);
                    bool shouldWalkBlend = Input.IsActionPressed("mod") || (hasMovementInput && isShooting);
                    const float WALK_MIN = 1f;
                    const float WALK_MAX = 1.5f;
                    const float WALK_CAP_LERP_SPEED = 0.1f;
                    float walkCap = 
                        Mathf.MoveToward((float)player.animationTree.Get("parameters/Move/WalkTimeScale/scale"), Input.IsActionPressed("action3") ? WALK_MAX : WALK_MIN, WALK_CAP_LERP_SPEED);
                    player.animationTree.Set("parameters/Move/WalkTimeScale/scale", walkCap);
                    float walkBlendAmount = 
                        Mathf.MoveToward((float)player.animationTree.Get("parameters/Move/WalkBlend/blend_amount"), shouldWalkBlend ? 1 : 0f, 0.1f);
                    player.animationTree.Set("parameters/Move/WalkBlend/blend_amount", walkBlendAmount);
                    bool shouldRunBlend = hasMovementInput && !isShooting;
                    player.animationTree.Set("parameters/Move/RunTimeScale/scale", isHyper);
                    float runBlendAmount = 
                        Mathf.MoveToward((float)player.animationTree.Get("parameters/Move/RunBlend/blend_amount"), shouldRunBlend ? isHyper : 0f, 0.1f);
                    player.animationTree.Set("parameters/Move/RunBlend/blend_amount", runBlendAmount);
                    bool shouldTeleportBlend = Input.IsActionPressed("action2");
                    float teleportBlendAmount = Mathf.MoveToward((float)player.animationTree.Get("parameters/Move/TeleportStartupBlend/blend_amount"), shouldTeleportBlend ? 1f : 0f, 0.1f);
                    player.animationTree.Set("parameters/Move/TeleportStartupBlend/blend_amount", teleportBlendAmount);
                    if ((float)player.animationTree.Get("parameters/Move/TeleportStartupBlend/blend_amount") <= ALMOST_ZERO && (float)player.animationTree.Get("parameters/Move/TeleportStartup/current_position") > 0.0f) {
                        player.animationTree.Set("parameters/Move/TeleportStartupSeek/seek_request", 0.0f);
                    }
                    if ((float)player.animationTree.Get("parameters/Move/TeleportStartupBlend/blend_amount") <= ALMOST_ZERO && !player.orb.node.TopLevel) {
                        player.orb.node.Visible = false;
                    } else {
                        player.orb.node.Visible = true;
                    }
                    bool shouldFireLeviathan = Input.IsActionJustReleased("action2") && (float)player.animationTree.Get("parameters/Move/TeleportStartup/current_position") > 0.8f;
                    if (shouldFireLeviathan) {
                        PlayerChangeAnimation("TeleportShoot");
                    }
                    if (Input.IsActionJustPressed("attack")) {
                        Node3D potentialTarget = PlayerTargetIndicatorUpdate();
                        bool isTargetValid = 
                            potentialTarget != null &&
                            potentialTarget != player.node &&
                            potentialTarget.GetType() != typeof(AudioStreamPlayer3D);
                        if (isTargetValid) {
                            player.targets[0] = potentialTarget;
                            player.targetCount = 1;
                        } else {
                            PlayerChangeAnimation("Slide");
                        }
                    }
                    if (player.targetCount > 0) {
                        PlayerChangeAnimation("Slide");
                    }
                    if (!player.isOnGround) { 
                        if (InputIsPressed(ref inputState.action3)) {
                            PlayerChangeAnimationEx("RunJump01", true, false);
                        } else {
                            PlayerChangeAnimation("Fall");
                            player.node.Velocity = Vector3.Down * 2 + playerForward;
                            break;
                        }
                    }
                    if (InputIsJustReleased(ref inputState.action3)) {
                        if (hasMovementInput && walkCap < WALK_MAX - WALK_CAP_LERP_SPEED && walkBlendAmount <= ALMOST_ZERO) {
                            Vector3 toLedge = Vector3.Zero;
                            if (PlayerCanLeap(ref toLedge, 0)) {
                                string climbAnimation = PlayerMatchLeapAnimation(toLedge, currentAnimation);
                                PlayerChangeAnimation(climbAnimation);
                                break;
                            } else {
                                RotateTowards(player.wishDirection, player.node, 0.1f);
                                PlayerChangeAnimationEx("RunJump01", true, false);
                            }
                        } else if (player.targetCount > 0 || InputIsPressedEx(ref inputState.attack, true, true)) {
                            PlayerChangeAnimation("Fire01");
                        } else if (runBlendAmount <= 0 && walkBlendAmount <= 0.1f) {
                            PlayerChangeAnimation("Jump");
                            player.animationTree.Set("parameters/Jump/JumpSeek/seek_request", 0.0f);
                        }
                    }
                    bool shouldClimb = 
                        player.node.IsOnWall() && 
                        (walkBlendAmount > 0.8f || runBlendAmount > 0.8f) && 
                        InputIsPressed(ref inputState.action3) && 
                        player.wishDirection.Dot(player.node.GetWallNormal()) < COSINE_DEGREES_45;
                    if (shouldClimb) {
                        Vector3 toLedge = Vector3.Zero;
                        if (PlayerCanLeap(ref toLedge, 1)) {
                            string climbAnimation = PlayerMatchLeapAnimation(toLedge, player.targetAnimation.name);
                            PlayerChangeAnimationEx(climbAnimation, true, false);
                        } else {
                            RotateTowards(-player.node.GetWallNormal(), player.node, 1);
                            PlayerChangeAnimation("WallClimb");
                        }
                        break;
                    }
                    int walkBlockIndex = GetPlaybackBlockIndex("Walk");
                    if (
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[walkBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[walkBlockIndex].currentPlaybackPosition, 0.33f) ||
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[walkBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[walkBlockIndex].currentPlaybackPosition, 1.54f)
                    ) {
                        PlaySoundUI(metalFootstepSFX, 0.2f * walkBlendAmount, globalSlightPitchVaration, true);
                    }
                    int runBlockIndex = GetPlaybackBlockIndex("Run");
                    if (
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[runBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[runBlockIndex].currentPlaybackPosition, 0.14f) ||
                        HasCrossedPlaybackPosition(player.animationPlaybackBlocks[runBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[runBlockIndex].currentPlaybackPosition, 0.68f)
                    ) {
                        PlaySoundUI(metalFootstepSFX, 0.2f * runBlendAmount, globalSlightPitchVaration, true);
                        if (hyper) {
                            float strength = 0.2f;
                            PlaySoundUI(rumbleSFX, strength * 0.1f, globalSlightPitchVaration, false);
                            PlayerShakeCamera(strength);
                        }
                    }
                    int fireWalkBlockIndex = GetPlaybackBlockIndex("FireWalk");
                    if (HasCrossedPlaybackPosition(player.animationPlaybackBlocks[fireWalkBlockIndex].previousPlaybackPosition, player.animationPlaybackBlocks[fireWalkBlockIndex].currentPlaybackPosition, 0.1f) && (float)player.animationTree.Get("parameters/Move/FireWalk/current_position") > 0.0f) {
                        PlayerShoot(1);
                    }
                    turnAnticipationTargetAngle = Mathf.Clamp(turnAnticipationTargetAngle, -0.8f, 0.8f);
                    player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, turnAnticipationTargetAngle, 0.2f);
                    float turnSpeed = Mathf.Lerp(1f, 0.15f, Mathf.Clamp(velocityLengthFlat / PLAYER_RUN_SPEED, 0f, 1f));
                    RotateTowards(movementDirection, player.node, turnSpeed);
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    float chestTwist = runBlendAmount > 0.5f ? 1.5f : 1f;
                    float chestRoll = runBlendAmount > 0.5f ? 1.5f : 0.15f;
                    float headTwist = 0.5f;
                    PlayerApplyDynamicBoneTransformations(chestTwist, chestRoll, headTwist);
                    break;
                }
                case "Fire01": {
                    if (!isAnimationSameAsPrevious) { 
                        RotateTowards(directionToTarget, player.node, 1f);
                        break; 
                    }
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Fire01/current_length")) {
                        PlayerChangeAnimation("Move");
                    } else {
                        GD.Print("Rotating");
                        RotateTowards(directionToTarget, player.node, 0.2f);
                    }
                    const float shootingTimeStamp = 0.66f;
                    if (HasCrossedPlaybackPosition(previousPlaybackPosition, currentPlaybackPosition, shootingTimeStamp)) {
                        PlayerShoot(1);
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    break;
                }
                case "Climb02":
                case "Climb01":
                case "Climb00": {
                    float distanceUp = 0f;
                    float distanceForward = 0f;
                    float landingTimeStamp = 0f;
                    switch (player.animationState.GetCurrentNode()) {
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
                        GD.Print("Collision: OFF");
                        PlayerSetCollision(false);
                        player.node.Velocity = Vector3.Zero;
                        break;
                    }
                    if (HasCrossedPlaybackPosition(previousPlaybackPosition, currentPlaybackPosition, landingTimeStamp)) {   
                        GD.Print("Collision: ON");
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
                        player.node.Velocity = rootVelocity;
                        break;
                    } else {
                        player.node.Velocity *= Y_FLAT;
                        PlayerSetCollision(true);
                    }
                    bool isAnimationComplete = currentPlaybackPosition >= animationLength;
                    if (!isAnimationComplete) {
                        if (InputIsJustPressed(ref inputState.action3)) {
                            Vector3 toLedge = Vector3.Zero;
                            if (PlayerCanLeap(ref toLedge, 6)) {
                                string climbAnimation = PlayerMatchLeapAnimation(toLedge, player.targetAnimation.name);
                                PlayerChangeAnimationEx(climbAnimation, true, false);
                            } else { 
                                RotateTowards(player.wishDirection, player.node, 1f); 
                                player.node.Velocity = Vector3.Up + (player.wishDirection * PLAYER_RUN_SPEED); 
                                PlayerChangeAnimationEx("RunJump01", true, false);
                            }
                            PlayerSetCollision(true);
                        }
                        break;
                    }
                    const float MAX_DISTANCE_TO_GROUND = PLAYER_LEG_LENGTH;
                    if (distanceToGround > MAX_DISTANCE_TO_GROUND) {
                        if (!InputIsPressed(ref inputState.action3)) {
                            PlayerSetCollision(true);
                            PlayerChangeAnimation("Fall");
                            break; 
                        }
                        Vector3 toLedge = Vector3.Zero;
                        if (PlayerCanLeap(ref toLedge, 6)) {
                            string climbAnimation = PlayerMatchLeapAnimation(toLedge, player.targetAnimation.name);
                            PlayerChangeAnimationEx(climbAnimation, true, false);
                        } else { 
                            RotateTowards(player.wishDirection, player.node, 1f); 
                            player.node.Velocity = Vector3.Up + (player.wishDirection * PLAYER_RUN_SPEED); 
                            PlayerChangeAnimationEx("RunJump01", true, false);
                        }
                        break;
                    } else {
                        player.node.GlobalPosition = new Vector3(player.node.GlobalPosition.X, player.groundRay.GetCollisionPoint().Y, player.node.GlobalPosition.Z);
                    }
                    if (InputIsPressed(ref inputState.action3)) {
                        Vector3 toLedge = Vector3.Zero;
                        if (PlayerCanLeap(ref toLedge, 6)) {
                            string climbAnimation = PlayerMatchLeapAnimation(toLedge, player.targetAnimation.name);
                            PlayerChangeAnimationEx(climbAnimation, true, false);
                            break;
                        }
                    }
                    PlayerChangeAnimation("Move");
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
                        player.node.Velocity += Vector3.Up * PLAYER_JUMP_STRENGTH * 2f;
                        player.node.Velocity += player.wishDirection.Length() > 0.1f ? player.wishDirection * 2f : player.node.Transform.Basis.Z.Normalized() * 2f;
                    }
                    if (currentPlaybackPosition >= JUMP_PLAYBACK_POSITION && player.node.IsOnWall()) {
                        PlayerChangeAnimation("WallGrab");
                        RotateTowards(-player.node.GetSlideCollision(0).GetNormal(), player.node, 1);
                    }
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Jump/Jump/current_length") || player.node.IsOnCeiling()) {
                        PlayerChangeAnimation("Fall");
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    if (!player.isOnGround) {
                        player.node.Velocity = new Vector3(
                            Mathf.Lerp(player.node.Velocity.X, wishDirectionOrVelocity.X * PLAYER_AIR_SPEED, 0.01f),
                            player.node.Velocity.Y,
                            Mathf.Lerp(player.node.Velocity.Z, wishDirectionOrVelocity.Z * PLAYER_AIR_SPEED, 0.01f)
                        );
                    }
                    break;
                }
                case "WallClimb": {
                    if (!isAnimationSameAsPrevious) {}
                    GD.Print(playerForward.Dot(player.wishDirection));
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/WallClimb/current_length") || InputIsJustPressed(ref inputState.action3)) {
                        if (player.wishDirection.Length() < ALMOST_ZERO) {
                            player.node.Velocity = -playerForward;
                            PlayerChangeAnimation("Fall");
                            break;
                        }
                        bool isMovingTowardsWall = playerForward.Dot(player.wishDirection) > COSINE_DEGREES_30;
                        if (isMovingTowardsWall) {
                            player.node.Velocity = Vector3.Up * PLAYER_JUMP_STRENGTH;
                            PlayerChangeAnimation("Fall");
                            break;
                        } else {
                            float intoWall = player.wishDirection.Dot(playerForward);
                            Vector3 ejectDirection = player.wishDirection - playerForward * Mathf.Max(0, intoWall);
                            ejectDirection = ejectDirection.Normalized();
                            RotateTowards(ejectDirection, player.node, 1);
                            player.node.GlobalPosition -= playerForward * 0.3f;
                            player.node.Velocity = ejectDirection * PLAYER_RUN_SPEED + Vector3.Up * PLAYER_JUMP_STRENGTH;
                            PlayerChangeAnimation("RunJump01");
                            break;
                        }
                    }
                    player.node.Velocity = new Vector3(rootVelocity.X, rootVelocity.Y, rootVelocity.Z);
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
                        InputIsPressed(ref inputState.action3)
                    ) {
                        RotateTowards(player.wishDirection, player.node, 1f);
                        PlayerChangeAnimation("RunJump02");
                        player.node.Velocity = player.wishDirection * 12f + Vector3.Up * 4f;
                        break;
                    }
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/WallGrab/current_length")) {
                        PlayerChangeAnimation("Fall");
                    }
                    player.node.Velocity = Vector3.Zero;
                    break;
                }
                case "RunJump01": {
                    if (!isAnimationSameAsPrevious) {
                        player.node.Velocity = Vector3.Up * PLAYER_JUMP_STRENGTH + (player.wishDirection * PLAYER_RUN_SPEED);
                        GD.Print("Jumping");
                        break;
                    }
                    RotateTowards(player.node.Velocity, player.node, 0.2f);
                    if (player.node.IsOnWall() && distanceToGround > 2f) {
                        PlayerChangeAnimation("WallGrab");
                        RotateTowards(-player.node.GetSlideCollision(0).GetNormal(), player.node, 1);
                    }
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/RunJump01/current_length")) {
                        GD.Print("end of runjump01");
                        PlayerChangeAnimation("Fall");
                    }
                    if (player.isOnGround) {
                        if (InputIsPressed(ref inputState.action3)) {
                            Vector3 toLedge = Vector3.Zero;
                            if (PlayerCanLeap(ref toLedge, 0)) {
                                PlayerChangeAnimation(PlayerMatchLeapAnimation(toLedge, currentAnimation));
                                break;
                            }
                        }
                    if (playerForward.Dot(player.wishDirection) > 0.2f) {
                            PlayerChangeAnimationEx("Roll", true, false);
                            float strength = 0.3f;
                            PlayerShakeCamera(strength);
                            PlaySoundUI(rumbleSFX, strength * 0.2f, globalSlightPitchVaration, false);
                        } else {
                            PlayerChangeAnimation("FallToIdle");
                        }
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    break;
                }
                case "RunJump02": {
                    if (!isAnimationSameAsPrevious) {
                        player.node.Velocity += Vector3.Up * PLAYER_JUMP_STRENGTH;
                        break;
                    }
                    RotateTowards(player.node.Velocity, player.node, 0.2f);
                    if (player.isOnGround) {
                        PlayerChangeAnimationEx("Roll", true, false);
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    break;
                }
                case "Slide": {
                    if (!isAnimationSameAsPrevious) {
                        PlaySoundUI(GD.Load<AudioStream>("res://assets/sound/slide01.wav"), 0.2f, globalSlightPitchVaration, false);
                        player.node.Velocity = velocityLength <= PLAYER_RUN_SPEED ? 
                            playerForward * PLAYER_RUN_SPEED : 
                            player.node.Velocity;
                        player.node.Velocity += Vector3.Up * PLAYER_JUMP_STRENGTH/2f;
                        break;
                    }
                    if (player.isOnGround) {
                        player.node.Velocity = player.node.Velocity.Lerp(Vector3.Zero, 0.02f);
                    } else {        
                        player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    }
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Slide/current_length")) {
                        PlayerChangeAnimation("Move");
                    }
                    const float landingTimeStamp = 0.33f;
                    const float standingTimeStamp = 0.8f;
                    const float shootingTimeStamp = standingTimeStamp;
                    float rotationSpeed = player.targets[0] == null ? 0.1f : 0.33f;
                    RotateTowards(directionToTarget, player.node, rotationSpeed);
                    if (InputIsJustPressed(ref inputState.attack)) {
                        if (!player.hasShotDuringThisAnimation) {
                            RotateTowards(directionToTarget, player.node, 1);
                            PlayerShoot(1);
                            player.hasShotDuringThisAnimation = true;
                        } else {
                            Node3D potentialTarget = PlayerTargetIndicatorUpdate();
                            bool isTargetValid = 
                                potentialTarget != null &&
                                potentialTarget != player.node && 
                                potentialTarget.GetType() != typeof(AudioStreamPlayer3D);
                            if (isTargetValid) {
                                player.targets[0] = potentialTarget;
                                player.targetCount = 1;
                            } else {
                                PlayerChangeAnimation("Fire01");
                            }
                        }
                    }
                    if (player.targetCount > 0 && player.hasShotDuringThisAnimation) {
                        PlayerChangeAnimation("Fire01");
                    }
                    if (HasCrossedPlaybackPosition(previousPlaybackPosition, currentPlaybackPosition, shootingTimeStamp)) {
                        if (!player.isOnGround) {
                            PlayerChangeAnimationEx("Fall", true, false);
                            player.hasShotDuringThisAnimation = false;
                        } else if (!player.hasShotDuringThisAnimation && player.targets[0] != null) {
                            PlayerShoot(1);
                        }
                    }
                    if (currentPlaybackPosition > shootingTimeStamp && InputIsJustPressed(ref inputState.attack)) {
                        Node3D potentialTarget = PlayerTargetIndicatorUpdate();
                        bool isTargetValid = 
                            potentialTarget != null &&
                            potentialTarget != player.node && 
                            potentialTarget.GetType() != typeof(AudioStreamPlayer3D);
                        if (isTargetValid) {
                            player.targets[0] = potentialTarget;
                            player.targetCount = 1;
                        }
                    }
                    if (currentPlaybackPosition >= standingTimeStamp && velocityLengthFlat <= PLAYER_RUN_SPEED/2) {
                        player.node.Velocity += rootVelocityXZ;
                    }
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
                        GD.Print("Roll end");
                        if (inputDirection != Vector2.Zero) { 
                            PlayerChangeAnimation("Move");
                        }
                    }
                    if (currentPlaybackPosition >= (float)player.animationTree.Get("parameters/Roll/current_length")) {
                        PlayerChangeAnimation("Move");
                    }
                    if (!player.isOnGround) { PlayerChangeAnimation("Fall"); }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    break;
                }
                case "Fall": {
                    if (player.isOnGround) {
                        if ((player.node.Velocity * Y_FLAT).Length() > 6f) {
                            PlayerChangeAnimationEx("Roll", true, false);
                        } else {
                            PlayerChangeAnimation("FallToIdle");
                        }
                    }
                    player.node.Velocity += GRAVITY_VECTOR * globalPhysicsDeltaFloat;
                    player.node.Velocity = new Vector3(
                        Mathf.Lerp(player.node.Velocity.X, wishDirectionOrVelocity.X * PLAYER_AIR_SPEED, 0.02f),
                        player.node.Velocity.Y,
                        Mathf.Lerp(player.node.Velocity.Z, wishDirectionOrVelocity.Z * PLAYER_AIR_SPEED, 0.02f)
                    );
                    break;
                }
                case "FallToIdle": {
                    if ((float)player.animationTree.Get("parameters/FallToIdle/FallToIdleBlend/blend_amount") < 0.05f) {
                        player.animationTree.Set("parameters/FallToIdle/FallToIdleBlend/blend_amount", 0.05f);
                        PlaySoundUI(metalFootstepSFX, 0.4f, globalSlightPitchVaration, true);
                    }
                    int FallToIdleBlockIndex = GetPlaybackBlockIndex("FallToIdle");
                    if (hasMovementInput) {
                        PlayerChangeAnimation("Move");
                        RotateTowards(player.wishDirection, player.node, 0.1f);
                    }
                    if (player.animationPlaybackBlocks[FallToIdleBlockIndex].currentPlaybackPosition >= (float)player.animationTree.Get("parameters/FallToIdle/FallToIdle/current_length")) {
                        PlayerChangeAnimation("Move");
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
                        PlayerChangeAnimationEx("Fall", true, false);
                        OrbReturn(false);
                    }
                    if (currentPlaybackPosition > 0.8f && currentPlaybackPosition < 1f && InputIsJustPressed(ref inputState.action3)) {
                        PlayerChangeAnimationEx("Jump", true, false);
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
                        PlayerChangeAnimation("OrbIdle");
                    }
                    player.node.Velocity = new Vector3(rootVelocity.X, rootVelocity.Y, rootVelocity.Z);
                    if (player.targetAnimation.name != "TeleportShoot" && player.targetAnimation.name != "OrbIdle") {
                        OrbReturn(false);
                    }
                    break;
                }
                case "OrbIdle": {
                    if (!isAnimationSameAsPrevious) {
                        break;
                    }
                    if (!player.orb.node.TopLevel) {
                        PlayerChangeAnimation(player.isOnGround ? "Move" : "Fall");
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
            bool shouldChangeAnimation = player.animationState.GetCurrentNode() != player.targetAnimation.name;
            if (shouldChangeAnimation && !isInTransition) {
                player.hasShotDuringThisAnimation = false;
                for (int i = 0; i < player.animationPlaybackBlocks.Length; i++) {
                    player.animationPlaybackBlocks[i].currentPlaybackPosition = 0f;
                }
                switch (player.targetAnimation.name) {
                    case "Move": {
                        if (!hasMovementInput) { 
                            player.animationTree.Set("parameters/Move/RunBlend/blend_amount", 0.0f);
                        }
                        break;
                    }
                    case "FallToIdle": {
                        float impactSpeed = -player.node.Velocity.Y;
                        float animationScale = Mathf.Clamp(impactSpeed / 12.0f, 0.5f, 2.0f);
                        float animationSpeed = Mathf.Clamp(2.0f - (impactSpeed / 10.0f), 0.5f, 2f);
                        player.animationTree.Set("parameters/FallToIdle/FallToIdleTimeSeek/seek_request", 0.0f);
                        player.animationTree.Set("parameters/FallToIdle/FallToIdleBlend/blend_amount", animationScale);
                        player.animationTree.Set("parameters/FallToIdle/FallToIdleTimeScale/scale", animationSpeed);
                        PlayerShakeCamera(impactSpeed * 0.05f);
                        PlaySoundUI(rumbleSFX, impactSpeed * 0.01f, globalSlightPitchVaration, false);
                        break;
                    }
                }
                switch (player.animationState.GetCurrentNode()) {
                    case "Move": {
                        if (!player.orb.node.TopLevel) { player.orb.node.Visible = false; }
                        break;
                    }
                }
                if (player.targetAnimation.shouldChangeImmediately) {
                    GD.Print(player.targetAnimation.name + " start  from " + player.animationState.GetCurrentNode());
                    player.animationState.Start(player.targetAnimation.name, true);
                } else {
                    GD.Print(player.targetAnimation.name + " travel from " + player.animationState.GetCurrentNode());
                    player.animationState.Travel(player.targetAnimation.name);
                }
                player.currentAnimationName = player.targetAnimation.name;
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
                if (Mathf.Abs(playerPosition.X) > MAX_ENTITY_DISTANCE ||
                    Mathf.Abs(playerPosition.Y) > MAX_ENTITY_DISTANCE ||
                    Mathf.Abs(playerPosition.Z) > MAX_ENTITY_DISTANCE) {
                    PlayerTeleportTo(Vector3.Zero, playerCamera);
                }
            }
            PlayerTargetIndicatorUpdate();
        }
        public void PlayerCameraUpdate(ref PlayerCamera inputCamera) {
            if (inputCamera.node == null) { return; }
            Vector3 cameraForward = -inputCamera.node.GlobalTransform.Basis.Z.Normalized();
            float isTurningThreshold = 30f;
            bool isTurning = Mathf.Abs(inputCamera.targetAngle - inputCamera.angle) > isTurningThreshold;
            if (Input.IsActionJustPressed("cameraRight") || Input.IsActionJustPressed("cameraLeft")) {
                float x = Input.GetActionStrength("cameraRight") -Input.GetActionStrength("cameraLeft");
                // if (isTurning) { 
                //     inputCamera.angle = inputCamera.targetAngle;
                // } else {
                //     inputCamera.targetAngle += x * 90f; 
                // }
                inputCamera.targetAngle += x * 90f; 
            }
            globalRayCastExceptions[0] = player.node;
            bool isOnGroundHit = RayCast(
                player.node.GlobalPosition + Vector3.Up,
                player.node.GlobalPosition + Vector3.Down * 500f,
                LAYER_WORLD_STATIC
            );
            float targetHeight =
                DEFAULT_CAMERA_HEIGHT +
                (isOnGroundHit ?
                    Mathf.Max(0f, player.node.GlobalPosition.Y - globalHitInfo.Position.Y) * 0.5f :
                    8f
                );
            inputCamera.offsetHeight = Mathf.Lerp(inputCamera.offsetHeight, targetHeight, 0.1f);
            inputCamera.targetAngle = Mathf.PosMod(inputCamera.targetAngle, 360f);
            float angleDifference = Mathf.PosMod(inputCamera.targetAngle - inputCamera.angle + 180f, 360f) - 180f;
            inputCamera.angle += angleDifference * inputCamera.rotationLerpSpeed;
            if (Mathf.Abs(inputCamera.targetAngle - inputCamera.angle) < ALMOST_ZERO) {
                inputCamera.angle = inputCamera.targetAngle;
            }
            float cameraAngleRadians = Mathf.DegToRad(inputCamera.angle);
            Vector3 offsetDirection = new Vector3(Mathf.Sin(cameraAngleRadians), 0, Mathf.Cos(cameraAngleRadians));
            Vector3 pivot = inputCamera.targetPosition.Lerp(player.orb.node.GlobalPosition, 0.3f);
            Vector3 desiredCameraPosition =
                pivot +
                (offsetDirection * inputCamera.offsetDistance) +
                new Vector3(0, inputCamera.offsetHeight, 0);
            globalRayCastExceptions[0] = player.node;
            bool wallHit = RayCast(pivot, desiredCameraPosition, LAYER_WORLD_STATIC);
            inputCamera.node.GlobalPosition = wallHit ?
                globalHitInfo.Position + globalHitInfo.Normal * 0.1f :
                desiredCameraPosition;
            inputCamera.targetPosition = pivot;
            inputCamera.node.LookAt(inputCamera.targetPosition);
            if (inputCamera.shakeAmount > 0f) {
                Vector3 shakeOffset = new Vector3(
                    (float)GD.RandRange(-inputCamera.shakeAmount, inputCamera.shakeAmount),
                    (float)GD.RandRange(-inputCamera.shakeAmount, inputCamera.shakeAmount),
                    (float)GD.RandRange(-inputCamera.shakeAmount, inputCamera.shakeAmount)
                ) * inputCamera.shakeAmount;
                inputCamera.node.GlobalPosition += shakeOffset;
                float shakePitch = Mathf.DegToRad((float)GD.RandRange(-inputCamera.shakeAmount, inputCamera.shakeAmount) * inputCamera.shakeAmount);
                inputCamera.node.RotateObjectLocal(Vector3.Right, shakePitch);
            }
            if (inputCamera.shakeAmount > 0f) {
                inputCamera.shakeAmount = Mathf.Lerp(inputCamera.shakeAmount, 0f, globalPhysicsDeltaFloat);
            }
            float speed = player.node.Velocity.LengthSquared();
            inputCamera.node.Fov = Mathf.Lerp(inputCamera.node.Fov, 70 + Mathf.Min(speed * 0.2f, 30f), 0.02f);
        }
    }
}