using Godot;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Gamma {
    public partial class Main : Node {
        /// <summary>
        /// Turns off or on shadows for a given node and its children.
        /// </summary>
        public void TurnShadowsOffOrOn(Node3D inputNode, bool inputDecision) {
            int maxStackSize = 256;
            Node[] stack = new Node[maxStackSize];
            int stackSize = 0;
            stack[stackSize++] = inputNode;
            GeometryInstance3D.ShadowCastingSetting shadowSetting = inputDecision ?
                GeometryInstance3D.ShadowCastingSetting.On :
                GeometryInstance3D.ShadowCastingSetting.Off;
            while (stackSize > 0) {
                Node currentNode = stack[--stackSize];
                if (currentNode.GetType() == typeof(MeshInstance3D)) {
                    MeshInstance3D mesh = (MeshInstance3D)currentNode;
                    mesh.CastShadow = shadowSetting;
                }
                int childCount = currentNode.GetChildCount();
                for (int i = 0; i < childCount; i++) {
                    if (stackSize >= maxStackSize) {
                        GD.PrintErr("TurnShadowsOffOrOn: Stack overflow");
                        return;
                    }
                    stack[stackSize++] = currentNode.GetChild(i);
                }
            }
        }
        /// <summary>
        /// Analyzes a struct's field layout and determines if fields are optimally ordered for memory alignment.
        /// Fields should be ordered from largest to smallest to minimize padding.
        /// Returns true if optimally ordered, false otherwise, along with suggestions.
        /// </summary>
        public static string IsStructOptimal<T>(T inputStruct) where T : struct {
            Type structType = typeof(T);
            FieldInfo[] fields = structType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            if (fields.Length == 0) { return "Struct empty"; }
            StringBuilder analysis = new StringBuilder();
            analysis.Append($"Memory Layout Analysis for {structType.Name}:\n");
            var fieldSizes = new List<(FieldInfo field, int size, string category)>();
            for (int i = 0; i < fields.Length; i++) {
                FieldInfo field = fields[i];
                int size = GetFieldSize(field.FieldType);
                string category = GetSizeCategory(size);
                fieldSizes.Add((field, size, category));
                analysis.Append($"{i + 1}. {field.Name,-30} {size} bytes ({category})\n");
            }
            bool isOptimal = true;
            for (int i = 0; i < fieldSizes.Count - 1; i++) {
                if (fieldSizes[i].size < fieldSizes[i + 1].size) {
                    isOptimal = false;
                    break;
                }
            }
            if (isOptimal) {
                analysis.Append("OPTIMAL: Fields are ordered from largest to smallest.");
            } else {
                analysis.Append("NOT OPTIMAL: Fields should be reordered for better memory alignment.\n");
                analysis.Append("Suggested order:\n");
                var sortedFields = fieldSizes.OrderByDescending(f => f.size).ToList();
                for (int i = 0; i < sortedFields.Count; i++) {
                    var field = sortedFields[i];
                    analysis.Append($"{i + 1}. {field.field.Name,-30} {field.size} bytes ({field.category})\n");
                }
            }
            return analysis.ToString();
        }
        /// <summary>
        /// Gets the size in bytes of a field type, accounting for reference types and special cases.
        /// </summary>
        private static int GetFieldSize(Type fieldType) {
            if (!fieldType.IsValueType) { return 8; }
            if (fieldType == typeof(byte) || fieldType == typeof(sbyte) || fieldType == typeof(bool)) { return 1; }
            if (fieldType == typeof(short) || fieldType == typeof(ushort) || fieldType == typeof(char)) { return 2; }
            if (fieldType == typeof(int) || fieldType == typeof(uint) || fieldType == typeof(float)) { return 4; }
            if (fieldType == typeof(long) || fieldType == typeof(ulong) || fieldType == typeof(double)) { return 8; }
            if (fieldType == typeof(decimal)) { return 16; }
            if (fieldType.Name == "Vector2") return 8;  // 2 floats
            if (fieldType.Name == "Vector3") return 12; // 3 floats
            if (fieldType.Name == "Vector4") return 16; // 4 floats
            if (fieldType.Name == "Quaternion") return 16; // 4 floats
            if (fieldType.Name == "Color") return 16; // 4 floats
            if (fieldType.Name == "Rect2") return 16; // 4 floats
            if (fieldType.Name == "Transform2D") return 24; // 6 floats
            if (fieldType.Name == "Basis") return 36; // 9 floats
            if (fieldType.Name == "Transform3D") return 48; // 12 floats
            try { return System.Runtime.InteropServices.Marshal.SizeOf(fieldType); } catch { return 8; }
        }
        /// <summary>
        /// Categorizes field size for readability.
        /// </summary>
        private static string GetSizeCategory(int size) {
            return size switch {
                1 => "byte",
                2 => "word",
                4 => "dword",
                8 => "qword/ref",
                12 => "Vector3",
                16 => "Vector4/decimal",
                _ => size > 8 ? "large struct" : "unknown"
            };
        }
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
        public static void DebugSpawnLine(Vector3 startPosition, Vector3 endPosition, float inputRadius, Node inputTarget) {
            MeshInstance3D line = new MeshInstance3D();
            line.TopLevel = true;
            Vector3 direction = endPosition - startPosition;
            float length = direction.Length();
            CylinderMesh cylinder = new CylinderMesh();
            cylinder.TopRadius = inputRadius;
            cylinder.BottomRadius = inputRadius;
            cylinder.Height = length;
            line.Mesh = cylinder;
            StandardMaterial3D material = new StandardMaterial3D();
            material.AlbedoColor = Colors.Red;
            material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            line.MaterialOverride = material;
            inputTarget.AddChild(line);
            line.GlobalPosition = (startPosition + endPosition) / 2;
            if (length > 0.0001f) {
                line.LookAt(endPosition, Vector3.Up);
                line.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2);
            }
        }

        public static double Measure(Action action) {
            var stopWatch = Stopwatch.StartNew();
            action();
            stopWatch.Stop();
            return stopWatch.Elapsed.TotalMilliseconds;
        }
    }
}
