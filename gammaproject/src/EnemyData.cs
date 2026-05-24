using Godot;
namespace Gamma {
    public partial class Main : Node {
        EnemyParameters GenericParameters = new EnemyParameters() {
            type = EnemyType.Generic,
            moveSpeed = 2f,
        };
        EnemyParameters Crab01Parameters = new EnemyParameters() {
            type = EnemyType.Crab01,
            moveSpeed = 3f,
        };
    }
}
