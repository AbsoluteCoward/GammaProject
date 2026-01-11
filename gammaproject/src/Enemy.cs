using Godot;
namespace Gamma {
    public partial class Main : Node {
        public struct Enemy {
            public CharacterBody3D node;
            public AnimationPlayer animationPlayer;
            public AnimationTree animationTree;
        }
        public const int DEFAULT_ENEMY_CAPACITY = 12;
        public Enemy[] enemies;
        public int enemyCount;
        public void EnemyInitialize(CharacterBody3D inputNode) {
            if (enemyCount >= enemies.Length) {
                Enemy[] newEnemies = new Enemy[enemies.Length * 2];
                for (int i = 0; i < enemies.Length; i++) {
                    newEnemies[i] = enemies[i];
                }
                enemies = newEnemies;
                GD.Print("Enemy array resized to " + enemies.Length);
            }
            int index = enemyCount;
            enemies[index].node = inputNode;
            enemies[index].animationPlayer = inputNode.GetNode<AnimationPlayer>("bear/AnimationPlayer");
            enemies[index].animationTree = inputNode.GetNode<AnimationTree>("AnimationTree");
            enemyCount++;
            GD.Print($"{inputNode.Name} Initialized at index {index}");
        }
        public void EnemyUpdate() {
            for (int i = 0; i < enemyCount; i++) {
                CharacterBody3D enemyNode = enemies[i].node;
                if (enemyNode == null) { continue; }
                AnimationNodeStateMachinePlayback animationState = (AnimationNodeStateMachinePlayback)enemies[i].animationTree.Get("parameters/playback");
                animationState.Travel("Run");
                enemyNode.Velocity = -enemyNode.GlobalTransform.Basis.Z * 1.0f;
                enemyNode.Velocity += new Vector3(0, -9.8f * (float)globalDelta, 0);
                enemyNode.MoveAndSlide();
            }
        }
        public void EnemyRemove(int inputIndex) {
            if (inputIndex < 0 || inputIndex >= enemyCount) {
                GD.PrintErr("Bear Failed to remove: Invalid index!");
                return;
            }
            if (enemies[inputIndex].node != null) { enemies[inputIndex].node.QueueFree(); }
            for (int i = inputIndex; i < enemyCount - 1; i++) {
                enemies[i] = enemies[i + 1];
            }
            enemies[enemyCount - 1] = new Enemy();
            enemyCount--;
        }
    }
}
