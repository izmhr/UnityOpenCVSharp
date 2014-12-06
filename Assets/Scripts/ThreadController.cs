using UnityEngine;
using System.Collections;

public class ThreadController : MonoBehaviour
{
	//------------------------------------------------------------
	// Awake
	//------------------------------------------------------------
	void Awake()
	{
		var thread_mgr = GameObject.Find("ThreadManager") as GameObject;
		m_ThreadManager = thread_mgr.GetComponent<ThreadManager>();
	}
	//------------------------------------------------------------
	// Update
	//------------------------------------------------------------
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.A))
		{
			m_ThreadManager.RequestWork();
		}
		if (Input.GetKeyDown(KeyCode.S))
		{
			m_ThreadManager.RequestWork((job) =>
			{
				Debug.Log("call back called.");
				Debug.Log(job.Count); // access job when finished work
			});
		}
		Debug.Log("random:" + m_ThreadManager.requestResult());
	}
	//------------------------------------------------------------
	// OnGUI
	//------------------------------------------------------------
	void OnGUI()
	{
		float w = Screen.width;
		float h = Screen.height;

		GUI.Label(
			new Rect(w * 0.0f, h * 0.0f, w * 0.5f, h * 0.1f),
			Time.realtimeSinceStartup.ToString("0.00000")
		);
	}

	//------------------------------------------------------------
	// member
	//------------------------------------------------------------
	private ThreadManager m_ThreadManager;
}