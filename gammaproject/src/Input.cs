using Godot;
namespace Gamma {
    public partial class Main : Node {
        public enum InputMode : byte {
            Game,
            Dialogue
        }
        public struct InputAction { 
            public bool isConsumed;
            public bool isPressed;
            public bool isJustPressed;
            public bool isJustReleased;
        }
        public struct InputState {
            public Vector2 inputDirection;
            public InputAction interact;
            public InputAction attack;
            public InputAction action1;
            public InputAction action2;
            public InputAction action3;
        }
        public void ResetInputState() {
            inputState.interact.isConsumed = false;
            inputState.attack.isConsumed = false;
            inputState.action1.isConsumed = false;
            inputState.action2.isConsumed = false;
            inputState.action3.isConsumed = false;
        }
        public bool InputJustPressed(ref InputAction inputAction, bool ignoreConsumed = false) {
            if (inputAction.isJustPressed && (ignoreConsumed || !inputAction.isConsumed)) {
                if (!ignoreConsumed) { inputAction.isConsumed = true; }
                return true;
            }
            return false;
        }
        public bool InputPressed(ref InputAction inputAction, bool ignoreConsumed = false) {
            if (inputAction.isPressed && (ignoreConsumed || !inputAction.isConsumed)) {
                if (!ignoreConsumed) { inputAction.isConsumed = true; }
                return true;
            }
            return false;
        }
        public bool InputJustReleased(ref InputAction inputAction, bool ignoreConsumed = false) {
            if (inputAction.isJustReleased && (ignoreConsumed || !inputAction.isConsumed)) {
                if (!ignoreConsumed) { inputAction.isConsumed = true; }
                return true;
            }
            return false;
        }
        public void UpdateInputState() {
            inputState.interact.isJustPressed = Input.IsActionJustPressed("interact");
            inputState.interact.isPressed = Input.IsActionPressed("interact");
            inputState.interact.isJustReleased = Input.IsActionJustReleased("interact");
            inputState.attack.isJustPressed = Input.IsActionJustPressed("attack");
            inputState.attack.isPressed = Input.IsActionPressed("attack");
            inputState.attack.isJustReleased = Input.IsActionJustReleased("attack");
            inputState.action1.isJustPressed = Input.IsActionJustPressed("action1");
            inputState.action1.isPressed = Input.IsActionPressed("action1");
            inputState.action1.isJustReleased = Input.IsActionJustReleased("action1");
            inputState.action2.isJustPressed = Input.IsActionJustPressed("action2");
            inputState.action2.isPressed = Input.IsActionPressed("action2");
            inputState.action2.isJustReleased = Input.IsActionJustReleased("action2");
            inputState.action3.isJustPressed = Input.IsActionJustPressed("action3");
            inputState.action3.isPressed = Input.IsActionPressed("action3");
            inputState.action3.isJustReleased = Input.IsActionJustReleased("action3");
        }
    }
}
