using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        public struct SubtitleBox {
            public Node2D node;
            public VBoxContainer vboxContainer;
            public SubtitleLabel[] activeSubtitles;
        }
        public struct SubtitleLabel {
            public Control node;
            public Action<Main>[] onSubtitleComplete;
            public Action<Main>[] onSubtitleStart;
            public Label dialogueTextLabel;
            public float lifeTime;
        }
        public struct SubtitleData {
            public string text;
            public Action<Main>[] onSubtitleStart;
            public Action<Main>[] onSubtitleComplete;
            public float textSpeed;
            public float lifeTime;
            public bool shouldFreezePlayer;
        }
        public void SubtitlesInitialize(Node2D inputSubtitleNode) {
            subtitleBox.node = inputSubtitleNode;
            subtitleBox.vboxContainer = subtitleBox.node.GetChild<VBoxContainer>(0);
            subtitleBox.activeSubtitles = new SubtitleLabel[4];
        }
    }
}
