using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meow;

/// <summary>
/// 以较小代价就可以添加的导航障碍物。(移除代价与geo mesh更新相同)
/// </summary>
public class NavObstacle
{
    public Vector3[] VerticesHorizontal;
    public float YBase;
    public float YHeight;

    public NavObstacle(Vector3[] verticesHorizontal, float yBase, float yHeight)
    {
        VerticesHorizontal = verticesHorizontal;
        YBase = yBase;
        YHeight = yHeight;
    }
}
