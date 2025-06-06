using Godot;
using System;
using System.Collections.Generic;

public partial class n_LineMeshes : MeshInstance3D
{
    [Export]
    public Vector3[] Points;
    [Export]
    public float[] UvSpan;
    [Export]
    public Color LineColor = Colors.AliceBlue;

    [Export]
    public float Width = 0.2f;

    List<Vector3> verts = [];
    List<Vector2> uvs = [];
    List<Vector3> normals = [];
    List<Color> colors = [];
    List<int> indices = [];

    public override void _Process(double delta)
    {
        if (Points != null)
        {
            if (Mesh != null)
            {
                Mesh.Dispose(); 
            }

            Mesh = new ArrayMesh();
            var arrMesh = Mesh as ArrayMesh;
            verts.Clear();
            uvs.Clear();
            normals.Clear();
            colors.Clear();
            indices.Clear();
            CreateLineSolid(Mesh as ArrayMesh, Vector3.Up, LineColor);
            Points = null;
        }

        base._Process(delta);
    }

    void CreateLineSolid(ArrayMesh arrMesh, Vector3 normalDir, Color colorBase)
    {
        
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        // mesh gen
        for (int i = 0; i < Points.Length - 1; i++)
        {
            var start = Points[i];
            var end = Points[i + 1];
            var dir = end - start;
            var right = dir.Cross(normalDir).Normalized()*Width;
            // 1
            var idx = verts.Count;
            var pos = start + right;
            var uv = Vector2.Zero;
            var color = colorBase;
            verts.Add(pos);
            uvs.Add(uv);
            colors.Add(color);
            normals.Add(normalDir);
            indices.Add(idx);

            // 2
            idx = verts.Count;
            pos = start - right;
            uv = Vector2.Zero;
            color = colorBase;
            verts.Add(pos);
            uvs.Add(uv);
            colors.Add(color);
            normals.Add(normalDir);
            indices.Add(idx);

            // 3
            idx = verts.Count;
            pos = start + right + dir;
            uv = Vector2.Zero;
            color = colorBase;
            verts.Add(pos);
            uvs.Add(uv);
            colors.Add(color);
            normals.Add(normalDir);
            indices.Add(idx); // first triangle finished

            indices.Add(idx); // next triangle
            indices.Add(idx - 1); // 

            // 4
            idx = verts.Count;
            pos = start - right + dir;
            uv = Vector2.Zero;
            color = colorBase;
            verts.Add(pos);
            uvs.Add(uv);
            colors.Add(color);
            normals.Add(normalDir);
            indices.Add(idx);
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Color] = colors.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        // No blendshapes, lods, or compression used.
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        //// Saves mesh to a .tres file with compression enabled.
        //ResourceSaver.Save(Mesh, "res://sphere.tres", ResourceSaver.SaverFlags.Compress);
    }


}
