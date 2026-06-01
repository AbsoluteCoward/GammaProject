using System;
using Godot;
namespace Gamma {
    public partial class Main : Node {
        public enum RewardType : byte {
            BloodVial,
        }
        public struct Reward {
            public Node3D node;
            public Node3D mesh;
            public RayCast3D raycast;
            public Vector3 desiredPosition;
        }
        public void RewardsInitialize(int inputSize) {
            GD.Print("Initializing rewards");
            rewards = new Reward[inputSize];
            rewardsCount = 0;
            for (int i = 0; i < inputSize; i++) {
                rewards[i] = new Reward();
            }
        }
        public void SpawnReward(RewardType inputType, Vector3 inputPosition, Vector3 inputDesiredPosition) {
            int availableSlot = -1;
            for (int i = 0; i < rewards.Length; i++) {
                if (rewards[i].node == null) {
                    availableSlot = i;
                    break;
                }
            }
            if (availableSlot == -1) {
                int oldSize = rewards.Length;
                int newSize = rewards.Length * ARRAY_GROWTH_FACTOR;
                Array.Resize(ref rewards, newSize);
                for (int i = rewards.Length / ARRAY_GROWTH_FACTOR; i < newSize; i++) {
                    rewards[i] = new Reward();
                }
                availableSlot = oldSize;
            }
            Reward reward = new Reward();
            switch (inputType) {
                case RewardType.BloodVial:
                    break;
                default:
                    break;
            }
            reward.node = rewardObjectScene.Instantiate<Node3D>();
            entitiesNode.AddChild(reward.node);
            reward.node.GlobalPosition = inputPosition;
            reward.mesh = reward.node.GetNode<Node3D>("Vial");
            reward.raycast = reward.node.GetNode<RayCast3D>("RayCast3D");
            reward.raycast.TopLevel = true;
            reward.raycast.GlobalPosition = inputPosition;
            reward.raycast.TargetPosition = reward.raycast.ToLocal(inputDesiredPosition);
            reward.raycast.ForceRaycastUpdate();
            Vector3 newDesiredPosition = reward.raycast.IsColliding() ? 
                reward.raycast.GetCollisionPoint() + reward.raycast.GetCollisionNormal() * 0.1f : 
                inputDesiredPosition;
            reward.desiredPosition = newDesiredPosition;
            rewards[availableSlot] = reward;
            rewardsCount++;
        }
        public void RewardsUpdate() {
            for (int i = 0; i < rewards.Length; i++) {
                if (rewards[i].node == null) { continue; }
                Reward reward = rewards[i];
                Vector3 distancetoPlayer = player.node.GlobalPosition - reward.node.GlobalPosition;
                float distanceToPlayerSq = distancetoPlayer.LengthSquared();
                if (distanceToPlayerSq < 2f) {
                    PlaySoundUI(sloshSFX, 0.1f, 1 + (float)GD.RandRange(-0.1f, 0.1f), true);
                    reward.node.QueueFree();
                    rewards[i] = new Reward();
                    rewardsCount--;
                    continue;
                }
                Vector3 distanceToDesired = reward.desiredPosition - reward.node.GlobalPosition;
                if (distanceToDesired.LengthSquared() > ALMOST_ZERO) {
                    Vector3 move = distanceToDesired.Normalized() * 5f * (float)globalPhysicsDelta;
                    if (move.LengthSquared() > distanceToDesired.LengthSquared()) { move = distanceToDesired; }
                    reward.node.GlobalPosition += move;
                    reward.raycast.GlobalPosition = reward.node.GlobalPosition;
                    reward.raycast.TargetPosition = Vector3.Down * 0.5f;
                    reward.raycast.ForceRaycastUpdate();
                } else {
                    reward.node.GlobalPosition = reward.desiredPosition;
                }
                reward.mesh.RotateY(1 * (float)globalPhysicsDelta);
                rewards[i] = reward;
            }
        }
    }
}
