using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Meow;

/// <summary>
/// 使用struct来保证内存连续
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct NavCell
{
    public uint CellTypeMask;
    
    public Vector3 F1V1;
    public Vector3 F1V2;
    public Vector3 F1V3;
    public Vector3 F2V1;
    public Vector3 F2V2;
    public Vector3 F2V3;

}
