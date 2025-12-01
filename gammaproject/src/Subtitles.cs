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
            public bool shouldFreezePlayer;
        }
        public void SubtitlesInitialize(VBoxContainer inputSubtitleNode) {
            subtitleBox.node = inputSubtitleNode;
            subtitleBox.activeSubtitles = new SubtitleLabel[3];
            for (int i = 0; i < subtitleBox.node.GetChildCount(); i++) {
                subtitleBox.activeSubtitles[i].dialogueTextLabel = subtitleBox.node.GetChild<Label>(i);
                subtitleBox.activeSubtitles[i].totalLifeTime = 10f;
                subtitleBox.activeSubtitles[i].currentLifeTime = subtitleBox.activeSubtitles[i].totalLifeTime;
            }
        }
        public void SubtitlesAdd(SubtitleData inputSubtitle) {
            for (int i = 0; i < subtitleBox.activeSubtitles.Length; i++) {
                if (subtitleBox.activeSubtitles[i].currentLifeTime <= 0f) {
                    subtitleBox.activeSubtitles[i].dialogueTextLabel.Text = inputSubtitle.text;
                    subtitleBox.activeSubtitles[i].dialogueTextLabel.Modulate = new Color(
                        inputSubtitle.textColor.R,
                        inputSubtitle.textColor.G,
                        inputSubtitle.textColor.B,
                        1f);
                    subtitleBox.activeSubtitles[i].textColor = inputSubtitle.textColor;
                    subtitleBox.activeSubtitles[i].totalLifeTime = inputSubtitle.totalLifeTime;
                    subtitleBox.activeSubtitles[i].currentLifeTime = inputSubtitle.totalLifeTime;
                    if (inputSubtitle.onSubtitleStart != null) {
                        for (int j = 0; j < inputSubtitle.onSubtitleStart.Length; j++) {
                            inputSubtitle.onSubtitleStart[j](this);
                        }
                    }
                    subtitleBox.activeSubtitles[i].onSubtitleComplete = inputSubtitle.onSubtitleComplete;
                    return;
                }
            }
        }
        public void SubtitlesUpdate() {
            for (int i = 0; i < subtitleBox.activeSubtitles.Length; i++) {
                if (subtitleBox.activeSubtitles[i].currentLifeTime > 0f) {
                    subtitleBox.activeSubtitles[i].currentLifeTime -= (float)globalDelta;
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
                        subtitleBox.activeSubtitles[i].onSubtitleComplete = null;
                    }
                    //reset subtitle
                    subtitleBox.activeSubtitles[i].dialogueTextLabel.Text = "";
                    subtitleBox.activeSubtitles[i].currentLifeTime = 0;
                    subtitleBox.activeSubtitles[i].totalLifeTime = 0;
                    subtitleBox.activeSubtitles[i].textColor = NULL_COLOR;
                    subtitleBox.activeSubtitles[i].onSubtitleStart = null;
                    subtitleBox.activeSubtitles[i].onSubtitleComplete = null;
                    subtitleBox.activeSubtitles[i].dialogueTextLabel.Modulate = NULL_COLOR;
                }
            }
        }
    }
}