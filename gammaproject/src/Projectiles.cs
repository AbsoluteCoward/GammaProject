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
        public struct Explosion {
            public Vector3 randomRotation;
            public MeshInstance3D node;
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
                if (rocket.collisionRaycast.IsColliding() || isProjectileTooFar(rocket.node.GlobalPosition) || rocket.timeAlive > MAX_PROJECTILE_LIFETIME) {
                    SpawnExplosion(rocket.node.GlobalPosition, GD.Randf());
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
        public void SpawnExplosion(Vector3 inputPosition, float inputTimeAlive) {
            int index = -1;
            for (int i = 0; i < explosions.Length; i++) {
                if (explosions[i].node == null) {
                    index = i;
                    break;
                }
            }
            if (index == -1) {
                GD.PrintErr("SpawnExplosion: No available explosion slots");
                return;
            }
            Explosion explosion = new Explosion();
            explosion.node = new MeshInstance3D();
            SphereMesh explosionMesh = new SphereMesh();
            explosionMesh.Rings = 8;
            explosionMesh.RadialSegments = 8;
            explosionMesh.Radius = 1f;
            explosionMesh.Height = 2f;
            StandardMaterial3D explosionMaterial = new StandardMaterial3D();
            explosionMaterial.AlbedoTexture = efxFire01;
            entitiesNode.AddChild(explosion.node);
            explosion.randomRotation = new Vector3(
                (float)GD.RandRange(-1f, 1f),
                (float)GD.RandRange(-1f, 1f),
                (float)GD.RandRange(-1f, 1f)
            );
            explosion.node.Mesh = explosionMesh;
            explosion.node.MaterialOverride = explosionMaterial;
            explosion.node.GlobalPosition = inputPosition;
            explosion.timeAlive = inputTimeAlive;
            explosions[index] = explosion;
        }
        public void UpdateExplosions() {
            for (int i = 0; i < explosions.Length; i++) {
                if (explosions[i].node == null) { continue; }
                Explosion explosion = explosions[i];
                explosion.timeAlive += (float)globalDelta;
                float scaleAmount = 2f + explosion.timeAlive * 6f;
                explosion.node.Scale = new Vector3(scaleAmount, scaleAmount, scaleAmount);
                explosion.node.Rotation += explosion.randomRotation * 2 * (float)globalDelta;
                float maxLifetime = 2f;
                if (explosion.timeAlive >= maxLifetime || explosion.node.Scale.X >= 6f) {
                    if (explosion.node.GetParent() == entitiesNode) { entitiesNode.RemoveChild(explosion.node); }
                    explosion.node.QueueFree();
                    explosions[i].node = null;
                } else {
                    explosions[i] = explosion;
                }
            }
        }
    }
}