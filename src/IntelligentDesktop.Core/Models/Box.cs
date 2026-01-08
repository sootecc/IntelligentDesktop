namespace IntelligentDesktop.Core.Models;

using System.Text.Json.Serialization;
using System.Windows;

/// <summary>
/// 박스의 시각적 스타일 정보
/// </summary>
public class BoxStyle
{
    /// <summary>배경 색상 (ARGB 형식)</summary>
    public string BackgroundColor { get; set; } = "#80000000";
    
    /// <summary>테두리 색상</summary>
    public string BorderColor { get; set; } = "#40FFFFFF";
    
    /// <summary>테두리 두께</summary>
    public double BorderThickness { get; set; } = 1;
    
    /// <summary>모서리 반경</summary>
    public double CornerRadius { get; set; } = 8;
    
    /// <summary>제목 표시 여부</summary>
    public bool ShowTitle { get; set; } = true;
}

/// <summary>
/// 데스크톱 박스(컨테이너) 모델
/// </summary>
public class Box
{
    /// <summary>고유 식별자</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>박스 이름</summary>
    public string Name { get; set; } = "New Box";
    
    /// <summary>박스 위치 (Left)</summary>
    public double X { get; set; }
    
    /// <summary>박스 위치 (Top)</summary>
    public double Y { get; set; }
    
    /// <summary>박스 너비</summary>
    public double Width { get; set; } = 200;
    
    /// <summary>박스 높이</summary>
    public double Height { get; set; } = 300;
    
    /// <summary>아이콘의 원래 데스크톱 위치 저장 (파일명 -> "x,y")</summary>
    public Dictionary<string, string> OriginalPositions { get; set; } = new();

    /// <summary>상대 좌표 (해상도 변경 대응용, 0~1)</summary>
    public double NormalizedX { get; set; }
    public double NormalizedY { get; set; }
    public double NormalizedWidth { get; set; }
    public double NormalizedHeight { get; set; }
    
    /// <summary>포함된 아이콘 경로 목록</summary>
    public List<string> IconPaths { get; set; } = new();
    
    /// <summary>시각적 스타일</summary>
    public BoxStyle Style { get; set; } = new();
    
    /// <summary>표시 여부</summary>
    public bool IsVisible { get; set; } = true;
    
    /// <summary>최소화 상태</summary>
    public bool IsMinimized { get; set; } = false;
}
