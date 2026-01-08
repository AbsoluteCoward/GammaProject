using Godot;
using System;
using System.Reflection;
namespace Gamma {
    public partial class Main : Node {
        public struct VideoPlayback {
            public VideoStreamPlayer node;
            public Action<Main>[] onVideoStart;
            public Action<Main>[] onVideoComplete;
        }
        public struct Video {
            public string videoPath;
            public Action<Main>[] onVideoStart;
            public Action<Main>[] onVideoComplete;
        }
        public void StartVideo(VideoPlayback inputVideo, Video inputVideoData) {
            inputVideo.node.Stream = GD.Load<VideoStream>(inputVideoData.videoPath);
            inputVideo.node.Play();
            if (inputVideo.onVideoStart == null) { return; }
            for (int i = 0; i < inputVideo.onVideoStart.Length; i++) {
                inputVideo.onVideoStart[i](this);
            }
        }
        public void UpdateVideo(ref VideoPlayback inputVideo) {
            if (!inputVideo.node.IsPlaying()) {
                if (!inputVideo.node.IsPlaying()) {
                    EndVideo(ref inputVideo);
                }
            }
        }
        public void EndVideo(ref VideoPlayback inputVideo) {
            inputVideo.node.Stop();
            if (inputVideo.onVideoComplete == null) { return; }
            for (int i = 0; i < inputVideo.onVideoComplete.Length; i++) { inputVideo.onVideoComplete[i](this); }
        }
    }
}
