using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meow;

public partial class n_NavType : Node
{

    //static var map_cell_size: float = 0.25
    //static var chunk_size: int = 16
    //static var cell_size: float = 0.25
    //static var agent_radius: float = 0.5
    //static var chunk_id_to_region: Dictionary = {}
    [Export]
    public Node3D ChunkRoot;
    [Export]
    public float CellSize = 0.25f;
    [Export]
    public float CellHeight = 0.25f;
    [Export]
    public float ChunkSize = 16;
    [Export]
    public float ChunkBorderSize = 1;


    [Export]
    public float AgentRadius = 0.5f;
    [Export]
    public float AgentHeight = 1.5f;

    public Dictionary<Vector3I, NavigationRegion3D> Id2Chunk = new Dictionary<Vector3I, NavigationRegion3D> ();

    public void CreateRegionChunks(NavigationMeshSourceGeometryData3D p_source_geometry)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();


        Id2Chunk.Clear ();
        // We need to know how many chunks are required for the input geometry.
        // So first get an axis aligned bounding box that covers all vertices.
        var input_geometry_bounds = CalculateSourceGeometryBounds(p_source_geometry);

        // Rasterize bounding box into chunk grid to know range of required chunks.
        Vector3 start_chunk = (input_geometry_bounds.Position / ChunkSize).Floor();

        Vector3 end_chunk = ((input_geometry_bounds.Position + input_geometry_bounds.Size)
        / ChunkSize).Floor();
        GD.Print("Bound: start: " + input_geometry_bounds.Position + " size: "+ input_geometry_bounds.Size);
        GD.Print(start_chunk.X + " " + start_chunk.Z + "  ---  " + end_chunk.X +" " +  end_chunk.Z);

        // NavigationMesh.border_size is limited to the xz-axis.
        // So we can only bake one chunk for the y-axis and also
        // need to span the bake bounds over the entire y-axis.
        // If we dont do this we would create duplicated polygons
        // and stack them on top of each other causing merge errors.
        float bounds_min_height = input_geometry_bounds.Position.Y - 0.1f;
        float bounds_max_height = input_geometry_bounds.Size.Y + input_geometry_bounds.Position.Y + 0.1f;
        int chunk_y = 0;


        for (int z = (int)start_chunk.Z; z < (int)end_chunk.Z + 1; z++)
        {
            for (int x = (int)start_chunk.X; x < (int)end_chunk.X + 1; x++)
            {
                Vector3I chunk_id = new Vector3I(x, chunk_y, z);
                Aabb chunk_bounding_box = new Aabb(new Vector3(x * ChunkSize, bounds_min_height, z * ChunkSize),
                new Vector3(ChunkSize, bounds_max_height - bounds_min_height, ChunkSize));

                // We grow the chunk bounding box to include geometry
                // from all the neighbor chunks so edges can align.
                // The border size is the same value as our grow amount so
                // the final navigation mesh ends up with the intended chunk size.
                Aabb baking_bounds = chunk_bounding_box.Grow(ChunkBorderSize);
                NavigationMesh chunk_navmesh = new NavigationMesh();
                chunk_navmesh.GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders;
                chunk_navmesh.GeometryCollisionMask = n_NavManager.Nav_Collision_Layer;
                chunk_navmesh.CellSize = CellSize;
                chunk_navmesh.CellHeight = CellHeight;
                chunk_navmesh.FilterBakingAabb = baking_bounds;
                chunk_navmesh.BorderSize = ChunkBorderSize;
                chunk_navmesh.AgentRadius = AgentRadius;
                chunk_navmesh.AgentHeight = AgentHeight;
                chunk_navmesh.SamplePartitionType = NavigationMesh.SamplePartitionTypeEnum.Layers;
                NavigationServer3D.BakeFromSourceGeometryData(chunk_navmesh, p_source_geometry);

                //// The only reason we reset the baking bounds here is to not render its debug.
                //chunk_navmesh.FilterBakingAabb = new Aabb();

                // Snap vertex positions to avoid most rasterization issues with float precision.
                Vector3[] navmesh_vertices = chunk_navmesh.GetVertices();
                for (int i = 0; i < navmesh_vertices.Length; i++)
                {
                    navmesh_vertices[i] = navmesh_vertices[i].Snapped(CellSize * 0.1f);
                }
                chunk_navmesh.SetVertices(navmesh_vertices);
                NavigationRegion3D chunk_region = new NavigationRegion3D();
                chunk_region.NavigationMesh = chunk_navmesh;
                ChunkRoot.AddChild(chunk_region);
                Id2Chunk.Add(chunk_id,chunk_region);
                if (Engine.IsEditorHint())
                    chunk_region.Owner = EditorInterface.Singleton.GetEditedSceneRoot();
            }
        }


        stopwatch.Stop();
        // 输出结果（单位：毫秒）
        long elapsedMs = stopwatch.ElapsedMilliseconds;
        GD.Print($"{this.Name} Bake Execution Time: {elapsedMs}ms");
    }

    /// <summary>
    /// WIP : ParseSourceGeometryData will process everything in the GMesh, need optimize
    /// </summary>
    /// <param name="id"></param>
    /// <param name="meshRoot"></param>
    public void BakeChunk(Vector3I id, Node meshRoot)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        //Id2Chunk[id].BakeNavigationMesh();
        var chunk_navmesh = Id2Chunk[id].NavigationMesh;

        // Parse the collision shapes below our parse root node.
        //NavigationMeshSourceGeometryData3D source_geometry = new NavigationMeshSourceGeometryData3D();
        //NavigationServer3D.ParseSourceGeometryData(chunk_navmesh, source_geometry, meshRoot);
        //GD.Print($"ParseSourceGeometryData Chunk {id} Execution Time: {elapsedUs * 1000000d / Stopwatch.Frequency}us");
        //GD.Print($"Path Execution Time: {stopwatch.ElapsedMilliseconds}ms");
        //GD.Print("Bound: start: " + source_geometry.GetBounds().Position + " size: " + source_geometry.GetBounds().Size);

        NavigationServer3D.BakeFromSourceGeometryData(chunk_navmesh, n_NavManager.source_geometry);


        // Snap vertex positions to avoid most rasterization issues with float precision.
        Vector3[] navmesh_vertices = chunk_navmesh.GetVertices();
        for (int i = 0; i < navmesh_vertices.Length; i++)
        {
            navmesh_vertices[i] = navmesh_vertices[i].Snapped(CellSize * 0.1f);
        }
        chunk_navmesh.SetVertices(navmesh_vertices);
        Id2Chunk[id].NavigationMesh = chunk_navmesh;

        stopwatch.Stop();
        var elapsedUs = stopwatch.ElapsedTicks;
        GD.Print($"BakeChunk {id} Execution Time: {elapsedUs * 1000000d / Stopwatch.Frequency}us");

    }

    public static Aabb CalculateSourceGeometryBounds(NavigationMeshSourceGeometryData3D p_source_geometry)
    {
        return p_source_geometry.GetBounds();
    }

}
