using System;
using System.Collections;
using RU.Core.VersionCheck;
using UnityEngine;
using UnityEngine.UI;

public class VersionUpdateManager : MonoBehaviour
{

    [SerializeField] private Slider m_assetProgressSlider;
    [SerializeField] private Text m_assetContentText;
    [SerializeField] private GameObject m_confirmPanel;
    [SerializeField] private Text m_updateContent;

    private Action StartLoadCallback;
    private string xmlURLFormat
    {
        //GameManager.Instance.VersionXmlURL 配置 http://172.16.16.4:8080/resourcemgr2/Download/StructurePrinciple
        get { return @"http://172.16.16.4:8080/resourcemgr2/Download/AssetDownloadTest_WZQ" + "/{0}/ServerInfo.xml"; }
    }

    public void Start()
    {
        Initialize(() => { Debug.LogError("Download complete"); });
    }

    //private Slider m_ProgressSlider;
    //private Slider ProgressSlider
    //{
    //    get
    //    {
    //        if (m_ProgressSlider == null)
    //        {
    //            GameObject g = GameObject.Find("LoadCanvas/ESPLoading/Slider");
    //            if (g == null) return null;
    //            m_ProgressSlider = g.GetComponent<Slider>();
    //        }
    //        return m_ProgressSlider;
    //    }
    //}
    //private Text m_InfoText;
    //private Text InfoText
    //{
    //    get
    //    {
    //        if (m_InfoText == null)
    //        {
    //            GameObject g = GameObject.Find("LoadCanvas/ESPLoading/Slider/Info");
    //            if (g == null) return null;
    //            m_InfoText = g.GetComponent<Text>();
    //        }
    //        return m_InfoText;
    //    }
    //}
    //private Text m_ProgressText;
    //private Text ProgressText
    //{
    //    get
    //    {
    //        if (m_ProgressText == null)
    //        {
    //            GameObject g = GameObject.Find("LoadCanvas/ESPLoading/Slider/Per");
    //            if (g == null) return null;
    //            m_ProgressText = g.GetComponent<Text>();
    //        }
    //        return m_ProgressText;
    //    }
    //}

    public void OnClickInitBtn()
    {
        StartCoroutine(OnDownload());
        m_confirmPanel.SetActive(false);
        //Initialize(() => { Debug.LogError("Download complete"); });
    }

    public void Initialize(Action startLoadCallback)
    {
        Debug.Log("VersionUpdateManager Initialize ");

        StartLoadCallback = startLoadCallback;
        string head = "";
        string m_UnPackPath = "";
        string m_DownLoadPath = "";
        string m_ServerXmlPath = "";
        string m_LocalXmlPath = "";
        string xmlURL = "";
        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            head = Application.streamingAssetsPath+ "/AssetBundles/PC";
            m_UnPackPath = head;
            m_DownLoadPath = head;
            m_ServerXmlPath = head + "/ServerInfo.xml";
            m_LocalXmlPath = head + "/LocalInfo.xml";
            xmlURL = String.Format(xmlURLFormat, "PC");
        }
        else if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WindowsEditor)
        {
            head = Application.persistentDataPath + "/AssetBundles/Android";
            m_UnPackPath = head;
            m_DownLoadPath = head;
            m_ServerXmlPath = head + "/ServerInfo.xml";
            m_LocalXmlPath = head + "/LocalInfo.xml";
            xmlURL = String.Format(xmlURLFormat, "Android");
        }

        HotPatchManager.Instance.Initialize(this, xmlURL, CheckVersionCallback, m_UnPackPath, m_DownLoadPath,
            m_ServerXmlPath, m_LocalXmlPath, OnDownloadProgressUpdate);
    }

    public void CheckVersionCallback(bool isHot)
    {
        Debug.Log("更新： " + isHot);
        if (isHot)
        {
            string sizeString = GetDownloadLength(HotPatchManager.Instance.LoadSumSize);
            string info = string.Format("存在热更新 : {0}   文件数量 :{1}  文件总大小: {2}内容 ：{3}", isHot,
                HotPatchManager.Instance.LoadFileCount, sizeString, HotPatchManager.Instance.CurrentPatches.Des);

            m_confirmPanel.SetActive(true);
            m_updateContent.text = info;
            //更新信息
            //LoadingPanel.Instance.SliderInfoText(info);
            //StartCoroutine(OnDownload());
        }
        else
        {
            StartCoroutine(OnEnterGame());
        }
        
    }

    #region 文件大小计算

    private const float COMPUTING_UNIT = 1024;
    private int recursionIndex = 0;
    private string GetDownloadLength(float totalByte)
    {
        string ret = string.Empty;
        var length = ByteComputer(totalByte, ref recursionIndex);

        if (recursionIndex == 0)
        {
            ret = length.ToString("F2") + " Byte";
        }
        else if (recursionIndex == 1)
        {
            ret = length.ToString("F2") + " kB";
        }
        else if (recursionIndex == 2)
        {
            ret = length.ToString("F2") + " MB";
        }
        else if (recursionIndex == 3)
        {
            ret = length.ToString("F2") + " GB";
        }
        return ret;
    }

    private float ByteComputer(float totalByte, ref int recursionIndex)
    {
        if (totalByte > COMPUTING_UNIT)
        {
            totalByte = totalByte / COMPUTING_UNIT;
            recursionIndex++;
            return ByteComputer(totalByte, ref recursionIndex);
        }
        else
        {
            return totalByte;
        }
    }

    #endregion

    public IEnumerator OnEnterGame()
    {
        yield return new WaitForSeconds(0.5f);
        if (StartLoadCallback != null)
        {
            StartLoadCallback();
        }
    }

    public IEnumerator OnDownload()
    {
        yield return new WaitForSeconds(0.5f);
        HotPatchManager.Instance.StartDownLoad(StartLoadCallback);
    }

    public void OnDownloadProgressUpdate(string info, float value)
    {
        m_assetContentText.text = info;
        m_assetProgressSlider.value = value;
        //更新进度
        //LoadingPanel.Instance.OnProgressUpdate(info, value);
    }
}
