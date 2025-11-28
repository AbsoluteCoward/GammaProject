using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        public struct DialogueBox {
            public Action<Main>[] onDialogueComplete;
            public Action<Main>[] onDialogueStart;
            public Label dialogueTextLabel;
            public Label speakerNameLabel;
            public TextureRect portraitTexture;
            public Panel portraitCoverPanel;
            public TextureRect gradient;
            public Control node;
            public float textSpeed;
            public float lifeTime;
            public float delay;
        }
        public struct DialogueData {
            public Texture2D speakerPortrait;
            public string speakerName;
            public string text;
            public Action<Main>[] onDialogueStart;
            public Action<Main>[] onDialogueComplete;
            public float textSpeed;
            public float lifeTime;
            public bool shouldSkipAnimation;
            public bool shouldFreezePlayer;
        }
        public void DialogueBoxInitialize(Control inputNode) {
            dialogueBox.node = inputNode;
            dialogueBox.node.Visible = false;
            dialogueBox.gradient = inputNode.GetChild<TextureRect>(0);
            dialogueBox.portraitTexture = inputNode.GetChild<CenterContainer>(1).GetChild<TextureRect>(0);
            dialogueBox.portraitTexture.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            dialogueBox.portraitCoverPanel = inputNode.GetChild<Panel>(2);
            dialogueBox.speakerNameLabel = inputNode.GetChild<VBoxContainer>(3).GetChild<Label>(0);
            dialogueBox.dialogueTextLabel = inputNode.GetChild<VBoxContainer>(3).GetChild<Label>(1);
        }
        public void DialogueStart(DialogueData inputDialogue) {
            dialogueBox.speakerNameLabel.Text = inputDialogue.speakerName;
            dialogueBox.portraitTexture.Texture = inputDialogue.speakerPortrait;
            dialogueBox.dialogueTextLabel.Text = inputDialogue.text;
            dialogueBox.dialogueTextLabel.VisibleCharacters = 0;
            dialogueBox.textSpeed = inputDialogue.textSpeed;
            dialogueBox.delay = 0f;
            if (inputDialogue.shouldSkipAnimation) {
                dialogueBox.node.Modulate = new Color(1f, 1f, 1f, 0.0f);
                dialogueBox.portraitCoverPanel.Modulate = new Color(1f, 1f, 1f, 0.0f);
                dialogueBox.portraitTexture.Modulate = new Color(1f, 1f, 1f, 0.0f);
                dialogueBox.speakerNameLabel.Modulate = new Color(1f, 1f, 1f, 0.0f);
                dialogueBox.delay = 1.5f;
            }
            dialogueBox.node.Visible = true;
            if (inputDialogue.onDialogueStart != null) {
                for (int i = 0; i < inputDialogue.onDialogueStart.Length; i++) {
                    inputDialogue.onDialogueStart[i](this);
                }
            }
            dialogueBox.onDialogueComplete = inputDialogue.onDialogueComplete;
            dialogueBox.lifeTime = inputDialogue.lifeTime;
        }
        public void DialogueEnd() {
            dialogueBox.node.Visible = false;
            Action<Main>[] completionActions = dialogueBox.onDialogueComplete;
            dialogueBox.onDialogueComplete = null;
            dialogueBox.onDialogueStart = null;
            if (completionActions != null) {
                for (int i = 0; i < completionActions.Length; i++) {
                    completionActions[i].Invoke(this);
                }
            }
        }
        public void DialogueUpdate() {
            if (!dialogueBox.node.Visible) { return; }
            if (dialogueBox.node.Modulate.A < 1f) {
                dialogueBox.node.Modulate = new Color(1f, 1f, 1f, dialogueBox.node.Modulate.A + 0.1f);
            }
            if (dialogueBox.portraitTexture.Modulate.A < 1f) {
                dialogueBox.portraitTexture.Modulate = new Color(1f, 1f, 1f, dialogueBox.portraitTexture.Modulate.A + 0.1f);
                return;
            }
            if (dialogueBox.delay > 1f) { dialogueBox.delay -= (float)globalDelta; return; }
            if (dialogueBox.portraitCoverPanel.Modulate.A < 1f) {
                dialogueBox.portraitCoverPanel.Modulate = new Color(1f, 1f, 1f, dialogueBox.portraitCoverPanel.Modulate.A + 0.1f);
            }
            if (dialogueBox.speakerNameLabel.Modulate.A < 1f) {
                dialogueBox.speakerNameLabel.Modulate = new Color(1f, 1f, 1f, dialogueBox.speakerNameLabel.Modulate.A + 0.03f);
                return;
            }
            if (sceneState.physicsFramesSinceSceneLoad % 4 == 0) {
                dialogueBox.dialogueTextLabel.VisibleCharacters += (int)dialogueBox.textSpeed;
            }
            bool isCharacterDoneTalking =
                dialogueBox.dialogueTextLabel.VisibleCharacters >= dialogueBox.dialogueTextLabel.Text.Length ||
                dialogueBox.dialogueTextLabel.VisibleRatio >= 1f;
            if (isCharacterDoneTalking) { dialogueBox.lifeTime--; }
            if (dialogueBox.lifeTime <= 0) { DialogueEnd(); }
        }
    }
}
