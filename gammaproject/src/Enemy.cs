using System;
using Godot;
namespace Gamma {
    public partial class Main : Node {
        public enum EnemyType : byte {
            Generic,
            Crab01
        }
        public enum EnemyState : byte {
            Idle,
            Wander,
            Attack,
            Flee,
            Dead
        }
        public struct EnemyParameters {
            public EnemyType type;
            public float moveSpeed;
        }
        public struct Enemy {
            public CharacterBody3D node;
            public AnimationPlayer animationPlayer;
            public AnimationTree animationTree;
            public AnimationNodeStateMachinePlayback animationState;
            public EnemyType type;
            public Vector3 wishDirection;
            static public int enemyCount;
        }
        public void EnemyGenericUpdate(Enemy enemy) {
            Vector3 enemyForward = -enemy.node.GlobalTransform.Basis.Z.Normalized();
            if (sceneState.physicsFramesSinceSceneLoad % 2 == 0) {
                enemy.wishDirection = new Vector3((float)GD.RandRange(-1f, 1f), 0, (float)GD.RandRange(-1f, 1f));
            }
            RotateTowards(enemy.wishDirection, enemy.node, 0.01f);
            enemy.node.Velocity = new Vector3(enemyForward.X, enemy.node.Velocity.Y, enemyForward.Z);
            enemy.node.Velocity += Vector3.Down * 9.8f * (float)globalPhysicsDelta;
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
            enemies[index].animationTree.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
            enemies[index].animationState = (AnimationNodeStateMachinePlayback)enemies[index].animationTree.Get("parameters/playback");
            enemies[index].animationState.Travel("Move");
            enemies[index].wishDirection = new Vector3((float)GD.RandRange(-1f, 1f), 0, (float)GD.RandRange(-1f, 1f));
            Enemy.enemyCount++;
            GD.Print($"{inputNode.Name} Initialized at index {index}");
        }
        public void EnemyUpdate() {
            for (int i = 0; i < Enemy.enemyCount; i++) {
                Enemy enemy = enemies[i];
                CharacterBody3D enemyNode = enemies[i].node;
                enemy.animationTree.Advance((float)globalPhysicsDelta);
                switch (enemy.type) {
                    case EnemyType.Generic:
                        EnemyGenericUpdate(enemy);
                        break;
                    case EnemyType.Crab01:
                        EnemyGenericUpdate(enemy);
                        break;
                }
                enemies[i] = enemy;
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
