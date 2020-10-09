using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Esp.Core.Utils.Core;
using UnityEngine;
using UnityEditor;


public class HotPackageDialog : EditorWindow
{
    string m_md5Path = string.Empty;
    string m_hotCount = "1";
    private string m_description = string.Empty;
    OpenFileName m_openFileName = null;

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        m_md5Path = EditorGUILayout.TextField("Standard Package MD5 File: ", m_md5Path, GUILayout.Width(500), GUILayout.Height(20));
        if (GUILayout.Button("Select MD5 File", GUILayout.Width(150), GUILayout.Height(30)))
        {
            m_openFileName = new OpenFileName();
            m_openFileName.structSize = Marshal.SizeOf(m_openFileName);
            m_openFileName.filter = "MD5 File(*.bytes)\0*.bytes";
            m_openFileName.file = new string(new char[256]);
            m_openFileName.maxFile = m_openFileName.file.Length;
            m_openFileName.fileTitle = new string(new char[64]);
            m_openFileName.maxFileTitle = m_openFileName.fileTitle.Length;
            m_openFileName.initialDir = (Application.dataPath + "/Resources").Replace("/", "\\");
            m_openFileName.title = "Select MD5 Dialog";
            m_openFileName.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;
            if (LocalDialog.GetSaveFileName(m_openFileName))
            {
                Debug.Log(m_openFileName.file);
                m_md5Path = m_openFileName.file;
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(30);

        GUILayout.BeginHorizontal();
        m_hotCount = EditorGUILayout.TextField("Update Version: ", m_hotCount, GUILayout.Width(350), GUILayout.Height(20));
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Update Description: ",GUILayout.Width(147));
            m_description = EditorGUILayout.TextArea(m_description, GUILayout.Height(50));
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("START PACKING", GUILayout.Width(150), GUILayout.Height(50)))
        {
            if (!string.IsNullOrEmpty(m_md5Path) && m_md5Path.EndsWith(".bytes"))
            {
                //BundleEditor.Build();
                CreateAssetBundle.ReadMd5Com(m_md5Path, m_hotCount, m_description);
            }
        }    
        GUILayout.EndHorizontal();
    }
}