using Godot;
using Meow;
using System;

public partial class TestTemp : Node2D
{
    public override void _Ready()
    {
        base._Ready();
        ArrayExtractorTests.RunTests();
    }
}
