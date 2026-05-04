using Godot;
using System;
namespace Gamma {
    public partial class Main : Node {
        public void OrbUpdate() {
            if (player.orb == null) { return; }
            if (!player.orb.TopLevel) { return; }
            if (!player.orb.Visible) { return; }
            player.orb.GlobalPosition += -player.orb.GlobalTransform.Basis.Z;
        }
    }
}