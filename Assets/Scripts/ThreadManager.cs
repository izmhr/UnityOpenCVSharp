using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class ThreadManager : MonoBehaviour
{

	private int randomnum = 0;
	System.Random cRandom = new System.Random();
	//------------------------------------------------------------
	// class Job
	//------------------------------------------------------------
	public class Job
	{
		// ctor
		public Job(System.Action<ThreadManager.Job> callback = null)
		{
			IsEnd = false;
			Count = 0;
			CallBack = callback;
		}
		// some blocking work
		public void LoopCount()
		{
			while (Count < 100000000)
			{
				Count++;
			}
		}
		// property
		public bool IsEnd
		{
			get;
			set;
		}
		public int Count
		{
			get;
			set;
		}
		public System.Action<ThreadManager.Job> CallBack
		{
			get;
			set;
		}
	}

	//------------------------------------------------------------
	// Awake
	//------------------------------------------------------------
	void Awake()
	{
		//m_Thread = new Thread(threadWork);
		m_Thread = new Thread(threadWork2);
		m_Thread.Start();
	}
	//------------------------------------------------------------
	// Update
	//------------------------------------------------------------
	void Update()
	{
		m_JobCount = m_JobList.Count;
	}
	//------------------------------------------------------------
	// OnApplicationQuit
	//------------------------------------------------------------
	void OnApplicationQuit()
	{
		m_Thread.Abort();
	}

	//------------------------------------------------------------
	// job process on thread
	//------------------------------------------------------------
	private void threadWork()
	{
		while (true)
		{
			//m_Thread.Sleep(0);
			Thread.Sleep(0);

			// if exist job, do job.
			if (m_JobList.Count > 0)
			{
				// only 1 work in this sample
				ThreadManager.Job job;
				lock (m_SyncObj)
				{
					job = m_JobList[0];
				}

				if (!job.IsEnd)
				{
					Debug.Log("job start.");

					job.LoopCount();    // job work
					job.IsEnd = true;

					// do callback
					if (job.CallBack != null)
					{
						job.CallBack(job);
					}

					Debug.Log("job end.");

					// remove job
					lock (m_SyncObj)
					{
						m_JobList.Remove(job);
						job = null;
					}
				}
			}
		}
	}

	//------------------------------------------------------------
	// thread loop
	//------------------------------------------------------------
	private void threadWork2()
	{
		while(true)
		{
			Thread.Sleep(0);

			var job = new ThreadManager.Job();
			Debug.Log("new job created");
			job.LoopCount();	// job work
			job.IsEnd = true;

			Debug.Log("job end");

			lock(m_SyncObj)
			{
				randomnum = cRandom.Next();
			}

		}
	}

	public int requestResult()
	{
		lock(m_SyncObj)
		{
			Debug.Log("returned Result");
			return randomnum;
		}
	}

	//------------------------------------------------------------
	// request work
	//------------------------------------------------------------
	public void RequestWork(System.Action<ThreadManager.Job> callback = null)
	{
		lock (m_SyncObj)
		{
			var job = new ThreadManager.Job(callback);
			m_JobList.Add(job);
		}

		Debug.Log("request job.");
	}

	//------------------------------------------------------------
	// member
	//------------------------------------------------------------
	private Thread m_Thread;
	private List<ThreadManager.Job> m_JobList = new List<ThreadManager.Job>();
	private Object m_SyncObj = new Object();
	public int m_JobCount;
}