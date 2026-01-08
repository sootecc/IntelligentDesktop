namespace IntelligentDesktop.Core.Models;

/// <summary>
/// 분류 규칙 정의
/// </summary>
public class SortRule
{
    /// <summary>고유 ID</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>규칙 이름</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>대상 확장자 목록 (예: [".docx", ".pdf", ".txt"])</summary>
    public List<string> Extensions { get; set; } = new();
    
    /// <summary>이 규칙에 해당하는 파일이 들어갈 박스 ID</summary>
    public Guid TargetBoxId { get; set; }
    
    /// <summary>규칙 활성화 여부</summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>우선순위 (낮을수록 먼저 적용)</summary>
    public int Priority { get; set; } = 100;
}

/// <summary>
/// 기본 제공 분류 카테고리
/// </summary>
public static class DefaultCategories
{
    public static readonly SortRule Documents = new()
    {
        Name = "문서",
        Extensions = new List<string> { ".doc", ".docx", ".pdf", ".txt", ".xlsx", ".xls", ".ppt", ".pptx", ".hwp", ".odt" }
    };
    
    public static readonly SortRule Images = new()
    {
        Name = "이미지",
        Extensions = new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp", ".ico", ".tiff" }
    };
    
    public static readonly SortRule Videos = new()
    {
        Name = "동영상",
        Extensions = new List<string> { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" }
    };
    
    public static readonly SortRule Music = new()
    {
        Name = "음악",
        Extensions = new List<string> { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a" }
    };
    
    public static readonly SortRule Programs = new()
    {
        Name = "프로그램",
        Extensions = new List<string> { ".exe", ".msi", ".lnk", ".bat", ".cmd", ".ps1" }
    };
    
    public static readonly SortRule Archives = new()
    {
        Name = "압축파일",
        Extensions = new List<string> { ".zip", ".rar", ".7z", ".tar", ".gz", ".iso" }
    };
    
    public static readonly SortRule Code = new()
    {
        Name = "코드",
        Extensions = new List<string> { ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h", ".html", ".css", ".json", ".xml" }
    };

    public static readonly SortRule Folders = new()
    {
        Name = "폴더",
        Extensions = new List<string> { ".folder" } // SortingService에서 가상 확장자로 처리
    };
    
    /// <summary>모든 기본 카테고리 목록</summary>
    public static List<SortRule> All => new()
    {
        Documents, Images, Videos, Music, Programs, Archives, Code, Folders
    };
}
