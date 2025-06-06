using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using System.Text;
using System.Threading.Tasks;

namespace Meow;


/// <summary>
/// For best data usage, map center should be Vector3.Zero.
/// 对于网格化地图，可以简单地划分网格来处理chunk。
/// 对于特殊地图，要如何处理chunk，手动分配？
/// 例如制作一个房间预制件，也将其作为一个chunk加入地图。
/// 同时，网格化的地图也应该可以任意拓展chunk。
/// 为了便于访问，chunk采用hashset存储，网格地图可以同时维护一个二维数组来便于随机访问对应位置的chunk。
/// 为了确保烘焙后chunk之间的连通性，网格状地图在需要烘焙chunk时可以拓展chunk拥有的网格区域，通过二维数组可以方便的提取网格
/// 因此navchunk不应该存储网格地图的地形mesh，而是在烘焙时在map的网格中读取子区域。
/// 任意划分的region，应该使用链接块来链接彼此，烘焙时将自己的mesh和所链接的链接块一并加入烘焙。
/// </summary>
public class NavMap
{
    public World3D World;
    public const string NavColliderRoot = "_NavCollider";
    public const string NavRegionRoot = "_NavRegion";


    public float CellSize;
    public int GridChunkSize;

    public int GridXLen;
    public int GridZLen;
    public Vector3 GridMinCorner;
    public NavCell[] GridCells;
    public NavChunk[] GridChunks;

    public bool HasGrid;

    public HashSet<NavChunk> OtherChunks;

    HashSet<NavCell> ShouldUpdate = new HashSet<NavCell>();

    public NavMap(int gridZlen, int gridXlen, float cellSize, int gridChunkSize, Vector3 gridMinCorner, World3D world)
    {
        if (gridXlen == 0 || gridZlen == 0)
        {
            HasGrid = false;
        }
        else
        {
            GridChunkSize = gridChunkSize;
            CellSize = cellSize;
            GridMinCorner = gridMinCorner;

            GridZLen = gridZlen;
            GridXLen = gridXlen;
            HasGrid = true;

            GridCells = new NavCell[GridZLen * GridXLen];
            for (int z = 0; z < GridZLen; z++) 
                for (int x = 0; x < GridXLen; x++)
                {
                    GridCells[z * GridXLen + x] = new NavCell()
                    {
                        F1V1 = new Vector3(x* CellSize, 0, z * CellSize) + gridMinCorner,
                        F1V2 = new Vector3((x+1) * CellSize, 0, z * CellSize) + gridMinCorner,
                        F1V3 = new Vector3((x + 1) * CellSize, 0, (z + 1) * CellSize) + gridMinCorner,
                        F2V1 = new Vector3(x * CellSize, 0, z * CellSize) + gridMinCorner,
                        F2V2 = new Vector3((x + 1) * CellSize, 0, (z + 1) * CellSize) + gridMinCorner,
                        F2V3 = new Vector3(x * CellSize, 0, (z + 1) * CellSize) + gridMinCorner,
                    };
                }
            int chunkXlen = GridXLen / GridChunkSize + (GridXLen % GridChunkSize == 0 ? 0 :1);
            int chunkZlen = GridZLen / GridChunkSize + (GridZLen % GridChunkSize == 0 ? 0 : 1);
            GridChunks = new NavChunk[chunkZlen * chunkXlen];
            for (int z = 0; z < chunkZlen; z++)
                for (int x = 0; x < chunkXlen; x++)
                {
                    GridChunks[z * chunkXlen + x] = new NavChunkGrid(x,z);
                }
        }

        World = world;

        OtherChunks = new HashSet<NavChunk>();
    }
    
}