namespace IntelligentDesktop.Core.Services.Storage;

using System.IO;
using IntelligentDesktop.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 애플리케이션 전체 설정
/// </summary>
public class AppConfiguration
{
    /// <summary>박스 목록</summary>
    public List<Box> Boxes { get; set; } = new();
    
    /// <summary>Windows 시작 시 자동 실행</summary>
    public bool StartWithWindows { get; set; } = false;
    
    /// <summary>바탕화면 더블클릭으로 아이콘 숨기기</summary>
    public bool DoubleClickToHide { get; set; } = true;
    
    /// <summary>아이콘 숨김 상태</summary>
    public bool IconsHidden { get; set; } = false;
    
    /// <summary>마지막 사용 해상도 (레이아웃 복원용)</summary>
    public double LastScreenWidth { get; set; }
    public double LastScreenHeight { get; set; }

    /// <summary>시계 위젯 표시 여부</summary>
    public bool IsClockWidgetVisible { get; set; } = false;
    /// <summary>시계 위젯 위치 (x,y)</summary>
    public string ClockWidgetPosition { get; set; } = "100,100";
    /// <summary>시계 위젯 투명도 (0.0~1.0)</summary>
    public double ClockWidgetOpacity { get; set; } = 0.5;
    public string ClockWidgetColor { get; set; } = "#000000"; // 기본 검정 // 기본값 반투명
}

/// <summary>
/// 설정 파일 관리 서비스 인터페이스
/// </summary>
public interface IConfigurationService
{
    AppConfiguration Load();
    void Save(AppConfiguration config);
    string ConfigFilePath { get; }
}

/// <summary>
/// JSON 기반 설정 저장 서비스
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly string _configDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ConfigFilePath { get; }

    public ConfigurationService()
    {
        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IntelligentDesktop");
        
        ConfigFilePath = Path.Combine(_configDirectory, "config.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        EnsureConfigDirectory();
    }

    private void EnsureConfigDirectory()
    {
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
        }
    }

    public AppConfiguration Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                return JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions) 
                       ?? new AppConfiguration();
            }
        }
        catch (Exception)
        {
            // 파일이 손상된 경우 새 설정 반환
        }

        return new AppConfiguration();
    }

    public void Save(AppConfiguration config)
    {
        EnsureConfigDirectory();
        string json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(ConfigFilePath, json);
    }
}
