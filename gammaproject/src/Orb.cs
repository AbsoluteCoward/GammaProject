using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public struct TeleportOrb {
            public Node3D node;
            public MeshInstance3D model;
            public Skeleton3D skeleton;
            public RayCast3D collisionRaycast;
            public AnimationPlayer animationPlayer;
            public Vector3 positionLastFrame;
            public Vector3 velocity;
            public float timeAlive;
            public static int chestBoneIndex;
            public static int leftArmBoneIndex;
            public static int rightArmBoneIndex;
        }
        public void OrbInitialize(Node3D inputOrb) {
            player.orb.node = inputOrb;
            player.orb.collisionRaycast = (RayCast3D)player.orb.node.GetChild(1);
            GD.Print(player.orb.node.GetChildren());
            player.orb.model = player.orb.node.GetNode<MeshInstance3D>("Mantaray/Skeleton3D/Mantaray");
            player.orb.model.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            player.orb.skeleton = player.orb.node.GetNode<Skeleton3D>("Mantaray/Skeleton3D");
            for (int i = 0; i < player.orb.skeleton.GetBoneCount() - 1; i++) {
                if (player.orb.skeleton.GetBoneName(i) == "Head") { GD.Print("Head bone index is " + i); }
                if (player.orb.skeleton.GetBoneName(i) == "Spine.001") { TeleportOrb.chestBoneIndex = i; }
                if (player.orb.skeleton.GetBoneName(i) == "Arm.L") { TeleportOrb.leftArmBoneIndex = i; }
                if (player.orb.skeleton.GetBoneName(i) == "Arm.R") { TeleportOrb.rightArmBoneIndex = i; }
            }
            if (TeleportOrb.chestBoneIndex == 0) { GD.PrintErr("Couldn't find chest bone!"); }
            if (TeleportOrb.leftArmBoneIndex == 0) { GD.PrintErr("Couldn't find left arm bone!"); }
            if (TeleportOrb.rightArmBoneIndex == 0) { GD.PrintErr("Couldn't find right arm bone!"); }
            player.orb.model.SetBlendShapeValue(0, 1);
            player.orb.animationPlayer = player.orb.node.GetNode<AnimationPlayer>("Mantaray/AnimationPlayer");
            player.orb.animationPlayer.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
            player.orb.animationPlayer.Play("Fly");
            float orbRadius = 0.1f;
            TrailsCreate(trails, player.orb.node, Vector3.Forward * orbRadius, Colors.Cyan, orbRadius, 2f, 256, true);
        }
        public void OrbShoot() {
            GD.Print("Orb shoot");
            player.orb.node.Visible = true;
            player.orb.node.TopLevel = true;
            player.orb.velocity = player.orb.node.GlobalTransform.Basis.Z.Normalized() * (player.node.Velocity.Length() * 1.5f);
            player.orb.timeAlive = 0f;
            player.orb.positionLastFrame = player.orb.node.GlobalPosition;
        }
        public void OrbReturn(bool inputShouldCancel) {
            GD.Print("Orb return");
            player.orb.velocity = Vector3.Zero;
            player.orb.timeAlive = 0f;
            player.orb.positionLastFrame = Vector3.Zero;
            if (!inputShouldCancel) { PlayerTeleportTo(player.orb.collisionRaycast.GetCollisionPoint(), playerCamera); }
            player.orb.node.GlobalPosition = player.node.GlobalPosition; 
            player.orb.node.Visible = false;
            player.orb.node.TopLevel = false;
            player.orb.node.Scale = Vector3.One;
            player.orb.node.Rotation = Vector3.Zero;
            player.orb.model.SetBlendShapeValue(0, 1);
        }
        public void OrbApplyDynamicBoneTransformations(float inputSpeed) {
            if (player.orb.skeleton == null) { return; }
            TeleportOrb orb = player.orb;
            Basis nodeGlobalBasis = orb.node.GlobalTransform.Basis;
            Vector3 direction = inputSpeed > ALMOST_ZERO ? orb.velocity / inputSpeed : -orb.node.GlobalTransform.Basis.Z.Normalized();
            float pitchFactor = nodeGlobalBasis.Y.Dot(-direction) * 1.0f;
            float rollFactor = nodeGlobalBasis.X.Dot(-direction) * 0.6f;
            Transform3D chestPose = orb.skeleton.GetBoneGlobalPose(TeleportOrb.chestBoneIndex);
            Vector3 chestPitchAxis = orb.skeleton.GetBoneGlobalRest(TeleportOrb.chestBoneIndex).Basis.X;
            Vector3 chestRollAxis = orb.skeleton.GetBoneGlobalRest(TeleportOrb.chestBoneIndex).Basis.Z;
            Quaternion chestPitch = new Quaternion(chestPitchAxis, pitchFactor);
            Quaternion chestRoll = new Quaternion(chestRollAxis, rollFactor);
            chestPose.Basis = chestPose.Basis * new Basis(chestPitch * chestRoll);
            player.orb.skeleton.SetBoneGlobalPose(TeleportOrb.chestBoneIndex, chestPose);
            Transform3D headPose = orb.skeleton.GetBoneGlobalPose(TeleportOrb.chestBoneIndex + 1);
            Vector3 headPitchAxis = orb.skeleton.GetBoneGlobalRest(TeleportOrb.chestBoneIndex).Basis.X;
            Vector3 headRollAxis = orb.skeleton.GetBoneGlobalRest(TeleportOrb.chestBoneIndex).Basis.Z;
            Quaternion headPitch = new Quaternion(headPitchAxis, pitchFactor * 3);
            Quaternion headRoll = new Quaternion(headRollAxis, rollFactor * 3);
            headPose.Basis = headPose.Basis * new Basis(headPitch * headRoll);
            player.orb.skeleton.SetBoneGlobalPose(TeleportOrb.chestBoneIndex + 1, headPose);
        }
        public void OrbUpdate() {
            TeleportOrb orb = player.orb;
            orb.animationPlayer.Advance((float)globalPhysicsDelta);
            if (!orb.node.TopLevel || !orb.node.Visible) { return; }
            orb.timeAlive += (float)globalPhysicsDelta;
            float currentSpeed = orb.velocity.Length();
            // orb.node.Scale = new Vector3(
            //     orb.node.Scale.X,
            //     Mathf.Max(0.8f, 1f - currentSpeed * 0.05f),
            //     orb.node.Scale.X + currentSpeed * 0.2f
            // );
            player.orb.model.SetBlendShapeValue(0, MathF.Max(0f, 1 - currentSpeed * 0.1f));
            float clamp = Mathf.Clamp(9f / Mathf.Max(9f, currentSpeed), 0f, 1f);
            player.orb.animationPlayer.SpeedScale = 1f + 11f * Mathf.Pow(clamp, 3f);
            Vector3 orbForward = -orb.node.GlobalTransform.Basis.Z.Normalized();
            orb.velocity += Vector3.Down * GRAVITY_STRENGTH * (float)globalPhysicsDelta;
            orb.velocity *= 1f - 0.25f * (float)globalPhysicsDelta;
            if (orb.velocity.Dot(Vector3.Down) > 0f) {
                orb.velocity += Vector3.Down * (GRAVITY_STRENGTH/2) * (float)globalPhysicsDelta;
            }
            float lift = 0.8f;
            orb.velocity = orb.velocity.Lerp(orbForward * currentSpeed, lift);
            if (Input.IsActionJustPressed("action3")) { orb.velocity += orbForward * 2f; }
            if (inputDirection != Vector2.Zero) {
                float inputMagnitude = Mathf.Clamp(1f - (currentSpeed / 6f), 0.6f, 2f);
                inputMagnitude *= Mathf.Clamp(orb.timeAlive / 2f, 0f, 1f);
                orb.node.RotateObjectLocal(Vector3.Right, Mathf.DegToRad(inputDirection.Y * 8f * inputMagnitude));
                if (Input.IsActionPressed("mod")) {
                    orb.node.RotateObjectLocal(Vector3.Down, Mathf.DegToRad(inputDirection.X * 4f * inputMagnitude));
                } else {
                    orb.node.RotateObjectLocal(Vector3.Forward, Mathf.DegToRad(inputDirection.X * 8f * inputMagnitude));
                    orb.node.RotateObjectLocal(Vector3.Down, Mathf.DegToRad(inputDirection.X * 1f * inputMagnitude));
                }
            } else {
                // Vector3 target = (Vector3.Up - orbForward * orbForward.Dot(Vector3.Up)).Normalized();
                // float angle = orb.node.GlobalTransform.Basis.Y.Normalized().SignedAngleTo(target, orbForward);
                // float rollSpeed = 4f;
                // orb.node.RotateObjectLocal(Vector3.Forward, angle * rollSpeed * (float)globalPhysicsDelta);
            }
            if (currentSpeed < 6f) {
                float factor = 1f - (currentSpeed / 6f);
                orb.node.Basis = orb.node.Basis.Orthonormalized().Slerp(orb.node.GlobalTransform.LookingAt(orb.node.GlobalPosition + Vector3.Down, Vector3.Right).Basis, factor * 0.05f) * Basis.FromScale(orb.node.Scale);
            }
            Vector3 newPosition = orb.node.GlobalPosition + orb.velocity * (float)globalPhysicsDelta;
            orb.collisionRaycast.GlobalPosition = orb.positionLastFrame != Vector3.Zero
                ? orb.positionLastFrame
                : orb.node.GlobalPosition;
            orb.collisionRaycast.TargetPosition = orb.collisionRaycast.ToLocal(newPosition) * 2f;
            orb.collisionRaycast.ForceRaycastUpdate();
            if (orb.collisionRaycast.IsColliding()) {
                OrbReturn(false);
                return;
            }
            OrbApplyDynamicBoneTransformations(currentSpeed);
            orb.positionLastFrame = newPosition;
            orb.node.GlobalPosition = newPosition;
            player.orb = orb;
        }
    }
}