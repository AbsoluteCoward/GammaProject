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

        /// <summary>
        /// Draws a debug line in 3D space from startPosition to endPosition, a child of inputTarget.
        /// This is useful for visualizing directions, raycasts, or connections between objects.
        /// example
        /// </summary>
        public static void DebugDrawLine(Vector3 startPosition, Vector3 endPosition, float thickness, Node inputTarget, Color? color = null) {
            MeshInstance3D line = new MeshInstance3D();
            line.TopLevel = true;
            Vector3 direction = endPosition - startPosition;
            float length = direction.Length();
            CylinderMesh cylinder = new CylinderMesh();
            cylinder.TopRadius = thickness;
            cylinder.BottomRadius = thickness;
            cylinder.Height = length;
            line.Mesh = cylinder;
            StandardMaterial3D material = new StandardMaterial3D();
            material.AlbedoColor = color ?? Colors.Red;
            material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            line.MaterialOverride = material;
            inputTarget.AddChild(line);
            line.GlobalPosition = (startPosition + endPosition) / 2;
            if (length > 0.0001f) {
                line.LookAt(endPosition, Vector3.Up);
                line.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2);
            }
        }
    }
}
