using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;


public class DownLoadFileItem : DownLoadItem
{
    private UnityWebRequest m_WebRequest;
    //private UnityWebRequestAsyncOperation asy;
    private float progress = 0f;
    public DownLoadFileItem(string url, string path) : base(url, path)
    {

    }

  
    public IEnumerator Download(Action callback = null)
    {
        m_StartDownLoad = true;
        progress = 0f;
        AppStart._instance.HotConfirmDialog.AddShow(m_Url);
        //WWW www = new WWW(m_Url);
        //while (www.progress<1)
        //{
        //    yield return new WaitForEndOfFrame();
        //    progress = www.progress;
        //    string progressStr = (((int)(www.progress * 100)) % 100) + "%";
        //    AppStart._instance.HotConfirmDialog.Show("下载 ： " + progressStr);
        //}

        m_WebRequest = UnityWebRequest.Get(m_Url);
        //m_WebRequest.timeout = 30;
        m_WebRequest.SendWebRequest();

        while (m_WebRequest.downloadProgress < 1)
        {
            yield return new WaitForEndOfFrame();
            progress = m_WebRequest.downloadProgress;
            string progressStr = (((int)(m_WebRequest.downloadProgress * 100)) % 100) + "%";
            AppStart._instance.HotConfirmDialog.Show("下载 ： " + progressStr);
        }

        yield return new WaitForEndOfFrame();

        m_StartDownLoad = false;
        progress = 1f;
        
        AppStart._instance.HotConfirmDialog.Show("m_WebRequest.isDone  " + m_WebRequest.isDone);
        yield return new WaitForSeconds(3f);

        if (m_WebRequest.isDone)
        {
            if (m_WebRequest.error == null)
            {
                Debug.Log("下载成功");
                yield return new WaitForEndOfFrame();
                AppStart._instance.HotConfirmDialog.Show("下载成功");

                string dir = Application.persistentDataPath;
                Debug.Log(dir);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
               
                yield return new WaitForEndOfFrame();
                Debug.Log("总大小 ：" + m_WebRequest.downloadHandler.data.Length);
                AppStart._instance.HotConfirmDialog.AddShow("总大小 ：" + m_WebRequest.downloadHandler.data.Length);
                yield return new WaitForSeconds(1.0f);
                
                //SaveZip(ZipID, url, www.bytes);

                //#region      Android
                //fileName = fileName.Replace('\\', '/');

                //if (fileName.EndsWith("/"))
                //{
                //    Directory.CreateDirectory(fileName);
                //    continue;
                //}
                //#endregion
                Debug.Log("创建文件   " + m_SaveFilePath);

                if (m_Url.EndsWith(".zip"))
                {
                    //全部解压到ZIP
                    yield return AppStart._instance.LoadZipFile.SaveZip("ZIP", "", m_WebRequest.downloadHandler.data);
                    AppStart._instance.HotConfirmDialog.AddShow("ZIP   ");
                }
                else
                {
                    //byte写入文件流  
                    yield return FileTool.Instance.CreateFile(m_SaveFilePath, m_WebRequest.downloadHandler.data);
                }
                
                yield return new WaitForSeconds(1.0f);
                if (callback != null)
                {
                    callback();
                }

            }
            else
            {
                AppStart._instance.HotConfirmDialog.AddShow("Error :  " + m_WebRequest.error);
            }
        }

    }


    public override void Destory()
    {
        if (m_WebRequest != null)
        {
            m_WebRequest.Dispose();
            m_WebRequest = null;
        }
    }

    public override long GetCurLength()
    {
        if (m_WebRequest != null)
        {
            return (long)m_WebRequest.downloadedBytes;
        }
        return 0;
    }

    public override long GetLength()
    {
        return 0;
    }

    public override float GetProcess()
    {
        return progress;
    }
}
