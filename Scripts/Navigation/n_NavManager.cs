using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace Meow;

public partial class n_NavManager : Node
{
    public const uint Nav_Collision_Layer = 2;
    /// <summary>
    /// The ground meshes root node. Manager will bake the navMesh base on this.
    /// </summary>
    [Export]
    public Node GMeshRoot;

    [Export]
    public float MapCellSize = 0.25f;

    public List<n_NavType> NavTypes = new List<n_NavType>();

    public Vector3 _PathStartPosition = Vector3.Zero;

    public Vector3 TestStartPos = Vector3.Zero;
    public Vector3 TestEndPos = Vector3.Zero;
    [Export]
    public MeshInstance3D PathTester;
    [Export]
    public MeshInstance3D PathTester2;
    [Export]
    public PackedScene Obstacle;

    public static NavigationMeshSourceGeometryData3D source_geometry;
    public static HashSet<NavObstacle> navObstacles = new HashSet<NavObstacle>();

    public static void BakeAll(Node meshRoot, World3D world3D, float baseCellSize, List<n_NavType> navTypes)
    {
        GD.Print("____Start collect " + meshRoot.Name + "'s info");
        NavigationServer3D.SetDebugEnabled(true);
        
        var navMap = world3D.NavigationMap;
        NavigationServer3D.MapSetCellSize(navMap, baseCellSize);

        // Disable performance costly edge connection margin feature.
        // This feature is not needed to merge navigation mesh edges.
        // If edges are well aligned they will merge just fine by edge key.
        NavigationServer3D.MapSetUseEdgeConnections(navMap, false);

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Parse the collision shapes below our parse root node.
        source_geometry = new NavigationMeshSourceGeometryData3D();
        NavigationMesh parse_settings = new NavigationMesh();
        parse_settings.GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders;
        parse_settings.GeometryCollisionMask = Nav_Collision_Layer;
        NavigationServer3D.ParseSourceGeometryData(parse_settings, source_geometry, meshRoot);


        stopwatch.Stop();
        long elapsedMs = stopwatch.ElapsedMilliseconds;
        GD.Print($"ParseSourceGeometryData Time: {elapsedMs}ms");

        foreach (var type in navTypes)
        {
            foreach (var n in type.ChunkRoot.GetChildren())
            {
                n.QueueFree();
            }
            type.CreateRegionChunks(source_geometry);
        }
    }

    public override void _Ready()
    {
        base._Ready();
        var nodes = GetChildren();
        GD.Print(GMeshRoot.GetChildCount(true));

        foreach (var node in nodes)
        {
            if (node is n_NavType)
            {
                NavTypes.Add(node as n_NavType);
                GD.Print("NavType: add " +node.Name);
            }
        }
    }

    double lastObstacleSpawnEplase;
    bool baked = false;
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!baked)
        {
            BakeAll(GMeshRoot, PathTester.GetWorld3D(), MapCellSize, NavTypes);
            baked = true;
        }

        var mouse_cursor_position = GetViewport().GetMousePosition();
        Rid map = PathTester.GetWorld3D().NavigationMap;
        if (NavigationServer3D.MapGetIterationId(map) == 0)
            return;
        Camera3D camera = GetViewport().GetCamera3D();


        //var camera_ray_start = camera.ProjectRayOrigin(mouse_cursor_position);
        //var camera_ray_end = camera_ray_start + camera.ProjectRayNormal(mouse_cursor_position) * 1000;
        //var closest_point_on_navmesh = NavigationServer3D.MapGetClosestPointToSegment(map, camera_ray_start, camera_ray_end, true);
        var closest_point_on_navmesh = Godot.Vector3.Zero;
        bool needpath = false;
        if (PointOnNavMap(camera, out closest_point_on_navmesh))
        {
            if (Input.IsMouseButtonPressed(MouseButton.Left) && TestStartPos != closest_point_on_navmesh)
            {
                TestStartPos = closest_point_on_navmesh; ;
                needpath = true;
            }
            else if (Input.IsMouseButtonPressed(MouseButton.Right) && TestEndPos != closest_point_on_navmesh)
            {
                TestEndPos = closest_point_on_navmesh; ;
                needpath = true;
            }

            lastObstacleSpawnEplase += delta;
            if (Input.IsKeyPressed(Key.Space) && lastObstacleSpawnEplase > 0.3f)
            {
                var addpos = closest_point_on_navmesh;
                lastObstacleSpawnEplase = 0;
                var instance = Obstacle.Instantiate();
                GMeshRoot.AddChild(instance);
                (instance as Node3D).GlobalPosition = addpos;
                var obstacle = new NavObstacle(
                    new Godot.Vector3[4] { new Godot.Vector3(-1,0,-1) + addpos, new Godot.Vector3(1,0,-1) + addpos, new Godot.Vector3(1,0,1) + addpos, new Godot.Vector3(-1,0,1) + addpos },
                    -1f,
                    5f
                    );
                navObstacles.Add(obstacle);
                Stopwatch stopwatch = Stopwatch.StartNew();

                source_geometry.AddProjectedObstruction(obstacle.VerticesHorizontal, obstacle.YBase, obstacle.YHeight, false);

                stopwatch.Stop();
                var elapsedUs = stopwatch.ElapsedTicks;
                GD.Print($"Add obstacle Execution Time: {elapsedUs * 1000000d / Stopwatch.Frequency}us");

                foreach (var type in NavTypes)
                {
                    Vector3I corner = (Vector3I)((addpos - new Vector3(1.1f, 0.5f, 1.1f)) / type.ChunkSize).Floor();
                    Vector3I max = (Vector3I)((addpos + new Vector3(1.1f, 0.5f, 1.1f)) / type.ChunkSize).Floor();
                    for (int x = corner.X; x <= max.X; x++)
                    {
                        for (int z = corner.Z; z <= max.Z; z++)
                        {
                            type.BakeChunk(new Vector3I(x, 0, z), GMeshRoot);
                        }
                    }
                }
            }

        }


        if (needpath)
        {
            Vector3[] path, pathSmooth;

            Stopwatch stopwatch = Stopwatch.StartNew();
            {
                pathSmooth = NavigationServer3D.MapGetPath(map, TestStartPos, TestEndPos, true);
            }
            stopwatch.Stop();
            long elapsedUs = stopwatch.ElapsedTicks;
            GD.Print($"Path Smooth Execution Time: {elapsedUs * 1000000d / Stopwatch.Frequency}us");

            stopwatch = Stopwatch.StartNew();
            {
                path = NavigationServer3D.MapGetPath(map, TestStartPos, TestEndPos, false);
            }
            stopwatch.Stop();
            elapsedUs = stopwatch.ElapsedTicks;
            GD.Print($"Path Execution Time: {elapsedUs * 1000000d / Stopwatch.Frequency}us");

            (PathTester as n_LineMeshes).Points = path;
            (PathTester2 as n_LineMeshes).Points = pathSmooth;
            
        }
    }

    public static bool PointOnNavMap(Camera3D camera, out Vector3 pos)
    {
        var mouseposition = camera.GetViewport().GetMousePosition();
        var from = camera.ProjectRayOrigin(mouseposition);
        var to = from + 100 * camera.ProjectRayNormal(mouseposition);

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithBodies = true;
        query.CollisionMask = Nav_Collision_Layer;
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
