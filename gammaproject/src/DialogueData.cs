using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        public static DialogueData defaultDialogue = new DialogueData {
            speakerPortrait = GD.Load<Texture2D>("res://assets/textures/NA_MISSINGTEXTURE.png"),
            speakerName = "ERROR",
            text = "YOU ARE NOT SUPPOSED TO SEE THIS",
            onDialogueStart = null,
            onDialogueComplete = null,
            shouldSkipAnimation = true,
            textSpeed = 1f,
        };
        public static DialogueData slinkTalkToSelf1 = new DialogueData {
            speakerPortrait = GD.Load<Texture2D>("res://assets/textures/dialogueportraits/slinkportrait.png"),
            speakerName = "Slink",
            text = 15 + " uncooked meat",
            textSpeed = 2f,
            shouldSkipAnimation = false
        };
        public static DialogueData slinkTalkToSelf0 = new DialogueData {
            speakerPortrait = GD.Load<Texture2D>("res://assets/textures/dialogueportraits/slinkportrait.png"),
            speakerName = "Slink",
            text = "placeholder test",
            onDialogueComplete = new Action<Main>[] {
                (Main) => Main.DialogueStart(slinkTalkToSelf1)
            },
            textSpeed = 2f,
            shouldSkipAnimation = true
        };
        public static DialogueData slinkSinkInteraction = new DialogueData {
            speakerPortrait = GD.Load<Texture2D>("res://assets/textures/dialogueportraits/slinkportrait.png"),
            speakerName = "Slink",
            text = "wishy washy wash my hands i wash myy hands in the sink",
            onDialogueStart = null,
            onDialogueComplete = null,
            shouldSkipAnimation = true,
            textSpeed = 2f,
        };
    }
}
