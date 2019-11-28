//#define UNITY_WEBGL // WEBGLのテスト
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Kayac
{
    // 個別終了待ち合わせできない低機能スレッドプール
    public class SimpleThreadPool
    {
        public SimpleThreadPool(int threadCount = -1, int jobCapacity = 128)
        {
            if (threadCount < 0)
            {
                var cpuCount = SystemInfo.processorCount;
                threadCount = System.Math.Min((cpuCount * 3) / 4, cpuCount - 1); //コア数-1か、0.75コア数、の小さい方(雑)
            }
#if UNITY_WEBGL
            threadCount = 0;
#endif
            jobStartSemaphore = new Semaphore(0, jobCapacity);
            jobEndSemaphore = new Semaphore(0, jobCapacity);
            queue = new Queue<System.Action>();
            if (threadCount > 0)
            {
                CreateSamplers(threadCount);
                threads = new Thread[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    threads[i] = new Thread(ThreadFunc);
                    threads[i].Priority = System.Threading.ThreadPriority.BelowNormal;
                    threads[i].Start(i);
                }
            }
            else
            {
                // サンプラ一個だけ作る
                CreateSamplers(1);
            }
        }

        void CreateSamplers(int count)
        {
            idleSamplers = new CustomSampler[count];
            jobSamplers = new CustomSampler[count];
            for (int i = 0; i < count; i++)
            {
                idleSamplers[i] = CustomSampler.Create("MyThreadPool.Idle");
                jobSamplers[i] = CustomSampler.Create("MyThreadPool.Job");
            }
        }

        public int threadCount
        {
            get
            {
                return (threads != null) ? threads.Length : 0;
            }
        }

        public void Dispose()
        {
            if (threads != null)
            {
                for (int i = 0; i < threads.Length; i++)
                {
                    queue.Enqueue(null); // ダミージョブ投入
                    jobStartSemaphore.Release(); // 投入通知
                }
                for (int i = 0; i < threads.Length; i++) // 終了待ち
                {
                    threads[i].Join();
                    Debug.Assert(!threads[i].IsAlive);
                }
                threads = null;
            }
            Debug.Assert(queue.Count == 0);
            jobStartSemaphore = null;
            jobEndSemaphore = null;
            queue = null;
        }

        public void AddJob(System.Action job)
        {
            Debug.Assert(job != null);
            lock (queue)
            {
                queue.Enqueue(job);
            }
            jobStartSemaphore.Release();
            queuedCount++;
            if (threads == null) // スレッドないので即実行
            {
                TryExecute(0);
            }
        }

        public void Wait()
        {
            // 終了待ち。終了済みの数がスレッド数になるまで待つ
            for (int i = 0; i < queuedCount; i++)
            {
                jobEndSemaphore.WaitOne();
            }
            queuedCount = 0;
        }

        public bool completed
        {
            get
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
        }
        // non public -------------------

        void ThreadFunc(object arg)
        {
            int index = (int)arg;
            Profiler.BeginThreadProfiling("MyThreadPool", "MyThread: " + index.ToString());
            while (true) // 終了要求が来ていなければ先へ。タイムアウトを0にすれば待たずに抜けられる。
            {
                if (!TryExecute(index))
                {
                    break;
                }
            }
            Profiler.EndThreadProfiling();
        }

        bool TryExecute(int index)
        {
            idleSamplers[index].Begin();
            jobStartSemaphore.WaitOne(); // ジョブが投入されるまで待つ
            idleSamplers[index].End();
            System.Action job = null;
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
                job();
                jobSamplers[index].End();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            jobEndSemaphore.Release();
            return true;
        }
        Semaphore jobStartSemaphore;
        Semaphore jobEndSemaphore;
        Thread[] threads;
        Queue<System.Action> queue;
        int queuedCount;
        CustomSampler[] idleSamplers;
        CustomSampler[] jobSamplers;
    }
}