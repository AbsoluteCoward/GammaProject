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
            public int trailIndex;
        }
        public struct Explosion {
            public Node3D node;
            public MeshInstance3D mesh;
            public CpuParticles3D fireParticles;
            public CpuParticles3D smokeParticles;
            public Vector3 randomRotation;
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
                Projectile[] newProjectiles = new Projectile[projectiles.Length * ARRAY_GROWTH_FACTOR];
                for (int i = 0; i < projectiles.Length; i++) {
                    newProjectiles[i] = projectiles[i];
                }
                index = projectiles.Length;
                projectiles = newProjectiles;
                GD.Print("Projectiles array resized to " + projectiles.Length);
            }
            Projectile rocket;
            rocket.node = rocketScene.Instantiate<Node3D>();
            entitiesNode.AddChild(rocket.node);
            TrailsCreateParams trailParams = new TrailsCreateParams{
                material = GD.Load<StandardMaterial3D>("res://assets/materials/trail-materials/MAT_TRAILROCKET.tres"),
                parent = rocket.node, 
                offset =Vector3.Back * 0.5f, 
                color = Colors.DarkGray, 
                width = 0.05f, 
                length = 0.5f, 
                maxCount = 256,
                isFullBright = true
            };
            TrailsCreate(ref trails, trailParams);
            rocket.trailIndex = trails.Length - 1;
            for (int j = 0; j < trails.Length; j++) {
                if (trails[j].node != null && trails[j].node.GetParent() == rocket.node) {
                    rocket.trailIndex = j;
                    break;
                }
            }
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
        public bool isProjectileTooFar(Vector3 inputPosition) {
            return
                inputPosition.X > MAX_PROJECTILE_DISTANCE ||
                inputPosition.X < -MAX_PROJECTILE_DISTANCE ||
                inputPosition.Z > MAX_PROJECTILE_DISTANCE ||
                inputPosition.Z < -MAX_PROJECTILE_DISTANCE;
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
                        float maxRotationThisFrame = 2f * globalPhysicsDeltaFloat;
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
                } else {
                    Vector3 rotationAxis = currentDirection.Cross(Vector3.Down).Normalized();
                    float rotationAmount = 0.4f * globalPhysicsDeltaFloat;
                    Basis rotationBasis = new Basis(rotationAxis, rotationAmount);
                    rocket.node.GlobalTransform = new Transform3D(
                        rotationBasis * rocket.node.GlobalTransform.Basis,
                        rocket.node.GlobalPosition
                    );
                    currentDirection = -rocket.node.GlobalTransform.Basis.Z;
                }
                Vector3 newPosition = rocket.node.GlobalPosition + currentDirection * rocket.speed * globalPhysicsDeltaFloat;
                rocket.node.TopLevel = true;
                rocket.node.GlobalPosition = newPosition;
                rocket.collisionRaycast.GlobalPosition = rocket.positionLastFrame != Vector3.Zero
                    ? rocket.positionLastFrame
                    : rocket.node.GlobalPosition;
                rocket.collisionRaycast.TargetPosition = rocket.collisionRaycast.ToLocal(newPosition);
                rocket.collisionRaycast.TargetPosition *= 2f;
                rocket.collisionRaycast.ForceRaycastUpdate();
                rocket.positionLastFrame = rocket.node.GlobalPosition;
                rocket.timeAlive += globalPhysicsDeltaFloat;
                if (rocket.collisionRaycast.IsColliding() || isProjectileTooFar(rocket.node.GlobalPosition) || rocket.timeAlive > MAX_PROJECTILE_LIFETIME) {
                    SpawnExplosion(rocket.node.GlobalPosition, Mathf.Min(GD.Randf(), 0.3f));
                    if (rocket.node.GetParent() == entitiesNode) { entitiesNode.RemoveChild(rocket.node); }
                    rocket.node.QueueFree();
                    projectiles[i].node = null;
                    if (rocket.trailIndex != -1) {
                        TrailsRemove(trails, rocket.trailIndex);
                    }
                } else {
                    projectiles[i] = rocket;
                }
            }
        }
        public void SpawnExplosion(Vector3 inputPosition, float inputTimeAlive) {
            const float EXPLOSION_RADIUS = 12f;
            int index = -1;
            for (int i = 0; i < explosions.Length; i++) {
                if (explosions[i].node == null) {
                    index = i;
                    break;
                }
            }
            if (index == -1) {
                Explosion[] newExplosions = new Explosion[explosions.Length * ARRAY_GROWTH_FACTOR];
                for (int i = 0; i < explosions.Length; i++) {
                    newExplosions[i] = explosions[i];
                }
                index = explosions.Length;
                explosions = newExplosions;
                GD.Print("Explosions array resized to " + explosions.Length);
            }
            for (int j = 0; j < enemies.Length; j++) {
                if (enemies[j].node == null) { continue; }
                if (enemies[j].node.GlobalPosition.DistanceTo(inputPosition) < EXPLOSION_RADIUS) {
                    enemies[j].behaviorState = EnemyState.Dead;
                }
            }
            Explosion explosion;
            explosion.node = explosionScene.Instantiate<Node3D>();
            explosion.mesh = explosion.node.GetChild<MeshInstance3D>(0);
            explosion.fireParticles = explosion.node.GetChild<CpuParticles3D>(1);
            explosion.smokeParticles = explosion.node.GetChild<CpuParticles3D>(2);
            entitiesNode.AddChild(explosion.node);
            explosion.randomRotation = new Vector3(
                (float)GD.RandRange(-1f, 1f),
                (float)GD.RandRange(-1f, 1f),
                (float)GD.RandRange(-1f, 1f)
            );
            explosion.node.GlobalPosition = inputPosition;
            explosion.fireParticles.Emitting = true;
            RaycastWorldHitInfo hit;
            if (RaycastWorld(globalWorld3D, player.node, inputPosition, inputPosition + Vector3.Down * 10, out hit)) {
                explosion.smokeParticles.GlobalPosition = hit.Position;
            }
            explosion.smokeParticles.Emitting = true;
            explosion.timeAlive = inputTimeAlive;
            explosions[index] = explosion;
            PlaySound3D(explosionSFX, inputPosition, 1f, 0.8f + GD.Randf() * 0.4f, true);
            if (directionalLight != null) {
                directionalLight.LightColor += new Color(1f, 0.4f, 0f);
                directionalLight.LightEnergy += 40f;
                worldEnvironment.Environment.AmbientLightColor += new Color(1f, 0.4f, 0f);
                worldEnvironment.Environment.AmbientLightEnergy += 1f;
                directionalLight.LookAt(player.node.GlobalPosition - (inputPosition + Vector3.Up * 2), Vector3.Up);
            }
        }
        public void UpdateExplosions() {
            const float LIGHT_FADE_SPEED = 0.5f;
            bool shouldFadeLight = true;
            for (int i = 0; i < explosions.Length; i++) {
                if (explosions[i].node == null) { continue; }
                Explosion explosion = explosions[i];
                explosion.timeAlive += globalPhysicsDeltaFloat;
                float scaleAmount = explosion.timeAlive * 6f;
                explosion.mesh.Scale = new Vector3(scaleAmount, scaleAmount, scaleAmount);
                explosion.mesh.Rotation += explosion.randomRotation * 6 * globalPhysicsDeltaFloat;
                const float MAX_LIFETIME = 4f;
                if (explosion.timeAlive > MAX_LIFETIME/2) { 
                    explosion.mesh.Visible = false;
                    explosion.fireParticles.Color = explosion.fireParticles.Color.Lerp(NULL_COLOR, 1 * globalPhysicsDeltaFloat);
                } else {
                    shouldFadeLight = false;
                }
                if (explosion.timeAlive >= MAX_LIFETIME) {
                    if (explosion.node.GetParent() == entitiesNode) { entitiesNode.RemoveChild(explosion.node); }
                    explosion.node.QueueFree();
                    explosions[i].node = null;
                } else {
                    explosions[i] = explosion;
                }
            }
            if (shouldFadeLight) {
                directionalLight.LightColor = directionalLight.LightColor.Lerp(directionalLightOriginal.LightColor, LIGHT_FADE_SPEED * globalPhysicsDeltaFloat);
                directionalLight.LightEnergy = Mathf.Lerp(directionalLight.LightEnergy, directionalLightOriginal.LightEnergy, LIGHT_FADE_SPEED * 8 * globalPhysicsDeltaFloat);
                worldEnvironment.Environment.AmbientLightColor = worldEnvironment.Environment.AmbientLightColor.Lerp(worldEnvironmentOriginal.Environment.AmbientLightColor, LIGHT_FADE_SPEED * globalPhysicsDeltaFloat);
                worldEnvironment.Environment.AmbientLightEnergy = Mathf.Lerp(worldEnvironment.Environment.AmbientLightEnergy, worldEnvironmentOriginal.Environment.AmbientLightEnergy, LIGHT_FADE_SPEED * globalPhysicsDeltaFloat);
            }
        }
    }
}