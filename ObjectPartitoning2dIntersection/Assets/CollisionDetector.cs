using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector
{
	public struct Result
	{
		public int agentIndex0;
		public int agentIndex1;
	}

	public void Detect(List<Result> results, IReadOnlyList<Agent> agents)
	{
		results.Clear();
		testCount = hitCount = 0;
		for (var i0 = 0; i0 < agents.Count; i0++)
		{
			for (var i1 = (i0 + 1); i1 < agents.Count; i1++)
			{
				Test(results, i0, i1, agents);
			}
		}
Debug.Log("Detect: " + hitCount + "/" + testCount);
	}

	public void DetectOpt(List<Result> results, IReadOnlyList<Agent> agents, int splitCount)
	{
		testCount = hitCount = 0;

		if (nodes == null)
		{
			nodes = new List<Node>();
		}
		nodes.Clear();

		var node = new Node();
		node.indices = new List<int>(agents.Count);
		node.left = node.right = -1;
		for (var i = 0; i < agents.Count; i++)
		{
			node.indices.Add(i);
		}

		Split(0, 0, splitCount, agents);
		nodes.Add(node);

		results.Clear();
		TestNode(results, 0, agents); // 絶対1段は割るのでここは確定
Debug.Log("Detect: " + hitCount + "/" + testCount);
	}

	// non public ----
	struct Node
	{
		public Vector2 min;
		public Vector2 max;
		public List<int> indices;
		public int left;
		public int right;
	}
	List<Node> nodes;
	int testCount;
	int hitCount;

	void Split(int nodeIndex, int depth, int splitCount, IReadOnlyList<Agent> agents)
	{
		// Boundingを測定
		var min = Vector2.positiveInfinity;
		var max = Vector2.negativeInfinity;
		for (var i = 0; i < agents.Count; i++)
		{
			var a = agents[nodes[nodeIndex].indices[i]];
			var p3 = a.transform.position;
			var p = new Vector2(p3.x, p3.y);
			var r = a.Radius;
			var bmin = p - new Vector2(r, r);
			var bmax = p + new Vector2(r, r);
			min = Vector2.Min(min, bmin);
			max = Vector2.Max(max, bmax);
		}

		if (depth < splitCount)
		{
			var left = new Node();
			var leftIndex = nodes.Count;
			var right = new Node();
			var rightIndex = nodes.Count + 1;

			// 一番長い辺を探す
			var size = max - min;
			var indices = nodes[nodeIndex].indices;
			var leftIndices = new List<int>(indices.Count);
			var rightIndices = new List<int>(indices.Count);
			if (size.x > size.y)
			{ // x軸で分割
				var mid = (min.x + max.x) * 0.5f;
				for (var i = 0; i < indices.Count; i++)
				{
					var agentIndex = indices[i];
					var a = agents[agentIndex];
					var p = a.transform.position;
					if (p.x < mid)
					{
						leftIndices.Add(agentIndex);
					}
					else
					{
						rightIndices.Add(agentIndex);
					}
				}
			}
			else
			{ // y軸で分割
				var mid = (min.y + max.y) * 0.5f;
				for (var i = 0; i < indices.Count; i++)
				{
					var agentIndex = indices[i];
					var a = agents[agentIndex];
					var p = a.transform.position;
					if (p.y < mid)
					{
						leftIndices.Add(agentIndex);
					}
					else
					{
						rightIndices.Add(agentIndex);
					}
				}
			}

			left.indices = leftIndices;
			right.indices = rightIndices;
			Split(leftIndex, depth + 1, splitCount, agents);
			Split(rightIndex, depth + 1, splitCount, agents);

			var splitted = new Node();
			splitted.min = min;
			splitted.max = max;
			splitted.left = leftIndex;
			splitted.right = rightIndex;
			splitted.indices = null;

			nodes[nodeIndex] = splitted;
		}
	}

	void TestNode(List<Result> results, int nodeIndex, IReadOnlyList<Agent> agents)
	{
		var node = nodes[nodeIndex];
		if (node.indices != null)
		{
			Debug.Assert((node.left < 0) && (node.right < 0));
			TestLeaf(results, nodeIndex, agents);
		}
		else
		{
			Debug.Assert((node.left >= 0) && (node.right >= 0));
			TestNode(results, node.left, agents);
			TestNode(results, node.right, agents);
			TestNodes(results, node.left, node.right, agents);
		}
	}

	void TestNodes(List<Result> results, int nodeIndex0, int nodeIndex1, IReadOnlyList<Agent> agents)
	{
		var node0 = nodes[nodeIndex0];
		var node1 = nodes[nodeIndex1];
		// まずBB判定
		if (TestBounding(in node0, in node1))
		{
			if (node0.indices != null) // 0が葉
			{
				Debug.Assert((node0.left < 0) && (node0.right < 0));
				if (node1.indices != null) // 1も葉
				{
					Debug.Assert((node1.left < 0) && (node1.right < 0));
					TestLeaves(results, nodeIndex0, nodeIndex1, agents);
				}
				else
				{
					Debug.Assert((node1.left >= 0) && (node1.right >= 0));
					TestNodes(results, nodeIndex0, node1.left, agents);
					TestNodes(results, nodeIndex0, node1.right, agents);
				}
			}
			else // 0が節
			{
				Debug.Assert((node0.left >= 0) && (node0.right >= 0));
				if (node1.indices != null) // 1は葉
				{
					Debug.Assert((node1.left < 0) && (node1.right < 0));
					TestNodes(results, node0.left, nodeIndex1, agents);
					TestNodes(results, node0.right, nodeIndex1, agents);
				}
				else // 両方節
				{
					Debug.Assert((node1.left >= 0) && (node1.right >= 0));
					TestNodes(results, node0.left, node1.left, agents);
					TestNodes(results, node0.left, node1.right, agents);
					TestNodes(results, node0.right, node1.left, agents);
					TestNodes(results, node0.right, node1.right, agents);
				}
			}
		}
	}

	void TestLeaves(List<Result> results, int nodeIndex0, int nodeIndex1, IReadOnlyList<Agent> agents)
	{
		var node0 = nodes[nodeIndex0];
		var node1 = nodes[nodeIndex1];
		Debug.Assert(node0.left < 0);
		Debug.Assert(node0.right < 0);
		Debug.Assert(node1.left < 0);
		Debug.Assert(node1.right < 0);
		Debug.Assert(node0.indices != null);
		Debug.Assert(node1.indices != null);

		for (var i0 = 0; i0 < node0.indices.Count; i0++)
		{
			for (var i1 = 0; i1 < node1.indices.Count; i1++)
			{
				testCount++;
				var agentIndex0 = node0.indices[i0];
				var agentIndex1 = node1.indices[i1];
				Test(results, agentIndex0, agentIndex1, agents);
			}
		}
	}


	void TestLeaf(List<Result> results, int nodeIndex, IReadOnlyList<Agent> agents)
	{
		var node = nodes[nodeIndex];
		Debug.Assert(node.left < 0);
		Debug.Assert(node.right < 0);
		Debug.Assert(node.indices != null);

		for (var i0 = 0; i0 < node.indices.Count; i0++)
		{
			for (var i1 = (i0 + 1); i1 < node.indices.Count; i1++)
			{
				testCount++;
				var agentIndex0 = node.indices[i0];
				var agentIndex1 = node.indices[i1];
				Test(results, agentIndex0, agentIndex1, agents);
			}
		}
	}

	void Test(List<Result> results, int agentIndex0, int agentIndex1, IReadOnlyList<Agent> agents)
	{
		var a0 = agents[agentIndex0];
		var a1 = agents[agentIndex1];

		testCount++;
		var p0 = a0.transform.position;
		var p1 = a1.transform.position;

		var r0 = a0.Radius;
		var r1 = a1.Radius;

		var d = p1 - p0;
		var l2 = d.sqrMagnitude;
		var r = r0 + r1;
		var r2 = r * r;

		if (l2 < r2)
		{
			hitCount++;
			var result = new Result();
			result.agentIndex0 = agentIndex0;
			result.agentIndex1 = agentIndex1;
			results.Add(result);
		}
	}

	static bool TestBounding(in Node node0, in Node node1)
	{
		var ret = true;
		if (node0.max.x < node1.min.x)
		{
			ret = false;
		}
		else if (node0.max.y < node1.min.y)
		{
			ret = false;
		}
		else if (node1.max.x < node0.min.x)
		{
			ret = false;
		}
		else if (node1.max.y < node0.min.y)
		{
			ret = false;
		}
		return ret;
	}
}
