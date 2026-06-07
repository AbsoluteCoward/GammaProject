using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct SurveillanceScanner {
            public Node3D node;
        }
        public void SurveillanceScannerInitialize(Node3D inputNode) {
            if (surveillanceScannerCount >= surveillanceScanners.Length) {
                SurveillanceScanner[] newSurveillanceScanners = new SurveillanceScanner[surveillanceScanners.Length * ARRAY_GROWTH_FACTOR];
                for (int i = 0; i < enemies.Length; i++) {
                    newSurveillanceScanners[i] = surveillanceScanners[i];
                }
                surveillanceScanners = newSurveillanceScanners;
            }
            int index = surveillanceScannerCount;
            surveillanceScanners[index].node = inputNode;
            AnimationPlayer animationPlayer = inputNode.GetNode<AnimationPlayer>("SurveillanceScanner01/AnimationPlayer");
            animationPlayer.Play("Fly");
            surveillanceScannerCount++;
            GD.Print($"{inputNode.Name} Initialized at index {index}");
        }
        public void SurveillanceScannerUpdate() {
            Node3D scanner = surveillanceScanners[0].node;
            if (scanner == null) { return; }
            scanner.LookAt(player.orb.node.GlobalPosition + Vector3.Up, Vector3.Up);
            Vector3 targetposition = 
                playerCamera.WallRayCast.TargetPosition + 
                playerCamera.WallRayCast.GlobalPosition +
                Vector3.Up +
                player.node.Transform.Basis.X.Normalized() * 3f;
            scanner.GlobalPosition = scanner.GlobalPosition.Lerp(targetposition, 0.1f);
        }
    }
}