using System.Collections.Generic;
using UnityEngine;

public enum CodexCategory
{
    ActiveItem, // 스페이스바 스킬
    Artifact,   // 패시브 유물
    Boss        // 보스 몬스터
}

[System.Serializable]
public struct CodexEntry
{
    public UnlockID UnlockID; // 해금 여부 체크용 키
    public CodexCategory Category;

    [Header("Display Info")]
    public string Name;
    public Sprite Icon;
    [TextArea] public string Description; // 기능 설명
    [TextArea] public string FlavorText;  // 설정/스토리 텍스트
}

[CreateAssetMenu(menuName = "CursorReboot/Codex Database")]
public class CodexDatabase : ScriptableObject
{
    public List<CodexEntry> Entries;

    /// <summary>
    /// 카테고리에 맞는 리스트만 반환
    /// </summary>
    public List<CodexEntry> GetEntriesByCategory(CodexCategory category)
    {
        return Entries.FindAll(x => x.Category == category);
    }
}