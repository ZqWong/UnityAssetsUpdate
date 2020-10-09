// Decompiled with JetBrains decompiler
// Type: ETPVersionCheck.VersionUtil.BinarySerializeOpt
// Assembly: ETPVersionUtil, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5C3B3BE6-B4E9-4BFE-BB17-66B03FC0EA25
// Assembly location: E:\Main\Focus\2018Project\Assets\Plugins\ETPVersionUtil.dll

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace Esp.Core.Utils.Core
{
  public class BinarySerializeOpt
  {
    public static bool Xmlserialize(string path, object obj)
    {
      try
      {
        using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
          using (StreamWriter streamWriter = new StreamWriter((Stream) fileStream, Encoding.UTF8))
            new XmlSerializer(obj.GetType()).Serialize((TextWriter) streamWriter, obj);
        }
        return true;
      }
      catch (Exception ex)
      {
        Debug.LogError( ("此类无法转换成xml " +  obj.GetType() + "," +  ex));
      }
      return false;
    }

    public static T XmlDeserialize<T>(string path) where T : class
    {
      T obj = default (T);
      try
      {
        using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
          obj = (T) new XmlSerializer(typeof (T)).Deserialize((Stream) fileStream);
      }
      catch (Exception ex)
      {
        Debug.LogError( ("此xml无法转成二进制: " + path + "," +  ex));
      }
      return obj;
    }

    public static object XmlDeserialize(string path, Type type)
    {
      object obj =  null;
      try
      {
        using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
          obj = new XmlSerializer(type).Deserialize((Stream) fileStream);
      }
      catch (Exception ex)
      {
        Debug.LogError( ("此xml无法转成二进制: " + path + "," +  ex));
      }
      return obj;
    }

    public static bool BinarySerilize(string path, object obj)
    {
      try
      {
        using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
          new BinaryFormatter().Serialize((Stream) fileStream, obj);
        return true;
      }
      catch (Exception ex)
      {
        Debug.LogError( ("此类无法转换成二进制 " +  obj.GetType() + "," +  ex));
      }
      return false;
    }
  }
}
