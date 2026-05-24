using Godot;
namespace Gamma {
    public partial class Main : Node {
        public enum EnemyType : byte {
            Bear,
            Crab
        }
        public enum EnemyState : byte {
            Idle,
            Wander,
            Attack,
            Flee,
            Dead
        }
        public struct Enemy {
            public CharacterBody3D node;
            public AnimationPlayer animationPlayer;
            public AnimationTree animationTree;
            public AnimationNodeStateMachinePlayback animationState;
            public Vector3 wishDirection;
            static public int enemyCount;
        }
        public void EnemyInitialize(CharacterBody3D inputNode) {
            if (Enemy.enemyCount >= enemies.Length) {
                Enemy[] newEnemies = new Enemy[enemies.Length * 2];
                for (int i = 0; i < enemies.Length; i++) {
                    newEnemies[i] = enemies[i];
                }
                enemies = newEnemies;
                GD.Print("Enemy array resized to " + enemies.Length);
            }
            int index = Enemy.enemyCount;
            enemies[index].node = inputNode;
            enemies[index].animationPlayer = inputNode.GetChild<Node3D>(0).GetNode<AnimationPlayer>("AnimationPlayer");
            enemies[index].animationTree = inputNode.GetNode<AnimationTree>("AnimationTree");
            enemies[index].animationState = (AnimationNodeStateMachinePlayback)enemies[index].animationTree.Get("parameters/playback");
            Enemy.enemyCount++;
            for (int i = 0; i < Enemy.enemyCount; i++) {
                Enemy enemy = enemies[i];
                enemy.wishDirection = new Vector3((float)GD.RandRange(-1f, 1f), 0, (float)GD.RandRange(-1f, 1f));
                enemies[i] = enemy;
            }
            GD.Print($"{inputNode.Name} Initialized at index {index}");
        }
        public void EnemyUpdate() {
            for (int i = 0; i < Enemy.enemyCount; i++) {
                Enemy enemy = enemies[i];
                CharacterBody3D enemyNode = enemies[i].node;
                Vector3 enemyForward = -enemyNode.GlobalTransform.Basis.Z.Normalized() * 2f;
                if (enemyNode == null) { continue; }
                enemyNode.Velocity += new Vector3(0, -9.8f * (float)globalPhysicsDelta, 0);
                enemy.animationState.Travel("Move");
                if (sceneState.physicsFramesSinceSceneLoad % 64 == 0) {
                    enemy.wishDirection = new Vector3((float)GD.RandRange(-1f, 1f), 0, (float)GD.RandRange(-1f, 1f));
                }
                RotateTowards(enemy.wishDirection, enemyNode, 0.01f);
                enemies[i] = enemy;
                enemyNode.Velocity = new Vector3(enemyForward.X, enemyNode.Velocity.Y, enemyForward.Z);
                enemyNode.MoveAndSlide();
            }
        }
        public void EnemyRemove(int inputIndex) {
            if (inputIndex < 0 || inputIndex >= Enemy.enemyCount) {
                GD.PrintErr("Enemy Failed to remove: Invalid index!");
                return;
            }
            if (enemies[inputIndex].node != null) { enemies[inputIndex].node.QueueFree(); }
            for (int i = inputIndex; i < Enemy.enemyCount - 1; i++) {
                enemies[i] = enemies[i + 1];
            }
            enemies[Enemy.enemyCount - 1] = new Enemy();
            Enemy.enemyCount--;
        }
    }
}
