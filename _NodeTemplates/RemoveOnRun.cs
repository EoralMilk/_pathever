using Godot;
using System;

public partial class RemoveOnRun : Node
{
    public override void _Ready()
    {
        base._Ready();
        QueueFree();
    }
}
