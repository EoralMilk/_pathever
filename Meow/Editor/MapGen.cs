using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meow.Editor;

public class MapGenerator
{
    public MapGenSettings Settings;
    public WorldMap Current;

    

    public WorldMap CreateMap(Node3D rootNode)
    {
        Current = new WorldMap();

        var mincorner = new Vector3(-Settings.GridXLen / 2 * Settings.CellSize, 0, -Settings.GridZLen / 2 * Settings.CellSize);
        //GD.Print("mincorner " + mincorner);
        Current.NavMap = new NavMap(Settings.GridZLen, Settings.GridXLen, Settings.CellSize, Settings.NavChunkSize, mincorner, rootNode.GetWorld3D());

        int chunkXlen = Settings.GridXLen / Settings.RenderChunkSize + (Settings.GridXLen % Settings.RenderChunkSize == 0 ? 0 : 1);
        int chunkZlen = Settings.GridZLen / Settings.RenderChunkSize + (Settings.GridZLen % Settings.RenderChunkSize == 0 ? 0 : 1);
        GD.Print("chunkXlen chunkZlen " + chunkXlen + " " + chunkZlen);
        GD.Print("chunkXlen chunkZlen should " + Settings.GridXLen / Settings.RenderChunkSize + " " + Settings.GridZLen / Settings.RenderChunkSize);

        Current.MapGridMeshes = new MapGridRenderChunk[chunkZlen * chunkXlen];
        for (int z = 0; z < chunkZlen; z++)
            for (int x = 0; x < chunkXlen; x++)
            {
                var mesh = new MapGridRenderChunk();
                mesh.SetMeshVertices(Settings, Current.NavMap, z, x);
                Current.MapGridMeshes[z * chunkXlen + x] = mesh;
            }

        return Current;
    }


}



public class MapGenSettings
{
    public float CellSize = 1f;
    public int NavChunkSize = 16;
    public int RenderChunkSize = 16;
    public int GridXLen = 255;
    public int GridZLen = 255;
}
