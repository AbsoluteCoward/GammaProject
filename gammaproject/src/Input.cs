using Godot;
namespace Gamma {
    public partial class Main : Node {
        public enum InputMode : byte {
            Game,
            Dialogue
        }
        public struct InputAction { public bool isConsumed; }
        public struct InputState {
            public InputAction interact;
            public InputAction action1;
            public InputAction action2;
            public InputAction action3;
        }
        public InputState inputState = new InputState();
        public InputMode inputMode = InputMode.Game;
    }
}
