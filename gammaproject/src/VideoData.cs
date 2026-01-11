using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        VideoData testVideoData = new VideoData {
            videoPath = "res://assets/videos/testvideo.ogv",
            onVideoStart = new Action<Main>[] {
                (Main) => Main.StartSound3D(GD.Load<AudioStream>("res://assets/sound/metalslam.wav"), Vector3.Zero, 1.5f, 1f, true),
                (Main) => GD.Print("Video started!"),
            },
            onVideoComplete = new Action<Main>[] {
                (Main) => GD.Print("Video ended!"),
            }
        };
    }
}
