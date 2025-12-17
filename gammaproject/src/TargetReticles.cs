using Godot;
namespace Gamma {
    public partial class Main : Node {
        public struct TargetReticle {
            public MeshInstance3D node;
            public Node3D targetNode;
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
            targetReticles[0].node.Visible = true;
            targetReticles[0].node.GlobalPosition = player.node.GlobalPosition + Vector3.Up;
        }
    }
}
