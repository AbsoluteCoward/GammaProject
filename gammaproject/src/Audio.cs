using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        public const int AUDIO_POOL_GROWTH_FACTOR = 2;
        public struct Sound3D {
            public AudioStreamPlayer3D node;
            public bool isPlaying;
            public float timeRemaining;
        }
        public struct SoundUI {
            public AudioStreamPlayer node;
            public bool isPlaying;
            public float timeRemaining;
        }
        public Sound3D[] sounds = new Sound3D[8];
        int soundCount = 0;
        public SoundUI[] soundsui = new SoundUI[2];
        int soundUICount = 0;
        public void Audio3DInitialize(int inputSize) {
            for (int i = 0; i < sounds.Length; i++) {
                if (sounds[i].node != null) {
                    if (sounds[i].node.GetParent() == entitiesNode) {
                        entitiesNode.RemoveChild(sounds[i].node);
                    }
                    sounds[i].node.QueueFree();
                }
            }
            sounds = new Sound3D[inputSize];
            soundCount = 0;
            for (int i = 0; i < inputSize; i++) {
                AudioStreamPlayer3D newNode = new AudioStreamPlayer3D();
                entitiesNode.AddChild(newNode);
                sounds[i].node = newNode;
                sounds[i].isPlaying = false;
                sounds[i].timeRemaining = 0f;
            }
        }
        
        public void Audio3DResize() {
            int newSize = sounds.Length * AUDIO_POOL_GROWTH_FACTOR;
            Array.Resize(ref sounds, newSize);

            for (int i = sounds.Length / AUDIO_POOL_GROWTH_FACTOR; i < newSize; i++) {
                AudioStreamPlayer3D newNode = new AudioStreamPlayer3D();
                entitiesNode.AddChild(newNode);
                sounds[i].node = newNode;
                sounds[i].isPlaying = false;
                sounds[i].timeRemaining = 0f;
            }
        }
        public void PlayAudio3D(AudioStream inputSound, Vector3 inputPosition, float inputVolume, float inputPitchModifier, bool inputShouldOverlap) {
            int availableSlot = -1;
            for (int i = 0; i < sounds.Length; i++) {
                if (sounds[i].node.Stream == inputSound && !inputShouldOverlap) {
                    GD.Print(inputSound + " does not want to overlap. Repeating instead");
                    sounds[i].node.GlobalPosition = inputPosition;
                    sounds[i].node.VolumeDb = Mathf.LinearToDb(inputVolume);
                    sounds[i].node.PitchScale = inputPitchModifier;
                    sounds[i].node.Play();
                    return;
                }
                if (!sounds[i].isPlaying) {
                    availableSlot = i;
                    break;
                }
            }
            if (availableSlot == -1) {
                int oldSize = sounds.Length;
                Audio3DResize();
                availableSlot = oldSize;
            }

            sounds[availableSlot].node.Stream = inputSound;
            sounds[availableSlot].node.GlobalPosition = inputPosition;
            sounds[availableSlot].node.VolumeDb = Mathf.LinearToDb(inputVolume);
            sounds[availableSlot].node.PitchScale = inputPitchModifier;
            sounds[availableSlot].node.Play();
            if (!sounds[availableSlot].isPlaying) {
                soundCount++;
            }
            sounds[availableSlot].isPlaying = true;
            sounds[availableSlot].timeRemaining = (float)inputSound.GetLength() / inputPitchModifier;
        }
    }
}
