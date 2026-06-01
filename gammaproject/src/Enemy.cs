using System;
using Godot;
namespace Gamma {
    public partial class Main : Node {
        public enum EnemyType : byte {
            Generic,
            Crab01
        }
        public enum EnemyState : byte {
            Wander,
            Attack,
            Flee,
            Dead
        }
        public struct EnemyParameters {
            public EnemyType type;
            public float moveSpeed;
            public float fear;
        }
        public struct Enemy {
            public CharacterBody3D node;
            public AnimationPlayer animationPlayer;
            public AnimationTree animationTree;
            public AnimationNodeStateMachinePlayback animationState;
            public EnemyType type;
            public EnemyState state;
            public Vector3 wishDirection;
        }
        public void EnemyRemove(int inputIndex) {
            int rewardAmount = (int)GD.RandRange(1, 4);
            for (int i = 0; i < rewardAmount; i++) {
                Vector3 RandomRewardposition = new Vector3(
                    (float)GD.RandRange(-1f, 1f),
                    1,
                    (float)GD.RandRange(-1f, 1f)
                ).Normalized() * (float)GD.RandRange(2f, 4f);
                SpawnReward(RewardType.BloodVial, enemies[inputIndex].node.GlobalPosition, enemies[inputIndex].node.GlobalPosition + RandomRewardposition);
            }
            if (inputIndex < 0 || inputIndex >= enemyCount) {
                GD.PrintErr("Enemy Failed to remove: Invalid index!");
                return;
            }
            for (int i = 0; i < player.targetCount; i++) {
                if (player.targets[i] == enemies[inputIndex].node) {
                    for (int j = i; j < player.targetCount - 1; j++) {
                        player.targets[j] = player.targets[j + 1];
                    }
                    player.targets[player.targetCount - 1] = null;
                    player.targetCount--;
                    if (player.currentTargetIndex > i) { 
                        player.currentTargetIndex--; 
                    } else if (player.currentTargetIndex == i) { 
                        player.currentTargetIndex = 0; 
                    }
                    break;
                }
            }
            enemies[inputIndex].node.Visible = false;
            enemies[inputIndex].node.ProcessMode = ProcessModeEnum.Disabled;
            enemies[inputIndex] = enemies[enemyCount - 1];
            enemies[enemyCount - 1] = new Enemy();
            enemyCount--;
        }
        public void EnemyGenericUpdate(ref Enemy inputEnemy) {
            Enemy enemy = inputEnemy;
            Vector3 enemyForward = -enemy.node.GlobalTransform.Basis.Z.Normalized();
            switch (enemy.state) {
                case EnemyState.Wander:
                    if (enemy.animationState.GetCurrentNode() == "Move") {
                        if (sceneState.physicsFramesSinceSceneLoad % 64 == 0) {
                            enemy.wishDirection = new Vector3((float)GD.RandRange(-1f, 1f), 0, (float)GD.RandRange(-1f, 1f)).Normalized();
                        }
                    }
                    if (sceneState.physicsFramesSinceSceneLoad % 128 == 0) {
                        bool shouldIdle = GD.Randf() < 0.5f;
                        if (shouldIdle) {
                            enemy.animationState.Travel("Idle");
                            enemy.wishDirection = Vector3.Zero;
                        } else {
                            enemy.animationState.Travel("Move");
                            enemy.wishDirection = new Vector3((float)GD.RandRange(-1f, 1f), 0, (float)GD.RandRange(-1f, 1f)).Normalized();
                        }
                    }
                    break;
                case EnemyState.Attack:
                    break;
                case EnemyState.Flee:
                    break;
                case EnemyState.Dead:
                    break;
            }
            switch (enemy.animationState.GetCurrentNode()) {
                case "Move":                    
                    break;
                case "Idle":
                    break;
            }
            enemy.node.Velocity = new Vector3(enemyForward.X * enemy.wishDirection.Length(), enemy.node.Velocity.Y, enemyForward.Z * enemy.wishDirection.Length());
            RotateTowards(enemy.wishDirection, enemy.node, 0.01f);
            enemy.node.Velocity += Vector3.Down * 9.8f * (float)globalPhysicsDelta;
            inputEnemy = enemy;
        }
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
            enemies[index].animationTree.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
            enemies[index].animationState = (AnimationNodeStateMachinePlayback)enemies[index].animationTree.Get("parameters/playback");
            enemies[index].animationState.Travel("Move");
            enemies[index].state = EnemyState.Wander;
            enemies[index].wishDirection = new Vector3((float)GD.RandRange(-1f, 1f), 0, (float)GD.RandRange(-1f, 1f));
            enemyCount++;
            GD.Print($"{inputNode.Name} Initialized at index {index}");
        }
        public void EnemyUpdate() {
            for (int i = 0; i < enemyCount; i++) {
                if (!enemies[i].node.Visible) { continue; }
                if (enemies[i].state == EnemyState.Dead) {
                    EnemyRemove(i);
                    i--;
                    continue; 
                }
                Enemy enemy = enemies[i];
                CharacterBody3D enemyNode = enemies[i].node;
                enemy.animationTree.Advance((float)globalPhysicsDelta);
                switch (enemy.type) {
                    case EnemyType.Generic:
                        EnemyGenericUpdate(ref enemy);
                        break;
                    case EnemyType.Crab01:
                        EnemyGenericUpdate(ref enemy);
                        break;
                }
                enemies[i] = enemy;
                enemyNode.MoveAndSlide();
            }
        }
    }
}
