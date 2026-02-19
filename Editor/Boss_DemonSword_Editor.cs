using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Boss_DemonSword))]
public class Boss_DemonSword_Editor : Editor
{
    private void OnSceneGUI()
    {
        var boss = (Boss_DemonSword)target;

        // --- 각 히트박스를 그리고, 수정할 수 있게 합니다. ---
        // 이제 DrawSection이 offset과 size를 모두 ref로 받아 처리합니다.
        DrawEditableHitbox(boss, ref boss.Attack1_HitboxOffset, ref boss.Attack1_HitboxSize, Color.yellow);
        DrawEditableHitbox(boss, ref boss.Attack2_HitboxOffset, ref boss.Attack2_HitboxSize, new Color(1f, 0.5f, 0f));
        DrawEditableHitbox(boss, ref boss.Attack3_HitboxOffset, ref boss.Attack3_HitboxSize, Color.red);
        DrawEditableHitbox(boss, ref boss.Attack4_HitboxOffset, ref boss.Attack4_HitboxSize, Color.blue);
    }

    private void DrawEditableHitbox(Boss_DemonSword boss, ref Vector2 offset, ref Vector2 size, Color color)
    {
        if (boss == null) return;
        Transform bossTransform = boss.transform;
        float facingDirection = bossTransform.rotation.y == 0f ? 1f : -1f;

        // --- 1. 현재 히트박스의 월드 좌표 계산 ---
        Vector2 worldOffset = new Vector2(offset.x * facingDirection, offset.y);
        Vector2 center = (Vector2)bossTransform.position + worldOffset;
        Vector2 halfSize = size * 0.5f;

        Vector2[] corners = {
        center + new Vector2(-halfSize.x,  halfSize.y), // 0: Top-Left
        center + new Vector2( halfSize.x,  halfSize.y), // 1: Top-Right
        center + new Vector2( halfSize.x, -halfSize.y), // 2: Bottom-Right
        center + new Vector2(-halfSize.x, -halfSize.y)  // 3: Bottom-Left
    };

        // --- 2. 위치 및 크기 조절 핸들 그리기 ---
        Handles.color = color;

        // 위치 조절 핸들 (이제 크기 조절 시 함께 움직이므로, 시각적 표시 역할)
        Vector2 newCenter = Handles.PositionHandle(center, Quaternion.identity);
        if (newCenter != center)
        {
            Undo.RecordObject(boss, "Move Hitbox");
            Vector2 newLocalOffset = newCenter - (Vector2)bossTransform.position;
            newLocalOffset.x *= facingDirection;
            offset = newLocalOffset;
            // 중심점이 바뀌었으므로, 이 변경사항을 즉시 corners 계산에 반영
            center = newCenter;
            for (int i = 0; i < 4; i++) { corners[i] += newCenter - ((Vector2)bossTransform.position + worldOffset); }
        }

        // 크기 조절 핸들 (개별적으로 변경 감지)
        for (int i = 0; i < 4; i++)
        {
            EditorGUI.BeginChangeCheck();
            var fmh_56_68_638908138658901646 = Quaternion.identity; Vector2 newCorner = Handles.FreeMoveHandle(corners[i], 0.05f * HandleUtility.GetHandleSize(corners[i]), Vector3.zero, Handles.RectangleHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(boss, "Resize Hitbox");

                int oppositeCornerIndex = (i + 2) % 4;
                Vector2 fixedCorner = corners[oppositeCornerIndex];

                // 움직인 점과 고정된 대각선 점을 기준으로 새 중심과 크기 계산
                Vector2 recalculatedCenter = (newCorner + fixedCorner) / 2;
                Vector2 recalculatedSize = new Vector2(Mathf.Abs(newCorner.x - fixedCorner.x), Mathf.Abs(newCorner.y - fixedCorner.y));

                // 계산된 값을 원본 변수에 업데이트
                Vector2 newLocalOffset = recalculatedCenter - (Vector2)bossTransform.position;
                newLocalOffset.x *= facingDirection;

                offset = newLocalOffset;
                size = recalculatedSize;

                break; // 한 번에 하나의 핸들만 처리
            }
        }

        // --- 3. 최종 히트박스 그리기 ---
        // corners 배열로 Rect를 생성하여 올바르게 그립니다.
        Rect rectToDraw = new Rect(corners[3], size);
        Handles.DrawSolidRectangleWithOutline(rectToDraw, new Color(color.r, color.g, color.b, 0.1f), color);
    }
}