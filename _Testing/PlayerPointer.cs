using Godot;
using System;
using System.Linq;

public partial class PlayerPointer : Camera3D
{
    [Export]
    public TestAgent TestAgent;
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Input.IsActionJustPressed("click right"))
        {
            GD.Print("click right");

            var mouseposition = GetViewport().GetMousePosition();
            var from = ProjectRayOrigin(mouseposition);
            var to = from + 100 * ProjectRayNormal(mouseposition);

            var query = PhysicsRayQueryParameters3D.Create(from, to);
            query.CollideWithBodies = true;
            var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (result.Any())
            {
               GD.Print(result["position"]);
               TestAgent.MovementTarget = (Vector3)result["position"];
            }
        }
    }
}
