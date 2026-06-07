using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct SurveillanceScanner {
            public CharacterBody3D node;
            public Skeleton3D skeleton;
            public static int lampBoneIndex;
        }
        public void SurveillanceScannerInitialize(CharacterBody3D inputNode) {
            if (surveillanceScannerCount >= surveillanceScanners.Length) {
                SurveillanceScanner[] newSurveillanceScanners = new SurveillanceScanner[surveillanceScanners.Length * ARRAY_GROWTH_FACTOR];
                for (int i = 0; i < enemies.Length; i++) {
                    newSurveillanceScanners[i] = surveillanceScanners[i];
                }
                surveillanceScanners = newSurveillanceScanners;
            }
            int index = surveillanceScannerCount;
            surveillanceScanners[index].node = inputNode;
            surveillanceScanners[index].skeleton = inputNode.GetNode<Skeleton3D>("SurveillanceScanner01/Skeleton3D");
            for (int i = 0; i < surveillanceScanners[index].skeleton.GetBoneCount(); i++) {
                if (surveillanceScanners[index].skeleton.GetBoneName(i) == "Lamp") {
                    SurveillanceScanner.lampBoneIndex = i;
                }
            }
            AnimationPlayer animationPlayer = inputNode.GetNode<AnimationPlayer>("SurveillanceScanner01/AnimationPlayer");
            animationPlayer.Play("Fly");
            surveillanceScannerCount++;
            GD.Print($"{inputNode.Name} Initialized at index {index}");
        }
        public void SurveillanceScannerUpdate() {
            SurveillanceScanner scanner = surveillanceScanners[0];
            if (scanner.node == null) { return; }
            Vector3 targetposition = 
                playerCamera.WallRayCast.TargetPosition + 
                playerCamera.WallRayCast.GlobalPosition +
                Vector3.Up * 1.4f +
                playerCamera.node.Transform.Basis.X.Normalized() * 3f;
            Vector3 toTarget = targetposition - scanner.node.GlobalPosition;
            const float speed = 4f;
            scanner.node.Velocity += toTarget * speed * globalPhysicsDeltaFloat;
            scanner.node.Velocity *= 0.9f;
            if (scanner.node.IsOnWall()) {
                scanner.node.Velocity = scanner.node.Velocity.Bounce(scanner.node.GetWallNormal());
                scanner.node.Velocity += scanner.node.GetWallNormal() * 15f;
            }
            scanner.node.MoveAndSlide();
            Transform3D lampPose = scanner.skeleton.GetBoneGlobalPose(SurveillanceScanner.lampBoneIndex);
            Vector3 globalTargetPosition = player.node.GlobalPosition + Vector3.Up;
            Vector3 skeletonLocalTargetPos = scanner.skeleton.ToLocal(globalTargetPosition);
            Vector3 localToTarget = (skeletonLocalTargetPos - lampPose.Origin).Normalized();
            if (localToTarget.LengthSquared() > ALMOST_ZERO) {
                Vector3 forward = -localToTarget.Normalized(); 
                Vector3 absoluteUp = Vector3.Up;
                if (Mathf.Abs(forward.Dot(absoluteUp)) > 0.99f) {
                    absoluteUp = Vector3.Forward; 
                }
                Vector3 right = absoluteUp.Cross(forward).Normalized();
                Vector3 up = forward.Cross(right).Normalized();
                Basis targetBasis = new Basis(right, up, forward).Orthonormalized();
                const float rotationSpeedMultiplier = 4f; 
                lampPose.Basis = lampPose.Basis.Slerp(targetBasis.Orthonormalized(), Mathf.Min(0.3f, rotationSpeedMultiplier * globalPhysicsDeltaFloat));
                scanner.skeleton.SetBoneGlobalPose(SurveillanceScanner.lampBoneIndex, lampPose);
            }
            surveillanceScanners[0] = scanner;
        }
    }
}