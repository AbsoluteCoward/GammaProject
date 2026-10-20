using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct VideoPlayer {
            public VideoStreamPlayer node;
            public Action<Main>[] onVideoStart;
            public Action<Main>[] onVideoComplete;
        }
        public struct VideoData {
            public string videoPath;
            public Action<Main>[] onVideoStart;
            public Action<Main>[] onVideoComplete;
        }
        public void StartVideo(ref VideoPlayer inputVideoPlayer, VideoData inputVideoData) {
            inputVideoPlayer.node.Stream = GD.Load<VideoStream>(inputVideoData.videoPath);
            inputVideoPlayer.node.Play();
            inputVideoPlayer.onVideoStart = inputVideoData.onVideoStart;
            inputVideoPlayer.onVideoComplete = inputVideoData.onVideoComplete;
            if (inputVideoPlayer.onVideoStart == null) { return; }
            GD.Print("Starting " + inputVideoPlayer.onVideoStart.Length + " video start functions");
            for (int i = 0; i < inputVideoPlayer.onVideoStart.Length; i++) {
                inputVideoPlayer.onVideoStart[i](this);
            }
        }
        public bool UpdateVideo(ref VideoPlayer inputVideoPlayer) {
            bool isPlaying = inputVideoPlayer.node.IsPlaying();
            if (inputVideoPlayer.node.Stream != null && !inputVideoPlayer.node.IsPlaying()) {
                EndVideo(ref inputVideoPlayer);
            }
            return isPlaying;
        }
        public void EndVideo(ref VideoPlayer inputVideoPlayer) {
            inputVideoPlayer.node.Stream = null;
            GD.Print("Starting " + inputVideoPlayer.onVideoComplete.Length + " video end functions");
            if (inputVideoPlayer.onVideoComplete == null) { return; }
            for (int i = 0; i < inputVideoPlayer.onVideoComplete.Length; i++) { inputVideoPlayer.onVideoComplete[i](this); }
            inputVideoPlayer.onVideoStart = null;
            inputVideoPlayer.onVideoComplete = null;
        }
    }
}
