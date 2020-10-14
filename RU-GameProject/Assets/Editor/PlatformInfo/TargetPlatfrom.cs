using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "PlatformConfig", menuName = "CreatPlatformConfigFile", order = 1)]
public class TargetPlatform : ScriptableObject
{    
    /// <summary>
    /// Platform Name List
    /// </summary>
    public List<PlatformNamePath> m_PlatformNamePath = new List<PlatformNamePath>();

    public Platform m_Platform;

    [SerializeField] private string m_branchName;
    public string BranchName
    {
        get
        {
            Debug.Assert(null != m_branchName, "Get branch name failed.");
            return m_branchName;
        }
    }

    public string GetCurrentPlatformPath()
    {
        foreach (var item in m_PlatformNamePath)
        {
            if (item.PlatformName == m_Platform.ToString())
            {
                return item.Path;
            }
        }
        return null;
    }

    public BuildTarget GetCurrentPlatformBuildTarget()
    {
        BuildTarget ret = BuildTarget.StandaloneWindows64;
        switch (m_Platform)
        {
            case Platform.PC:
            case Platform.VR:
                ret = BuildTarget.StandaloneWindows64;
                break;
            case Platform.Android:
                ret = BuildTarget.Android;
                break;
            case Platform.iOS:
                ret = BuildTarget.iOS;
                break;
        }
        return ret;
    }

    public string ServerUrl;

    public enum Platform
    {
        PC = 0,
        VR = 1,
        Android = 2,
        iOS =3,
    }

    [System.Serializable]
    public struct PlatformNamePath
    {
        public string PlatformName;
        public string Path;
    }
}
