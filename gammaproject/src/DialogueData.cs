using Godot;
using System;

namespace Gamma {
    public partial class Main : Node {
        public static DialogueData testDialogueData = new DialogueData {
            speakerPortrait = GD.Load<Texture2D>("res://assets/textures/dialogueportraits/spritesheet.png"),
            speakerName = "TEST",
            text = "This is a test dialogue. This is a test dialogue. This is a test dialogue. This is a test dialogue.",
            onDialogueStart = new Action<Main>[] {
                (Main) => Main.PlaySound3D(GD.Load<AudioStream>("res://assets/sound/notification.wav"), Main.player.node.GlobalPosition, 1f, 1f, true),
            },
            onDialogueComplete = new Action<Main>[] {
                (Main) => GD.Print("Dialogue completed!"),
            },
            shouldSkipAnimation = false,
            textSpeed = 2f,
        };
        public static DialogueData errorDialogue = new DialogueData {
            speakerPortrait = GD.Load<Texture2D>("res://assets/textures/NA_MISSINGTEXTURE.png"),
            speakerName = "ERROR",
            text = "YOU ARE NOT SUPPOSED TO SEE THIS",
            onDialogueStart = null,
            onDialogueComplete = null,
            shouldSkipAnimation = true,
            textSpeed = 1f,
        };
        public static DialogueData slinkTalkToSelf0 = new DialogueData {
            speakerPortrait = GD.Load<Texture2D>("res://assets/textures/dialogueportraits/spritesheet.png"),
            speakerName = "Hello player",
            text = "This is the 5/7/2026 build of pot fighter deluxe\n\nThank you for playing my game. If you find major bugs, please tell me\n\nIn particular, I really want the \"paper airplane orb\" to be fine-tuned and fun.",
            onDialogueComplete = new Action<Main>[] {
                (Main) => Main.DialogueStart(slinkTalkToSelf1)
            },
            textSpeed = 2f,
            shouldSkipAnimation = true
        };
        public static DialogueData slinkTalkToSelf1 = new DialogueData {
            speakerPortrait = GD.Load<Texture2D>("res://assets/textures/dialogueportraits/slinkportrait01.png"),
            speakerName = "Hello player",
            text = "If you have suggestions, also tell me that!\n\nWithout other people, it is impossible for a person to think for themselves\n\nI hope that this game, when it is complete, will help you think for yourself.",
            onDialogueComplete = new Action<Main>[] {
                (Main) => Main.DialogueStart(slinkTalkToSelf2)
            },
            textSpeed = 2f,
            shouldSkipAnimation = false
        };
        public static DialogueData slinkTalkToSelf2 = new DialogueData {
            speakerPortrait = GD.Load<Texture2D>("res://assets/textures/dialogueportraits/slinkportrait01.png"),
            speakerName = "Hello player",
            text = "Next, I will list some \"features\" of the game that you can either explore to play with or to find bugs",
            onDialogueComplete = new Action<Main>[] {
                (Main) => Main.DialogueStart(slinkTalkToSelf3)
            },
            textSpeed = 2f,
            shouldSkipAnimation = false
        };
        public static DialogueData slinkTalkToSelf3 = new DialogueData {
            speakerPortrait = GD.Load<Texture2D>("res://assets/textures/dialogueportraits/slinkportrait01.png"),
            speakerName = "Hello player",
            text = "There are 3 main areas to explore, but the testing stage has the most to do. The other two serve as blockouts to establish atmosphere\n\n In the testing area, there are enemies to shoot, mixed geometry to play with, and some interactables in the corner.",
            textSpeed = 2f,
            shouldSkipAnimation = false
        };
        public static DialogueData slinkSinkInteraction = new DialogueData {
            speakerPortrait = GD.Load<Texture2D>("res://assets/textures/dialogueportraits/slinkportrait01.png"),
            speakerName = "Slink",
            text = "wishy washy wash my hands i wash myy hands in the sink",
            onDialogueStart = null,
            onDialogueComplete = null,
            shouldSkipAnimation = true,
            textSpeed = 2f,
        };
    }
}
