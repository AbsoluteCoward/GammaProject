using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct Projectile {
            public Node3D node;
            public Node3D targetNode;
            public RayCast3D collisionRaycast;
            public Vector3 positionLastFrame;
            public float speed;
            public float timeAlive;
        }
        public void ProjectilesCreate(Vector3 inputStartPosition, Node3D inputTarget, Vector3 inputDirection, float inputSpeed) {
            int index = -1;
            for (int i = 0; i < projectiles.Length; i++) {
                if (projectiles[i].node == null) {
                    index = i;
                    break;
                }
            }
            if (index == -1) {
                GD.PrintErr("ProjectilesCreate: No available projectile slots");
                return;
            }
            Projectile rocket = new Projectile();
            rocket.node = rocketScene.Instantiate<Node3D>();
            entitiesNode.AddChild(rocket.node);
            rocket.collisionRaycast = (RayCast3D)rocket.node.GetChild(0);
            rocket.targetNode = inputTarget;
            rocket.positionLastFrame = inputStartPosition;
            rocket.speed = inputSpeed;
            rocket.timeAlive = 0f;
            rocket.node.LookAtFromPosition(inputStartPosition, inputStartPosition + inputDirection, Vector3.Up);
            rocket.collisionRaycast.TopLevel = true;
            rocket.collisionRaycast.GlobalPosition = inputStartPosition;
            rocket.collisionRaycast.TargetPosition = inputDirection.Normalized();
            rocket.collisionRaycast.ForceRaycastUpdate();
            projectiles[index] = rocket;
        }
        public void ProjectilesUpdate() {
            for (int i = 0; i < projectiles.Length; i++) {
                if (projectiles[i].node == null) { continue; }
                Projectile rocket = projectiles[i];
                Vector3 currentDirection = -rocket.node.GlobalTransform.Basis.Z;
                if (rocket.targetNode != null && IsInstanceValid(rocket.targetNode)) {
                    Vector3 directionToTarget = ((rocket.targetNode.GlobalPosition + Vector3.Up) - rocket.node.GlobalPosition).Normalized();
                    float angleToTarget = currentDirection.AngleTo(directionToTarget);
                    if (angleToTarget > 0.001f) {
                        float maxRotationThisFrame = 2f * (float)globalDelta;
                        if (rocket.timeAlive > 1f) { maxRotationThisFrame *= rocket.timeAlive; }
                        float rotationAmount = Mathf.Min(angleToTarget, maxRotationThisFrame);
                        float randomOffsetIntensity = 1f;
                        Vector3 randomOffset = Vector3.Zero;
                        if (GD.Randf() < 0.5f) {
                            randomOffset = new Vector3(
                                (float)GD.RandRange(-randomOffsetIntensity, randomOffsetIntensity),
                                (float)GD.RandRange(-randomOffsetIntensity, randomOffsetIntensity),
                                (float)GD.RandRange(-randomOffsetIntensity, randomOffsetIntensity)
                            ).Normalized();
                        }
                        directionToTarget += randomOffset;
                        Vector3 rotationAxis = currentDirection.Cross(directionToTarget).Normalized();
                        if (rotationAxis.LengthSquared() < 0.001f) {
                            rotationAxis = Vector3.Up;
                        }
                        Basis rotationBasis = new Basis(rotationAxis, rotationAmount);
                        rocket.node.GlobalTransform = new Transform3D(
                            rotationBasis * rocket.node.GlobalTransform.Basis,
                            rocket.node.GlobalPosition
                        );
                        currentDirection = -rocket.node.GlobalTransform.Basis.Z;
                    }
                }
                currentDirection = -rocket.node.GlobalTransform.Basis.Z;
                rocket.node.GlobalPosition += currentDirection * rocket.speed * (float)globalDelta;
                rocket.collisionRaycast.GlobalPosition = rocket.positionLastFrame;
                rocket.collisionRaycast.TargetPosition = rocket.collisionRaycast.ToLocal(rocket.node.GlobalPosition);
                rocket.collisionRaycast.ForceRaycastUpdate();
                rocket.positionLastFrame = rocket.node.GlobalPosition;
                rocket.timeAlive += (float)globalDelta;
                if (rocket.collisionRaycast.IsColliding() || isProjectileTooFar(rocket.node.GlobalPosition)) {
                    if (rocket.node.GetParent() == entitiesNode) { entitiesNode.RemoveChild(rocket.node); }
                    rocket.node.QueueFree();
                    projectiles[i].node = null;
                } else {
                    projectiles[i] = rocket;
                }
            }
        }
        public bool isProjectileTooFar(Vector3 inputPosition) {
            return
                inputPosition.X > MAX_PROJECTILE_DISTANCE ||
                inputPosition.X < -MAX_PROJECTILE_DISTANCE ||
                inputPosition.Z > MAX_PROJECTILE_DISTANCE ||
                inputPosition.Z < -MAX_PROJECTILE_DISTANCE;
        }
    }
}