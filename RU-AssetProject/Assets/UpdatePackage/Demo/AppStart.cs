using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppStart : MonoBehaviour
{
    public static AppStart _instance;

    public ConfirmDialog HotConfirmDialog;
    public LoadZipFile LoadZipFile;

    void Awake()
    {
        _instance = this;
        HotPatchManager.Instance.Init(this);
        HotPatchManager.Instance.CheckVersion(CheckVersionCallback);
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        
        //if (HotPatchManager.Instance.StartDownload)
        //{
        //    HotConfirmDialog.Show("下载中。。。" + HotPatchManager.Instance.GetProgress().ToString("F2"));
        //}
        //else
        //{
        //    if (HotPatchManager.Instance.DownloadFinish)
        //    {
        //        HotConfirmDialog.Show("下载完成 100 %");
        //        HotPatchManager.Instance.DownloadFinish = false;
        //    }
        //}
	}

    void CheckVersionCallback(bool isHot)
    {
        Debug.Log("是否更新：" + isHot);
        HotConfirmDialog.AddShow("是否更新：" + isHot);
        if (isHot)
        {
            string info = string.Format("存在热更新 : {0}   文件数量 :{1}  文件总大小: {2:F1} KB 内容 ：{3}", isHot,
                HotPatchManager.Instance.LoadFileCount, HotPatchManager.Instance.LoadSumSize, HotPatchManager.Instance.CurrentPatches.Des);
            Debug.Log(info);
            HotConfirmDialog.Show(info);
        }
    }


    public void StartDownload()
    {
        StartCoroutine(HotPatchManager.Instance.StartDownLoadAB(StartOnFinish));
    }

    void StartOnFinish()
    {
        Debug.Log(" 文件正常 或者 下载完毕 开始解压");
        HotConfirmDialog.Show("下载完成 自动解压完成 ");

    }




}
