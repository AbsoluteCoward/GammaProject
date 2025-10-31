using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        /// <summary>
        /// Spawns a debug cube at the given position and size, a child of inputTarget. 
        /// This is useful for visualizing positions/objects that you can't otherwise see.
        /// </summary>
        public static void DebugSpawnCube(Vector3 inputPosition, float inputSize, Node inputTarget) {
            MeshInstance3D cube = new MeshInstance3D();
            cube.Mesh = new BoxMesh();
            cube.TopLevel = true;
            ((BoxMesh)cube.Mesh).Size = new Vector3(inputSize, inputSize, inputSize);
            inputTarget.AddChild(cube);
            cube.GlobalPosition = inputPosition;
        }
    }
}
