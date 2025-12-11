using Godot;
using System;
using System.Reflection;
namespace Gamma {
    public partial class Main : Node {
        public struct Projectile {
            public Node3D node;
            public Node3D targetNode;
            public RayCast3D collisionRaycast;
            public Vector3 positionLastFrame;
            public Vector3 direction;
            public float speed;
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
            rocket.positionLastFrame = inputStartPosition + rocket.node.GlobalTransform.Basis.Z;
            rocket.direction = inputDirection;
            rocket.speed = inputSpeed;
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
                Vector3 movementThisFrame = rocket.direction.Normalized() * rocket.speed * (float)globalDelta;
                rocket.node.GlobalPosition += movementThisFrame;
                rocket.collisionRaycast.GlobalPosition = rocket.node.GlobalPosition;
                rocket.collisionRaycast.ForceRaycastUpdate();
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