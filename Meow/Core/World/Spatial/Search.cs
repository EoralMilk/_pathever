
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Meow;


public class Quadtree<T>
{
    // 默认每个节点最多容纳4个对象
    private const int DefaultCapacity = 4;
    // 节点合并阈值（当所有子节点对象总数小于此值时合并）
    private const int MergeThreshold = 2;

    // 根节点
    private QuadtreeNode root;

    // 对象到节点映射表（快速定位对象所在节点）
    private readonly Dictionary<T, QuadtreeNode> objectToNodeMap = new Dictionary<T, QuadtreeNode>();

    // 获取对象边界的方法（由外部提供）
    private Func<T, Rect2> getBoundsFunc;

    // 节点池（复用节点减少GC）
    private readonly Stack<QuadtreeNode> nodePool = new Stack<QuadtreeNode>();

    // 统计信息
    public int TotalNodes { get; private set; }
    public int TotalObjects { get { return objectToNodeMap.Count; } }

    public Quadtree(Rect2 boundary, Func<T, Rect2> boundsGetter, int capacity = DefaultCapacity)
    {
        if (boundary.Size == Vector2.Zero)
            throw new ArgumentException("Quadtree boundary cannot be zero-sized");

        getBoundsFunc = boundsGetter ?? throw new ArgumentNullException(nameof(boundsGetter));
        root = CreateNode(boundary, capacity);
    }

    #region 核心操作
    /// <summary>
    /// 插入对象
    /// </summary>
    public void Insert(T obj)
    {
        Rect2 bounds = getBoundsFunc(obj);
        if (bounds.Size == Vector2.Zero)
        {
            GD.PrintErr("Cannot insert object with zero-sized bounds");
            return;
        }

        // 如果对象已在树中，先移除旧位置
        if (objectToNodeMap.ContainsKey(obj))
        {
            GD.Print("Object already in quadtree, updating position");
            Remove(obj);
        }

        // 递归插入到合适节点
        InsertRecursive(root, obj, bounds);
    }

    /// <summary>
    /// 移除对象
    /// </summary>
    public void Remove(T obj)
    {
        if (!objectToNodeMap.TryGetValue(obj, out var node))
        {
            GD.PrintErr("Object not found in quadtree");
            return;
        }

        // 从节点移除对象
        node.Remove(obj);

        // 从映射表中移除
        objectToNodeMap.Remove(obj);

        // 如果节点对象数量过少，尝试合并
        TryMerge(node);
    }

    /// <summary>
    /// 查询区域内的所有对象
    /// </summary>
    public List<T> Query(Rect2 area)
    {
        List<T> results = new List<T>();
        QueryRecursive(root, area, results);
        return results;
    }

    /// <summary>
    /// 查询并执行操作（避免分配列表）
    /// </summary>
    public void Query(Rect2 area, Action<T> action)
    {
        QueryRecursive(root, area, action);
    }

    /// <summary>
    /// 清空四叉树
    /// </summary>
    public void Clear()
    {
        ClearRecursive(root);
        objectToNodeMap.Clear();

        // 重置根节点（保留边界和容量）
        root.Children = null;
        root.Objects.Clear();
    }

    /// <summary>
    /// 更新对象位置（优化操作）
    /// </summary>
    public void Update(T obj)
    {
        if (!objectToNodeMap.TryGetValue(obj, out var currentNode))
            return;

        Rect2 newBounds = getBoundsFunc(obj);
        Rect2 oldBounds = currentNode.GetCachedBounds(obj);

        // 检查新位置是否仍在原节点内
        if (currentNode.Bounds.Encloses(newBounds))
        {
            currentNode.UpdateCachedBounds(obj, newBounds);
            return;
        }

        // 从原位置移除
        currentNode.Remove(obj);
        objectToNodeMap.Remove(obj);

        // 尝试向上查找合适的节点
        QuadtreeNode node = currentNode;
        while (node != null)
        {
            if (node.Bounds.Encloses(newBounds))
            {
                InsertRecursive(node, obj, newBounds);
                return;
            }
            node = node.Parent;
        }

        // 如果向上找不到，从根节点重新插入
        InsertRecursive(root, obj, newBounds);
    }
    #endregion

    #region 内部实现
    // 递归插入
    private void InsertRecursive(QuadtreeNode node, T obj, Rect2 bounds)
    {
        // 如果不是叶子节点，尝试插入到子节点
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                if (child.Bounds.Encloses(bounds))
                {
                    InsertRecursive(child, obj, bounds);
                    return;
                }
            }
        }

        // 没有合适的子节点或当前是叶子节点
        node.Add(obj, bounds);
        objectToNodeMap[obj] = node;

        // 检查是否达到分裂条件
        if (node.IsLeaf && node.Objects.Count > node.Capacity)
        {
            Split(node);
        }
    }

    // 递归查询
    private void QueryRecursive(QuadtreeNode node, Rect2 area, List<T> results)
    {
        // 当前节点与查询区域无交集，跳过
        if (!node.Bounds.Intersects(area))
            return;

        // 将当前节点与查询区域相交的对象加入结果
        foreach (var kvp in node.Objects)
        {
            if (kvp.Value.Intersects(area))
            {
                results.Add(kvp.Key);
            }
        }

        // 递归查询子节点
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                QueryRecursive(child, area, results);
            }
        }
    }

    // 查询操作的变体（避免分配集合）
    private void QueryRecursive(QuadtreeNode node, Rect2 area, Action<T> action)
    {
        if (!node.Bounds.Intersects(area))
            return;

        foreach (var kvp in node.Objects)
        {
            if (kvp.Value.Intersects(area))
            {
                action(kvp.Key);
            }
        }

        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                QueryRecursive(child, area, action);
            }
        }
    }

    // 分裂节点
    private void Split(QuadtreeNode node)
    {
        if (!node.IsLeaf) return;

        Vector2 halfSize = node.Bounds.Size / 2;
        Vector2 center = node.Bounds.Position + halfSize;

        // 创建四个象限
        node.Children = new QuadtreeNode[4];

        // 西北（左上）
        node.Children[0] = CreateNode(new Rect2(
            node.Bounds.Position,
            halfSize), node.Capacity);

        // 东北（右上）
        node.Children[1] = CreateNode(new Rect2(
            new Vector2(center.X, node.Bounds.Position.Y),
            halfSize), node.Capacity);

        // 西南（左下）
        node.Children[2] = CreateNode(new Rect2(
            new Vector2(node.Bounds.Position.X, center.Y),
            halfSize), node.Capacity);

        // 东南（右下）
        node.Children[3] = CreateNode(new Rect2(
            center,
            halfSize), node.Capacity);

        // 设置父节点
        foreach (var child in node.Children)
        {
            child.Parent = node;
        }

        // 重新分配当前节点的对象
        var objectsToRedistribute = new List<KeyValuePair<T, Rect2>>(node.Objects);
        node.Objects.Clear();

        foreach (var kvp in objectsToRedistribute)
        {
            bool placed = false;
            foreach (var child in node.Children)
            {
                if (child.Bounds.Encloses(kvp.Value))
                {
                    child.Add(kvp.Key, kvp.Value);
                    objectToNodeMap[kvp.Key] = child;
                    placed = true;
                    break;
                }
            }

            // 没有子节点能容纳，保留在当前节点
            if (!placed)
            {
                node.Add(kvp.Key, kvp.Value);
            }
        }
    }

    // 尝试合并节点
    private void TryMerge(QuadtreeNode node)
    {
        if (node == null || node.IsLeaf || node.Parent == null) return;

        int totalDescendants = CountDescendants(node);
        if (totalDescendants > MergeThreshold) return;

        // 提升所有后代节点的对象到当前节点
        LiftDescendants(node);

        // 销毁所有子节点
        DestroyChildren(node);

        // 递归尝试合并父节点
        TryMerge(node.Parent);
    }

    // 提升后代节点的对象
    private void LiftDescendants(QuadtreeNode node)
    {
        if (node.Children == null) return;

        foreach (var child in node.Children)
        {
            // 递归处理子节点
            if (!child.IsLeaf)
            {
                LiftDescendants(child);
            }

            // 将所有对象移到当前节点
            foreach (var kvp in child.Objects.ToList())
            {
                child.Remove(kvp.Key);
                node.Add(kvp.Key, kvp.Value);
                objectToNodeMap[kvp.Key] = node;
            }
        }
    }

    // 销毁所有子节点
    private void DestroyChildren(QuadtreeNode node)
    {
        if (node.Children == null) return;

        foreach (var child in node.Children)
        {
            DestroyChildren(child);
            ReleaseNode(child);
        }

        node.Children = null;
    }

    // 递归清空节点
    private void ClearRecursive(QuadtreeNode node)
    {
        if (node == null) return;

        // 清空当前节点对象（但保留映射关系）
        node.Objects.Clear();

        // 递归清空子节点
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                ClearRecursive(child);
                ReleaseNode(child);
            }
            node.Children = null;
        }
    }

    // 统计后代节点总对象数
    private int CountDescendants(QuadtreeNode node)
    {
        int count = node.Objects.Count;
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                count += CountDescendants(child);
            }
        }
        return count;
    }

    // 从池中创建节点
    private QuadtreeNode CreateNode(Rect2 boundary, int capacity)
    {
        if (nodePool.Count > 0)
        {
            var node = nodePool.Pop();
            node.Init(boundary, capacity);
            TotalNodes++;
            return node;
        }
        return new QuadtreeNode(boundary, capacity);
    }

    // 释放节点到池中
    private void ReleaseNode(QuadtreeNode node)
    {
        node.Reset();
        nodePool.Push(node);
        TotalNodes--;
    }

    // 四叉树节点
    private class QuadtreeNode
    {
        public Rect2 Bounds { get; private set; }
        public int Capacity { get; private set; }
        public Dictionary<T, Rect2> Objects { get; private set; }
        public QuadtreeNode[] Children { get; set; }
        public QuadtreeNode Parent { get; set; }

        public bool IsLeaf => Children == null;

        public QuadtreeNode(Rect2 bounds, int capacity)
        {
            Init(bounds, capacity);
        }

        public void Init(Rect2 bounds, int capacity)
        {
            Bounds = bounds;
            Capacity = capacity;
            Objects = new Dictionary<T, Rect2>();
            Children = null;
            Parent = null;
        }

        public void Reset()
        {
            Objects.Clear();
            Children = null;
            Parent = null;
        }

        public void Add(T obj, Rect2 bounds)
        {
            Objects[obj] = bounds;
        }

        public void Remove(T obj)
        {
            Objects.Remove(obj);
        }

        public Rect2 GetCachedBounds(T obj)
        {
            return Objects[obj];
        }

        public void UpdateCachedBounds(T obj, Rect2 newBounds)
        {
            Objects[obj] = newBounds;
        }
    }
    #endregion
}