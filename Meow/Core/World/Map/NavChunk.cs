using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Meow;

public abstract class NavChunk
{
    public abstract Vector3[] GetGeoVertices(NavMap map, float borderSize, NavType navType);

    /// <summary>
    /// min corner position is min x, min y, min z;
    /// </summary>
    public abstract Vector3 GetMinCorner(NavMap map);

    /// <summary>
    /// max corner position is max x, max y, max z;
    /// </summary>
    public abstract Vector3 GetMaxCorner(NavMap map);

    public abstract void BakeNavMesh(NavMap map, NavType navType);
}

public class NavChunkGrid: NavChunk
{
    /// <summary>
    /// the index in NavMap grid array
    /// </summary>
    public int _x, _z;
    public readonly Dictionary<NavType, NavigationRegion3D> _regions = new Dictionary<NavType, NavigationRegion3D>();
    public float _maxHeight, _minHeight;

    public NavChunkGrid(int x, int z)
    {
        _x = x;
        _z = z;
    }

    public NavCell[] GetCellsToBake(NavMap map, float borderSize)
    {
        int border = (int)((borderSize / map.CellSize) + 1);
        var minx = Mathf.Max(_x * map.GridChunkSize - border, 0);
        var minz = Mathf.Max(_z * map.GridChunkSize - border, 0);
        var xlen = Mathf.Min(map.GridChunkSize + border * 2, map.GridXLen - minx);
        var zlen = Mathf.Min(map.GridChunkSize + border * 2, map.GridZLen - minz);
        return map.GridCells.ExtractSubRectangleArray(minx, minz, xlen, zlen, map.GridXLen);
    }

    public void OnUpdateCell(NavCell navCell, int cx, int cz)
    {
        _maxHeight = Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(navCell.F1V1.Y, navCell.F1V2.Y), navCell.F1V3.Y), navCell.F2V1.Y), navCell.F2V2.Y),navCell.F2V3.Y)
            , _maxHeight);
        _minHeight = Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(navCell.F1V1.Y, navCell.F1V2.Y), navCell.F1V3.Y), navCell.F2V1.Y), navCell.F2V2.Y), navCell.F2V3.Y)
            , _minHeight);
    }

    public override Vector3[] GetGeoVertices(NavMap map, float borderSize, NavType navType)
    {
        var cells = GetCellsToBake(map, borderSize);
        Vector3[] verts = new Vector3[cells.Length * 6];
        for(int i = 0; i < cells.Length; i++)
        {
            verts[i * 6 + 0] = cells[i].F1V1;
            verts[i * 6 + 1] = cells[i].F1V2;
            verts[i * 6 + 2] = cells[i].F1V3;
            verts[i * 6 + 3] = cells[i].F2V1;
            verts[i * 6 + 4] = cells[i].F2V2;
            verts[i * 6 + 5] = cells[i].F2V3;
        }
        return verts;
    }

    public override Vector3 GetMinCorner(NavMap map)
    {
        return map.GridMinCorner + new Vector3(_x * map.CellSize * map.GridChunkSize, _minHeight  - 1f, _z * map.CellSize * map.GridChunkSize);
    }

    /// <summary>
    /// 不考虑原navmap边界，多出来的部分也拓展吧
    /// </summary>
    public override Vector3 GetMaxCorner(NavMap map)
    {
        return map.GridMinCorner + new Vector3((_x + 1) * map.CellSize * map.GridChunkSize, _maxHeight + 1f, (_z + 1) * map.CellSize * map.GridChunkSize);
    }

    public override void BakeNavMesh(NavMap map, NavType navType)
    {
        var mincorner = GetMinCorner(map);
        var maxcorner = GetMaxCorner(map);
        Aabb chunk_bounding_box = new Aabb(mincorner,
new Vector3(map.CellSize * map.GridChunkSize, maxcorner.Y - mincorner.Y, map.CellSize * map.GridChunkSize));
        var borderSize = navType.AgentRadius + navType.BakeCellRadius;
        Aabb baking_bounds = chunk_bounding_box.Grow(borderSize);
        NavigationMesh chunk_navmesh = new NavigationMesh();
        chunk_navmesh.GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders;
        chunk_navmesh.GeometryCollisionMask = NavType.CollisionLayer;
        chunk_navmesh.CellSize = 0.25f;
        chunk_navmesh.CellHeight = navType.BakeCellHeight;
        chunk_navmesh.FilterBakingAabb = baking_bounds;
        chunk_navmesh.BorderSize = borderSize;
        chunk_navmesh.AgentRadius = navType.AgentRadius;
        chunk_navmesh.AgentHeight = navType.AgentHeight;
        chunk_navmesh.SamplePartitionType = NavigationMesh.SamplePartitionTypeEnum.Layers;

        NavigationMeshSourceGeometryData3D source_geometry = new NavigationMeshSourceGeometryData3D();

        var node = map.World.GetLocalScene().FindChild(NavMap.NavColliderRoot);
        if (node == null)
        {
            throw new Exception("NavChunkBake: Can't find NavColliderRoot node");
        }
        else
        {
            // area ans shape must be set
            var area = map.World.GetLocalScene().FindChild("_NavAreaChecker") as Area3D;
            var cls = area.FindChild("CollisionShape3D") as CollisionShape3D;
            var box = cls.Shape as BoxShape3D;

            baking_bounds = baking_bounds.Grow(1);
            box.Size = baking_bounds.Size;
            cls.GlobalPosition = baking_bounds.Position + baking_bounds.Size / 2f;
            var bodies = area.GetOverlappingBodies();
            var tempnode = new Node();
            node.AddChild(tempnode);
            foreach (var b in bodies )
            {
                tempnode.AddChild(b);
            }
            NavigationServer3D.ParseSourceGeometryData(chunk_navmesh, source_geometry, tempnode);
            foreach (var b in bodies)
            {
                node.AddChild(b);
            }
            tempnode.QueueFree();

            //chunk_navmesh.GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.Both;
            source_geometry.AddFaces(GetGeoVertices(map, borderSize + 1f, navType), Transform3D.Identity);

            NavigationServer3D.BakeFromSourceGeometryData(chunk_navmesh, source_geometry);
            
            if (!_regions.ContainsKey(navType))
            {
                var rgn = new NavigationRegion3D();
                rgn.UseEdgeConnections = true;
                var rgnroot = map.World.GetLocalScene().FindChild(NavMap.NavRegionRoot);
                rgnroot.AddChild(rgn);
                if (Engine.IsEditorHint())
                    rgn.Owner = EditorInterface.Singleton.GetEditedSceneRoot();
                _regions.Add(navType, rgn);
            }

            var region = _regions[navType];
            //Vector3[] navmesh_vertices = chunk_navmesh.GetVertices();
            //for (int i = 0; i < navmesh_vertices.Length; i++)
            //{
            //    navmesh_vertices[i] = navmesh_vertices[i].Snapped(navType.BakeCellRadius * 0.1f);
            //}
            //chunk_navmesh.SetVertices(navmesh_vertices);

            region.NavigationMesh = chunk_navmesh;

        }


    }
}

