using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct Trail {
            public MeshInstance3D node;
            public Vector3 lastEmitPosition;
            public Vector3 offset;
            public Color color;
            public Vector3[] points;
            public float[] lifeTimes;
            public float width;
            public float length;
            public int count;
        }
        public void TrailsCreate(Trail[] inputTrails, Node3D parent, Vector3 inputOffset, Color inputColor, float inputWidth, float inputLength, int maxCount, bool isFullbright) {
            int index = -1;
            for (int i = 0; i < inputTrails.Length; i++) {
                if (inputTrails[i].node == null) {
                    index = i;
                    break;
                }
            }
            if (index == -1) {
                GD.PrintErr("TrailsCreate: No available trail slots");
                return;
            }
            Trail trail = new Trail();
            trail.node = new MeshInstance3D();
            trail.node.Mesh = new ImmediateMesh();
            trail.points = new Vector3[maxCount];
            trail.lastEmitPosition = inputOffset;
            trail.offset = inputOffset;
            trail.lifeTimes = new float[maxCount];
            trail.width = inputWidth;
            trail.length = inputLength;
            trail.count = 0;
            trail.color = inputColor;
            StandardMaterial3D material = new StandardMaterial3D();
            material.AlbedoColor = inputColor;
            material.ShadingMode = isFullbright ? StandardMaterial3D.ShadingModeEnum.Unshaded : StandardMaterial3D.ShadingModeEnum.PerVertex;
            trail.node.MaterialOverride = material;
            trail.node.TopLevel = true;
            trail.node.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            parent.AddChild(trail.node);
            inputTrails[index] = trail;
        }
        public void TrailUpdate(Trail[] inputTrails, float inputDistance) {
            for (int i = 0; i < inputTrails.Length; i++) {
                if (inputTrails[i].node == null) { continue; }
                for (int j = 0; j < inputTrails[i].count; j++) { inputTrails[i].lifeTimes[j] -= (float)globalPhysicsDelta; }
                while (inputTrails[i].count > 0 && inputTrails[i].lifeTimes[0] <= 0) {
                    for (int k = 1; k < inputTrails[i].count; k++) {
                        inputTrails[i].points[k - 1] = inputTrails[i].points[k];
                        inputTrails[i].lifeTimes[k - 1] = inputTrails[i].lifeTimes[k];
                    }
                    inputTrails[i].count--;
                }
                Vector3 emitPosition = inputTrails[i].node.GetParent<Node3D>().GlobalPosition + inputTrails[i].node.GetParent<Node3D>().GlobalBasis * inputTrails[i].offset;
                if ((emitPosition - inputTrails[i].lastEmitPosition).Length() >= inputDistance && inputTrails[i].count < inputTrails[i].points.Length) {
                    inputTrails[i].points[inputTrails[i].count] = emitPosition;
                    inputTrails[i].lifeTimes[inputTrails[i].count] = inputTrails[i].length;
                    inputTrails[i].count++;
                    inputTrails[i].lastEmitPosition = emitPosition;
                }
                ImmediateMesh mesh = (ImmediateMesh)inputTrails[i].node.Mesh;
                mesh.ClearSurfaces();
                if (inputTrails[i].count < 2) { continue; }
                mesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip);
                for (int j = 0; j < inputTrails[i].count; j++) {
                    float factor = j / (float)(inputTrails[i].count - 1);
                    float width = inputTrails[i].width * factor;
                    Vector3 pathDirection = j < inputTrails[i].count - 1 ?
                        (inputTrails[i].points[j + 1] - inputTrails[i].points[j]).Normalized() :
                        (inputTrails[i].points[j] - inputTrails[i].points[j - 1]).Normalized();
                    Vector3 toCamera = (inputTrails[i].points[j] - currentCamera.GlobalPosition).Normalized();
                    Vector3 normal = toCamera.Cross(pathDirection).Normalized();
                    mesh.SurfaceSetColor(inputTrails[i].color);
                    mesh.SurfaceSetUV(new Vector2(factor, 0));
                    mesh.SurfaceAddVertex(inputTrails[i].points[j] + normal * width);
                    mesh.SurfaceSetColor(inputTrails[i].color);
                    mesh.SurfaceSetUV(new Vector2(factor, 1));
                    mesh.SurfaceAddVertex(inputTrails[i].points[j] - normal * width);
                }
                mesh.SurfaceEnd();
            }
        }
    }
}