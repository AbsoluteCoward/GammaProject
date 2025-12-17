using Godot;
namespace Gamma {
    public partial class Main : Node {
        public struct Enemy {
            public CharacterBody3D node;
        }
        public const int DEFAULT_ENEMY_CAPACITY = 12;
        public Enemy[] enemies = new Enemy[DEFAULT_ENEMY_CAPACITY];
        public int enemyCount;
        public void EnemyInitialize(CharacterBody3D inputNode) {
            enemies[0].node = inputNode;
        }
    }
}
