using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        public struct SubtitleBox {
            public VBoxContainer node;
            public SubtitleLabel[] activeSubtitles;
        }
        public struct SubtitleLabel {
            public Color textColor;
            public Label dialogueTextLabel;
            public Action<Main>[] onSubtitleComplete;
            public Action<Main>[] onSubtitleStart;
            public float totalLifeTime;
            public float currentLifeTime;
        }
        public struct SubtitleData {
            public Color textColor;
            public string text;
            public Action<Main>[] onSubtitleStart;
            public Action<Main>[] onSubtitleComplete;
            public float totalLifeTime;
            public float currentLifeTime;
        }
        public SubtitleBox subtitleBox;
        public void SubtitlesInitialize(VBoxContainer inputSubtitleNode) {
            subtitleBox.node = inputSubtitleNode;
            subtitleBox.activeSubtitles = new SubtitleLabel[3];
            for (int i = 0; i < subtitleBox.node.GetChildCount(); i++) {
                subtitleBox.activeSubtitles[i].dialogueTextLabel = subtitleBox.node.GetChild<Label>(i);
                subtitleBox.activeSubtitles[i].dialogueTextLabel.Text = "";
                subtitleBox.activeSubtitles[i].dialogueTextLabel.Modulate = NULL_COLOR;
                subtitleBox.activeSubtitles[i].currentLifeTime = 0;
                subtitleBox.activeSubtitles[i].totalLifeTime = 0;
                subtitleBox.activeSubtitles[i].onSubtitleStart = null;
                subtitleBox.activeSubtitles[i].onSubtitleComplete = null;
                subtitleBox.activeSubtitles[i].textColor = NULL_COLOR;
            }
        }
        public void SubtitlesAdd(SubtitleData inputSubtitle) {
            for (int i = 0; i < subtitleBox.activeSubtitles.Length - 1; i++) {
                subtitleBox.activeSubtitles[i].textColor = subtitleBox.activeSubtitles[i + 1].textColor;
                subtitleBox.activeSubtitles[i].dialogueTextLabel.Text = subtitleBox.activeSubtitles[i + 1].dialogueTextLabel.Text;
                subtitleBox.activeSubtitles[i].dialogueTextLabel.Modulate = subtitleBox.activeSubtitles[i + 1].dialogueTextLabel.Modulate;
                subtitleBox.activeSubtitles[i].onSubtitleComplete = subtitleBox.activeSubtitles[i + 1].onSubtitleComplete;
                subtitleBox.activeSubtitles[i].onSubtitleStart = subtitleBox.activeSubtitles[i + 1].onSubtitleStart;
                subtitleBox.activeSubtitles[i].totalLifeTime = subtitleBox.activeSubtitles[i + 1].totalLifeTime;
                subtitleBox.activeSubtitles[i].currentLifeTime = subtitleBox.activeSubtitles[i + 1].currentLifeTime;
            }
            int lastIndex = subtitleBox.activeSubtitles.Length - 1;
            subtitleBox.activeSubtitles[lastIndex].textColor = inputSubtitle.textColor;
            subtitleBox.activeSubtitles[lastIndex].dialogueTextLabel.Text = inputSubtitle.text;
            subtitleBox.activeSubtitles[lastIndex].dialogueTextLabel.Modulate = inputSubtitle.textColor;
            subtitleBox.activeSubtitles[lastIndex].dialogueTextLabel.VisibleCharacters = 0;
            subtitleBox.activeSubtitles[lastIndex].onSubtitleComplete = inputSubtitle.onSubtitleComplete;
            subtitleBox.activeSubtitles[lastIndex].onSubtitleStart = inputSubtitle.onSubtitleStart;
            subtitleBox.activeSubtitles[lastIndex].totalLifeTime = inputSubtitle.totalLifeTime;
            subtitleBox.activeSubtitles[lastIndex].currentLifeTime = inputSubtitle.currentLifeTime;
            if (subtitleBox.activeSubtitles[lastIndex].onSubtitleStart != null) {
                for (int j = 0; j < subtitleBox.activeSubtitles[lastIndex].onSubtitleStart.Length; j++) {
                    subtitleBox.activeSubtitles[lastIndex].onSubtitleStart[j](this);
                }
            }
        }
        public void SubtitlesUpdate() {
            for (int i = 0; i < subtitleBox.activeSubtitles.Length; i++) {
                if (subtitleBox.activeSubtitles[i].currentLifeTime > 0f) {
                    if ((sceneState.physicsFramesSinceSceneLoad % 2) == 0) { subtitleBox.activeSubtitles[i].dialogueTextLabel.VisibleCharacters += 1; }
                    subtitleBox.activeSubtitles[i].currentLifeTime -= (float)globalPhysicsDelta;
                    subtitleBox.activeSubtitles[i].dialogueTextLabel.Modulate = new Color(
                        subtitleBox.activeSubtitles[i].dialogueTextLabel.Modulate.R,
                        subtitleBox.activeSubtitles[i].dialogueTextLabel.Modulate.G,
                        subtitleBox.activeSubtitles[i].dialogueTextLabel.Modulate.B,
                        subtitleBox.activeSubtitles[i].currentLifeTime < (subtitleBox.activeSubtitles[i].totalLifeTime / 2f) ?
                            subtitleBox.activeSubtitles[i].currentLifeTime / (subtitleBox.activeSubtitles[i].totalLifeTime / 2f) :
                            1f);
                } else {
                    if (subtitleBox.activeSubtitles[i].onSubtitleComplete != null) {
                        for (int j = 0; j < subtitleBox.activeSubtitles[i].onSubtitleComplete.Length; j++) {
                            subtitleBox.activeSubtitles[i].onSubtitleComplete[j](this);
                        }
                        GD.Print("Resetting subtitle slot " + i);
                        subtitleBox.activeSubtitles[i].dialogueTextLabel.Text = "";
                        subtitleBox.activeSubtitles[i].dialogueTextLabel.Modulate = NULL_COLOR;
                        subtitleBox.activeSubtitles[i].dialogueTextLabel.VisibleCharacters = 0;
                        subtitleBox.activeSubtitles[i].currentLifeTime = 0;
                        subtitleBox.activeSubtitles[i].totalLifeTime = 0;
                        subtitleBox.activeSubtitles[i].onSubtitleStart = null;
                        subtitleBox.activeSubtitles[i].onSubtitleComplete = null;
                        subtitleBox.activeSubtitles[i].textColor = NULL_COLOR;
                    }
                }
            }
        }
    }
}