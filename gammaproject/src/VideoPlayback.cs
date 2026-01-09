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
        public void UpdateVideo(ref VideoPlayer inputVideoPlayer) {
            if (!inputVideoPlayer.node.IsPlaying()) {
                if (!inputVideoPlayer.node.IsPlaying()) { EndVideo(ref inputVideoPlayer); }
            }
        }
        public void EndVideo(ref VideoPlayer inputVideoPlayer) {
            inputVideoPlayer.node.Stop();
            if (inputVideoPlayer.onVideoComplete == null) { return; }
            for (int i = 0; i < inputVideoPlayer.onVideoComplete.Length; i++) { inputVideoPlayer.onVideoComplete[i](this); }
            inputVideoPlayer.onVideoStart = null;
            inputVideoPlayer.onVideoComplete = null;
        }
    }
}
