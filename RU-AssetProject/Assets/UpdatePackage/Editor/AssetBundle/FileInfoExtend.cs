using System.Collections.Generic;
using System.IO;

/// <summary>
/// 文件信息扩展类
/// </summary>
public class FileInfoExtend
{
    public FileInfo FileInfo;
    public string RelativePath;

    public FileInfoExtend(FileInfo FileInfo, string RelativePath)
    {
        this.FileInfo = FileInfo;
        this.RelativePath = RelativePath;
    }
}
