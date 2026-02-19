using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.IO;

public class CodexImporter : EditorWindow
{
    private TextAsset _csvFile;
    private CodexDatabase _targetDatabase;

    [MenuItem("CursorReboot/Import Codex Data")]
    public static void ShowWindow()
    {
        GetWindow<CodexImporter>("Codex Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Smart Data Importer (Auto Icon)", EditorStyles.boldLabel);
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("아이콘 파일은 'Resources/Icons' 폴더에 'UnlockID'와 같은 이름으로 넣어주세요.\n(예: Item_CursorBlade.png)", MessageType.Info);

        _csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File (.csv)", _csvFile, typeof(TextAsset), false);
        _targetDatabase = (CodexDatabase)EditorGUILayout.ObjectField("Target Database", _targetDatabase, typeof(CodexDatabase), false);

        GUILayout.Space(20);

        if (GUILayout.Button("Import Data"))
        {
            if (_csvFile == null || _targetDatabase == null)
            {
                EditorUtility.DisplayDialog("Error", "CSV 파일과 DB를 할당해주세요.", "OK");
                return;
            }
            ImportData();
        }
    }

    private void ImportData()
    {
        string[] lines = _csvFile.text.Split('\n');
        _targetDatabase.Entries.Clear();

        int successCount = 0;
        int iconMissingCount = 0;

        // i=1 (헤더 건너뜀)
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = SplitCsvLine(line);

            // 데이터 컬럼이 5개여도 됨 (IconPath 없어도 됨)
            if (values.Length < 5) continue;

            try
            {
                // CSV 순서: ID, Category, Name, Description, FlavorText 
                // (IconPath 열은 아예 없어도 됩니다!)
                string idStr = values[0];
                string catStr = values[1];
                string name = values[2];
                string desc = values[3];
                string flavor = values[4];

                if (!System.Enum.TryParse(idStr, out UnlockID id)) continue;
                if (!System.Enum.TryParse(catStr, out CodexCategory category)) continue;

                // 자동 아이콘 검색 로직
                // 1순위: UnlockID와 같은 이름 찾기 (가장 정확함)
                Sprite loadedIcon = Resources.Load<Sprite>($"Icons/{idStr}");

                // 2순위: 못 찾았으면 Name과 같은 이름 찾기 (공백 제거)
                if (loadedIcon == null)
                {
                    string cleanName = name.Replace(" ", "");
                    loadedIcon = Resources.Load<Sprite>($"Icons/{cleanName}");
                }

                if (loadedIcon == null) iconMissingCount++;

                CodexEntry newEntry = new CodexEntry
                {
                    UnlockID = id,
                    Category = category,
                    Name = name,
                    Icon = loadedIcon, // 찾은 아이콘 자동 할당
                    Description = desc.Replace("\"", ""),
                    FlavorText = flavor.Replace("\"", "")
                };

                _targetDatabase.Entries.Add(newEntry);
                successCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Importer] 줄 {i + 1} 에러: {e.Message}");
            }
        }

        EditorUtility.SetDirty(_targetDatabase);
        AssetDatabase.SaveAssets();

        string resultMsg = $"{successCount}개 데이터 로드 완료!";
        if (iconMissingCount > 0) resultMsg += $"\n({iconMissingCount}개의 아이콘을 못 찾았습니다. 이름을 확인하세요)";

        EditorUtility.DisplayDialog("Import Result", resultMsg, "확인");
    }

    private string[] SplitCsvLine(string line)
    {
        string pattern = ",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))";
        string[] result = Regex.Split(line, pattern);
        for (int i = 0; i < result.Length; i++) result[i] = result[i].Trim().Trim('"');
        return result;
    }
}