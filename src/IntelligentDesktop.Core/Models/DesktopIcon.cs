namespace IntelligentDesktop.Core.Models;

using System.Windows;
using System.Windows.Media;

/// <summary>
/// 데스크톱 아이콘 정보
/// </summary>
public class DesktopIcon
{
    /// <summary>아이콘 표시 이름</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>파일/폴더 전체 경로</summary>
    public string FullPath { get; set; } = string.Empty;
    
    /// <summary>데스크톱에서의 위치</summary>
    public Point Position { get; set; }
    
    /// <summary>아이콘 이미지 (캐시용)</summary>
    public ImageSource? Icon { get; set; }
    
    /// <summary>파일인지 폴더인지 여부</summary>
    public bool IsDirectory { get; set; }
    
    /// <summary>숨김 파일 여부</summary>
    public bool IsHidden { get; set; }
    
    /// <summary>선택 상태</summary>
    public bool IsSelected { get; set; }
}
