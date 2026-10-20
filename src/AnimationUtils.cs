using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct PlaybackPositionData {
            public string parameterName;
            public float previousPlaybackPosition;
            public float currentPlaybackPosition;
        }
        public int GetPlaybackBlockIndex(string inputParameterPath) {
            for (int i = 0; i < player.animationPlaybackBlocks.Length; i++) {
                if (player.animationPlaybackBlocks[i].parameterName == inputParameterPath) { return i; }
            }
            GD.PrintErr($"Couldn't find playback block for parameter {inputParameterPath}");
            return 0;
        }
        public bool HasCrossedPlaybackPosition(float inputPreviousPosition, float inputCurrentPosition, float inputEventPosition) {
            if (inputCurrentPosition >= inputPreviousPosition) { return inputPreviousPosition < inputEventPosition && inputEventPosition <= inputCurrentPosition; }
            return inputPreviousPosition < inputEventPosition || inputEventPosition <= inputCurrentPosition;
        }
        public string[] SetOneShotFilters(bool inputEnabled, string inputRootBoneName, Skeleton3D inputSkeleton, Node3D inputRootNode, AnimationNodeOneShot inputOneShotNode) {
            string[] filteredBoneNames = new string[inputSkeleton.GetBoneCount()];
            int filteredBoneCount = 0;
            inputOneShotNode.SetFilterEnabled(inputEnabled);
            if (!inputEnabled) { return filteredBoneNames; }
            int rootBone = inputSkeleton.FindBone(inputRootBoneName);
            if (rootBone == -1) {
                GD.PrintErr($"Bone \"{inputRootBoneName}\" not found!");
                return filteredBoneNames;
            }
            int[] boneChain = new int[32];
            int boneChainCount = 0;
            int currentBone = rootBone;
            boneChain[boneChainCount++] = rootBone;
            for (int i = 0; i < 28; i++) {
                int[] children = inputSkeleton.GetBoneChildren(currentBone);
                if (children.Length == 0) { break; }
                boneChain[boneChainCount++] = children[0];
                currentBone = children[0];
            }
            NodePath skeletonPath = inputRootNode.GetPathTo(inputSkeleton);
            int totalBones = inputSkeleton.GetBoneCount();
            for (int b = 0; b < totalBones; b++) {
                bool isInChain = false;
                for (int c = 0; c < boneChainCount; c++) {
                    if (boneChain[c] == b) { isInChain = true; break; }
                }
                if (isInChain) { continue; }
                string boneName = inputSkeleton.GetBoneName(b);
                inputOneShotNode.SetFilterPath(skeletonPath + ":" + boneName, true);
                filteredBoneNames[filteredBoneCount++] = boneName;
            }
            return filteredBoneNames;
        }
    }
}
