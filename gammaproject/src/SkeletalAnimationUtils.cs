using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct PlaybackPositionData {
            public string parameterName;
            public float previousPlaybackPosition;
            public float currentPlaybackPosition;
        }
        public int GetPlaybackBlockIndex(string inputParameterPath) {
            for (int i = 0; i < player.animationPlaybackBlocks.Length; i++) {
                if (player.animationPlaybackBlocks[i].parameterName == inputParameterPath) { return i; }
            }
            GD.PrintErr($"Couldn't find playback block for parameter {inputParameterPath}");
            return 0;
        }
        bool HasCrossedPlaybackPosition(float inputPreviousPosition, float inputCurrentPosition, float inputEventPosition) {
            if (inputCurrentPosition >= inputPreviousPosition) { return inputPreviousPosition < inputEventPosition && inputEventPosition <= inputCurrentPosition; }
            return inputPreviousPosition < inputEventPosition || inputEventPosition <= inputCurrentPosition;
        }
    }
}
