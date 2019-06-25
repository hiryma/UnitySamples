//#define UNITY_WEBGL // WEBGLのテスト

using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Kayac
{
	// 個別終了待ち合わせできない低機能スレッドプール
	public class ThreadPool
	{
		public interface IJob
		{
			void Execute();
		}
		public ThreadPool(int threadCount, int jobCapacity = 128)
		{
			jobStartSemaphore = new Semaphore(0, jobCapacity);
			jobEndSemaphore = new Semaphore(0, jobCapacity);
			queue = new Queue<IJob>();
#if !UNITY_WEBGL
			threads = new Thread[threadCount];
#endif
			jobSamplers = new CustomSampler[threadCount];
			for (int i = 0; i < threadCount; i++)
			{
				jobSamplers[i] = CustomSampler.Create("MyThreadPool.Job");
#if !UNITY_WEBGL
				threads[i] = new Thread(ThreadFunc);
				threads[i].Priority = System.Threading.ThreadPriority.BelowNormal;
				threads[i].Start(i);
#endif
			}
		}

		public void Dispose()
		{
#if !UNITY_WEBGL
			for (int i = 0; i < threads.Length; i++)
			{
				queue.Enqueue(null); // ダミージョブ投入
				jobStartSemaphore.Release(); // 投入通知
			}
			for (int i = 0; i < threads.Length; i++) // 終了待ち
			{
				threads[i].Join();
			}
#endif
			threads = null;
			jobStartSemaphore = null;
			jobEndSemaphore = null;
			queue = null;
		}

		public void AddJob(IJob job)
		{
			Debug.Assert(job != null);
			lock (queue)
			{
				queue.Enqueue(job);
			}
			jobStartSemaphore.Release();
			queuedCount++;
#if UNITY_WEBGL // 今すぐ実行
			TryExecute(0);
#endif
		}

		public void Wait()
		{
			UnityEngine.Profiling.Profiler.BeginSample("ThreadPool.Wait");
			// 終了待ち。終了済みの数がスレッド数になるまで待つ
			for (int i = 0; i < queuedCount; i++)
			{
				jobEndSemaphore.WaitOne();
			}
			queuedCount = 0;
			UnityEngine.Profiling.Profiler.EndSample();
		}

		public bool IsComplete()
		{
			// 終了待ち。終了済みの数がスレッド数になるまで待つ
			for (int i = 0; i < queuedCount; i++)
			{
				if (jobEndSemaphore.WaitOne(0))
				{
					queuedCount--;
				}
				else
				{
					break;
				}
			}
			return (queuedCount == 0);
		}

		// non public ---------------------
		Semaphore jobStartSemaphore;
		Semaphore jobEndSemaphore;
		Thread[] threads;
		Queue<IJob> queue;
		int queuedCount;
		CustomSampler[] jobSamplers;

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
			jobStartSemaphore.WaitOne(); // ジョブが投入されるまで待つ
			IJob job = null;
			lock (queue)
			{
				job = queue.Dequeue();
			}
			if (job == null) // 終了のダミージョブ
			{
				return false;
			}
			try
			{
				jobSamplers[index].Begin();
				job.Execute();
				jobSamplers[index].End();
			}
			catch (System.Exception e)
			{
				Debug.Log("Job Threw Exception: " + e.GetType().Name);
			}
			jobEndSemaphore.Release();
			return true;
		}
	}
}