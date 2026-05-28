using Godot;
using System;
using System.Collections.Generic;

namespace Gamma {
    public partial class Main : Node {
        public struct Sound {
            public Node node;
            public AudioStream stream;
            public bool isPlaying;
            public float timeRemaining;
        }
        public Sound[] sounds;
        int soundsCount = 0;
        int uiStartIndex;
        public readonly Dictionary<string, AudioStream> newsoundCache = new();
        public AudioStream NewGetOrLoadSound(string path) {
            if (!newsoundCache.TryGetValue(path, out var stream)) {
                stream = GD.Load<AudioStream>(path);
                newsoundCache[path] = stream;
            }
            return stream;
        }
        public void AudioInitialize(int input3DSize, int inputUISize) {
            sounds = new Sound[input3DSize + inputUISize];
            uiStartIndex = input3DSize;
            soundsCount = 0;
            for (int i = 0; i < input3DSize; i++) {
                var n = new AudioStreamPlayer3D();
                entitiesNode.AddChild(n);
                sounds[i] = new Sound { node = n };
            }
            for (int i = input3DSize; i < sounds.Length; i++) {
                var n = new AudioStreamPlayer();
                uiNode.AddChild(n);
                sounds[i] = new Sound { node = n };
            }
        }
        public void AudioResize(bool isUI) {
            int sectionSize = isUI ? sounds.Length - uiStartIndex : uiStartIndex;
            int growBy = sectionSize * (ARRAY_GROWTH_FACTOR - 1);
            int oldLength = sounds.Length;
            Array.Resize(ref sounds, oldLength + growBy);
            if (isUI) {
                for (int i = oldLength; i < sounds.Length; i++) {
                    var n = new AudioStreamPlayer();
                    uiNode.AddChild(n);
                    sounds[i] = new Sound { node = n };
                }
            } else {
                Array.Copy(sounds, uiStartIndex, sounds, uiStartIndex + growBy, oldLength - uiStartIndex);
                for (int i = uiStartIndex; i < uiStartIndex + growBy; i++) {
                    var n = new AudioStreamPlayer3D();
                    entitiesNode.AddChild(n);
                    sounds[i] = new Sound { node = n };
                }
                uiStartIndex += growBy;
            }
        }
        private void SetAndPlay(ref Sound inputSound, AudioStream inputStream, Vector3 inputPosition, float inputVolume, float inputPitch, bool isUI) {
            inputSound.stream = inputStream;
            if (isUI) {
                AudioStreamPlayer player = (AudioStreamPlayer)inputSound.node;
                player.Stream = inputStream;
                player.VolumeDb = Mathf.LinearToDb(inputVolume);
                player.PitchScale = inputPitch;
                player.Play();
            } else {
                AudioStreamPlayer3D player = (AudioStreamPlayer3D)inputSound.node;
                player.Stream = inputStream;
                player.GlobalPosition = inputPosition;
                player.VolumeDb = Mathf.LinearToDb(inputVolume);
                player.PitchScale = inputPitch;
                player.Play();
            }
        }
        public void PlaySound(AudioStream inputSound, Vector3 inputPosition, float inputVolume, float inputPitch, bool inputShouldOverlap) {
            bool isUI = float.IsInfinity(inputPosition.X);
            int start = isUI ? uiStartIndex : 0;
            int end   = isUI ? sounds.Length : uiStartIndex;
            int availableSlot = -1;
            for (int i = start; i < end; i++) {
                if (sounds[i].stream == inputSound && !inputShouldOverlap) {
                    SetAndPlay(ref sounds[i], inputSound, inputPosition, inputVolume, inputPitch, isUI);
                    return;
                }
                if (!sounds[i].isPlaying && availableSlot == -1) {
                    availableSlot = i;
                }
            }
            if (availableSlot == -1) {
                // Capture before resize since AudioResize(false) mutates uiStartIndex
                int firstNewSlot = isUI ? sounds.Length : uiStartIndex;
                AudioResize(isUI);
                availableSlot = firstNewSlot;
            }
            sounds[availableSlot].isPlaying = true;
            sounds[availableSlot].timeRemaining = (float)inputSound.GetLength() / inputPitch;
            soundsCount++;
            SetAndPlay(ref sounds[availableSlot], inputSound, inputPosition, inputVolume, inputPitch, isUI);
        }
    }
}
