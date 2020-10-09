// Decompiled with JetBrains decompiler
// Type: ETPVersionCheck.VersionUtil.ZipUtil
// Assembly: ETPVersionUtil, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5C3B3BE6-B4E9-4BFE-BB17-66B03FC0EA25
// Assembly location: E:\Main\Focus\2018Project\Assets\Plugins\ETPVersionUtil.dll

using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace Esp.Core.Utils.Core
{
    public class ZipUtil : Singleton<ZipUtil>
    {
        private int deltaSize = 2048;
        private long m_decompressionFileSize = 0;
        private long m_currentFileSize = 0;
        private long m_UnPackSpeed = 1400000;
        public Action<float> UpdateProgress = (Action<float>)null;
        private FileStream fs = (FileStream)null;
        private ZipInputStream zipStream = (ZipInputStream)null;
        private ZipEntry ent = (ZipEntry)null;
        private byte[] data;

        public void SetUnPackSpeedLimit(long speed) => this.m_UnPackSpeed = speed;

        public float Progress { get; private set; }

        public IEnumerator SaveZip(
          string ZipID,
          byte[] ZipByte,
          Action<float> updateProgress,
          string password = null)
        {
            this.UpdateProgress = updateProgress;
            yield return (object)new WaitForSeconds(0.5f);
            if (!Directory.Exists(ZipID))
                Directory.CreateDirectory(ZipID);
            Encoding gbk = Encoding.GetEncoding("gbk");
            ZipConstants.DefaultCodePage = gbk.CodePage;
            Stream stream = (Stream)new MemoryStream(ZipByte);
            this.zipStream = new ZipInputStream(stream);
            this.m_currentFileSize = this.GetZipBytes(ZipByte);
            if (!string.IsNullOrEmpty(password))
                this.zipStream.Password = password;
            while ((this.ent = this.zipStream.GetNextEntry()) != null)
            {
                this.ent.IsUnicodeText = true;
                if (!string.IsNullOrEmpty(this.ent.Name) && !this.ent.Name.EndsWith(".meta"))
                {
                    string fileName = Path.Combine(ZipID, this.ent.Name);
                    FileInfo file = new FileInfo(fileName);
                    if (file.Directory != null && !file.Directory.Exists)
                        file.Directory.Create();
                    fileName = fileName.Replace('\\', '/');
                    if (fileName.EndsWith("/"))
                    {
                        Directory.CreateDirectory(fileName);
                    }
                    else
                    {
                        this.fs = File.Create(fileName);
                        int size = this.deltaSize;
                        this.data = new byte[size];
                        yield return (object)this.ReadZip(this.zipStream, this.data, size, this.fs);
                        fileName = (string)null;
                        file = (FileInfo)null;
                    }
                }
            }
            yield return (object)new WaitForEndOfFrame();
            if (this.UpdateProgress != null)
                this.UpdateProgress(1f);
            this.AllDispose();
            yield return (object)null;
        }

        private IEnumerator ReadZip(
          ZipInputStream zipStream,
          byte[] buffer,
          int size,
          FileStream fs)
        {
            long temp = 0;
            this.m_decompressionFileSize = 0L;
            while (true)
            {
                size = zipStream.Read(buffer, 0, buffer.Length);
                if (size > 0)
                {
                    this.m_decompressionFileSize += (long)size;
                    temp += (long)size;
                    fs.Write(buffer, 0, size);
                    this.Progress = (float)this.m_decompressionFileSize / (float)this.m_currentFileSize;
                    if (temp > this.m_UnPackSpeed)
                    {
                        if (this.UpdateProgress != null)
                            this.UpdateProgress(this.Progress);
                        yield return (object)new WaitForEndOfFrame();
                        temp = 0L;
                    }
                }
                else
                    break;
            }
        }

        private long GetZipBytes(byte[] ZipByte)
        {
            long num = 0;
            ZipInputStream zipInputStream = new ZipInputStream((Stream)new MemoryStream(ZipByte));
            while ((this.ent = zipInputStream.GetNextEntry()) != null)
                num += this.ent.Size;
            zipInputStream.Close();
            zipInputStream.Dispose();
            return num;
        }

        private void AllDispose()
        {
            if (this.fs != null)
            {
                this.fs.Close();
                this.fs.Dispose();
            }
            if (this.zipStream != null)
            {
                this.zipStream.Close();
                this.zipStream.Dispose();
            }
            if (this.ent != null)
                this.ent = (ZipEntry)null;
            GC.Collect();
            GC.Collect(1);
        }

        public string StringToUnicode(string s)
        {
            char[] charArray = s.ToCharArray();
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < charArray.Length; ++index)
            {
                byte[] bytes = Encoding.Unicode.GetBytes(charArray[index].ToString());
                stringBuilder.Append(string.Format("\\u{0:X2}{1:X2}", (object)bytes[1], (object)bytes[0]));
            }
            return stringBuilder.ToString();
        }
    }
}
