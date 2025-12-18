using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct TeleportEntity {
            public CharacterBody3D node;
            public SpotLight3D light;
            public RayCast3D topRayCast;
            public Node3D teleportShadowMesh;
        }
        public struct Player {
            public TeleportEntity teleportEntity;
            public Vector3 wishDirection;
            public CharacterBody3D node;
            public Node3D gunBarrel;
            public MeshInstance3D headMesh;
            public AnimationPlayer animationPlayer;
            public AnimationTree animationTree;
            public AnimationNodeStateMachinePlayback animationState;
            public Skeleton3D skeleton;
            public string previousAnimationName;
            public float moveSpeed;
            public float turnAnticipation;
            public float maxTeleportDistance;
            public float previousAnimationPlaybackPosition;
            public int chestBoneIndex;
            public int targetCount;
            public bool isOnGround;
            public bool shouldBeAsleep;
            public bool hasPlayerModelCopy;
            public bool isTeleporting;
            public Node3D[] targets;
            public static float maxDistance = 1000f;
            public static int meatCount = 0;
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
            public float positionLerpSpeed;
        }
        public Player player;
        public PlayerCamera playerCamera;
        public void PlayerInitialize(CharacterBody3D inputPlayerNode) {
            player.wishDirection = Vector3.Zero;
            player.node = inputPlayerNode;
            player.gunBarrel = player.node.GetNode<MeshInstance3D>("Slink/metarig/Skeleton3D/Gun_2/Gun_2");
            player.headMesh = player.node.GetNode<MeshInstance3D>("Slink/metarig/Skeleton3D/Head/Head");
            player.teleportEntity.node = inputPlayerNode.GetChild<CharacterBody3D>(4);
            player.teleportEntity.light = player.teleportEntity.node.GetChild<SpotLight3D>(0);
            player.teleportEntity.topRayCast = player.teleportEntity.node.GetChild<RayCast3D>(1);
            player.targets = new Node3D[16];
            player.targetCount = 0;
            player.animationPlayer = inputPlayerNode.GetChild<Node3D>(2).GetChild<AnimationPlayer>(1);
            player.animationTree = inputPlayerNode.GetChild<AnimationTree>(3);
            player.animationTree.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Physics;
            player.animationPlayer.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Physics;
            player.animationState = (AnimationNodeStateMachinePlayback)player.animationTree.Get("parameters/playback");
            player.skeleton = inputPlayerNode.GetChild<Node3D>(2).GetChild<Node3D>(0).GetChild<Skeleton3D>(0);
            for (int i = 0; i < player.skeleton.GetBoneCount(); i++) { if (player.skeleton.GetBoneName(i) == "Abdomen") { player.chestBoneIndex = i; } }
            if (player.chestBoneIndex == -1) { GD.PrintErr("Couldn't find chest bone!"); }
            player.moveSpeed = 8.0f;
            player.turnAnticipation = 0f;
            player.previousAnimationPlaybackPosition = 0f;
            player.previousAnimationName = "";
            player.node.FloorSnapLength = 0.8f;
            player.teleportEntity.light.LightEnergy = 0;
            player.hasPlayerModelCopy = false;
            player.isTeleporting = false;
            GD.Print("Player Initialized");
        }
        public void ApplyDynamicBoneTransformations() {
            if (player.skeleton == null) { return; }
            float chestRotation = player.turnAnticipation;
            Transform3D pelvisPose = player.skeleton.GetBoneGlobalPose(1);
            Transform3D chestPose = DEFAULT_SLINK_WALK_CHEST_POSE;
            chestPose.Origin = new Vector3(
                chestPose.Origin.X,
                chestPose.Origin.Y + (pelvisPose.Origin.Y - 1.184f),
                chestPose.Origin.Z
            );
            Vector3 chestRotationAxis = player.skeleton.GetBoneGlobalRest(player.chestBoneIndex).Basis.Y;
            Quaternion chestTwist = new Quaternion(chestRotationAxis, chestRotation);
            chestPose.Basis = chestPose.Basis * new Basis(chestTwist);
            player.skeleton.SetBoneGlobalPose(player.chestBoneIndex, chestPose);
        }
        public void PlayerCameraInitialize(Camera3D inputCamera) {
            playerCamera.node = inputCamera;
            playerCamera.WallRayCast = inputCamera.GetChild<RayCast3D>(0);
            playerCamera.SpotLight = inputCamera.GetChild<SpotLight3D>(1);
            playerCamera.WallRayCast.TopLevel = true;
            playerCamera.WallRayCast.AddException(player.node);
            playerCamera.WallRayCast.CollisionMask = 2;
            playerCamera.node.Far = 1000.0f;
            playerCamera.SpotLight.SpotRange = 120;
            playerCamera.SpotLight.SpotAngle = 120;
            playerCamera.SpotLight.LightEnergy = GetTree().CurrentScene.Name == "Prison" ? 0f : 0.5f;
            playerCamera.offsetDistance = Mathf.Pi;
            playerCamera.offsetHeight = Mathf.Sqrt(5);
            playerCamera.node.Fov = 75;
            playerCamera.maxLerpDistance = 200f;
            playerCamera.rotationLerpSpeed = 0.6f;
            playerCamera.positionLerpSpeed = 0.1f;
            GD.Print("Player Camera Initialized");
        }
        public void CreatePlayerModelCopy() {
            if (player.hasPlayerModelCopy) { return; }
            Node3D originalModel = player.node.GetChild<Node3D>(2);
            Node3D modelCopy = (Node3D)originalModel.Duplicate();
            player.teleportEntity.node.AddChild(modelCopy);
            int maxStackSize = 256;
            Node[] stack = new Node[maxStackSize];
            int stackSize = 0;
            stack[stackSize++] = modelCopy;
            while (stackSize > 0) {
                Node currentNode = stack[--stackSize];
                if (currentNode.GetType() == typeof(MeshInstance3D)) {
                    MeshInstance3D mesh = (MeshInstance3D)currentNode;
                    mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
                }
                int childCount = currentNode.GetChildCount();
                for (int i = 0; i < childCount; i++) {
                    if (stackSize >= maxStackSize) {
                        GD.PrintErr("CreatePlayerModelCopy: Stack overflow");
                        return;
                    }
                    stack[stackSize++] = currentNode.GetChild(i);
                }
            }
            player.teleportEntity.teleportShadowMesh = modelCopy;
            player.hasPlayerModelCopy = true;
        }
        public void RemovePlayerModelCopy() {
            if (!player.hasPlayerModelCopy) return;
            if (player.teleportEntity.teleportShadowMesh != null) {
                player.teleportEntity.teleportShadowMesh.QueueFree();
                player.teleportEntity.teleportShadowMesh = null;
            }
            player.hasPlayerModelCopy = false;
        }
        public void PlayerUpdate() {
            if (sceneState.physicsFramesSinceSceneLoad % (int)GD.RandRange(20f, 100f) == 0) {
                Vector3 playerPosition = player.node.GlobalPosition;
                if (Mathf.Abs(playerPosition.X) > Player.maxDistance ||
                    Mathf.Abs(playerPosition.Y) > Player.maxDistance ||
                    Mathf.Abs(playerPosition.Z) > Player.maxDistance) {
                    player.node.GlobalPosition = Vector3.Zero;
                }
            }
            player.isOnGround = player.node.IsOnFloor();
            Camera3D currentCamera = GetViewport().GetCamera3D();
            Vector3 playerForward = -player.node.Transform.Basis.Z.Normalized();
            player.wishDirection =
                (currentCamera.GlobalTransform.Basis.Z.Normalized() * inputDirection.Y + currentCamera.GlobalTransform.Basis.X.Normalized() * inputDirection.X) *
                Y_FLAT;
            Vector3 rootPosition = player.animationTree.GetRootMotionPosition();
            Vector3 rootVelocity = player.node.Transform.Basis * rootPosition;
            rootVelocity *= player.moveSpeed;
            string targetAnimation = player.animationState.GetCurrentNode();
            bool isAnimationSameAsPrevious = player.animationState.GetCurrentNode() == player.previousAnimationName;
            bool action3JustPressed = Input.IsActionJustPressed("action3") && !inputState.action3.isConsumed;
            bool action3Pressed = Input.IsActionPressed("action3");
            bool action3JustReleased = Input.IsActionJustReleased("action3");
            bool hasMovementInput = player.wishDirection.Length() > 0.1f;
            if (player.animationState.GetCurrentNode() == "") { return; }
            float currentAnimationPlaybackPosition = player.animationState.GetCurrentPlayPosition();
            bool isIdleAndStill = player.animationState.GetCurrentNode() == "Idle" && !hasMovementInput;
            bool shouldStartJump = action3JustPressed && isIdleAndStill;
            bool shouldStartTeleport = action3JustPressed && !isIdleAndStill;
            switch (player.animationState.GetCurrentNode()) {
                case "Idle":
                    if (hasMovementInput) { targetAnimation = "Walk"; }
                    if (!player.isOnGround) { targetAnimation = "Fall"; }
                    if (shouldStartJump) {
                        targetAnimation = "Jump";
                        inputState.action3.isConsumed = true;
                    }
                    player.node.Velocity = player.node.Velocity.Lerp(Vector3.Zero, 0.2f);
                    player.node.Velocity += Vector3.Down * 9.8f * (float)globalDelta;
                    break;
                case "Walk":
                    if (!hasMovementInput && !player.isTeleporting) { targetAnimation = "Idle"; }
                    if (!player.isOnGround) { targetAnimation = "Fall"; }
                    if (Input.IsActionPressed("action2") && player.wishDirection.Length() < 0.1f) { targetAnimation = "Jump"; }
                    if (HasCrossedPlaybackPosition(player.previousAnimationPlaybackPosition, currentAnimationPlaybackPosition, 0.33f) ||
                        HasCrossedPlaybackPosition(player.previousAnimationPlaybackPosition, currentAnimationPlaybackPosition, 1.54f)) {
                        if (player.isOnGround) {
                            PlayAudio3D(footStepMetalSFX, player.node.GlobalPosition, 0.1f, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), true);
                        }
                    }
                    Vector3 direction = player.isTeleporting ? playerForward : player.wishDirection;
                    float targetAngle = Mathf.Atan2(playerForward.Cross(direction).Y, playerForward.Dot(direction));
                    player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, targetAngle, 0.2f);
                    RotateTowards(direction, player.node, 0.2f);
                    player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
                    player.node.Velocity += Vector3.Down * 9.8f * (float)globalDelta;
                    break;
                case "Jump":
                    bool shouldJump =
                        HasCrossedPlaybackPosition(
                            inputPreviousPosition: player.previousAnimationPlaybackPosition,
                            inputCurrentPosition: currentAnimationPlaybackPosition,
                            inputEventPosition: 0.66f
                        ) &&
                        player.isOnGround &&
                        isAnimationSameAsPrevious;
                    if (shouldJump) {
                        player.node.Velocity += Vector3.Up * 12f;
                        player.node.Velocity += player.wishDirection.Length() > 0.1f ? player.wishDirection * 2f : player.node.Transform.Basis.Z.Normalized() * 2f;
                    }
                    player.node.Velocity += Vector3.Down * 9.8f * (float)globalDelta;
                    break;
                case "Fall":
                    if (player.isOnGround) { targetAnimation = "FallToIdle"; }
                    player.node.Velocity += Vector3.Down * 9.8f * (float)globalDelta;
                    player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, 0f, 0.1f);
                    break;
                case "FallToIdle":
                    if (HasCrossedPlaybackPosition(player.previousAnimationPlaybackPosition, currentAnimationPlaybackPosition, 0.22f)) {
                        if (player.isOnGround) {
                            PlayAudio3D(footStepMetalSFX, player.node.GlobalPosition, 0.4f, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), true);
                        }
                    }
                    player.node.Velocity = player.node.Velocity.Lerp(Vector3.Zero, 0.08f);
                    player.node.Velocity += Vector3.Down * 9.8f * (float)globalDelta;
                    player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, 0f, 0.1f);
                    break;
            }
            player.previousAnimationPlaybackPosition = currentAnimationPlaybackPosition;
            player.previousAnimationName = player.animationState.GetCurrentNode();
            bool shouldChangeAnimation = player.animationState.GetCurrentNode() != targetAnimation;
            bool isInTransition = player.animationState.GetTravelPath().Count > 0;
            if (shouldChangeAnimation && !isInTransition) {
                GD.Print("changing animation to " + targetAnimation + " from " + player.animationState.GetCurrentNode());
                player.previousAnimationPlaybackPosition = 0f;
                player.animationState.Travel(targetAnimation);
            }
            if (shouldStartTeleport) {
                player.isTeleporting = true;
                inputState.action3.isConsumed = true;
                TurnShadowsOffOrOn(player.skeleton, false);
                CreatePlayerModelCopy();
                player.teleportEntity.node.TopLevel = true;
                player.teleportEntity.node.GlobalPosition = player.node.GlobalPosition;
            }
            if (player.isTeleporting && action3Pressed) {
                Vector3 teleportDirection = hasMovementInput ? player.wishDirection : playerForward;
                player.teleportEntity.node.Velocity = teleportDirection * TELEPORTENTITY_SPEED_MODIFIER;
                player.teleportEntity.light.LightEnergy = 1f;
                player.wishDirection = playerForward;
                RotateTowards((player.teleportEntity.node.Velocity * Y_FLAT).Normalized(), player.teleportEntity.node, 0.4f);
                if (player.teleportEntity.topRayCast.IsColliding()) {
                    Vector3 collisionPoint = player.teleportEntity.topRayCast.GetCollisionPoint();
                    float heightDifference = Mathf.Abs(player.teleportEntity.node.GlobalPosition.Y - collisionPoint.Y);
                    bool canClimbSurface =
                        heightDifference > TELEPORTENTITY_CLIMB_MINIMUM_HEIGHT_DIFFERENCE &&
                        heightDifference < TELEPORTENTITY_CLIMB_MAXIMUM_HEIGHT_DIFFERENCE &&
                        player.teleportEntity.topRayCast.GetCollisionNormal().Dot(Vector3.Up) > TELEPORTENTITY_CLIMB_SURFACE_NORMAL_THRESHOLD;
                    bool isVerticallyCloseToCollision = player.teleportEntity.node.GlobalPosition.Y < collisionPoint.Y;
                    if (canClimbSurface || isVerticallyCloseToCollision) {
                        player.teleportEntity.node.GlobalPosition = collisionPoint + TELEPORT_VERTICAL_OFFSET;
                    }
                }
                player.teleportEntity.node.MoveAndSlide();
            } else if (player.isTeleporting && action3JustReleased) {
                player.isTeleporting = false;
                TurnShadowsOffOrOn(player.skeleton, true);
                RemovePlayerModelCopy();
                PlayAudio3D(teleportSFX, player.teleportEntity.node.GlobalPosition, 0.01f, 1.0f, false);
                player.node.GlobalPosition = player.teleportEntity.node.GlobalPosition;
                player.teleportEntity.light.LightEnergy = 0;
                player.teleportEntity.node.TopLevel = false;
            }
            if (Input.IsActionPressed("attack")) {
                if (Input.IsActionJustPressed("attack")) {
                    for (int i = 0; i < player.targets.Length; i++) { player.targets[i] = null; }
                    player.targetCount = 0;
                }
                for (int i = 0; i < enemyCount; i++) {
                    Node3D potentialTarget = enemies[i].node;
                    bool isTargetInvalid = potentialTarget == player.node || potentialTarget == player.teleportEntity.node || potentialTarget.GetType() == typeof(AudioStreamPlayer3D);
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
                PlayAudio3D(shootSFX, gunPosition, 0.1f, Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), false);
                for (int i = 0; i < player.targets.Length; i++) {
                    if (player.targets[i] == null) { continue; }
                    GD.Print("Firing at target " + player.targets[i].Name);
                    ProjectilesCreate(
                        inputStartPosition: gunPosition,
                        inputTarget: player.targets[i],
                        inputDirection:-player.node.GlobalTransform.Basis.Z,
                        inputSpeed: 15f
                    );
                }
                for (int i = 0; i < player.targets.Length; i++) { player.targets[i] = null; }
                player.targetCount = 0;
            }
            player.node.MoveAndSlide();
            ApplyDynamicBoneTransformations();
        }
        public void PlayerCameraUpdate(ref PlayerCamera inputCamera) {
            if (inputCamera.node == null) { return; }
            if (Input.IsActionJustPressed("cameraRight")) {
                inputCamera.targetAngle -= 90f;
            } else if (Input.IsActionJustPressed("cameraLeft")) {
                inputCamera.targetAngle += 90f;
            }
            inputCamera.targetAngle = Mathf.PosMod(inputCamera.targetAngle, 360f);
            float angleDifference = Mathf.PosMod(inputCamera.targetAngle - inputCamera.angle + 180f, 360f) - 180f;
            inputCamera.angle += angleDifference * inputCamera.rotationLerpSpeed;
            float cameraAngleRadians = Mathf.DegToRad(inputCamera.angle);
            Vector3 offsetDirection = new Vector3(Mathf.Sin(cameraAngleRadians), 0, Mathf.Cos(cameraAngleRadians));
            inputCamera.WallRayCast.TargetPosition = inputCamera.WallRayCast.ToLocal(
                inputCamera.WallRayCast.GlobalPosition +
                (offsetDirection * inputCamera.offsetDistance) +
                new Vector3(0, inputCamera.offsetHeight, 0)
            );
            inputCamera.WallRayCast.GlobalPosition =
                //player.node.GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild<MeshInstance3D>(0).GlobalPosition +
                player.node.GlobalPosition +
                player.skeleton.GetBoneGlobalPose(player.chestBoneIndex).Origin;
            inputCamera.node.GlobalPosition = inputCamera.WallRayCast.IsColliding() ?
                inputCamera.node.GlobalPosition.Lerp(
                    inputCamera.WallRayCast.GetCollisionPoint() + inputCamera.WallRayCast.GetCollisionNormal() * 0.1f,
                    playerCamera.positionLerpSpeed * 2) :
                inputCamera.node.GlobalPosition.Lerp(
                    inputCamera.WallRayCast.ToGlobal(inputCamera.WallRayCast.TargetPosition),
                    inputCamera.positionLerpSpeed);
            inputCamera.targetPosition = inputCamera.targetPosition.Lerp(inputCamera.WallRayCast.GlobalPosition, inputCamera.rotationLerpSpeed);
            inputCamera.node.LookAt(inputCamera.targetPosition);
        }
    }
}