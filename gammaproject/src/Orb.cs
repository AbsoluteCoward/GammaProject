using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct TeleportOrb {
            public MeshInstance3D node;
            public RayCast3D collisionRaycast;
            public Vector3 positionLastFrame;
            public Vector3 velocity;
            public float timeAlive;
        }
        public void OrbInitialize(MeshInstance3D inputOrb) {
            player.orb.node = inputOrb;
            player.orb.collisionRaycast = (RayCast3D)player.orb.node.GetChild(0);
            SphereMesh sphere = (SphereMesh)player.orb.node.Mesh;
            float orbRadius = sphere.Radius / 2f;
            //TrailsCreate(trails, player.orb.node, Vector3.Forward * orbRadius, Colors.Cyan, orbRadius * 1.5f, 2f, 256, true);
        }
        public void OrbShoot() {
            GD.Print("Orb shoot");
            player.orb.node.Visible = true;
            player.orb.node.TopLevel = true;
            player.orb.velocity = player.orb.node.GlobalTransform.Basis.Z.Normalized() * (player.node.Velocity.Length() * 1.5f);
            player.orb.timeAlive = 0f;
            player.orb.positionLastFrame = player.orb.node.GlobalPosition;
        }
        public void OrbReturn(bool inputShouldCancel) {
            GD.Print("Orb return");
            player.orb.velocity = Vector3.Zero;
            player.orb.timeAlive = 0f;
            player.orb.positionLastFrame = Vector3.Zero;
            if (!inputShouldCancel) { PlayerTeleportTo(player.orb.node.GlobalPosition, playerCamera); }
            player.orb.node.GlobalPosition = player.node.GlobalPosition; 
            player.orb.node.Visible = false;
            player.orb.node.TopLevel = false;
            player.orb.node.Scale = Vector3.One;
            player.orb.node.Rotation = Vector3.Zero;
        }
        public void OrbUpdate() {
            TeleportOrb orb = player.orb;
            if (!orb.node.TopLevel || !orb.node.Visible) { return; }
            orb.timeAlive += (float)globalPhysicsDelta;
            float currentSpeed = orb.velocity.Length();
            float speedFactor = currentSpeed * 0.2f;
            orb.node.Scale = new Vector3(
                orb.node.Scale.X,
                Mathf.Max(0.8f, 1f - speedFactor * 0.5f),
                orb.node.Scale.X + speedFactor
            );
            Vector3 orbForward = -orb.node.GlobalTransform.Basis.Z.Normalized();
            orb.velocity += Vector3.Down * GRAVITY * (float)globalPhysicsDelta;
            orb.velocity *= 1f - 0.25f * (float)globalPhysicsDelta;
            float lift = 0.8f;
            orb.velocity = orb.velocity.Lerp(orbForward * currentSpeed, lift);
            GD.Print(orb.node.GlobalTransform.Basis.Y);
            if (inputDirection != Vector2.Zero) {
                float inputMagnitude = Mathf.Clamp(1f - (currentSpeed / 20f), 0.4f, 1f);
                inputMagnitude *= Mathf.Clamp(orb.timeAlive / 2f, 0f, 1f);
                orb.node.RotateObjectLocal(Vector3.Right, Mathf.DegToRad(inputDirection.Y * 3f * inputMagnitude));
                orb.node.RotateObjectLocal(Vector3.Down, Mathf.DegToRad(inputDirection.X * 3f * inputMagnitude));
            } else {
                // Vector3 target = (Vector3.Up - orbForward * orbForward.Dot(Vector3.Up)).Normalized();
                // float angle = orb.node.GlobalTransform.Basis.Y.Normalized().SignedAngleTo(target, orbForward);
                // float rollSpeed = 4f;
                // orb.node.RotateObjectLocal(Vector3.Forward, angle * rollSpeed * (float)globalPhysicsDelta);
            }
            if (currentSpeed < 6f) {
                float factor = 1f - (currentSpeed / 6f);
                orb.node.Basis = orb.node.Basis.Orthonormalized().Slerp(orb.node.GlobalTransform.LookingAt(orb.node.GlobalPosition + Vector3.Down, Vector3.Right).Basis, factor * 0.05f) * Basis.FromScale(orb.node.Scale);
            }
            Vector3 newPosition = orb.node.GlobalPosition + orb.velocity * (float)globalPhysicsDelta;
            orb.collisionRaycast.GlobalPosition = orb.positionLastFrame != Vector3.Zero
                ? orb.positionLastFrame
                : orb.node.GlobalPosition;
            orb.collisionRaycast.TargetPosition = orb.collisionRaycast.ToLocal(newPosition) * 2f;
            orb.collisionRaycast.ForceRaycastUpdate();
            if (orb.collisionRaycast.IsColliding()) {
                OrbReturn(false);
                return;
            }
            orb.positionLastFrame = newPosition;
            orb.node.GlobalPosition = newPosition;
            player.orb = orb;
        }
    }
}