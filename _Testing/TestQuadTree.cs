using Godot;
using Meow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class TestQuadTree : Node3D
{
    [Export]
    public Rect2 TreeBound = new Rect2(new Vector2(-256, -256), new Vector2(512, 512));

    [Export]
    public n_Bound NodeTemp;
    [Export]
    public MeshInstance3D SelectorRange;
    [Export]
    public Material UnseletedMat;
    [Export]
    public Material SeletedMat;
    [Export]
    public n_LineMeshes SelectLine;
    [Export]
    public n_LineMeshes NodeLine;

    public Quadtree<n_Bound> Quadtree;

    public bool IsSelecting = false;
    public Vector3 LastPos = new Vector3();

    float timeLastAddNode = 0;

    public List<n_Bound> SelectingNodes = new List<n_Bound>();
    public override void _Ready()
    {
        base._Ready();
        Quadtree = new Quadtree<n_Bound>(TreeBound, (t) => t.GlobalRect, 4);
        NodeTemp.Visible = false;
        SelectorRange.Visible = false;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        var mouse_cursor_position = GetViewport().GetMousePosition();
 
        Camera3D camera = GetViewport().GetCamera3D();
        Vector3 cursorPoint;
        if (PointOnCursor(camera, out cursorPoint))
        {
            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                if (!IsSelecting)
                {
                    IsSelecting = true;
                    LastPos = cursorPoint;
                    SelectorRange.Visible = IsSelecting;
                }

                Vector3 min = new Vector3(Mathf.Min(cursorPoint.X, LastPos.X), 0.01f, Mathf.Min(cursorPoint.Z, LastPos.Z));
                Vector3 max = new Vector3(Mathf.Max(cursorPoint.X, LastPos.X), 0.01f, Mathf.Max(cursorPoint.Z, LastPos.Z));
                SelectLine.Points = new Vector3[2]
                {
                    min , max
                };

                SelectorRange.GlobalPosition = (cursorPoint + LastPos) / 2 * new Vector3(1,0,1);
                SelectorRange.Scale = new Vector3(max.X - min.X, 1, max.Z - min.Z) / 2;
            }
            else if (IsSelecting)
            {
                IsSelecting = false;
                //SelectorRange.Visible = IsSelecting;

                foreach (var item in SelectingNodes)
                {
                    var n = (item.FindChild("MeshInstance3D", false, false) as MeshInstance3D);
                    if (n != null)
                    {
                        n.MaterialOverride = UnseletedMat;

                    }
                    else
                        GD.PrintErr("MeshInstance3D is null");

                }

                // search tree
                Vector3 min = new Vector3(Mathf.Min(cursorPoint.X, LastPos.X), 0.01f, Mathf.Min(cursorPoint.Z, LastPos.Z));
                Vector3 max = new Vector3(Mathf.Max(cursorPoint.X, LastPos.X), 0.01f, Mathf.Max(cursorPoint.Z, LastPos.Z));
                SelectLine.Points = new Vector3[2]
                {
                    min , max
                };

                Stopwatch stopwatch = Stopwatch.StartNew();
                SelectingNodes = Quadtree.Query(new Rect2(min.ProjectToXZ(), (max - min).ProjectToXZ()));
                stopwatch.Stop();
                var elapsedUs = stopwatch.ElapsedTicks;
                GD.Print($"Query Execution Time: {elapsedUs * 1000000d / Stopwatch.Frequency}us   " + stopwatch.ElapsedMilliseconds + "ms" + "   SelectingNodes: " + SelectingNodes.Count);

                foreach (var item in SelectingNodes)
                {
                    var n = (item.FindChild("MeshInstance3D", false, false) as MeshInstance3D);
                    if (n != null)
                    {
                        n.MaterialOverride = SeletedMat;
                    }
                    else
                        GD.PrintErr("MeshInstance3D is null");
                }
            }

            if (Input.IsMouseButtonPressed(MouseButton.Right) && timeLastAddNode > 0.3f)
            {
                timeLastAddNode = 0;
                var newnode = NodeTemp.Duplicate() as n_Bound;
                NodeTemp.GetParent().AddChild(newnode);
                newnode.Visible = true;
                newnode.GlobalPosition = cursorPoint;
                newnode.Scale = new Vector3(GD.Randf() * 6f + 1, 1, GD.Randf() * 6f + 1f);
                newnode.Bound.Position = new Vector3(-0.5f,-0.5f,-0.5f);
                newnode.Bound.End = new Vector3(0.5f, 0.5f, 0.5f);
                newnode.Rotate(new Vector3(GD.Randf() - 0.5f, GD.Randf() - 0.5f, GD.Randf() - 0.5f).Normalized(), GD.Randf() * float.Pi);

                Stopwatch stopwatch = Stopwatch.StartNew();
                newnode.UpdateBound();
                var elapsedUs = stopwatch.ElapsedTicks;
                GD.Print($"UpdateBound Execution Time: {elapsedUs * 1000000d / Stopwatch.Frequency}us   " + stopwatch.ElapsedMilliseconds + "ms");
                var line = NodeLine.Duplicate() as n_LineMeshes;
                var nodeV = new Node();
                newnode.AddChild(nodeV); // 创建一个基础node，阻止父级的transform影响linemesh
                nodeV.AddChild(line);
                line.GlobalPosition = Vector3.Zero;
                line.Points = RectCornerInWorld(newnode.GlobalRect, 0.02f);
                line.Visible = true;

                stopwatch = Stopwatch.StartNew();
                Quadtree.Insert(newnode);
                stopwatch.Stop();
                elapsedUs = stopwatch.ElapsedTicks;
                GD.Print($"Insert Execution Time: {elapsedUs * 1000000d / Stopwatch.Frequency}us   " + stopwatch.ElapsedMilliseconds + "ms");
            }
        }
        timeLastAddNode += (float)delta;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3[] RectCornerInWorld(Rect2 rect, float Yoffset)
    {
        Vector3[] points = new Vector3[5];

        points[0] = new Vector3(rect.Position.X, Yoffset, rect.Position.Y);
        points[1] = new Vector3(rect.End.X, Yoffset, rect.Position.Y);
        points[2] = new Vector3(rect.End.X, Yoffset, rect.End.Y);
        points[3] = new Vector3(rect.Position.X, Yoffset, rect.End.Y);
        points[4] = new Vector3(rect.Position.X, Yoffset, rect.Position.Y);

        return points;
    }

    public static bool PointOnCursor(Camera3D camera, out Vector3 pos)
    {
        var mouseposition = camera.GetViewport().GetMousePosition();
        var from = camera.ProjectRayOrigin(mouseposition);
        var to = from + 100 * camera.ProjectRayNormal(mouseposition);

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithBodies = true;
        var result = camera.GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Any())
        {
            pos = (Vector3)result["position"];
            return true;
        }

        pos = Vector3.Zero;
        return false;
    }
}
