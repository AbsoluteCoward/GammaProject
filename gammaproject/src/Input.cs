using Godot;
namespace Gamma {
    public partial class Main : Node {
        public enum InputMode : byte {
            Game,
            Dialogue
        }
        public struct InputState {
            InputAction action1;
            InputAction action2;
            InputAction action3;
        }
        public struct InputAction {
            bool isConsumed;
        }
        public InputMode inputMode = InputMode.Game;
        public bool interactInputConsumed;
    }
}
