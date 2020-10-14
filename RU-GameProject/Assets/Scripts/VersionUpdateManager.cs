using System;
using System.Collections;
using Esp.Core.VersionCheck;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

public class VersionUpdateManager : MonoBehaviour
{

    [SerializeField] private Slider m_assetProgressSlider;
    [SerializeField] private Text m_assetContentText;
    [SerializeField] private GameObject m_confirmPanel;
    [SerializeField] private Text m_updateContent;
    [SerializeField] private Text m_messageText;

    private Action StartLoadCallback;
    private string xmlURLFormat
    {
        //GameManager.Instance.VersionXmlURL 配置 http://172.16.16.4:8080/resourcemgr2/Download/StructurePrinciple
        get { return @"http://172.16.16.4:8080/resourcemgr2/Download/AssetDownloadTest_WZQ" + "/PC/ServerInfo.json";}
    }

    public void Start()
    {
        //StaticJsonManager.Instance.Initialize();
        //Initialize(() => { Debug.LogError("Download complete"); });
        ////  Initialize(() => { Debug.LogError("Download complete"); });
        //Debug.LogError("Version :" + StaticJsonManager.Instance.VersionInfo.VersionInfo.Version);
        //Debug.LogError("PackageName :" + StaticJsonManager.Instance.VersionInfo.VersionInfo.PackageName);


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

    private string localAssetPath;

    public void Initialize(Action startLoadCallback)
    {
        Debug.Log("VersionUpdateManager Initialize ");

        StartLoadCallback = startLoadCallback;
        string head = "";
        string m_UnPackPath = "";
        string m_DownLoadPath = "";
        string m_ServerXmlPath = "";
        string m_LocalXmlPath = "";
        string m_branchName = "";
        string xmlURL = "";
        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            m_branchName = "Elevator";
            head = Application.persistentDataPath + "/AssetBundles/PC/" + m_branchName;
            m_UnPackPath = head;
            m_DownLoadPath = head;
            m_ServerXmlPath = head + "/ServerInfo.json";
            m_LocalXmlPath = head + "/LocalInfo.json";
            xmlURL = String.Format(xmlURLFormat, "Elevator", "PC");
          
        }
        else if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WindowsEditor)
        {
            head = Application.persistentDataPath + "/AssetBundles/Android/" + m_branchName;
            m_UnPackPath = head;
            m_DownLoadPath = head;
            m_ServerXmlPath = head + "/ServerInfo.xml";
            m_LocalXmlPath = head + "/LocalInfo.xml";
            xmlURL = String.Format(xmlURLFormat, "Elevator", "Android");
            m_branchName = "Elevator";
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.WindowsEditor)
        {
            head = Application.persistentDataPath + "/AssetBundles/IOS/" + m_branchName;
            m_UnPackPath = head;
            m_DownLoadPath = head;
            m_ServerXmlPath = head + "/ServerInfo.json";
            m_LocalXmlPath = head + "/LocalInfo.json";
            xmlURL = String.Format(xmlURLFormat, "Elevator", "PC");
            m_branchName = "Elevator";
        }

        localAssetPath = head;

        HotPatchManager.Instance.Initialize(this, xmlURL, m_branchName, CheckVersionCallback, m_UnPackPath, m_DownLoadPath,
            m_ServerXmlPath, m_LocalXmlPath, OnDownloadProgressUpdate);
    }

    public void CheckVersionCallback(bool isHot)
    {
        Debug.Log("更新： " + isHot);
        if (isHot)
        {
            string des = string.Empty;
#if XML
            des = HotPatchManager.Instance.CurrentPatches.Des;     
#elif JSON

            foreach (var patch in HotPatchManager.Instance.CurrentPatches)
            {
                des += "\n" + patch.Des;
            }

#endif
            string sizeString = GetDownloadLength(HotPatchManager.Instance.LoadSumSize);
            string info = string.Format("存在热更新 : {0}   文件数量 :{1}  文件总大小: {2}内容 ：{3}", isHot,
                HotPatchManager.Instance.LoadFileCount, sizeString, des);

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

    public void OnClickDeleteFilesBtn()
    {
        if (System.IO.Directory.Exists(localAssetPath))
        {
            Debug.LogError("   存在 删除文件夹 ");
            m_messageText.text = string.Format(DateTime.Now + " 存在 删除文件夹 【{0}】", localAssetPath);
            // System.IO.Directory.Delete(@updateAssets.list[0].LocalUrl);
            try
            {
                var dir = new System.IO.DirectoryInfo(localAssetPath);
                var files = dir.GetFiles();
                foreach (var file in files)
                {
                    m_messageText.text += "\n fileName :" + file.Name;
                }
                dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
                dir.Delete(true);
            }
            catch (Exception ex)
            {
                m_messageText.text = string.Format(DateTime.Now + " 文件夹存在 删除文件夹时 出现错误 【{0}】", ex.Message);
            }
        }
        else
        {
            m_messageText.text = DateTime.Now + "不存在指定文件夹 ：" + localAssetPath;
        }
    }

    public void OnClickStart()
    {
        Initialize(() => { Debug.LogError("Download complete"); });
    }
}
