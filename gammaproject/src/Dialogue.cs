using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        public struct DialogueBox {
            public Action<Main>[] onDialogueComplete;
            public Action<Main>[] onDialogueStart;
            public Label dialogueTextLabel;
            public Label speakerNameLabel;
            public Sprite2D portraitSprite;
            public Panel portraitCoverPanel;
            public TextureRect gradient;
            public Control node;
            public float textSpeed;
            public float delay;
        }
        public struct DialogueData {
            public Texture2D speakerPortrait;
            public string speakerName;
            public string text;
            public Action<Main>[] onDialogueStart;
            public Action<Main>[] onDialogueComplete;
            public float textSpeed;
            public bool shouldSkipAnimation;
            public bool shouldFreezePlayer;
        }
        public DialogueBox dialogueBox;
        public void DialogueBoxInitialize(Control inputNode) {
            dialogueBox.node = inputNode;
            dialogueBox.node.Visible = false;
            dialogueBox.gradient = inputNode.GetChild<TextureRect>(0);
            dialogueBox.portraitSprite = inputNode.GetChild<CenterContainer>(1).GetChild<Sprite2D>(0);
            dialogueBox.portraitSprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            dialogueBox.portraitCoverPanel = inputNode.GetChild<Panel>(2);
            dialogueBox.speakerNameLabel = inputNode.GetNode<Label>("NameLabel");
            dialogueBox.dialogueTextLabel = inputNode.GetNode<Label>("TextLabel");
        }
        public void DialogueStart(DialogueData inputDialogue) {
            dialogueBox.speakerNameLabel.Text = inputDialogue.speakerName;
            dialogueBox.portraitSprite.Texture = inputDialogue.speakerPortrait;
            dialogueBox.dialogueTextLabel.Text = inputDialogue.text;
            dialogueBox.dialogueTextLabel.VisibleCharacters = 0;
            dialogueBox.textSpeed = inputDialogue.textSpeed;
            dialogueBox.delay = 0f;
            if (inputDialogue.speakerPortrait != null) {
                Vector2 textureSize = inputDialogue.speakerPortrait.GetSize();
                int hFrames = Mathf.Max(1, (int)(textureSize.X / 192));
                int vFrames = Mathf.Max(1, (int)(textureSize.Y / 192));
                dialogueBox.portraitSprite.Hframes = hFrames;
                dialogueBox.portraitSprite.Vframes = vFrames;
                dialogueBox.portraitSprite.Frame = 0;
                if (hFrames * vFrames > 1) {
                    GD.Print($"Sprite sheet detected: {hFrames}x{vFrames} = {hFrames * vFrames} frames");
                } else {
                    GD.Print("Single frame portrait detected.");
                }

            }
            if (inputDialogue.shouldSkipAnimation) {
                dialogueBox.node.Modulate = new Color(1f, 1f, 1f, 0.0f);
                dialogueBox.portraitCoverPanel.Modulate = new Color(1f, 1f, 1f, 0.0f);
                dialogueBox.portraitSprite.Modulate = new Color(1f, 1f, 1f, 0.0f);
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
            NoiseTexture2D noiseTexture = (NoiseTexture2D)dialogueBox.gradient.Texture;
            FastNoiseLite noise = (FastNoiseLite)noiseTexture.Noise;
            noise.Offset = new Vector3(noise.Offset.X + 1f, noise.Offset.Y, noise.Offset.Z);
            if (dialogueBox.node.Modulate.A < 1f) {
                dialogueBox.node.Modulate = new Color(1f, 1f, 1f, dialogueBox.node.Modulate.A + 0.1f);
            }
            if (dialogueBox.portraitSprite.Modulate.A < 1f) {
                dialogueBox.portraitSprite.Modulate = new Color(1f, 1f, 1f, dialogueBox.portraitSprite.Modulate.A + 0.1f);
                return;
            }
            if (dialogueBox.portraitSprite.Frame < dialogueBox.portraitSprite.Hframes * dialogueBox.portraitSprite.Vframes - 1 && sceneState.physicsFramesSinceSceneLoad % 4 == 0) {
                dialogueBox.portraitSprite.Frame += 1;
                return;
            }
            if (dialogueBox.delay > 0f) {
                dialogueBox.delay -= (float)globalPhysicsDelta;
                return;
            }
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
            if (Input.IsActionJustPressed("interact") && isCharacterDoneTalking && !inputState.interact.isConsumed) {
                DialogueEnd();
            }
        }
    }
}