using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct Sound3D {
            public AudioStreamPlayer3D node;
        }
        public struct SoundUI {
            public AudioStreamPlayer node;
        }
        public void Audio3DInitialize(int inputSize) {
            GD.Print("Initializing 3D audio");
            sounds3D = new Sound3D[inputSize];
            sounds3DCount = 0;
            for (int i = 0; i < inputSize; i++) {
                AudioStreamPlayer3D newNode = new AudioStreamPlayer3D();
                entitiesNode.AddChild(newNode);
                sounds3D[i].node = newNode;
            }
        }
        public void Audio3DResize() {
            int newSize = sounds3D.Length * ARRAY_GROWTH_FACTOR;
            Array.Resize(ref sounds3D, newSize);
            for (int i = sounds3D.Length / ARRAY_GROWTH_FACTOR; i < newSize; i++) {
                AudioStreamPlayer3D newNode = new AudioStreamPlayer3D();
                entitiesNode.AddChild(newNode);
                sounds3D[i].node = newNode;
            }
        }
        public void PlaySound3D(AudioStream inputSound, Vector3 inputPosition, float inputVolume, float inputPitchModifier, bool inputShouldOverlap) {
            int availableSlot = -1;
            for (int i = 0; i < sounds3D.Length; i++) {
                if (sounds3D[i].node.Stream == inputSound && !inputShouldOverlap) {
                    GD.Print(inputSound + " does not want to overlap. Repeating instead");
                    sounds3D[i].node.GlobalPosition = inputPosition;
                    sounds3D[i].node.VolumeDb = Mathf.LinearToDb(inputVolume);
                    sounds3D[i].node.PitchScale = inputPitchModifier;
                    sounds3D[i].node.Play();
                    return;
                }
                if (!sounds3D[i].node.Playing) {
                    availableSlot = i;
                    break;
                }
            }
            if (availableSlot == -1) {
                int oldSize = sounds3D.Length;
                Audio3DResize();
                availableSlot = oldSize;
            }
            sounds3D[availableSlot].node.Stream = inputSound;
            sounds3D[availableSlot].node.GlobalPosition = inputPosition;
            sounds3D[availableSlot].node.VolumeDb = Mathf.LinearToDb(inputVolume);
            sounds3D[availableSlot].node.PitchScale = inputPitchModifier;
            sounds3D[availableSlot].node.Play();
            if (!sounds3D[availableSlot].node.Playing) { sounds3DCount++; }
        }
        public void AudioUIInitialize(int inputSize) {
            GD.Print("Initializing UI audio");
            soundsUI = new SoundUI[inputSize];
            soundsUICount = 0;
            for (int i = 0; i < inputSize; i++) {
                AudioStreamPlayer newNode = new AudioStreamPlayer();
                entitiesNode.AddChild(newNode);
                soundsUI[i].node = newNode;
            }
        }
        public void AudioUIResize() {
            int newSize = soundsUI.Length * ARRAY_GROWTH_FACTOR;
            Array.Resize(ref soundsUI, newSize);
            for (int i = soundsUI.Length / ARRAY_GROWTH_FACTOR; i < newSize; i++) {
                AudioStreamPlayer newNode = new AudioStreamPlayer();
                entitiesNode.AddChild(newNode);
                soundsUI[i].node = newNode;
            }
        }
        public void PlaySoundUI(AudioStream inputSound, float inputVolume, float inputPitchModifier, bool inputShouldOverlap) {
            int availableSlot = -1;
            for (int i = 0; i < soundsUI.Length; i++) {
                if (soundsUI[i].node.Stream == inputSound && !inputShouldOverlap) {
                    GD.Print(inputSound + " does not want to overlap. Repeating instead");
                    soundsUI[i].node.VolumeDb = Mathf.LinearToDb(inputVolume);
                    soundsUI[i].node.PitchScale = inputPitchModifier;
                    soundsUI[i].node.Play();
                    return;
                }
                if (!soundsUI[i].node.Playing) {
                    availableSlot = i;
                    break;
                }
            }
            if (availableSlot == -1) {
                int oldSize = soundsUI.Length;
                AudioUIResize();
                availableSlot = oldSize;
            }
            soundsUI[availableSlot].node.Stream = inputSound;
            soundsUI[availableSlot].node.VolumeDb = Mathf.LinearToDb(inputVolume);
            soundsUI[availableSlot].node.PitchScale = inputPitchModifier;
            soundsUI[availableSlot].node.Play();
            if (!soundsUI[availableSlot].node.Playing) { soundsUICount++; }
        }
    }
}
