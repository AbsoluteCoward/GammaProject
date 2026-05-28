using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        VideoData testVideoData = new VideoData {
            videoPath = "res://assets/videos/testvideo2.ogv",
            onVideoStart = new Action<Main>[] {
                (Main) => Main.PlaySoundUI(GD.Load<AudioStream>("res://assets/sound/metalslam.wav"), 0.7f, 1f, true),
                (Main) => GD.Print("Video started!"),
            },
            onVideoComplete = new Action<Main>[] {
                (Main) => GD.Print("Video ended!"),
            }
        };
    }
}
