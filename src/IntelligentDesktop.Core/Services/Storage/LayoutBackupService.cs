namespace IntelligentDesktop.Core.Services.Storage;

using System.IO;
using System.Text.Json;
using IntelligentDesktop.Core.Models;

/// <summary>
/// 레이아웃 백업/복원 서비스
/// </summary>
public class LayoutBackupService
{
    private readonly string _backupDirectory;
    
    public LayoutBackupService()
    {
        _backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IntelligentDesktop",
            "Backups");
        
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    /// <summary>
    /// 현재 레이아웃 백업
    /// </summary>
    public string CreateBackup(AppConfiguration config, string? customName = null)
    {
        string fileName = customName ?? $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string filePath = Path.Combine(_backupDirectory, fileName);
        
        var backup = new LayoutBackup
        {
            CreatedAt = DateTime.Now,
            ScreenWidth = config.LastScreenWidth,
            ScreenHeight = config.LastScreenHeight,
            Boxes = config.Boxes.Select(b => new BoxBackup
            {
                Id = b.Id,
                Name = b.Name,
                X = b.X,
                Y = b.Y,
                Width = b.Width,
                Height = b.Height,
                NormalizedX = b.NormalizedX,
                NormalizedY = b.NormalizedY,
                IconPaths = new List<string>(b.IconPaths),
                StyleJson = JsonSerializer.Serialize(b.Style)
            }).ToList()
        };
        
        string json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
        
        return filePath;
    }

    /// <summary>
    /// 백업에서 레이아웃 복원
    /// </summary>
    public void RestoreBackup(string filePath, AppConfiguration config)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("백업 파일을 찾을 수 없습니다.", filePath);
        
        string json = File.ReadAllText(filePath);
        var backup = JsonSerializer.Deserialize<LayoutBackup>(json);
        
        if (backup == null)
            throw new InvalidDataException("백업 파일 형식이 올바르지 않습니다.");
        
        // 기존 박스 제거
        config.Boxes.Clear();
        
        // 백업된 박스 복원
        foreach (var boxBackup in backup.Boxes)
        {
            var box = new Box
            {
                Id = boxBackup.Id,
                Name = boxBackup.Name,
                X = boxBackup.X,
                Y = boxBackup.Y,
                Width = boxBackup.Width,
                Height = boxBackup.Height,
                NormalizedX = boxBackup.NormalizedX,
                NormalizedY = boxBackup.NormalizedY,
                IconPaths = boxBackup.IconPaths
            };
            
            // 스타일 복원
            if (!string.IsNullOrEmpty(boxBackup.StyleJson))
            {
                var style = JsonSerializer.Deserialize<BoxStyle>(boxBackup.StyleJson);
                if (style != null) box.Style = style;
            }
            
            config.Boxes.Add(box);
        }
    }

    /// <summary>
    /// 모든 백업 파일 목록 가져오기
    /// </summary>
    public IEnumerable<BackupInfo> GetAllBackups()
    {
        if (!Directory.Exists(_backupDirectory))
            yield break;
        
        foreach (var file in Directory.GetFiles(_backupDirectory, "*.json"))
        {
            var fileInfo = new FileInfo(file);
            yield return new BackupInfo
            {
                FilePath = file,
                FileName = fileInfo.Name,
                CreatedAt = fileInfo.CreationTime,
                Size = fileInfo.Length
            };
        }
    }

    /// <summary>
    /// 백업 삭제
    /// </summary>
    public void DeleteBackup(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}

/// <summary>
/// 레이아웃 백업 데이터
/// </summary>
public class LayoutBackup
{
    public DateTime CreatedAt { get; set; }
    public double ScreenWidth { get; set; }
    public double ScreenHeight { get; set; }
    public List<BoxBackup> Boxes { get; set; } = new();
}

/// <summary>
/// 박스 백업 데이터
/// </summary>
public class BoxBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double NormalizedX { get; set; }
    public double NormalizedY { get; set; }
    public List<string> IconPaths { get; set; } = new();
    public string StyleJson { get; set; } = string.Empty;
}

/// <summary>
/// 백업 정보
/// </summary>
public class BackupInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long Size { get; set; }
}
