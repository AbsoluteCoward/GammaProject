using Godot;
namespace Gamma {
    public partial class Main : Node {
        public struct TargetReticle {
            public MeshInstance3D node;
        }
        public TargetReticle[] targetReticles;
        public StandardMaterial3D targetReticleMaterial;
        public void TargetReticlesInitialize() {
            targetReticles = new TargetReticle[DEFAULT_TARGET_RETICLES_SIZE];
            for (int i = 0; i < targetReticles.Length; i++) {
                RenderingServer.SetDebugGenerateWireframes(true);
                targetReticles[i].node = new MeshInstance3D();
                targetReticles[i].node.Mesh = new BoxMesh();
                ((BoxMesh)targetReticles[i].node.Mesh).Size = new Vector3(0.3f, 0.3f, 0.3f);
                targetReticles[i].node.MaterialOverride = targetReticleMaterial;
                entitiesNode.AddChild(targetReticles[i].node);
            }
        }
        public void TargetReticlesUpdate() {
            for (int i = 0; i < targetReticles.Length; i++) {
                targetReticles[i].node.Visible = false;
            }
            for (int i = 0; i < player.targetCount; i++) {
                targetReticles[i].node.Visible = true;
                targetReticles[i].node.GlobalPosition = player.targets[i].GlobalPosition;
                targetReticles[i].node.RotateY(0.1f);
                targetReticles[i].node.RotateX(0.1f);
            }
        }
    }
}
