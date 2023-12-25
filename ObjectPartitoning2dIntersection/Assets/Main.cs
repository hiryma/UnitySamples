using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField] int agentCount = 128;
	[SerializeField] float centeringForce = 0.001f;
	[SerializeField] float repulsionForce = 0.001f;
	[SerializeField] float drag = 0.001f;
	[SerializeField] bool physicsEnabled;
	[SerializeField] Agent agentPrefab;

	void Start()
	{
		agents = new List<Agent>();
		for (var i = 0; i < agentCount; i++)
		{
			var agent = Instantiate(agentPrefab, transform, false);

			var scale = Random.Range(0.1f, 1f);
			var position = Random.insideUnitCircle * 15f;

			agent.ManualStart(physicsEnabled, position, scale);
			agents.Add(agent);
		}

		detector = new CollisionDetector();
		results = new List<CollisionDetector.Result>();
		forces = new Vector2[agentCount];
	}

	void FixedUpdate()
	{
		var dt = Time.fixedDeltaTime;
		if (!physicsEnabled)
		{
UnityEngine.Profiling.Profiler.BeginSample("Detect");
			detector.Detect(results, agents);
UnityEngine.Profiling.Profiler.EndSample();

UnityEngine.Profiling.Profiler.BeginSample("Reset Force");
			for (var i = 0; i < agents.Count; i++)
			{
				forces[i] = Vector2.zero;
			}
UnityEngine.Profiling.Profiler.EndSample();

UnityEngine.Profiling.Profiler.BeginSample("Apply Force");
			for (var i = 0; i < results.Count; i++)
			{
				var result = results[i];
				var a0 = agents[result.agentIndex0];
				var a1 = agents[result.agentIndex1];

				var p0 = a0.transform.position;
				var p1 = a1.transform.position;

				var d = new Vector2(p1.x, p1.y) - new Vector2(p0.x, p0.y);
				var l = d.magnitude;
				var r = a0.Radius + a1.Radius;
				var f = (r - l) * 0.5f * repulsionForce;

				var force = (d / l) * f;

				forces[result.agentIndex0] -= force;
				forces[result.agentIndex1] += force;
			}
		}
UnityEngine.Profiling.Profiler.EndSample();

UnityEngine.Profiling.Profiler.BeginSample("Update Agents");
		for (var i = 0; i < agents.Count; i++)
		{
			agents[i].ManualFixedUpdate(dt, centeringForce, drag, forces[i]);
		}
UnityEngine.Profiling.Profiler.EndSample();
	}

	void Update()
	{
		var dt = Time.deltaTime;
		for (var i = 0; i < agents.Count; i++)
		{
			agents[i].ManualUpdate(dt);
		}		
	}	

	// non public ----
	List<Agent> agents;
	CollisionDetector detector;
	List<CollisionDetector.Result> results;
	Vector2[] forces;
}
