using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleCardData))]
public class BattleCardDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        BattleCardData data = (BattleCardData)target;

        DrawIdentitySection();
        DrawVisualSection();
        DrawCostSection();
        DrawEffectsSection(data);
        DrawPreviewSection(data);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentitySection()
    {
        EditorGUILayout.LabelField("식별", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardId"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardName"));
        EditorGUILayout.Space(4);
    }

    private void DrawVisualSection()
    {
        EditorGUILayout.LabelField("표시", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("artwork"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
        EditorGUILayout.Space(4);
    }

    private void DrawCostSection()
    {
        EditorGUILayout.LabelField("코스트", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("costMana"));
        EditorGUILayout.Space(4);
    }

    private void DrawEffectsSection(BattleCardData data)
    {
        EditorGUILayout.LabelField("효과", EditorStyles.boldLabel);

        SerializedProperty useLimit = serializedObject.FindProperty("useEffectLimit");
        SerializedProperty maxEffects = serializedObject.FindProperty("maxEffects");
        SerializedProperty effects = serializedObject.FindProperty("effects");

        EditorGUILayout.PropertyField(useLimit, new GUIContent("효과 개수 제한 사용"));
        if (useLimit.boolValue)
            EditorGUILayout.PropertyField(maxEffects, new GUIContent("최대 효과 수"));

        EditorGUILayout.Space(4);

        int limit = useLimit.boolValue ? Mathf.Max(1, maxEffects.intValue) : int.MaxValue;

        for (int i = 0; i < effects.arraySize; i++)
        {
            SerializedProperty e = effects.GetArrayElementAtIndex(i);
            SerializedProperty effectType = e.FindPropertyRelative("effectType");
            SerializedProperty targetType = e.FindPropertyRelative("targetType");
            SerializedProperty attackMultiplier = e.FindPropertyRelative("attackMultiplier");
            SerializedProperty amount = e.FindPropertyRelative("amount");
            SerializedProperty hitEffectSprite = e.FindPropertyRelative("hitEffectSprite");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Effect #{i + 1}", EditorStyles.boldLabel);

            GUI.enabled = i > 0;
            if (GUILayout.Button("▲", GUILayout.Width(28)))
                effects.MoveArrayElement(i, i - 1);

            GUI.enabled = i < effects.arraySize - 1;
            if (GUILayout.Button("▼", GUILayout.Width(28)))
                effects.MoveArrayElement(i, i + 1);

            GUI.enabled = true;
            if (GUILayout.Button("✕", GUILayout.Width(28)))
            {
                effects.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(effectType, new GUIContent("Effect Type"));
            EditorGUILayout.PropertyField(targetType, new GUIContent("Target Type"));

            BattleCardEffectType et = (BattleCardEffectType)effectType.enumValueIndex;
            if (et == BattleCardEffectType.Attack)
            {
                EditorGUILayout.PropertyField(attackMultiplier, new GUIContent("Attack Multiplier"));
                EditorGUILayout.PropertyField(hitEffectSprite, new GUIContent("Hit Effect Sprite"));
            }

            EditorGUILayout.PropertyField(amount, new GUIContent("Amount"));
            EditorGUILayout.EndVertical();
        }

        GUI.enabled = effects.arraySize < limit;
        if (GUILayout.Button("+ Effect 추가"))
        {
            effects.InsertArrayElementAtIndex(effects.arraySize);
            SerializedProperty ne = effects.GetArrayElementAtIndex(effects.arraySize - 1);
            ne.FindPropertyRelative("effectType").enumValueIndex = (int)BattleCardEffectType.Attack;
            ne.FindPropertyRelative("targetType").enumValueIndex = (int)BattleCardTargetType.EnemySingle;
            ne.FindPropertyRelative("attackMultiplier").floatValue = 1f;
            ne.FindPropertyRelative("amount").floatValue = 0f;
            ne.FindPropertyRelative("hitEffectSprite").objectReferenceValue = null;
        }
        GUI.enabled = true;

        if (effects.arraySize >= limit && useLimit.boolValue)
            EditorGUILayout.HelpBox($"최대 효과 수({limit})에 도달했습니다.", MessageType.Info);

        EditorGUILayout.Space(8);

        if (effects.arraySize == 0)
        {
            EditorGUILayout.HelpBox("effects가 비어 있습니다. 레거시 단일 효과 필드가 자동 사용됩니다.", MessageType.Warning);
            DrawLegacySection();
        }
    }

    private void DrawLegacySection()
    {
        EditorGUILayout.LabelField("Legacy 단일 효과", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("effectType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("targetType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackMultiplier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("amount"));
    }

    private void DrawPreviewSection(BattleCardData data)
    {
        EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
        var effects = data.GetEffects();
        if (effects == null || effects.Count == 0)
        {
            EditorGUILayout.HelpBox("효과 없음", MessageType.None);
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < effects.Count; i++)
        {
            var e = effects[i];
            if (e == null) continue;
            sb.Append(i + 1).Append(") ").Append(e.effectType).Append(" / ").Append(e.targetType)
              .Append(" / mul:").Append(e.attackMultiplier.ToString("0.##"))
              .Append(" / amt:").Append(e.amount.ToString("0.##"));
            if (i < effects.Count - 1) sb.AppendLine();
        }

        EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);
    }
}
