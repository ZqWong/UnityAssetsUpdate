using System.Collections;
using System.Collections.Generic;
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
            m_OpenFileName = new OpenFileName();
            m_OpenFileName.structSize = Marshal.SizeOf(m_OpenFileName);
            m_OpenFileName.filter = "MD5 File(*.bytes)\0*.bytes";
            m_OpenFileName.file = new string(new char[256]);
            m_OpenFileName.maxFile = m_OpenFileName.file.Length;
            m_OpenFileName.fileTitle = new string(new char[64]);
            m_OpenFileName.maxFileTitle = m_OpenFileName.fileTitle.Length;
            m_OpenFileName.initialDir = (Application.dataPath + "/../Version").Replace("/", "\\");
            m_OpenFileName.title = "Select MD5 Dialog";
            m_OpenFileName.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;
            if (LocalDialog.GetSaveFileName(m_OpenFileName))
            {
                Debug.Log(m_OpenFileName.file);
                md5Path = m_OpenFileName.file;
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(30);

        GUILayout.BeginHorizontal();
        hotCount = EditorGUILayout.TextField("Update Version: ", hotCount, GUILayout.Width(350), GUILayout.Height(20));
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Update Description: ",GUILayout.Width(147));
            des = EditorGUILayout.TextArea(des, GUILayout.Height(50));
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("START PACKING", GUILayout.Width(150), GUILayout.Height(50)))
        {
            if (!string.IsNullOrEmpty(md5Path) && md5Path.EndsWith(".bytes"))
            {
                //BundleEditor.Build();
                CreateAssetBundle.ReadMd5Com(md5Path, hotCount, des);
            }
        }    
        GUILayout.EndHorizontal();
    }
}