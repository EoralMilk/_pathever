using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meow;

using Godot;
using System;

public class ArrayExtractorTests
{
    public static void RunTests()
    {
        int[,] originalGrid = CreateTestGrid();

        // Test1: 中间区域提取
        int[,] sub1 = originalGrid.ExtractSubRectangle(1, 1, 3, 3);
        PrintResult("Test1", sub1, new int[,]
        {
            { 7, 8, 9 },
            { 12, 13, 14 },
            { 17, 18, 19 }
        });

        // Test2: 提取整个数组
        int[,] sub2 = originalGrid.ExtractSubRectangle(0, 0, 5, 5);
        PrintResult("Test2", sub2, originalGrid);

        // Test3: 右下角 2x2
        int[,] sub3 = originalGrid.ExtractSubRectangle(3, 3, 2, 2);
        PrintResult("Test3", sub3, new int[,]
        {
            { 19, 20 },
            { 24, 25 }
        });

        // Test4: 单行提取
        int[,] sub4 = originalGrid.ExtractSubRectangle(0, 2, 3, 1);
        PrintResult("Test4", sub4, new int[,]
        {
            { 11, 12, 13 }
        });

        // Test5: 负坐标异常
        try
        {
            int[,] sub5 = originalGrid.ExtractSubRectangle(-1, 0, 2, 2);
            GD.Print("Test5: 未捕获异常（预期失败）");
        }
        catch (ArgumentException ex)
        {
            GD.Print($"Test5: 捕获异常（成功）: {ex.Message}");
        }

        // Test6: 超出宽度异常
        try
        {
            int[,] sub6 = originalGrid.ExtractSubRectangle(3, 0, 3, 1);
            GD.Print("Test6: 未捕获异常（预期失败）");
        }
        catch (ArgumentException ex)
        {
            GD.Print($"Test6: 捕获异常（成功）: {ex.Message}");
        }
    }

    private static int[,] CreateTestGrid()
    {
        return new int[,]
        {
            { 1,  2,  3,  4,  5 },
            { 6,  7,  8,  9, 10 },
            { 11, 12, 13, 14, 15 },
            { 16, 17, 18, 19, 20 },
            { 21, 22, 23, 24, 25 }
        };
    }

    private static void PrintResult(string testName, int[,] actual, int[,] expected)
    {
        bool isMatch = AreArraysEqual(actual, expected);
        GD.Print($"{testName}: {(isMatch ? "通过" : "失败")}");
        if (!isMatch)
        {
            GD.Print("实际结果:");
            PrintArray(actual);
            GD.Print("预期结果:");
            PrintArray(expected);
        }
    }

    private static bool AreArraysEqual(int[,] a, int[,] b)
    {
        if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
            return false;

        for (int i = 0; i < a.GetLength(0); i++)
        {
            for (int j = 0; j < a.GetLength(1); j++)
            {
                if (a[i, j] != b[i, j])
                    return false;
            }
        }
        return true;
    }

    private static void PrintArray(int[,] array)
    {
        for (int i = 0; i < array.GetLength(0); i++)
        {
            string str = "";
            for (int j = 0; j < array.GetLength(1); j++)
            {
                str += array[i, j] + "\t";
            }
            GD.Print(str);
        }
    }
}