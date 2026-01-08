namespace IntelligentDesktop.Core.Services.Sorting;

using System.IO;
using IntelligentDesktop.Core.Models;

/// <summary>
/// 자동 정렬 엔진 서비스 인터페이스
/// </summary>
public interface ISortingService
{
    /// <summary>파일 경로에 맞는 규칙 찾기</summary>
    SortRule? FindMatchingRule(string filePath);
    
    /// <summary>모든 데스크톱 아이콘을 규칙에 따라 분류</summary>
    Dictionary<Guid, List<string>> AutoSortAll(IEnumerable<string> iconPaths, IEnumerable<Box> boxes);
    
    /// <summary>규칙 목록</summary>
    List<SortRule> Rules { get; }
    
    /// <summary>규칙 추가</summary>
    void AddRule(SortRule rule);
    
    /// <summary>규칙 제거</summary>
    void RemoveRule(Guid ruleId);
}

/// <summary>
/// 자동 정렬 엔진 서비스 구현
/// </summary>
public class SortingService : ISortingService
{
    private readonly List<SortRule> _rules = new();
    
    public List<SortRule> Rules => _rules;

    public SortingService()
    {
        // 기본 규칙 초기화
        InitializeDefaultRules();
    }

    private void InitializeDefaultRules()
    {
        foreach (var rule in DefaultCategories.All)
        {
            _rules.Add(rule);
        }
    }

    /// <summary>
    /// 파일 경로에 맞는 규칙 찾기
    /// </summary>
    public SortRule? FindMatchingRule(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return null;

        string extension;

        // 폴더인지 확인
        if (Directory.Exists(filePath))
        {
            extension = ".folder"; // 가상의 확장자 사용
        }
        else
        {
            extension = Path.GetExtension(filePath).ToLowerInvariant();
        }
        
        // 확장자가 없으면(파일인데 확장자 없음) .none 등으로 처리 가능하나 여기선 패스
        if (string.IsNullOrEmpty(extension)) return null;
        
        // 우선순위 순으로 정렬된 활성 규칙 중 매칭되는 첫 번째 반환
        return _rules
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.Priority)
            .FirstOrDefault(r => r.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 모든 데스크톱 아이콘을 규칙에 따라 분류
    /// </summary>
    public Dictionary<Guid, List<string>> AutoSortAll(IEnumerable<string> iconPaths, IEnumerable<Box> boxes)
    {
        var result = new Dictionary<Guid, List<string>>();
        var boxList = boxes.ToList();
        
        // 각 박스에 대해 빈 리스트 초기화
        foreach (var box in boxList)
        {
            result[box.Id] = new List<string>();
        }
        
        // 각 규칙과 박스 이름 매칭 (규칙 이름과 박스 이름이 같으면 연결)
        var ruleBoxMapping = new Dictionary<Guid, Guid>();
        foreach (var rule in _rules.Where(r => r.IsEnabled))
        {
            // 이미 TargetBoxId가 설정되어 있으면 사용
            if (rule.TargetBoxId != Guid.Empty)
            {
                ruleBoxMapping[rule.Id] = rule.TargetBoxId;
            }
            else
            {
                // 박스 이름과 규칙 이름이 일치하는 박스 찾기
                var matchingBox = boxList.FirstOrDefault(b => 
                    b.Name.Equals(rule.Name, StringComparison.OrdinalIgnoreCase));
                
                if (matchingBox != null)
                {
                    ruleBoxMapping[rule.Id] = matchingBox.Id;
                }
            }
        }
        
        // 각 파일에 대해 규칙 적용
        foreach (var path in iconPaths)
        {
            var matchingRule = FindMatchingRule(path);
            if (matchingRule != null && ruleBoxMapping.TryGetValue(matchingRule.Id, out var boxId))
            {
                if (result.ContainsKey(boxId))
                {
                    result[boxId].Add(path);
                }
            }
        }
        
        return result;
    }

    public void AddRule(SortRule rule)
    {
        _rules.Add(rule);
    }

    public void RemoveRule(Guid ruleId)
    {
        _rules.RemoveAll(r => r.Id == ruleId);
    }

    /// <summary>
    /// 박스에 규칙 연결
    /// </summary>
    public void LinkRuleToBox(string ruleName, Guid boxId)
    {
        var rule = _rules.FirstOrDefault(r => r.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase));
        if (rule != null)
        {
            rule.TargetBoxId = boxId;
        }
    }

    /// <summary>
    /// 규칙에 확장자 추가
    /// </summary>
    public void AddExtensionToRule(Guid ruleId, string extension)
    {
        var rule = _rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule != null && !string.IsNullOrEmpty(extension))
        {
            string ext = extension.StartsWith('.') ? extension : $".{extension}";
            if (!rule.Extensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                rule.Extensions.Add(ext.ToLowerInvariant());
            }
        }
    }
}
