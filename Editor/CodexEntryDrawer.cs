using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(CodexEntry))]
public class CodexEntryDrawer : PropertyDrawer
{
    // 리스트 항목의 높이를 계산 (기본 높이 유지)
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    // 화면에 그리는 함수
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // UnlockID 속성 찾기
        SerializedProperty idProp = property.FindPropertyRelative("UnlockID");

        string newLabel = label.text; // 기본값 (예: "Element 0")

        if (idProp != null)
        {
            // 현재 항목의 인덱스 번호 추출 (PropertyPath 파싱)
            // path 예시: "Entries.Array.data[5]" -> 여기서 5를 가져옴
            string indexStr = GetIndexFromPath(property.propertyPath);

            // Enum의 현재 이름 가져오기 (예: "Item_CursorBlade")
            // enumNames 배열에서 현재 선택된 인덱스(enumValueIndex)의 이름을 가져옵니다.
            string enumName = "";
            if (idProp.enumValueIndex >= 0 && idProp.enumValueIndex < idProp.enumNames.Length)
            {
                enumName = idProp.enumNames[idProp.enumValueIndex];
            }
            else
            {
                enumName = "Unknown";
            }

            // 라벨 변경 ("0. Item_CursorBlade" 형식)
            newLabel = $"{indexStr}. {enumName}";
        }

        // 라벨 텍스트 교체
        label.text = newLabel;

        // 원래대로 속성 그리기 (접었다 폈다 기능 유지)
        EditorGUI.PropertyField(position, property, label, true);
    }

    // 경로 문자열에서 인덱스 숫자만 빼오는 헬퍼 함수
    private string GetIndexFromPath(string path)
    {
        try
        {
            int startIndex = path.LastIndexOf('[') + 1;
            int endIndex = path.LastIndexOf(']');
            if (startIndex > 0 && endIndex > startIndex)
            {
                return path.Substring(startIndex, endIndex - startIndex);
            }
        }
        catch { }
        return "?";
    }
}