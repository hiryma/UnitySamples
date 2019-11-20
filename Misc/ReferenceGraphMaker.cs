using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ReferenceGraphMaker
{
    class Node
    {
        public Node(string path, string[] dependencies)
        {
            this.path = path;
            this.dependencies = dependencies;
            children = new List<Node>();
        }
        public string path;
        public string[] dependencies;
        public List<Node> children;
    }

    static IList<string> FindAssets(string filter)
    {
        var guids = AssetDatabase.FindAssets(filter);
        var set = new HashSet<string>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
			set.Add(path);
        }
        var list = new List<string>();
        list.AddRange(set);
        return list;
    }

    [MenuItem("Kayac/Make Reference Graph")]
    public static void Make()
    {
        try
        {
            var nodes = new Dictionary<string, Node>(); //Keyはpath
            // まず全アセットデータ取ってくる
            var scenePaths = FindAssets("t: scene");
            AddNodes(nodes, scenePaths, "Scene検索中");

            var prefabPaths = FindAssets("t: prefab");
            AddNodes(nodes, prefabPaths, "Prefab検索中");

            var scriptablePaths = FindAssets("t: ScriptableObject");
            AddNodes(nodes, scriptablePaths, "ScriptableObject検索中");

            var allPaths = FindAssets("");
            AddNodes(nodes, allPaths, "全アセット検索中");

            // グラフにつなぐ
            int i = 0;
            foreach (var node in nodes.Values)
            {
                EditorUtility.DisplayProgressBar("参照グラフ生成中", null, (float)i / (float)nodes.Count);
                foreach (var dependency in node.dependencies)
                {
                    Node child;
                    if (nodes.TryGetValue(dependency, out child))
                    {
                        node.children.Add(child);
                    }
                }
                i++;
            }

            // これで全Assetの片方向グラフができた。まずは吐き出す。
            WriteEach(nodes.Values, nodes.Count);
            WriteRecursiveUniqued(nodes, scenePaths, "referenceGraphScene.txt");
            WriteRecursiveUniqued(nodes, prefabPaths, "referenceGraphPrefab.txt");
            WriteRecursiveUniqued(nodes, scriptablePaths, "referenceGraphScriptable.txt");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        EditorUtility.ClearProgressBar();
    }

    static void AddNodes(
        Dictionary<string, Node> nodes,
        IList<string> paths,
        string progressBarTitle)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            EditorUtility.DisplayProgressBar(progressBarTitle, null, (float)i / (float)paths.Count);
            AddNode(nodes, paths[i]);
        }
    }

    static void AddNode(
        Dictionary<string, Node> nodes,
        string path)
    {
        if (!nodes.ContainsKey(path))
        {
            var dependencies = AssetDatabase.GetDependencies(path, recursive: false);
            var node = new Node(path, dependencies);
            nodes.Add(path, node);
        }
    }

    static void WriteEach(IEnumerable<Node> nodes, int nodeCount)
    {
        var writer = new System.IO.StreamWriter("referenceGraph.txt");
        int i = 0;
        foreach (var node in nodes)
        {
            EditorUtility.DisplayProgressBar("依存関係ファイル生成中", null, (float)i / (float)nodeCount);
            if (node.children.Count > 0) // 子がいないプレハブは吐かない
            {
                writer.WriteLine(node.path);
                foreach (var child in node.children)
                {
                    writer.Write('\t');
                    writer.WriteLine(child.path);
                }
            }
            i++;
        }
        writer.Close();
    }

    static void WriteRecursiveUniqued(
        Dictionary<string, Node> nodes,
        IList<string> rootGuids,
        string filename)
    {
        var writer = new System.IO.StreamWriter(filename);
        int i = 0;
        foreach (var rootGuid in rootGuids)
        {
            EditorUtility.DisplayProgressBar(filename, null, (float)i / (float)rootGuids.Count);
            Node node;
            if (nodes.TryGetValue(rootGuid, out node))
            {
                var dependentItems = new Dictionary<string, int>();
                var stack = new Stack<string>();
                stack.Push(rootGuid);
                dependentItems.Add(rootGuid, 1);
                ListDependencyRecursive(dependentItems, stack, node);
                writer.WriteLine(node.path);
                foreach (var item in dependentItems)
                {
                    var guid = item.Key;
                    var count = item.Value;
                    if (guid != rootGuid)
                    {
                        Node child;
                        if (nodes.TryGetValue(guid, out child))
                        {
                            writer.Write('\t');
                            writer.Write(count);
                            writer.Write('\t');
                            writer.WriteLine(child.path);
                        }
                    }
                }
            }
            writer.Write('\n');
            i++;
        }
        writer.Close();
    }

    static void ListDependencyRecursive(
        Dictionary<string, int> dependentItems,
        Stack<string> stack,
        Node node)
    {
        foreach (var child in node.children)
        {
            int count;
            if (!dependentItems.TryGetValue(child.path, out count))
            {
                dependentItems.Add(child.path, 1);
            }
            else
            {
                dependentItems[child.path] = count + 1;
            }
            if (!stack.Contains(child.path)) // 参照ループがあるので、再帰呼び出ししない
            {
                stack.Push(child.path);
                ListDependencyRecursive(dependentItems, stack, child);
                stack.Pop();
            }
        }
    }
}
