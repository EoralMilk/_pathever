#if TOOLS
using Godot;
using Meow;
using Meow.Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

[Tool]
public partial class test_nav : EditorPlugin
{
    private Button button1, button2;
    private Control _dock;
    public override void _EnterTree()
    {
        //_dock = GD.Load<PackedScene>("res://addons/test_nav/test_dock.tscn").Instantiate<Control>();
        //AddControlToDock(DockSlot.LeftUl, _dock);

        var script = GD.Load<Script>("res://addons/test_nav/testButton.cs");
        var texture = GD.Load<Texture2D>("res://icon.svg");
        AddCustomType("TestButton", "Button", script, texture);

        button1 = new Button();
        button1.Text = "Test Bake All";
        button1.Pressed += TestBake;
        // 注册自定义按钮到编辑器界面
        AddControlToContainer(CustomControlContainer.Toolbar, button1);

        button2 = new Button();
        button2.Text = "Test Build Map";
        button2.Pressed += TestBuildMap;
        // 注册自定义按钮到编辑器界面
        AddControlToContainer(CustomControlContainer.Toolbar, button2);
    }

    public override void _ExitTree()
    {
        // 清理资源
        if (button1 != null)
        {
            RemoveControlFromContainer(CustomControlContainer.Toolbar, button1);
            button1.QueueFree();
        }
        if (button2 != null)
        {
            RemoveControlFromContainer(CustomControlContainer.Toolbar, button2);
            button2.QueueFree();
        }
        //if (_dock != null)
        //{
        //    RemoveControlFromDocks(_dock);
        //    // Erase the control from the memory.
        //    _dock.Free();
        //}

        RemoveCustomType("TestButton");
    }

    public static void TestBuildMap()
    {
        var mapgen = new MapGenerator();
        mapgen.Settings = new MapGenSettings();
        GD.Print(mapgen.Settings.GridZLen);
        GD.Print(mapgen.Settings.GridXLen);


        var root = EditorInterface.Singleton.GetEditedSceneRoot() as Node3D;
        var nodeMapMesh = root.FindChild("_MapMesh", false);
        var navMeshTemplate = root.FindChild("_NavMeshInstanceTemplate") as MeshInstance3D;

        if (nodeMapMesh != null)
        {
            nodeMapMesh.Free();
        }


        nodeMapMesh = new Node();
        nodeMapMesh.Name = "_MapMesh";
        root.AddChild(nodeMapMesh);
        nodeMapMesh.Owner = root;

        var map = mapgen.CreateMap(root);

        foreach (var m in map.MapGridMeshes)
        {
            var mi = new MeshInstance3D();
            nodeMapMesh.AddChild(mi);
            mi.MaterialOverride = navMeshTemplate.MaterialOverride;
            mi.Owner = root;
            mi.Mesh = m.LocalMesh;
        }

        var navNode = root.FindChild(NavMap.NavRegionRoot);
        foreach(var n in navNode.GetChildren())
        {
            n.Free();
        }

        var navType = new NavType(0.25f, 0.25f, 0.5f, 1f);
        foreach (var chunk in map.NavMap.GridChunks)
        {
            chunk.BakeNavMesh(map.NavMap, navType);
        }
    }

    public static void TestBake()
    {
        GD.Print("Bake start");
        Stopwatch stopwatch = Stopwatch.StartNew();
        var root = EditorInterface.Singleton.GetEditedSceneRoot();

        var navManager = root.FindChild("NavManager");

        if (navManager != null)
        {
            var list = new List<n_NavType>();
            var nodePathType = new n_NavType()
            {
                ChunkRoot = root.FindChild("NavChunkRoot") as Node3D,
            };

            list.Add(nodePathType);

            n_NavManager.BakeAll(root.FindChild("NavCollisionRoot"), (root.FindChild("Path1") as Node3D).GetWorld3D(), 0.25f, list);
            
        }
        else
        {
            GD.Print("Bake failed: can't find navManager");
        }

        stopwatch.Stop();
        // 输出结果（单位：毫秒）
        long elapsedMs = stopwatch.ElapsedMilliseconds;
        GD.Print($"Execution Time: {elapsedMs}ms");

        GD.Print("Bake end");
    }
}
#endif
