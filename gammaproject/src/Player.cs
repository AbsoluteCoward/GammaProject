using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct Player {
            public CharacterBody3D node;
            public AnimationPlayer animationPlayer;
            public AnimationTree animationTree;
            public AnimationNodeStateMachinePlayback animationState;
            public Skeleton3D skeleton;
            public int headBoneIndex;
            public int chestBoneIndex;
            public bool animationCanMove;
            public Vector3 wishDirection;
            public float moveSpeed;
            public bool isOnGround;
            public float turnAnticipation;
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
            player.node = inputPlayerNode;
            player.animationPlayer = inputPlayerNode.GetChild<Node3D>(1).GetChild<AnimationPlayer>(1);
            player.animationTree = inputPlayerNode.GetChild<AnimationTree>(2);
            player.animationState = (AnimationNodeStateMachinePlayback)player.animationTree.Get("parameters/playback");
            player.skeleton = inputPlayerNode.GetChild<Node3D>(1).GetChild<Node3D>(0).GetChild<Skeleton3D>(0);
            for (int i = 0; i < player.skeleton.GetBoneCount(); i++) {
                if (player.skeleton.GetBoneName(i) == "Head") {
                    player.headBoneIndex = i;
                }
                if (player.skeleton.GetBoneName(i) == "Chest") {
                    player.chestBoneIndex = i;
                }
            }
            player.moveSpeed = 2.0f;
            player.wishDirection = Vector3.Zero;
            player.turnAnticipation = 0f;
        }
        public void PlayerUpdate() {
            player.isOnGround = player.node.IsOnFloor();
            Camera3D currentCamera = GetViewport().GetCamera3D();
            Vector3 playerForward = -player.node.Transform.Basis.Z.Normalized();
            player.wishDirection = currentCamera.GlobalTransform.Basis.Z.Normalized() * inputDirection.Y + currentCamera.GlobalTransform.Basis.X.Normalized() * inputDirection.X;
            player.wishDirection = new Vector3(player.wishDirection.X, 0, player.wishDirection.Z).Normalized();
            Vector3 rootPosition = player.animationTree.GetRootMotionPosition();
            Vector3 rootVelocity = player.node.Transform.Basis * rootPosition / (float)globalDelta;
            if (player.wishDirection.Length() > 0.1f && player.isOnGround) {
                player.animationState.Travel("Walk");
                float targetAngle = Mathf.Atan2(playerForward.Cross(player.wishDirection).Y, playerForward.Dot(player.wishDirection));
                player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, targetAngle, 0.2f);
                RotateTowards(player.wishDirection, player.node, 0.2f);
            } else {
                player.animationState.Travel("Idle");
                player.turnAnticipation = Mathf.Lerp(player.turnAnticipation, 0f, 0.15f);
            }
            if (Input.IsActionJustPressed("Teleport")) {
                player.node.GlobalPosition += player.wishDirection * 5f;
                player.node.Velocity = player.wishDirection * player.node.Velocity.Length();
            }
            if (!player.isOnGround) {
                player.node.Velocity += Vector3.Down * 9.8f * (float)globalDelta;
            }
            player.node.Velocity = new Vector3(rootVelocity.X, player.node.Velocity.Y, rootVelocity.Z);
            player.node.MoveAndSlide();
            ApplyProceduralBoneRotation();
            if (timeSinceSceneLoad % 1.0f < globalDelta) {
                GD.Print("chestpose: " + player.skeleton.GetBoneGlobalPose(player.chestBoneIndex));
                GD.Print("anticipation: " + player.turnAnticipation);
            }
        }
        public void ApplyProceduralBoneRotation() {
            if (player.skeleton == null) return;
            float chestTurnAmount = 1f;
            float chestRotation = player.turnAnticipation * chestTurnAmount;
            Transform3D chestPose = DEFAULT_SLINK_CHEST_POSE;
            Quaternion chestTwist = new Quaternion(Vector3.Up, chestRotation);
            chestPose.Basis = chestPose.Basis * new Basis(chestTwist);
            player.skeleton.SetBoneGlobalPoseOverride(player.chestBoneIndex, chestPose, 1.0f, true);
        }
        public void PlayerCameraInitialize(Camera3D inputCamera) {
            playerCamera.node = inputCamera;
            playerCamera.WallRayCast = inputCamera.GetChild<RayCast3D>(0);
            playerCamera.SpotLight = inputCamera.GetChild<SpotLight3D>(1);
            playerCamera.WallRayCast.TopLevel = true;
            playerCamera.WallRayCast.AddException(player.node);
            playerCamera.node.Far = 1000.0f;
            playerCamera.SpotLight.SpotRange = 120;
            playerCamera.SpotLight.SpotAngle = 120;
            playerCamera.SpotLight.LightEnergy = GetTree().CurrentScene.Name == "Prison" ? 0f : 2f;
            playerCamera.offsetDistance = Mathf.Pi;
            playerCamera.offsetHeight = Mathf.Sqrt(5);
            playerCamera.node.Fov = 75;
            playerCamera.maxLerpDistance = 200f;
            playerCamera.rotationLerpSpeed = 0.3f;
            playerCamera.positionLerpSpeed = 0.1f;
        }
        public void PlayerCameraUpdate(ref PlayerCamera inputCamera) {
            if (inputCamera.node == null) { return; }
            if (Input.IsActionJustPressed("cameraRight")) {
                GD.Print("Camera Right Pressed");
                inputCamera.targetAngle -= 90f;
            } else if (Input.IsActionJustPressed("cameraLeft")) {
                GD.Print("Camera Left Pressed");
                inputCamera.targetAngle += 90f;
            }
            inputCamera.targetAngle = Mathf.PosMod(inputCamera.targetAngle, 360f);
            float angleDifference = Mathf.PosMod(inputCamera.targetAngle - inputCamera.angle + 180f, 360f) - 180f;
            inputCamera.angle += angleDifference * inputCamera.rotationLerpSpeed;
            float cameraAngleRadians = Mathf.DegToRad(inputCamera.angle);
            Vector3 offsetDirection = new Vector3(Mathf.Sin(cameraAngleRadians), 0, Mathf.Cos(cameraAngleRadians));
            inputCamera.WallRayCast.TargetPosition = inputCamera.WallRayCast.ToLocal(inputCamera.WallRayCast.GlobalPosition + (offsetDirection * inputCamera.offsetDistance) + new Vector3(0, inputCamera.offsetHeight, 0));
            inputCamera.WallRayCast.GlobalPosition = player.node.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild<MeshInstance3D>(0).GlobalPosition + VECTOR3_DEFAULT_UPWARD_CAMERA_OFFSET;
            inputCamera.node.GlobalPosition = inputCamera.WallRayCast.IsColliding() ?
                inputCamera.node.GlobalPosition.Lerp(inputCamera.WallRayCast.GetCollisionPoint(), inputCamera.positionLerpSpeed)
                : inputCamera.node.GlobalPosition.Lerp(inputCamera.WallRayCast.ToGlobal(inputCamera.WallRayCast.TargetPosition), inputCamera.positionLerpSpeed);
            inputCamera.targetPosition = inputCamera.targetPosition.Lerp(inputCamera.WallRayCast.GlobalPosition, inputCamera.rotationLerpSpeed);
            inputCamera.node.LookAt(inputCamera.targetPosition);
        }
    }
}