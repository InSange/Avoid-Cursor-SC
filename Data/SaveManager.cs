using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 딕셔너리 저장을 위한 헬퍼 구조체입니다.
/// </summary>
[Serializable]
public struct QuestProgressEntry
{
    public UnlockID ID;
    public int Progress;
}

[Serializable]
public class AvoidCursorSaveData : ISerializationCallbackReceiver
{
    // --- 런타임용 (게임 로직에서 사용하는 고속 조회용 데이터) ---
    // JsonUtility는 이 친구들을 무시합니다.
    public HashSet<UnlockID> UnlockedElements = new HashSet<UnlockID>();
    public Dictionary<UnlockID, int> QuestProgress = new Dictionary<UnlockID, int>();

    // --- 저장용 (JsonUtility가 실제로 저장하는 리스트) ---
    [SerializeField] private List<UnlockID> _savedUnlockList = new List<UnlockID>();
    [SerializeField] private List<QuestProgressEntry> _savedQuestList = new List<QuestProgressEntry>();

    /// <summary>
    /// 저장하기 직전(ToJson 호출 시) 자동으로 실행됩니다.
    /// HashSet/Dictionary -> List 변환
    /// </summary>
    public void OnBeforeSerialize()
    {
        // 1. 해금 목록 변환
        _savedUnlockList.Clear();
        foreach (var id in UnlockedElements)
        {
            _savedUnlockList.Add(id);
        }

        // 2. 퀘스트 진행도 변환
        _savedQuestList.Clear();
        foreach (var kvp in QuestProgress)
        {
            _savedQuestList.Add(new QuestProgressEntry { ID = kvp.Key, Progress = kvp.Value });
        }
    }

    /// <summary>
    /// 불러온 직후(FromJson 호출 시) 자동으로 실행됩니다.
    /// List -> HashSet/Dictionary 복구
    /// </summary>
    public void OnAfterDeserialize()
    {
        // 1. 해금 목록 복구
        UnlockedElements.Clear();
        foreach (var id in _savedUnlockList)
        {
            UnlockedElements.Add(id);
        }

        // 2. 퀘스트 진행도 복구
        QuestProgress.Clear();
        foreach (var entry in _savedQuestList)
        {
            // 중복 키 방지를 위해 Add 대신 인덱서 사용
            QuestProgress[entry.ID] = entry.Progress;
        }
    }
}

public static class SaveManager
{
    private static readonly string SavePath = Application.persistentDataPath + "/save.json";

    public static void Save(AvoidCursorSaveData data)
    {
        // JsonUtility가 내부적으로 OnBeforeSerialize를 호출합니다.
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        // (디버깅용) 저장 경로 출력
        // Debug.Log($"[SaveManager] Saved to: {SavePath}");
    }

    public static AvoidCursorSaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveManager] 세이브 파일이 없어 새로 생성합니다.");
            var newData = new AvoidCursorSaveData();

            // 초기 해금 데이터 설정
            newData.UnlockedElements.Add(UnlockID.Cursor_Default);
            newData.UnlockedElements.Add(UnlockID.Quest_Survive10Min);

            // 파일 생성
            Save(newData);
            return newData;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            // JsonUtility가 내부적으로 OnAfterDeserialize를 호출하여 Dictionary를 복구합니다.
            return JsonUtility.FromJson<AvoidCursorSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 로드 실패! 초기화합니다. 오류: {e.Message}");
            return new AvoidCursorSaveData();
        }
    }

    /// <summary>
    /// (테스트용) 세이브 파일을 삭제합니다.
    /// </summary>
    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[SaveManager] 세이브 파일 삭제됨.");
        }
    }
}