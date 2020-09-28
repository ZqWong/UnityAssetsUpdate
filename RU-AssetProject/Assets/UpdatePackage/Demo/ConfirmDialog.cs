using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmDialog : MonoBehaviour
{
    public Text Info;
    public Text Add;

    // Use this for initialization
    void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    public void OnOKClick()
    {
        StartDownLoad();
    }

    public void OnCancelClick()
    {

    }
    public void Show(string info)
    {
        Info.text = info;
    }

   
    public Queue<string> InfoQueue = new Queue<string>();
    
    public void AddShow(string info)
    {
        if (InfoQueue.Count > 15)
        {
            InfoQueue.Dequeue();
        }

        InfoQueue.Enqueue(info);
        Add.text = "";
        foreach (var item in InfoQueue)
        {
            Add.text += item + "\n";
        }

    }

    void StartDownLoad()
    {
        Debug.Log("开始下载");
        AppStart._instance.StartDownload();

    }
}
