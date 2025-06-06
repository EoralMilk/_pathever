using Godot;
using Meow.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meow;

public class WorldMap
{
    public MapGridRenderChunk[] MapGridMeshes;
    public NavMap NavMap;

}


public class MapGridRenderChunk
{
    public ArrayMesh LocalMesh;
    Vector3[] verts;
    Vector2[] uvs;
    Vector3[] normals;
    Color[] colors;


    public void SetMeshVertices(MapGenSettings settings, NavMap map, int z, int x)
    {
        if (LocalMesh == null)
        {
            LocalMesh = new ArrayMesh();
        }

        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);


        var minx = Mathf.Max(x * settings.RenderChunkSize, 0);
        var minz = Mathf.Max(z * settings.RenderChunkSize, 0);
        var xlen = Mathf.Min(settings.RenderChunkSize, map.GridXLen - minx);
        var zlen = Mathf.Min(settings.RenderChunkSize, map.GridZLen - minz);
        var cells = map.GridCells.ExtractSubRectangleArray(minx, minz, xlen, zlen, map.GridXLen);
        verts = new Vector3[cells.Length * 6];
        uvs = new Vector2[cells.Length * 6];
        normals = new Vector3[cells.Length * 6];
        colors = new Color[cells.Length * 6];
        for (int i = 0; i < cells.Length; i++)
        {
            var nf1 = (cells[i].F1V2 - cells[i].F1V1).Cross(cells[i].F1V2 - cells[i].F1V3).Normalized();
            var nf2 = (cells[i].F2V2 - cells[i].F2V1).Cross(cells[i].F2V2 - cells[i].F2V3).Normalized();

            verts[i * 6 + 0] = cells[i].F1V1;
            uvs[i * 6 + 0] = new Vector2(cells[i].F1V1.X, cells[i].F1V1.Z) * 0.125f;
            normals[i * 6 + 0] = nf1;
            colors[i * 6 + 0] = Colors.AliceBlue;

            verts[i * 6 + 1] = cells[i].F1V2;
            uvs[i * 6 + 1] = new Vector2(cells[i].F1V2.X, cells[i].F1V2.Z) * 0.125f;
            normals[i * 6 + 1] = nf1;
            colors[i * 6 + 0] = Colors.AliceBlue;

            verts[i * 6 + 2] = cells[i].F1V3;
            uvs[i * 6 + 2] = new Vector2(cells[i].F1V3.X, cells[i].F1V3.Z) * 0.125f;
            normals[i * 6 + 2] = nf1;
            colors[i * 6 + 0] = Colors.AliceBlue;

            verts[i * 6 + 3] = cells[i].F2V1;
            uvs[i * 6 + 3] = new Vector2(cells[i].F2V1.X, cells[i].F2V1.Z) * 0.125f;
            normals[i * 6 + 3] = nf2;
            colors[i * 6 + 0] = Colors.AliceBlue;

            verts[i * 6 + 4] = cells[i].F2V2;
            uvs[i * 6 + 4] = new Vector2(cells[i].F2V2.X, cells[i].F2V2.Z) * 0.125f;
            normals[i * 6 + 4] = nf2;
            colors[i * 6 + 0] = Colors.AliceBlue;

            verts[i * 6 + 5] = cells[i].F2V3;
            uvs[i * 6 + 5] = new Vector2(cells[i].F2V3.X, cells[i].F2V3.Z) * 0.125f;
            normals[i * 6 + 5] = nf2;
            colors[i * 6 + 0] = Colors.AliceBlue;
        }

        // WIP smooth normals


        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Color] = colors.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();

        LocalMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

    }
}