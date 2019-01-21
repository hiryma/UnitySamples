//#define UNITY_WEBGL // WEBGLのテスト

using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class ThreadPool
{
	Semaphore _jobStartSemaphore;
	Semaphore _jobEndSemaphore;
	Thread[] _threads;
	Queue<System.Action> _queue;
	int _queuedCount;
	CustomSampler[] _idleSamplers;
	CustomSampler[] _jobSamplers;

	// 個別終了待ち合わせできない低機能スレッドプール
	public ThreadPool(int threadCount, int jobCapacity = 128)
	{
		_jobStartSemaphore = new Semaphore(0, jobCapacity);
		_jobEndSemaphore = new Semaphore(0, jobCapacity);
		_queue = new Queue<System.Action>();
#if !UNITY_WEBGL
		_threads = new Thread[threadCount];
#endif
		_idleSamplers = new CustomSampler[threadCount];
		_jobSamplers = new CustomSampler[threadCount];
		for (int i = 0; i < threadCount; i++)
		{
			_idleSamplers[i] = CustomSampler.Create("MyThreadPool.Idle");
			_jobSamplers[i] = CustomSampler.Create("MyThreadPool.Job");
#if !UNITY_WEBGL
			_threads[i] = new Thread(ThreadFunc);
			_threads[i].Priority = System.Threading.ThreadPriority.BelowNormal;
			_threads[i].Start(i);
#endif
		}
	}

	void ThreadFunc(object arg)
	{
		int index = (int)arg;
		UnityEngine.Profiling.Profiler.BeginThreadProfiling("MyThreadPool", "MyThread: " + index.ToString());
		while (true) // 終了要求が来ていなければ先へ。タイムアウトを0にすれば待たずに抜けられる。
		{
			if (!TryExecute(index))
			{
				break;
			}
		}
		UnityEngine.Profiling.Profiler.EndThreadProfiling();
	}

	bool TryExecute(int index)
	{
		_idleSamplers[index].Begin();
		_jobStartSemaphore.WaitOne(); // ジョブが投入されるまで待つ
		_idleSamplers[index].End();
		System.Action job = null;
		lock (_queue)
		{
			job = _queue.Dequeue();
		}
		if (job == null) // 終了のダミージョブ
		{
			return false;
		}
		try
		{
			_jobSamplers[index].Begin();
			job();
			_jobSamplers[index].End();
		}
		catch (System.Exception e)
		{
			Debug.Log("Job Threw Exception: " + e.GetType().Name);
		}
		_jobEndSemaphore.Release();
		return true;
	}

	public void Dispose()
	{
#if !UNITY_WEBGL
		for (int i = 0; i < _threads.Length; i++)
		{
			_queue.Enqueue(null); // ダミージョブ投入
			_jobStartSemaphore.Release(); // 投入通知
		}
		for (int i = 0; i < _threads.Length; i++) // 終了待ち
		{
			_threads[i].Join();
		}
#endif
		_threads = null;
		_jobStartSemaphore = null;
		_jobEndSemaphore = null;
		_queue = null;
	}

	public void AddJob(System.Action job)
	{
		Debug.Assert(job != null);
		lock (_queue)
		{
			_queue.Enqueue(job);
		}
		_jobStartSemaphore.Release();
		_queuedCount++;
#if UNITY_WEBGL // 今すぐ実行
		TryExecute(0);
#endif
	}

	public void Wait()
	{
		// 終了待ち。終了済みの数がスレッド数になるまで待つ
		for (int i = 0; i < _queuedCount; i++)
		{
			_jobEndSemaphore.WaitOne();
		}
		_queuedCount = 0;
	}

	public bool IsComplete()
	{
		// 終了待ち。終了済みの数がスレッド数になるまで待つ
		for (int i = 0; i < _queuedCount; i++)
		{
			if (_jobEndSemaphore.WaitOne(0))
			{
				_queuedCount--;
			}
			else
			{
				break;
			}
		}
		return (_queuedCount == 0);
	}
}
