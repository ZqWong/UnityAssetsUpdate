using System.Collections;
using System.Collections.Generic;
using System.IO;
using Esp.VersionCheck.DataModule.Json;
using LitJson;
using UnityEngine;
using System.Linq;

public class JsonOrderedTest : MonoBehaviour
{
    private ServerInfoDataModule m_localInfoDataModule;

    private string m_localJsonPath;

    // Start is called before the first frame update
    void Start()
    {
        m_localJsonPath = Application.dataPath + "/Resources/ServerInfo.json";
        Debug.Log("m_localJsonPath :" + m_localJsonPath);
        StreamReader sr = new StreamReader(m_localJsonPath);
        var content = sr.ReadToEnd();
        m_localInfoDataModule = new ServerInfoDataModule(JsonMapper.ToObject(content));
        sr.Close();

        // 将重新排序的数组重新赋值给datamodule， 且排序时应该是对int类型就行排序，否则执行的是字符串的默认排序
        m_localInfoDataModule.GameVersionInfos[0].Branches[0].Patches = m_localInfoDataModule.GameVersionInfos[0].Branches[0].Patches.OrderBy(i => int.Parse(i.Version)).ToList();

        foreach (var item in m_localInfoDataModule.GameVersionInfos[0].Branches[0].Patches)
        {
            Debug.Log("Item :" + item.Version);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
