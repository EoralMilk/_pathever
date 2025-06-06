using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meow;

public class NavType
{
    public const uint CollisionLayer = 2;
    public float BakeCellRadius { get; private set; }
    public float BakeCellHeight { get; private set; }

    public float AgentRadius { get; private set; }
    public float AgentHeight { get; private set; }

    /// <summary>
    /// 一个cell mesk 用于决定哪些类型的cell可以通行
    /// </summary>
    public uint CellTypeMask { get; private set; }

    /// <param name="bakeCellRadius">can be 0.25f</param>
    /// <param name="bakeCellHeight">can be 0.25f</param>
    /// <param name="agentRadius">can be 0.5f</param>
    /// <param name="agentHeight">can be 1f</param>
    public NavType(float bakeCellRadius, float bakeCellHeight, float agentRadius, float agentHeight)
    {
        BakeCellRadius = bakeCellRadius;
        BakeCellHeight = bakeCellHeight;
        AgentRadius = agentRadius;
        AgentHeight = agentHeight;
    }

}

