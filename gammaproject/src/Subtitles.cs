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
                        GD.Print("Resetting subtitle slot " + i);
                        subtitleBox.activeSubtitles[i].dialogueTextLabel.Text = "";
                        subtitleBox.activeSubtitles[i].dialogueTextLabel.Modulate = NULL_COLOR;
                        subtitleBox.activeSubtitles[i].currentLifeTime = 0;
                        subtitleBox.activeSubtitles[i].totalLifeTime = 0;
                        subtitleBox.activeSubtitles[i].onSubtitleStart = null;
                        subtitleBox.activeSubtitles[i].onSubtitleComplete = null;
                        subtitleBox.activeSubtitles[i].textColor = NULL_COLOR;
                    }
                }
            }
        }
        public void TestSubtitles() {
            SubtitleData test1 = new SubtitleData {
                text = "Hello, world!",
                textColor = Colors.White,
                totalLifeTime = 3f,
                currentLifeTime = 3f,
                onSubtitleStart = new Action<Main>[] {
                        (main) => GD.Print("Test 1 started!")
                    },
                onSubtitleComplete = new Action<Main>[] {
                        (main) => GD.Print("Test 1 completed!")
                    }
            };
            SubtitlesAdd(test1);
            SubtitleData test2 = new SubtitleData {
                text = "Warning: Low health!",
                textColor = Colors.Red,
                totalLifeTime = 2.5f,
                currentLifeTime = 2.5f,
                onSubtitleStart = new Action<Main>[] {
                        (main) => GD.Print("Warning subtitle displayed!")
                    }
            };
            SubtitlesAdd(test2);
            SubtitleData test3 = new SubtitleData {
                text = "This is a longer subtitle that might wrap to multiple lines in the UI.",
                textColor = Colors.Yellow,
                totalLifeTime = 5f,
                currentLifeTime = 5f,
                onSubtitleComplete = new Action<Main>[] {
                        (main) => GD.Print("Long subtitle finished!"),
                        (main) => GD.Print("Player unfrozen!")
                    }
            };
            SubtitlesAdd(test3);
            SubtitleData test4 = new SubtitleData {
                text = "Quick message",
                textColor = Colors.Green,
                totalLifeTime = 1f,
                currentLifeTime = 1f,
            };
            SubtitlesAdd(test4);
            GD.Print("Filling all subtitle slots...");
            for (int i = 0; i < 5; i++) {
                SubtitleData overflowTest = new SubtitleData {
                    text = $"Overflow test {i + 1}",
                    textColor = Colors.Cyan,
                    totalLifeTime = 2f,
                    currentLifeTime = 2f,
                    onSubtitleStart = new Action<Main>[] {
                            (main) => GD.Print($"Overflow subtitle {i + 1} started")
                        }
                };
                SubtitlesAdd(overflowTest);
            }
        }
    }
}