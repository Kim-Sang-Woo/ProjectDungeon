// ============================================================
// EventResultEditor.cs — EventResult SO 커스텀 에디터
// 위치: Assets/Editor/EventResultEditor.cs
// ============================================================
// [개요]
//   EventResult.effects ([SerializeReference] 배열)에
//   Inspector에서 효과 타입을 드롭다운으로 추가/제거할 수 있도록 한다.
//
// [지원 효과 타입]
//   - GainItemEffect      : 아이템 획득
//   - RemoveItemEffect    : 아이템 제거
//   - GainGoldEffect      : 골드 획득
//   - LoseGoldEffect      : 골드 소모
//   - ApplyStatusEffect   : 상태이상 부여
//   - ConsumeObjectEffect : 오브젝트 소진
//   - OpenRewardEffect    : 보상 UI 열기
//
// [새 효과 타입 추가 방법]
//   EFFECT_TYPES 배열에 타입을 추가하면 드롭다운에 자동 반영된다.
// ============================================================
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EventResult))]
public class EventResultEditor : Editor
{
    // ── 지원 효과 타입 목록 ──────────────────────────────
    // 새 EventEffect 서브클래스를 추가하면 여기에도 등록할 것.
    private static readonly (string label, Type type)[] EFFECT_TYPES =
    {
        ("아이템 획득 (GainItemEffect)",       typeof(GainItemEffect)),
        ("아이템 제거 (RemoveItemEffect)",      typeof(RemoveItemEffect)),
        ("골드 획득 (GainGoldEffect)",          typeof(GainGoldEffect)),
        ("골드 소모 (LoseGoldEffect)",          typeof(LoseGoldEffect)),
        ("상태이상 부여 (ApplyStatusEffect)",   typeof(ApplyStatusEffect)),
        ("오브젝트 소진 (ConsumeObjectEffect)", typeof(ConsumeObjectEffect)),
        ("보상 UI 열기 (OpenRewardEffect)",     typeof(OpenRewardEffect)),
    };

    // ─────────────────────────────────────────────────────
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EventResult result = (EventResult)target;

        // 기본 필드 (resultId, resultDesc, resultImage, nextChoices) 자동 출력
        DrawPropertiesExcluding(serializedObject, "effects");

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("효과 (Effects)", EditorStyles.boldLabel);

        // ── effects 배열 편집 ─────────────────────────────
        SerializedProperty effectsProp = serializedObject.FindProperty("effects");

        if (result.effects == null)
            result.effects = new EventEffect[0];

        // 기존 효과 목록 표시
        for (int i = 0; i < result.effects.Length; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EventEffect effect = result.effects[i];
            string typeName = effect != null ? effect.GetType().Name : "(null)";
            EditorGUILayout.LabelField($"[{i}] {typeName}", EditorStyles.boldLabel);

            // 위로 이동
            GUI.enabled = i > 0;
            if (GUILayout.Button("▲", GUILayout.Width(24)))
            {
                Undo.RecordObject(target, "Move Effect Up");
                (result.effects[i - 1], result.effects[i]) = (result.effects[i], result.effects[i - 1]);
                EditorUtility.SetDirty(target);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            // 아래로 이동
            GUI.enabled = i < result.effects.Length - 1;
            if (GUILayout.Button("▼", GUILayout.Width(24)))
            {
                Undo.RecordObject(target, "Move Effect Down");
                (result.effects[i + 1], result.effects[i]) = (result.effects[i], result.effects[i + 1]);
                EditorUtility.SetDirty(target);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            GUI.enabled = true;

            // 삭제
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                Undo.RecordObject(target, "Remove Effect");
                var list = new List<EventEffect>(result.effects);
                list.RemoveAt(i);
                result.effects = list.ToArray();
                EditorUtility.SetDirty(target);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            // 효과 필드 편집 (SerializedProperty 경유)
            SerializedProperty elemProp = effectsProp.GetArrayElementAtIndex(i);
            if (elemProp != null)
            {
                EditorGUI.indentLevel++;
                SerializedProperty child = elemProp.Copy();
                SerializedProperty end   = elemProp.GetEndProperty();
                bool entered = child.NextVisible(true);
                while (entered && !SerializedProperty.EqualContents(child, end))
                {
                    EditorGUILayout.PropertyField(child, true);
                    entered = child.NextVisible(false);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        // ── 효과 추가 드롭다운 ────────────────────────────
        EditorGUILayout.Space(4);

        var labels = new string[EFFECT_TYPES.Length + 1];
        labels[0] = "+ 효과 추가...";
        for (int i = 0; i < EFFECT_TYPES.Length; i++)
            labels[i + 1] = EFFECT_TYPES[i].label;

        int selected = EditorGUILayout.Popup(0, labels);
        if (selected > 0)
        {
            Undo.RecordObject(target, "Add Effect");
            Type effectType = EFFECT_TYPES[selected - 1].type;
            EventEffect newEffect = (EventEffect)Activator.CreateInstance(effectType);
            var list = new List<EventEffect>(result.effects ?? new EventEffect[0]);
            list.Add(newEffect);
            result.effects = list.ToArray();
            EditorUtility.SetDirty(target);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
