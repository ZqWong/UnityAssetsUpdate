using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Esp.Core.VersionCheck;

public class VersionTest : MonoBehaviour
{
    public static VersionTest _instance;
    public Text ShowText;
    public Text AddText;
    public Button DownloadButton;
    public Button StartButton;



    public string xmlURL = "http://172.16.16.4:8080/resourcemgr2/Download/Hot/Android/ServerInfo.xml";

    void Awake()
    {
        _instance = this;

        string m_UnPackPath = Application.persistentDataPath + "/Origin";
        string m_DownLoadPath = Application.persistentDataPath + "/DownLoad";
        string m_ServerXmlPath = Application.persistentDataPath + "/ServerInfo.xml";
        string m_LocalXmlPath = Application.persistentDataPath + "/LocalInfo.xml";

        HotPatchManager.Instance.Initialize(this, xmlURL, CheckVersionCallback, m_UnPackPath, m_DownLoadPath,
            m_ServerXmlPath, m_LocalXmlPath, OnDownloadProgressUpdate);
    }

    public void CheckVersionCallback(bool isHot)
    {
        Debug.Log("是否更新：" + isHot);
        AddShow("是否更新：" + isHot);

        DownloadButton.gameObject.SetActive(isHot);
        StartButton.gameObject.SetActive(!isHot);

        if (isHot)
        {
            float size = HotPatchManager.Instance.LoadSumSize / 1024;
            string sizeString = "";
            if (size < 0.1f)
            {
                sizeString = HotPatchManager.Instance.LoadSumSize.ToString("F1") + " KB";
            }
            else
            {
                sizeString = size.ToString("F1") + " MB";
            }

            string info = string.Format("存在热更新 : {0}   文件数量 :{1}  文件总大小: {2}内容 ：{3}", isHot,
                HotPatchManager.Instance.LoadFileCount, sizeString, HotPatchManager.Instance.CurrentPatches.Des);
            Debug.Log(info);
            ShowInfo(info);
        }

        //ShowString.Instance.Show = AddShow;
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //if (HotPatchManager.Instance.CurrentDownLoadFileItem != null)
        //{
        //    DownloadInfo info = HotPatchManager.Instance.CurrentDownLoadFileItem.Info;
        //    OnDownloadProgressUpdate(info.GetStateInfo(), info.GetProgress());
        //}
    }

    public void ShowInfo(string info)
    {
        ShowText.text = info;
    }

    private Queue<string> InfoQueue = new Queue<string>();

    public void AddShow(string info)
    {
        if (InfoQueue.Count > 15)
        {
            InfoQueue.Dequeue();
        }

        InfoQueue.Enqueue(info);
        AddText.text = "";
        foreach (var item in InfoQueue)
        {
            AddText.text += item + "\n";
        }

    }

    public void OnEnterGameButtonClick()
    {
        StartOnFinish();
    }

    public void OnDownloadButtonClick()
    {
        HotPatchManager.Instance.StartDownLoad(OnDownloadComplete);
    }


    private void OnDownloadComplete()
    {
        DownloadButton.gameObject.SetActive(false);
        StartButton.gameObject.SetActive(true);
    }

    private void StartOnFinish()
    {
        Debug.Log("StartOnFinish！！！！！");
    }

    public Slider ProgressSlider;
    public Text InfoText;
    public Text ProgressText;

    public void SetProgress(float value)
    {
        ProgressSlider.value = value;
        ProgressText.text = ((int) (value * 100)).ToString("F0") + " %";
    }
    public void SliderInfoText(string info)
    {
        InfoText.text = info;
    }
    public void OnDownloadProgressUpdate(string info, float value)
    {
        SliderInfoText(info);
        SetProgress(value);
    }

}
