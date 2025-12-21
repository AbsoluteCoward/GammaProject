using Godot;
namespace Gamma {
    public partial class Main : Node {
        public struct TargetReticle {
            public Node3D node;
            public Sprite2D onScreenReticle;
            public Sprite2D offScreenReticle;
        }
        public TargetReticle[] targetReticles;
        public StandardMaterial3D targetReticleMaterial;
        public void TargetReticlesInitialize() {
            targetReticles = new TargetReticle[DEFAULT_TARGET_RETICLES_SIZE];
            for (int i = 0; i < targetReticles.Length; i++) {
                targetReticles[i].node = targetReticleScene.Instantiate<Node3D>();
                targetReticles[i].onScreenReticle = targetReticles[i].node.GetChild<Sprite2D>(0);
                targetReticles[i].onScreenReticle.TopLevel = true;
                entitiesNode.AddChild(targetReticles[i].node);
                targetReticles[i].node.Visible = false;
            }
        }
        public void TargetReticlesUpdate() {
            for (int i = 0; i < targetReticles.Length; i++) {
                targetReticles[i].node.Visible = false;
                targetReticles[i].onScreenReticle.Visible = false;
            }
            for (int i = 0; i < player.targetCount; i++) {
                targetReticles[i].node.GlobalPosition = player.targets[i].GlobalPosition;
                if (currentCamera.IsPositionInFrustum(targetReticles[i].node.GlobalPosition)) {
                    targetReticles[i].node.Visible = true;
                    targetReticles[i].onScreenReticle.Visible = true;
                    Vector2 reticlePosition = currentCamera.UnprojectPosition(targetReticles[i].node.GlobalPosition);
                    targetReticles[i].onScreenReticle.GlobalPosition = reticlePosition;
                    float distance = currentCamera.GlobalPosition.DistanceTo(targetReticles[i].node.GlobalPosition);
                    float minDistance = 5f;
                    float maxDistance = 50f;
                    float t = Mathf.Clamp((distance - minDistance) / (maxDistance - minDistance), 0f, 1f);
                    float scale = Mathf.Lerp(0.1f, 0.5f, t);
                    targetReticles[i].onScreenReticle.Frame = (targetReticles[i].onScreenReticle.Frame + 1) % 24;
                    targetReticles[i].onScreenReticle.Scale = Vector2.One * scale;
                }
            }
        }
    }
}