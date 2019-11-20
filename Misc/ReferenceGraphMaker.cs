using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ReferenceGraphMaker
{
    class Node
    {
        public Node(string guid, string path, string[] dependencies)
        {
            this.guid = guid;
            this.path = path;
            this.dependencies = dependencies;
            children = new List<Node>();
        }
        public string guid;
        public string path;
        public string[] dependencies;
        public List<Node> children;
    }

    [MenuItem("Kayac/Make Reference Graph")]
    public static void Make()
    {
        try
        {
            var nodes = new Dictionary<string, Node>(); //KeyはGUID
            var pathToGuids = new Dictionary<string, List<string>>();
            // まず全アセットデータ取ってくる
            var sceneGuids = AssetDatabase.FindAssets("t: scene");
            AddNodes(nodes, pathToGuids, sceneGuids, "Scene検索中");
            var prefabGuids = AssetDatabase.FindAssets("t: prefab");
            AddNodes(nodes, pathToGuids, prefabGuids, "Prefab検索中");
            var scriptableGuids = AssetDatabase.FindAssets("t: ScriptableObject");
            AddNodes(nodes, pathToGuids, scriptableGuids, "ScriptableObject検索中");
            var allGuids = AssetDatabase.FindAssets("");
            AddNodes(nodes, pathToGuids, allGuids, "全アセット検索中");

            // グラフにつなぐ
            int i = 0;
            foreach (var node in nodes.Values)
            {
                EditorUtility.DisplayProgressBar("参照グラフ生成中", null, (float)i / (float)nodes.Count);
                foreach (var dependency in node.dependencies)
                {
                    List<string> guids;
                    if (pathToGuids.TryGetValue(dependency, out guids))
                    {
                        foreach (var guid in guids)
                        {
                            Node child;
                            if (nodes.TryGetValue(guid, out child))
                            {
                                node.children.Add(child);
                            }
                        }
                    }
                }
                i++;
            }

            // これで全Assetの片方向グラフができた。まずは吐き出す。
            WriteEach(nodes.Values, nodes.Count);
            WriteRecursiveUniqued(nodes, sceneGuids, "referenceGraphScene.txt");
            WriteRecursiveUniqued(nodes, prefabGuids, "referenceGraphPrefab.txt");
            WriteRecursiveUniqued(nodes, scriptableGuids, "referenceGraphScriptable.txt");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        EditorUtility.ClearProgressBar();
    }

    static void AddNodes(
        Dictionary<string, Node> nodes,
        Dictionary<string, List<string>> pathToGuids,
        IList<string> guids,
        string progressBarTitle)
    {
        for (int i = 0; i < guids.Count; i++)
        {
            EditorUtility.DisplayProgressBar(progressBarTitle, null, (float)i / (float)guids.Count);
            var guid = guids[i];
            AddNode(nodes, pathToGuids, guid);
        }
    }

    static void AddNode(
        Dictionary<string, Node> nodes,
        Dictionary<string, List<string>> pathToGuids,
        string guid)
    {
        if (!nodes.ContainsKey(guid))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var dependencies = AssetDatabase.GetDependencies(path, recursive: false);
            var node = new Node(guid, path, dependencies);
            nodes.Add(guid, node);
            List<string> guidsOfThePath;
            if (!pathToGuids.TryGetValue(path, out guidsOfThePath))
            {
                guidsOfThePath = new List<string>();
                pathToGuids.Add(path, guidsOfThePath);
            }
            guidsOfThePath.Add(guid);
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
            if (!dependentItems.TryGetValue(child.guid, out count))
            {
                dependentItems.Add(child.guid, 1);
            }
            else
            {
                dependentItems[child.guid] = count + 1;
            }
            if (!stack.Contains(child.guid)) // 参照ループがあるので、再帰呼び出ししない
            {
                stack.Push(child.guid);
                ListDependencyRecursive(dependentItems, stack, child);
                stack.Pop();
            }
        }
    }
}
