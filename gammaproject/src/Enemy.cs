using Godot;
namespace Gamma {
    public partial class Main : Node {
        public struct Enemy {
            public CharacterBody3D node;
            public AnimationPlayer animationPlayer;
            public AnimationTree animationTree;
            public Vector3 wishDirection;
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
            enemies[index].animationPlayer = inputNode.GetChild<Node3D>(0).GetNode<AnimationPlayer>("AnimationPlayer");
            enemies[index].animationTree = inputNode.GetNode<AnimationTree>("AnimationTree");
            enemyCount++;
            for (int i = 0; i < enemyCount; i++) {
                Enemy enemy = enemies[i];
                enemy.wishDirection = new Vector3((float)GD.RandRange(-1f, 1f), 0, (float)GD.RandRange(-1f, 1f));
                enemies[i] = enemy;
            }
            GD.Print($"{inputNode.Name} Initialized at index {index}");
        }
        public void EnemyUpdate() {
            for (int i = 0; i < enemyCount; i++) {
                CharacterBody3D enemyNode = enemies[i].node;
                if (enemyNode == null) { continue; }
                Enemy enemy = enemies[i];
                AnimationNodeStateMachinePlayback animationState = (AnimationNodeStateMachinePlayback)enemies[i].animationTree.Get("parameters/playback");
                animationState.Travel("Move");
                if (sceneState.physicsFramesSinceSceneLoad % 64 == 0) {
                    enemy.wishDirection = new Vector3((float)GD.RandRange(-1f, 1f), 0, (float)GD.RandRange(-1f, 1f));
                }
                RotateTowards(enemy.wishDirection, enemyNode, 1f);
                enemies[i] = enemy;
                Vector3 enemyForward = -enemyNode.GlobalTransform.Basis.Z.Normalized() * 2f;
                enemyNode.Velocity = new Vector3(enemyForward.X, enemyNode.Velocity.Y, enemyForward.Z);
                enemyNode.Velocity += new Vector3(0, -9.8f * (float)globalPhysicsDelta, 0);
                enemyNode.MoveAndSlide();
            }
        }
        public void EnemyRemove(int inputIndex) {
            if (inputIndex < 0 || inputIndex >= enemyCount) {
                GD.PrintErr("Enemy Failed to remove: Invalid index!");
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
