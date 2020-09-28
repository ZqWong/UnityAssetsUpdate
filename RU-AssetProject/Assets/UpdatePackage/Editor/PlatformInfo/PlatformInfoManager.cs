using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine;

public class PlatformInfoManager : MonoBehaviour
{
    public const string TARGET_PLATFORM_CONFIG_PATH = "Assets/Resources/PlatformConfig/PlatformConfig.asset";

    /// <summary>
    /// Get current platform path by asset
    /// </summary>
    /// <returns>a path string</returns>
    public static string GetCurrentPlatformPath()
    {
        TargetPlatform targetPlatformConfig = AssetDatabase.LoadAssetAtPath<TargetPlatform>(TARGET_PLATFORM_CONFIG_PATH);
        return targetPlatformConfig.GetCurrentPlatformPath();
    }

    /// <summary>
    /// Get current platform server url by asset
    /// </summary>
    /// <returns>a url string</returns>
    public static string GetServerUrl()
    {
        TargetPlatform targetPlatformConfig = AssetDatabase.LoadAssetAtPath<TargetPlatform>(TARGET_PLATFORM_CONFIG_PATH);
        return targetPlatformConfig.ServerUrl;
    }

    /// <summary>
    /// Get current platform asset bundle build target by asset
    /// </summary>
    /// <returns>BuildTarget enum</returns>
    public static BuildTarget GetCurrentPlatformBuildTarget()
    {
         TargetPlatform targetPlatformConfig = AssetDatabase.LoadAssetAtPath<TargetPlatform>(TARGET_PLATFORM_CONFIG_PATH);
         return targetPlatformConfig.GetCurrentPlatformBuildTarget();
    }
}