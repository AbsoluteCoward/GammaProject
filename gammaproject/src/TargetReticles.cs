using Godot;
namespace Gamma {
    public partial class Main : Node {
        public struct TargetReticle {
            public Node3D node;
            public Sprite2D onScreenReticle;
            public Sprite2D offScreenReticle;
        }
        public TargetReticle[] targetReticles;
        int targetReticleTotalFrames;
        public StandardMaterial3D targetReticleMaterial;
        private Vector2 viewportCenter;
        private Vector2 maxReticlePosition;
        private Vector2 borderOffset = new Vector2(20, 20);
        public void TargetReticlesInitialize() {
            targetReticles = new TargetReticle[DEFAULT_TARGET_RETICLES_SIZE];
            Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
            viewportCenter = viewportSize / 2.0f;
            maxReticlePosition = viewportCenter - borderOffset;
            for (int i = 0; i < targetReticles.Length; i++) {
                targetReticles[i].node = targetReticleScene.Instantiate<Node3D>();
                targetReticles[i].onScreenReticle = targetReticles[i].node.GetChild<Sprite2D>(0);
                targetReticleTotalFrames = targetReticles[i].onScreenReticle.Hframes * targetReticles[i].onScreenReticle.Vframes;
                targetReticles[i].offScreenReticle = targetReticles[i].node.GetChild<Sprite2D>(1);
                targetReticles[i].onScreenReticle.TopLevel = true;
                targetReticles[i].offScreenReticle.TopLevel = true;
                entitiesNode.AddChild(targetReticles[i].node);
                targetReticles[i].node.Visible = false;
            }
        }

        public void TargetReticlesUpdate() {
            for (int i = 0; i < targetReticles.Length; i++) {
                targetReticles[i].node.Visible = false;
                targetReticles[i].onScreenReticle.Visible = false;
                targetReticles[i].offScreenReticle.Visible = false;
            }
            for (int i = 0; i < player.targetCount; i++) {
                targetReticles[i].node.GlobalPosition = player.targets[i].GlobalPosition;
                targetReticles[i].node.Visible = true;
                if (currentCamera.IsPositionInFrustum(targetReticles[i].node.GlobalPosition)) {
                    targetReticles[i].onScreenReticle.Visible = true;
                    targetReticles[i].offScreenReticle.Visible = false;
                    Vector2 reticlePosition = currentCamera.UnprojectPosition(targetReticles[i].node.GlobalPosition);
                    targetReticles[i].onScreenReticle.GlobalPosition = reticlePosition;
                    float distance = currentCamera.GlobalPosition.DistanceTo(targetReticles[i].node.GlobalPosition);
                    float minDistance = 1f;
                    float maxDistance = 20f;
                    float normalizedDistance = Mathf.Clamp((distance - minDistance) / (maxDistance - minDistance), 0f, 1f);
                    float scale = Mathf.Lerp(0.3f, 0.05f, normalizedDistance);

                    targetReticles[i].onScreenReticle.Frame = (targetReticles[i].onScreenReticle.Frame + 1) % targetReticleTotalFrames;
                    targetReticles[i].onScreenReticle.Scale = Vector2.One * scale;
                } else {
                    targetReticles[i].onScreenReticle.Visible = false;
                    targetReticles[i].offScreenReticle.Visible = true;
                    Vector3 localToCamera = currentCamera.ToLocal(targetReticles[i].node.GlobalPosition);
                    Vector2 reticlePosition = new Vector2(localToCamera.X, -localToCamera.Y);
                    Vector2 absReticlePos = reticlePosition.Abs();
                    float reticleAspect = absReticlePos.X / absReticlePos.Y;
                    float viewportAspect = maxReticlePosition.X / maxReticlePosition.Y;
                    if (reticleAspect > viewportAspect) {
                        reticlePosition *= maxReticlePosition.X / absReticlePos.X;
                    } else {
                        reticlePosition *= maxReticlePosition.Y / absReticlePos.Y;
                    }
                    targetReticles[i].offScreenReticle.GlobalPosition = viewportCenter + reticlePosition;
                    float angle = Vector2.Right.AngleTo(reticlePosition);
                    targetReticles[i].offScreenReticle.Rotation = angle;
                }
            }
        }
    }
}