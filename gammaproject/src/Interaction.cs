using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        public struct Interactable {
            public Node3D node;
            public InteractableLookup interaction;
        }
        public void Interact() {
            if (dialogueBox.node.Visible) {
                if (dialogueBox.dialogueTextLabel.VisibleRatio >= 1f || dialogueBox.dialogueTextLabel.VisibleCharacters >= dialogueBox.dialogueTextLabel.Text.Length) { DialogueEnd(); return;}
                dialogueBox.dialogueTextLabel.VisibleCharacters = dialogueBox.dialogueTextLabel.Text.Length;
                dialogueBox.node.Modulate = new Color(1f, 1f, 1f, 1.0f);
                dialogueBox.portraitCoverPanel.Modulate = new Color(1f, 1f, 1f, 1.0f);
                dialogueBox.portraitTexture.Modulate = new Color(1f, 1f, 1f, 1.0f);
                dialogueBox.speakerNameLabel.Modulate = new Color(1f, 1f, 1f, 1.0f);
                return;
            }
            PlayAudio3D(metalDinkSFX, player.node.GlobalPosition, 0.1f, 1.0f, false);
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
                        case InteractableLookup.ExitDungeon:
                            GD.Print("Entering level2");
                            ChangeScene("res://scenes/maps/level2.tscn");
                            return;
                        case InteractableLookup.EnterDungeon:
                            GD.Print("Entering level1");
                            ChangeScene("res://scenes/maps/level1.tscn");
                            return;
                        case InteractableLookup.SlinkSinkDialogueStart:
                            DialogueStart(slinkSinkInteraction);
                            return;
                    }
                }
            }
            DialogueStart(slinkTalkToSelf0);
        }
    }
}