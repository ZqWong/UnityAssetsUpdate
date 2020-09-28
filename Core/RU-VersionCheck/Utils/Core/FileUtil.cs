// Decompiled with JetBrains decompiler
// Type: ETPVersionCheck.VersionUtil.FileUtil
// Assembly: ETPVersionUtil, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5C3B3BE6-B4E9-4BFE-BB17-66B03FC0EA25
// Assembly location: E:\Main\Focus\2018Project\Assets\Plugins\ETPVersionUtil.dll

using System;
using System.Collections;
using System.IO;
using RU.Core.Utils.Scripts.BaseFrame;
using UnityEngine;

namespace RU.Core.Utils.Core
{
    public class FileUtil : Singleton<FileUtil>
    {
        public Action<float> UpdateProgress = (Action<float>)null;
        private long m_SpeedLimit = 1000000;

        public void SetSpeedLimit(long speed) => this.m_SpeedLimit = speed;

        public IEnumerator CreateFile(
          string filePath,
          byte[] bytes,
          Action<float> updateProgress)
        {
            Debug.Log((object)("CreateFile： " + filePath));
            this.UpdateProgress = updateProgress;
            if (File.Exists(filePath))
                File.Delete(filePath);
            FileInfo file = new FileInfo(filePath);
            if (file.Directory != null && !file.Directory.Exists)
                file.Directory.Create();
            int size = 2048;
            byte[] buffer = new byte[size];
            FileStream fs = File.Create(filePath);
            Stream stream = (Stream)new MemoryStream(bytes);
            long tempWrite = 0;
            long alreadyWrite = 0;
            long totalSize = (long)bytes.Length;
            while (true)
            {
                size = stream.Read(buffer, 0, buffer.Length);
                if (size > 0)
                {
                    tempWrite += (long)size;
                    alreadyWrite += (long)size;
                    fs.Write(buffer, 0, size);
                    if (tempWrite > this.m_SpeedLimit)
                    {
                        float per = (float)alreadyWrite / (float)totalSize;
                        Action<float> updateProgress1 = this.UpdateProgress;
                        if (updateProgress1 != null)
                            updateProgress1(per);
                        yield return (object)new WaitForEndOfFrame();
                        tempWrite = 0L;
                    }
                }
                else
                    break;
            }
            Action<float> updateProgress2 = this.UpdateProgress;
            if (updateProgress2 != null)
                updateProgress2(1f);
            yield return (object)new WaitForEndOfFrame();
            stream.Close();
            stream.Dispose();
            fs.Close();
            fs.Dispose();
            yield return (object)null;
        }
    }
}
