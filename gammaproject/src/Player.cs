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
            public Vector3 wishDirection;
            public CharacterBody3D node;
            public MeshInstance3D headMesh;
            public TeleportEntity teleportEntity;
            public AnimationPlayer animationPlayer;
            public AnimationTree animationTree;
            public AnimationNodeStateMachinePlayback animationState;
            public Skeleton3D skeleton;
            public float moveSpeed;
            public float turnAnticipation;
            public float maxTeleportDistance;
            public float previousAnimationFrame;
            public int chestBoneIndex;
            public bool wasLightActive;
            public bool canAnimationMove;
            public bool isOnGround;
            public bool shouldBeAsleep;
            public bool hasPlayerModelCopy;  // Added: Track if copy exists
            public static float maxDistance = 1000f;
            public static int meatCount = 0;
        }
        public struct PlayerCamera {
            public Camera3D node;
            public RayCast3D WallRayCast;
            public SpotLight3D SpotLight;
            public Vector3 targetPosition;
            public float offsetDistance;
            public float offsetHeight;
            public float targetAngle;
            public float angle;
            public float maxLerpDistance;
            public float rotationLerpSpeed;
            public float positionLerpSpeed;
        }
        public void PlayerInitialize(CharacterBody3D inputPlayerNode) {
            player.wishDirection = Vector3.Zero;
            player.node = inputPlayerNode;
            player.headMesh = player.node.GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild<MeshInstance3D>(0);
            player.teleportEntity.node = inputPlayerNode.GetChild<CharacterBody3D>(4);
            player.teleportEntity.light = player.teleportEntity.node.GetChild<SpotLight3D>(0);
            player.teleportEntity.topRayCast = player.teleportEntity.node.GetChild<RayCast3D>(1);
            player.animationPlayer = inputPlayerNode.GetChild<Node3D>(2).GetChild<AnimationPlayer>(1);
            player.animationTree = inputPlayerNode.GetChild<AnimationTree>(3);
            player.animationState = (AnimationNodeStateMachinePlayback)player.animationTree.Get("parameters/playback");
            player.skeleton = inputPlayerNode.GetChild<Node3D>(2).GetChild<Node3D>(0).GetChild<Skeleton3D>(0);
            for (int i = 0; i < player.skeleton.GetBoneCount(); i++) {
                string boneName = player.skeleton.GetBoneName(i);
                if (boneName == "Abdomen") { player.chestBoneIndex = i; }
            }
            player.moveSpeed = 2.0f;
            player.turnAnticipation = 0f;
            player.previousAnimationFrame = 0f;
            player.node.FloorSnapLength = 0.8f;
            player.teleportEntity.light.LightEnergy = 0;
            player.hasPlayerModelCopy = false;
        }

        public void TurnShadowsOffOrOn(Node3D inputNode, bool inputDecision) {
            int maxStackSize = 256;
            Node[] stack = new Node[maxStackSize];
            int stackSize = 0;
            stack[stackSize++] = inputNode;
            GeometryInstance3D.ShadowCastingSetting shadowSetting = inputDecision ?
                GeometryInstance3D.ShadowCastingSetting.On :
                GeometryInstance3D.ShadowCastingSetting.Off;
            while (stackSize > 0) {
                Node currentNode = stack[--stackSize];
                if (currentNode.GetType() == typeof(MeshInstance3D)) {
                    MeshInstance3D mesh = (MeshInstance3D)currentNode;
                    mesh.CastShadow = shadowSetting;
                }
                int childCount = currentNode.GetChildCount();
                for (int i = 0; i < childCount; i++) {
                    if (stackSize >= maxStackSize) {
                        GD.PrintErr("TurnShadowsOffOrOn: Stack overflow");
                        return;
                    }
                    stack[stackSize++] = currentNode.GetChild(i);
                }
            }
        }
        public void ApplyWalkLean() {
            if (player.skeleton == null) return;
            float chestRotation = player.turnAnticipation;
            Transform3D pelvisPose = player.skeleton.GetBoneGlobalPose(1);
            Transform3D chestPose = DEFAULT_SLINK_CHEST_POSE;
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
            playerCamera.SpotLight.LightEnergy = GetTree().CurrentScene.Name == "Prison" ? 0f : 2f;
            playerCamera.offsetDistance = Mathf.Pi;
            playerCamera.offsetHeight = Mathf.Sqrt(5);
            playerCamera.node.Fov = 75;
            playerCamera.maxLerpDistance = 200f;
            playerCamera.rotationLerpSpeed = 0.6f;
            playerCamera.positionLerpSpeed = 0.1f;
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
            float currentFrame = (float)Math.Round(player.animationState.GetCurrentPlayPosition(), 2);
            if (player.animationState.GetCurrentNode() == "Walk") {
                if (HasCrossedFrame(player.previousAnimationFrame, currentFrame, 0.33f) ||
                    HasCrossedFrame(player.previousAnimationFrame, currentFrame, 1.54f)) {
                    if (player.isOnGround) {
                        PlayAudio3D(footStepMetalSFX, player.node.GlobalPosition, 0.1f,
                            Mathf.Pow(2.0f, (float)GD.Randfn(0.0, 17.0f) / 1200.0f), true);
                    }
                }
                ApplyWalkLean();
            }
            player.previousAnimationFrame = currentFrame;
            player.isOnGround = player.node.IsOnFloor();
            Camera3D currentCamera = GetViewport().GetCamera3D();
            Vector3 playerForward = -player.node.Transform.Basis.Z.Normalized();
            player.wishDirection = (currentCamera.GlobalTransform.Basis.Z.Normalized() * inputDirection.Y +
                                   currentCamera.GlobalTransform.Basis.X.Normalized() * inputDirection.X)
                                   * Y_FLAT;
            Vector3 rootPosition = player.animationTree.GetRootMotionPosition();
            Vector3 rootVelocity = player.node.Transform.Basis * rootPosition / (float)globalDelta;
            bool isTeleporting = Input.IsActionPressed("Teleport");
            bool hasMovementInput = player.wishDirection.Length() > 0.1f;
            if ((hasMovementInput || isTeleporting) && player.isOnGround) {
                player.animationState.Travel("Walk");
                Vector3 direction = isTeleporting ? playerForward : player.wishDirection;
                float targetAngle = Mathf.Atan2(playerForward.Cross(direction).Y, playerForward.Dot(direction));
                player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, targetAngle, 0.2f);
                RotateTowards(direction, player.node, 0.2f);
            } else {
                player.animationState.Travel("Idle");
                player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, 0f, 0.15f);
            }
            if (isTeleporting) {
                if (Input.IsActionJustPressed("Teleport")) {
                    TurnShadowsOffOrOn(player.skeleton, false);
                    CreatePlayerModelCopy();
                    player.teleportEntity.node.TopLevel = true;
                    player.teleportEntity.node.GlobalPosition = player.node.GlobalPosition;
                }
                Vector3 teleportDirection = hasMovementInput ? player.wishDirection : playerForward;
                player.teleportEntity.node.Velocity = teleportDirection * player.moveSpeed * 4f;
                player.teleportEntity.light.LightEnergy = 1f;
                player.wishDirection = playerForward;
                RotateTowards((player.teleportEntity.node.Velocity * Y_FLAT).Normalized(), player.teleportEntity.node, 0.4f);
                if (player.teleportEntity.topRayCast.IsColliding()) {
                    Vector3 collisionPoint = player.teleportEntity.topRayCast.GetCollisionPoint();
                    float heightDifference = Mathf.Abs(player.teleportEntity.node.GlobalPosition.Y - collisionPoint.Y);
                    bool canClimbSurface = heightDifference > 0.1f && player.teleportEntity.topRayCast.GetCollisionNormal().Dot(Vector3.Up) > 0.7f;
                    bool isVerticallyCloseToCollision = player.teleportEntity.node.GlobalPosition.Y < collisionPoint.Y;
                    if (canClimbSurface || isVerticallyCloseToCollision) {
                        player.teleportEntity.node.GlobalPosition = collisionPoint + TELEPORT_VERTICAL_OFFSET;
                    }
                }
                player.teleportEntity.node.MoveAndSlide();
            } else if (Input.IsActionJustReleased("Teleport")) {
                TurnShadowsOffOrOn(player.skeleton, true);
                RemovePlayerModelCopy();
                PlayAudio3D(teleportSFX, player.teleportEntity.node.GlobalPosition, 0.01f, 1.0f, false);
                player.node.GlobalPosition = player.teleportEntity.node.GlobalPosition;
                player.teleportEntity.light.LightEnergy = 0;
                player.teleportEntity.node.TopLevel = false;
            }
            if (!player.isOnGround) player.node.Velocity += Vector3.Down * 9.8f * (float)globalDelta;
            player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
            player.node.MoveAndSlide();
        }
        public void PlayerCameraUpdate(ref PlayerCamera inputCamera) {
            if (inputCamera.node == null) { return; }
            if (Input.IsActionJustPressed("cameraRight")) {
                inputCamera.targetAngle -= 90f;
            } else if (Input.IsActionJustPressed("cameraLeft")) { inputCamera.targetAngle += 90f; }
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
                player.node.GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild<MeshInstance3D>(0).GlobalPosition +
                DEFAULT_UPWARD_CAMERA_OFFSET;
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