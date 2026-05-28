using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        public enum InteractableLookup : byte {
            None,
            ChangeLevel,
            ExitDungeon,
            EnterDungeon,
            SlinkSinkDialogueStart,
            PotOpen,
            OilCandle,
            VideoTest,
            TestDialogue
        }
        public struct Interactable {
            public Node3D node;
            public InteractableLookup interaction;
        }
        public void InteractablesInitialize(Node3D inputNode, InteractableLookup inputInteraction) {
            for (int i = 0; i < interactables.Length; i++) {
                if (interactables[i].node == null) {
                    interactables[i].node = inputNode;
                    interactables[i].interaction = inputInteraction;
                    GD.Print($"{inputNode.Name} Initialized at index {i}");
                    return;
                }
            }
            GD.PrintErr("No space to add new interactable!");
        }
        public void Interact() {
            if (inputState.interact.isConsumed) { return; }
            inputState.interact.isConsumed = true;
            if (dialogueBox.node.Visible) {
                if (dialogueBox.dialogueTextLabel.VisibleRatio >= 1f || dialogueBox.dialogueTextLabel.VisibleCharacters >= dialogueBox.dialogueTextLabel.Text.Length) { DialogueEnd(); return; }
                dialogueBox.dialogueTextLabel.VisibleCharacters = dialogueBox.dialogueTextLabel.Text.Length;
                dialogueBox.node.Modulate = new Color(1f, 1f, 1f, 1.0f);
                dialogueBox.portraitCoverPanel.Modulate = new Color(1f, 1f, 1f, 1.0f);
                dialogueBox.portraitSprite.Modulate = new Color(1f, 1f, 1f, 1.0f);
                dialogueBox.speakerNameLabel.Modulate = new Color(1f, 1f, 1f, 1.0f);
                return;
            }
            int interactBoxSize = 2;
            Vector3 interactBoxCenter = player.node.GlobalPosition - (player.node.GlobalTransform.Basis.Z * interactBoxSize / 2f);
            float halfSize = interactBoxSize / 2f;
            for (int i = 0; i < interactables.Length; i++) {
                if (interactables[i].node == null) continue;
                var pos = interactables[i].node.GlobalPosition;
                if (pos.X >= interactBoxCenter.X - halfSize &&
                    pos.X <= interactBoxCenter.X + halfSize &&
                    pos.Z >= interactBoxCenter.Z - halfSize &&
                    pos.Z <= interactBoxCenter.Z + halfSize) {
                    switch (interactables[i].interaction) {
                        case InteractableLookup.None:
                            // SubtitlesAdd(new SubtitleData() {
                            //     text = "\"I will not.\"",
                            //     textColor = Colors.DarkRed,
                            //     totalLifeTime = 5f,
                            //     currentLifeTime = 5f,
                            //     onSubtitleStart = null,
                            //     onSubtitleComplete = null
                            // });
                            PlaySoundUI(GD.Load<AudioStream>("res://assets/sound/switch02.ogg"), 0.4f, 1f, false);
                            return;
                        case InteractableLookup.ChangeLevel:
                            string levelPath = (string)interactables[i].node.GetMeta("LevelPath");
                            ChangeScene(levelPath);
                            return;
                        case InteractableLookup.ExitDungeon:
                            ChangeScene("res://scenes/maps/level2.tscn");
                            return;
                        case InteractableLookup.EnterDungeon:
                            ChangeScene("res://scenes/maps/level1.tscn");
                            return;
                        case InteractableLookup.SlinkSinkDialogueStart:
                            DialogueStart(slinkSinkInteraction);
                            return;
                        case InteractableLookup.PotOpen:
                            if (interactables[i].node.GetChild<Node3D>(0).GetChild<AnimationPlayer>(2).CurrentAnimation == "Open") { goto case InteractableLookup.None; }
                            interactables[i].node.GetChild<Node3D>(0).GetChild<AnimationPlayer>(2).Play("Open");
                            SubtitlesAdd(new SubtitleData() {
                                text = "10 meat inside the pot.",
                                textColor = Colors.White,
                                totalLifeTime = 5f,
                                currentLifeTime = 5f,
                                onSubtitleStart = null,
                                onSubtitleComplete = null
                            });
                            interactables[i].interaction = InteractableLookup.None;
                            return;
                        case InteractableLookup.OilCandle:
                            if (!interactables[i].node.GetChild<Node3D>(0).GetChild<AnimationPlayer>(2).IsPlaying()) { goto case InteractableLookup.None; }
                            SubtitlesAdd(new SubtitleData() {
                                text = "You blow out the candle.",
                                textColor = Colors.White,
                                totalLifeTime = 5f,
                                currentLifeTime = 5f,
                                onSubtitleStart = null,
                                onSubtitleComplete = new Action<Main>[] {
                                    (Main) => Main.PlaySoundUI(GD.Load<AudioStream>("res://assets/sound/sigh.wav"), 1.5f, 1f, true),
                                    (Main) => interactables[i].node.GetChild<Node3D>(0).GetChild<MeshInstance3D>(1).Visible = false,
                                    (Main) => interactables[i].node.GetChild<Node3D>(0).GetChild<AnimationPlayer>(2).Stop(),
                                    (Main) => interactables[i].node.GetChild<Node3D>(0).GetChild<AnimationPlayer>(2).Free()
                                },
                            });
                            interactables[i].interaction = InteractableLookup.None;
                            return;
                        case InteractableLookup.VideoTest:
                            StartVideo(ref videoPlayer, testVideoData);
                            return;
                        case InteractableLookup.TestDialogue:
                            DialogueStart(testDialogueData);
                            return;
                    }
                }
            }
            DialogueStart(slinkTalkToSelf0);
        }
    }
}