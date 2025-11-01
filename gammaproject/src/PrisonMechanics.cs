using Godot;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Gamma {
    public partial class Main : Node {
        public const float PRISON_SPOTLIGHT_HEIGHT = 5.0f;
        public const float PRISON_SPOTLIGHT_SPEED = 1.2f;
        public struct PrisonSpotLight {
            public Node3D node;
            public SpotLight3D spotlight;
            public Vector3 velocity;
            public Vector3 targetPosition;
            public float offsetHeight;
            public float speed;
        }
        public void PrisonSpotlightUpdate(ref PrisonSpotLight prisonSpotlight) {
            prisonSpotlight.targetPosition = new Vector3(
                0.0f,
                PRISON_SPOTLIGHT_HEIGHT,
                Mathf.Clamp(player.node.GlobalPosition.Z, -30f, 50f)
            );
            Vector3 directionToTarget = prisonSpotlight.targetPosition - prisonSpotlight.node.GlobalPosition;
            prisonSpotlight.velocity += directionToTarget * prisonSpotlight.speed * (float)globalDelta;
            prisonSpotlight.velocity *= 0.98f;
            switch (prisonSpotlight.node.GlobalPosition.Z) {
                case < -32f:
                    prisonSpotlight.velocity.Z = Math.Abs(prisonSpotlight.velocity.Z);
                    GD.Print(prisonSpotlight.velocity.Z);
                    if (prisonSpotlight.velocity.Z > 10f) {
                        PlayAudio3D(MetalSlam1, prisonSpotlight.node.GlobalPosition, 1f, 1f, true);
                    }
                    PlayAudio3D(MetalSlam, prisonSpotlight.node.GlobalPosition, 0.05f, 0.8f, true);
                    break;
                case > 52f:
                    prisonSpotlight.velocity.Z = -Math.Abs(prisonSpotlight.velocity.Z);
                    if (prisonSpotlight.velocity.Z < -10f) {
                        PlayAudio3D(MetalSlam1, prisonSpotlight.node.GlobalPosition, 1f, 1f, true);
                    }
                    PlayAudio3D(MetalSlam, prisonSpotlight.node.GlobalPosition, 0.05f, 0.8f, true);
                    break;
            }
            prisonSpotlight.node.GlobalPosition += prisonSpotlight.velocity * (float)globalDelta;
            prisonSpotlight.node.LookAt(player.node.GlobalPosition, Vector3.Up);
        }
    }
}
