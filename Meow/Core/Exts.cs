using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Meow;

public static class Exts
{
    /// <summary>
    /// The array should be T[row_Id,col_Id] 
    /// </summary>
    public static T[,] ExtractSubRectangle<T>(this T[,] source, int startX, int startY, int xlen, int ylen)
    {
        if (startX < 0 || startY < 0 ||
    startX + xlen > source.GetLength(1) ||
    startY + ylen > source.GetLength(0))
        {
            throw new ArgumentException("Sub-rectangle exceeds source array bounds.");
        }

        T[,] result = new T[ylen, xlen];

        for (int i = 0; i < ylen; i++)
        {
            // 计算源数组每行的起始索引
            int sourceIndex = (startY + i) * source.GetLength(1) + startX;
            // 计算目标数组每行的起始索引
            int destIndex = i * xlen;

            // 批量复制单行数据
            Array.Copy(
                source, sourceIndex,
                result, destIndex,
                xlen
            );
        }

        return result;
    }


    /// <summary>
    /// The array should be T[row_Id,col_Id] 
    /// </summary>
    public static T[] ExtractSubRectangleArray<T>(this T[] source, int startX, int startY, int xlen, int ylen, int sourceRowLen)
    {
        if (startX < 0 || startY < 0 ||
    startX + xlen + startY * sourceRowLen > source.Length)
        {
            throw new ArgumentException("Sub-rectangle exceeds source array bounds.");
        }

        T[] result = new T[ylen * xlen];

        for (int i = 0; i < ylen; i++)
        {
            // 计算源数组每行的起始索引
            int sourceIndex = (startY + i) * sourceRowLen + startX;
            // 计算目标数组每行的起始索引
            int destIndex = i * xlen;

            // 批量复制单行数据
            Array.Copy(
                source, sourceIndex,
                result, destIndex,
                xlen
            );
        }

        return result;
    }

    /// <summary>
    /// 获取3D AABB在世界空间XZ平面上的投影矩形
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetGlobalBoundary(this Node3D node, Aabb localAabb, ref Rect2 globalrect, ref Aabb globalAabb)
    {
        // 快速获取全局变换和基础向量
        Transform3D gt = node.GlobalTransform;
        //Vector3 origin = gt.Origin;
        //Basis basis = gt.Basis;

        // 内联计算AABB的8个角点（使用栈分配数组）
        Span<Vector3> corners = stackalloc Vector3[8];
        CalculateAabbCorners(ref localAabb, ref corners);

        // 变换到世界空间并投影到XZ平面
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        for (int i = 0; i < 8; i++)
        {
            // 内联向量变换: gt * corner
            Vector3 gtCorner = gt * corners[i];

            // 更新XZ平面的最小/最大值
            minX = Mathf.Min(minX, gtCorner.X);
            maxX = Mathf.Max(maxX, gtCorner.X);
            minY = Mathf.Min(minY, gtCorner.Y);
            maxY = Mathf.Max(maxY, gtCorner.Y);
            minZ = Mathf.Min(minZ, gtCorner.Z);
            maxZ = Mathf.Max(maxZ, gtCorner.Z);
        }

        globalrect = new Rect2(minX, minZ, maxX - minX, maxZ - minZ);
        globalAabb = new Aabb(minX, minY, minZ, globalrect.Size.X, maxY -minY, globalrect.Size.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CalculateAabbCorners(ref Aabb aabb, ref Span<Vector3> corners)
    {
        // 使用局部变量避免重复访问属性
        Vector3 min = aabb.Position;
        Vector3 max = aabb.End;

        corners[0] = new Vector3(min.X, min.Y, min.Z);      // (0,0,0)
        corners[1] = new Vector3(min.X, min.Y, max.Z);      // (0,0,1)
        corners[2] = new Vector3(min.X, max.Y, min.Z);      // (0,1,0)
        corners[3] = new Vector3(min.X, max.Y, max.Z);      // (0,1,1)
        corners[4] = new Vector3(max.X, min.Y, min.Z);      // (1,0,0)
        corners[5] = new Vector3(max.X, min.Y, max.Z);      // (1,0,1)
        corners[6] = new Vector3(max.X, max.Y, min.Z);      // (1,1,0)
        corners[7] = new Vector3(max.X, max.Y, max.Z);      // (1,1,1)
    }

    /// <summary>
    /// Only caculate position translate
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rect2 GetGlobalXZBoundaryOffsetOnly(this Node3D node, Aabb localAabb)
    {
        Transform3D gt = node.GlobalTransform;
        Vector3 center = gt.Origin + gt.Basis * localAabb.GetCenter();
        Vector3 size = gt.Basis * localAabb.Size;

        return new Rect2(
            center.X - MathF.Abs(size.X) * 0.5f,
            center.Z - MathF.Abs(size.Z) * 0.5f,
            MathF.Abs(size.X),
            MathF.Abs(size.Z)
        );
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rect2 GetGlobalXYRect(this Node3D node, Rect2 xzBoundary)
    {
        Transform3D globalTransform = node.GlobalTransform;

        // 仅计算四个主要角点（已足够）
        Vector2[] points = new Vector2[4];

        points[0] = ProjectToXZ(globalTransform * new Vector3(xzBoundary.Position.X, 0, xzBoundary.Position.Y));
        points[1] = ProjectToXZ(globalTransform * new Vector3(xzBoundary.Position.X, 0, xzBoundary.End.Y));
        points[2] = ProjectToXZ(globalTransform * new Vector3(xzBoundary.End.X, 0, xzBoundary.Position.Y));
        points[3] = ProjectToXZ(globalTransform * new Vector3(xzBoundary.End.X, 0, xzBoundary.End.Y));

        Vector2 min = points[0];
        Vector2 max = points[0];

        foreach (Vector2 point in points)
        {
            min = new Vector2(Mathf.Min(min.X, point.X), Mathf.Min(min.Y, point.Y));
            max = new Vector2(Mathf.Max(max.X, point.X), Mathf.Max(max.Y, point.Y));
        }

        return new Rect2(min, max - min);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ProjectToXZ(this Vector3 point) => new Vector2(point.X, point.Z);


}
