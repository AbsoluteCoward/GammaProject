using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct SurveillanceScanner {
            public CharacterBody3D node;
            public Skeleton3D skeleton;
            public Area3D detectionArea;
            public CollisionObject3D target;
            public Vector3 targetPosition;
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
            surveillanceScanners[index].detectionArea = inputNode.GetNode<Area3D>("Area3D");
            SphereShape3D detectionAreaShape = new SphereShape3D {
                Radius = SCANNER_AVOIDANCE_RANGE * 2f
            };
            surveillanceScanners[index].detectionArea.GetChild<CollisionShape3D>(0).Shape = detectionAreaShape;
            surveillanceScanners[index].skeleton = inputNode.GetNode<Skeleton3D>("SurveillanceScanner01/Skeleton3D");
            surveillanceScanners[index].target = null;
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
            Vector3 scannerForward = -scanner.node.Transform.Basis.Z.Normalized();
            float scannerVelocityLength = scanner.node.Velocity.Length();
            Vector3 toPlayerFlat = (player.node.GlobalPosition - scanner.node.GlobalPosition) * Y_FLAT;
            if (scanner.target == null) {
                float dotProduct = scannerForward.Dot(toPlayerFlat);
                float angleToTarget = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f));
                float angleInDegrees = Mathf.RadToDeg(angleToTarget);
                if (angleInDegrees > 66f) { return; }
                //bool hitSomething = RaycastWorldEx(globalWorld3D, scanner.node, scanner.node.GlobalPosition, player.node.GlobalPosition + Vector3.Up, out hit);
                bool hitSomething = RayCast(scanner.node.GlobalPosition, player.node.GlobalPosition + Vector3.Up, Mask2(LAYER_PLAYERS, LAYER_WORLD_STATIC));
                if (hitSomething && globalHitInfo.Collider != player.node) { return; }
                scanner.target = player.node;
            }
            Vector3 targetCenter = scanner.target.GlobalPosition + Vector3.Up * 1.5f;
            Vector3 targetOffset = 
                Vector3.Up * 3f +
                playerCamera.node.Transform.Basis.Z.Normalized() * 3f +
                playerCamera.node.Transform.Basis.X.Normalized() * 3f;
            Vector3 newPosition;
            globalRayCastExceptions[1] = scanner.node;
            globalRayCastExceptions[2] = scanner.target;
            if (RayCastEx(globalWorld3D, 1, ref globalRayCastExceptions, targetCenter, targetCenter + targetOffset, out globalHitInfo)) {
                newPosition = globalHitInfo.Position + globalHitInfo.Normal;
            } else {
                newPosition = targetCenter + targetOffset;
            }
            scanner.targetPosition = newPosition;
            bool canSeeTargetPosition = !RaycastWorldEx(globalWorld3D, scanner.node, scanner.node.GlobalPosition, scanner.targetPosition, out _);
            bool canSeeTargetNode = 
                RayCast(scanner.node.GlobalPosition, targetCenter, Mask(LAYER_WORLD_STATIC)) &&
                globalHitInfo.Collider == scanner.target;
            if (!canSeeTargetPosition && canSeeTargetNode) {
                scanner.targetPosition = targetCenter;
                GD.Print("Can't see target position");
            }
            if (!canSeeTargetPosition && !canSeeTargetNode) {
                for (int i = 0; i < 24; i++) {
                    float randomRange = (float)GD.RandRange(2f, 12f);
                    Vector3 randomDirection = new Vector3(
                        (float)GD.RandRange(-1f, 1f),
                        (float)GD.RandRange(-1f, 1f),
                        (float)GD.RandRange(-1f, 1f)
                    );
                    Vector3 rayEnd = scanner.node.GlobalPosition + randomDirection * randomRange;
                    Vector3 searchPosition;
                    if (RayCast(scanner.node.GlobalPosition, rayEnd, Mask(LAYER_WORLD_STATIC))) {
                        searchPosition = globalHitInfo.Position;
                    } else {
                        searchPosition = rayEnd;
                    }
                    bool canSearchPositionSeeTargetPosition = !RayCast(searchPosition, scanner.targetPosition, Mask(LAYER_WORLD_STATIC));
                    bool canSearchPositionSeeTargetNode = 
                        RayCast(searchPosition, targetCenter, Mask(LAYER_WORLD_STATIC)) &&
                        globalHitInfo.Collider == scanner.target;
                    if (canSearchPositionSeeTargetNode || canSearchPositionSeeTargetPosition) {
                        scanner.targetPosition = searchPosition;
                        break;
                    }
                }
            }
            Vector3 directionToTarget = (scanner.targetPosition - scanner.node.GlobalPosition).Normalized();
            Vector3 toTarget = scanner.targetPosition - scanner.node.GlobalPosition;
            //TODO: this only makes it move away from the average normal of the whole body
            Vector3 averageAreaNormal = Vector3.Zero;
            int areaHitCount = 0;
            const float SCANNER_COLLISION_RADIUS = 0.3f;
            if (directionToTarget.LengthSquared() > ALMOST_ZERO) {
                Vector3 temp = Mathf.Abs(directionToTarget.Y) > ALMOST_ONE ? Vector3.Forward : Vector3.Up;
                Vector3 right = temp.Cross(directionToTarget).Normalized();
                Vector3 up = directionToTarget.Cross(right).Normalized();
                Vector3[] offsets = new Vector3[] {
                    right * SCANNER_COLLISION_RADIUS,
                    -right * SCANNER_COLLISION_RADIUS,
                    up * SCANNER_COLLISION_RADIUS,
                    -up * SCANNER_COLLISION_RADIUS,
                    Vector3.Zero,
                    Vector3.Down,
                    Vector3.Up
                };
                for (int i = 0; i < offsets.Length; i++) {
                    Vector3 rayStart = scanner.node.GlobalPosition + offsets[i];
                    Vector3 rayEnd = rayStart + directionToTarget * SCANNER_AVOIDANCE_RANGE;
                    if (RayCast(rayStart, rayEnd, Mask(LAYER_WORLD_STATIC))) {
                        if (globalHitInfo.Collider == scanner.target) { continue; }
                        averageAreaNormal += globalHitInfo.Normal;
                        areaHitCount++;
                    }
                }
            }
            if (areaHitCount > 0) {
                averageAreaNormal /= areaHitCount;
                const float AVOIDANCE_STRENGTH = 1f;
                scanner.node.Velocity += averageAreaNormal.Normalized() * AVOIDANCE_STRENGTH;
            }
            const float SCANNER_SPEED_FACTOR = 8F;
            float scannerSpeed = SCANNER_SPEED_FACTOR * toTarget.Length() * globalPhysicsDeltaFloat;
            const float MAX_SPEED = 20f;
            scanner.node.Velocity += directionToTarget * scannerSpeed;
            scanner.node.Velocity *= 0.9f;
            if (scanner.node.IsOnWall()) {
                scanner.node.Velocity = scanner.node.Velocity.Bounce(scanner.node.GetWallNormal());
                scanner.node.Velocity += scanner.node.GetWallNormal() * SCANNER_SPEED_FACTOR;
            }
            if (scanner.node.Velocity.LengthSquared() > MAX_SPEED * MAX_SPEED) {
                scanner.node.Velocity = scanner.node.Velocity.Normalized() * MAX_SPEED;
            }
            scanner.node.MoveAndSlide();
            Transform3D lampPose = scanner.skeleton.GetBoneGlobalPose(SurveillanceScanner.lampBoneIndex);
            Vector3 globalTargetPosition = player.node.GlobalPosition + Vector3.Up - playerCamera.node.Transform.Basis.Z.Normalized() * 3f;
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
                Basis targetBasis = new Basis(right, up, forward);
                Quaternion current = lampPose.Basis.GetRotationQuaternion();
                Quaternion target = targetBasis.GetRotationQuaternion();
                const float ROTATION_SPEED = 2f;
                lampPose.Basis = new Basis(current.Slerp(target, ROTATION_SPEED * globalPhysicsDeltaFloat));
                scanner.skeleton.SetBoneGlobalPose(SurveillanceScanner.lampBoneIndex, lampPose);
            }
            surveillanceScanners[0] = scanner;
        }
    }
}