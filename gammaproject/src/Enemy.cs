using Godot;
namespace Gamma {
    public partial class Main : Node {
        public struct Enemy {
            public CharacterBody3D node;
        }
        public const int DEFAULT_ENEMY_CAPACITY = 12;
        public Enemy[] enemies;
        public int enemyCount;
        public void EnemyInitialize(CharacterBody3D inputNode) {
            if (enemyCount >= enemies.Length) {
                GD.PrintErr("Bear Failed to initialize: No space to add new enemy!");
                return;
            }
            int index = enemyCount;
            enemies[index].node = inputNode;
            enemyCount++;
            GD.Print($"{inputNode.Name} Initialized at index {index}");
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
