using Godot;
using Meow;
using System;
using System.Runtime.CompilerServices;

public partial class n_Bound : Node3D
{
    [Export]
    public Aabb Bound;

    public Rect2 GlobalRect;
    public Aabb GlobalBound;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateBound() => this.GetGlobalBoundary(Bound, ref GlobalRect, ref GlobalBound);
}
