using System.Collections;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System;

public class LoadZipFile : MonoBehaviour
{
    public int deltaSize = 2048;
    public float Progress = 0;
    public long m_decompressionFileSize = 0;
    public long m_currentFileSize = 0;

    //限速  
    public long UnPackSpeed = 1400000;
    /// <summary>
    /// 下载
    /// </summary>
    /// <param name="ZipID"> ZipID的名字，用于存储解压出的每一个Zip文件</param>
    /// <param name="url" >Zip下载地址</param>
    /// <returns></returns>
    public IEnumerator Wait_LoadDown(string ZipID, string url, Action<byte[]> callback = null)
    {
        WWW www = new WWW(url);
        yield return www;

        while (!www.isDone)
        {
            string progress = (((int)(www.progress * 100)) % 100) + "%";
            yield return 1;
        }


        if (www.isDone)
        {
            if (www.error == null)
            {
                Debug.Log("下载成功");
                string dir = Application.persistentDataPath;
                Debug.Log(dir);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                yield return new WaitForEndOfFrame();
                Debug.Log("总大小 ：" + www.bytes.Length);

                //直接使用 将byte转换为Stream，省去先保存到本地在解压的过程
                //SaveZip(ZipID, url, www.bytes);


                if (callback != null)
                {
                    callback(www.bytes);
                }

            }
            else
            {
                Debug.Log(www.error);
            }
        }

    }

    FileStream fs = null;
    ZipInputStream zipStream = null;
    ZipEntry ent = null;
    byte[] data;

    public IEnumerator SaveZip(string ZipID, string url, byte[] ZipByte, string password = null)
    {
        yield return new WaitForSeconds(0.5f);
        bool result = true;

        string fileName;

        ZipID = Application.persistentDataPath + "/" + ZipID;

        if (!Directory.Exists(ZipID))
        {
            Directory.CreateDirectory(ZipID);


            if (AppStart._instance)
            {
                AppStart._instance.HotConfirmDialog.AddShow("CreateDirectory");
            }
            
        }
        else
        {
            


            if (AppStart._instance)
            {
                AppStart._instance.HotConfirmDialog.AddShow("ExistsDirectory");
            }
        }

        //直接使用 将byte转换为Stream，省去先保存到本地在解压的过程
        Stream stream = new MemoryStream(ZipByte);


        if (AppStart._instance)
        {
            AppStart._instance.HotConfirmDialog.AddShow("MemoryStream");
        }

        zipStream = new ZipInputStream(stream);



        if (AppStart._instance)
        {
            AppStart._instance.HotConfirmDialog.AddShow("zipStream");
        }

        yield return new WaitForSeconds(0.5f);
        m_currentFileSize = GetZipBytes(ZipByte);
        yield return new WaitForSeconds(0.5f);



        if (AppStart._instance)
        {
            AppStart._instance.HotConfirmDialog.AddShow("m_currentFileSize" + m_currentFileSize);
        }
        yield return new WaitForSeconds(2f);
        if (!string.IsNullOrEmpty(password))
        {
            zipStream.Password = password;
        }

        ////////////////////////


        if (AppStart._instance)
        {
            AppStart._instance.HotConfirmDialog.AddShow("password");
        }
        ///////////////

        //遍历文件
        while ((ent = zipStream.GetNextEntry()) != null)
        {
            
            //ZipTest._instance.AddShowText("ent.Name" + ent.Name);
            if (!string.IsNullOrEmpty(ent.Name))
            {
                fileName = Path.Combine(ZipID, ent.Name);
                //ZipTest._instance.AddShowText("Now" + fileName);
                Debug.Log(" fileName : " + fileName);
                #region      Android

                fileName = fileName.Replace('\\', '/');
                Debug.Log(" Replace : " + fileName);
                if (fileName.EndsWith("/"))
                {
                    Directory.CreateDirectory(fileName);
                    continue;
                }
                #endregion
               



                if (AppStart._instance)
                {
                    AppStart._instance.HotConfirmDialog.AddShow(fileName);
                }
                ///////////////
                fs = File.Create(fileName);
                

                ////////////////////////


                if (AppStart._instance)
                {
                    AppStart._instance.HotConfirmDialog.AddShow("fs == null :" + (fs == null));
                }
                ///////////////

                int size = deltaSize;
                data = new byte[size];

                //协程方法 
                yield return ReadZip(zipStream, data, size, fs);

                Debug.Log("Next");
                
                ////////////////////////


                if (AppStart._instance)
                {
                    AppStart._instance.HotConfirmDialog.AddShow("Next");
                }
                ///////////////
            }
        }

        AllDispose();
        Debug.Log("解压完毕！");
        

        ////////////////////////

        if (AppStart._instance)
        {
            AppStart._instance.HotConfirmDialog.AddShow("解压完毕");
        }
        ///////////////

        yield return null;
    }

    private IEnumerator ReadZip(ZipInputStream zipStream, byte[] buffer, int size, FileStream fs)
    {
        Debug.Log(fs.Name + "开始解压");

        ////////////////////////

        if (AppStart._instance)
        {
            AppStart._instance.HotConfirmDialog.AddShow(fs.Name + "开始解压");
        }
        ///////////////

        long temp = 0;
        while (true)
        {
            //Debug.Log(fs.Name + "正在解压");
            size = zipStream.Read(buffer, 0, buffer.Length);
            if (size > 0)
            {
                m_decompressionFileSize += size;
                temp += size;
                //Debug.Log("m_decompressionFileSize ：" + m_decompressionFileSize);
                fs.Write(buffer, 0, size); //解决读取不完整情况 
                Progress = (float) m_decompressionFileSize / m_currentFileSize * 100;
                
                if (temp > UnPackSpeed)
                {
                    AppStart._instance.HotConfirmDialog.Show(fs.Name + "  解压中：" + m_decompressionFileSize);
                    yield return new WaitForEndOfFrame();
                    temp = 0;
                }

            }
            else
            {
                break;
            }
        }
    }

    private long GetZipBytes(byte[] ZipByte)
    {
        long zipSize = 0;
        Stream lenStream = new MemoryStream(ZipByte);
        ZipInputStream lenzip = new ZipInputStream(lenStream);

        while ((ent = lenzip.GetNextEntry()) != null)
        {
            zipSize += ent.Size;
        }
        lenzip.Close();
        lenzip.Dispose();
        return zipSize;
    }


    private void AllDispose()
    {
        if (fs != null)
        {
            fs.Close();
            fs.Dispose();
        }
        if (zipStream != null)
        {
            zipStream.Close();
            zipStream.Dispose();
        }
        if (ent != null)
        {
            ent = null;
        }
        GC.Collect();
        GC.Collect(1);
    }
  
}


