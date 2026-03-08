using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EventChoice))]
public class EventChoiceEditor : Editor
{
    private static readonly (string label, Type type)[] EFFECT_TYPES =
    {
        ("아이템 획득 (GainItemEffect)",       typeof(GainItemEffect)),
        ("아이템 제거 (RemoveItemEffect)",      typeof(RemoveItemEffect)),
        ("골드 획득 (GainGoldEffect)",          typeof(GainGoldEffect)),
        ("골드 소모 (LoseGoldEffect)",          typeof(LoseGoldEffect)),
        ("상태이상 부여 (ApplyStatusEffect)",   typeof(ApplyStatusEffect)),
        ("오브젝트 소진 (ConsumeObjectEffect)", typeof(ConsumeObjectEffect)),
        ("보상 UI 열기 (OpenRewardEffect)",     typeof(OpenRewardEffect)),
        ("계단 이동 (StairsMoveEffect)",        typeof(StairsMoveEffect)),
        ("전투 시작 (StartBattleEffect)",       typeof(StartBattleEffect)),
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EventChoice choice = (EventChoice)target;

        DrawPropertiesExcluding(serializedObject, "directEffects");

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("직접 효과 (Direct Effects)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("onSuccess가 비어 있을 때 directEffects를 즉시 실행하고 팝업을 닫습니다.", MessageType.Info);

        SerializedProperty effectsProp = serializedObject.FindProperty("directEffects");

        if (choice.directEffects == null)
            choice.directEffects = new EventEffect[0];

        for (int i = 0; i < choice.directEffects.Length; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EventEffect effect = choice.directEffects[i];
            string typeName = effect != null ? effect.GetType().Name : "(null)";
            EditorGUILayout.LabelField($"[{i}] {typeName}", EditorStyles.boldLabel);

            GUI.enabled = i > 0;
            if (GUILayout.Button("▲", GUILayout.Width(24)))
            {
                Undo.RecordObject(target, "Move Direct Effect Up");
                (choice.directEffects[i - 1], choice.directEffects[i]) = (choice.directEffects[i], choice.directEffects[i - 1]);
                EditorUtility.SetDirty(target);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            GUI.enabled = i < choice.directEffects.Length - 1;
            if (GUILayout.Button("▼", GUILayout.Width(24)))
            {
                Undo.RecordObject(target, "Move Direct Effect Down");
                (choice.directEffects[i + 1], choice.directEffects[i]) = (choice.directEffects[i], choice.directEffects[i + 1]);
                EditorUtility.SetDirty(target);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            GUI.enabled = true;
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                Undo.RecordObject(target, "Remove Direct Effect");
                var list = new List<EventEffect>(choice.directEffects);
                list.RemoveAt(i);
                choice.directEffects = list.ToArray();
                EditorUtility.SetDirty(target);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            SerializedProperty elemProp = effectsProp.GetArrayElementAtIndex(i);
            if (elemProp != null)
            {
                EditorGUI.indentLevel++;
                SerializedProperty child = elemProp.Copy();
                SerializedProperty end = elemProp.GetEndProperty();
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

        var labels = new string[EFFECT_TYPES.Length + 1];
        labels[0] = "+ 직접 효과 추가...";
        for (int i = 0; i < EFFECT_TYPES.Length; i++)
            labels[i + 1] = EFFECT_TYPES[i].label;

        int selected = EditorGUILayout.Popup(0, labels);
        if (selected > 0)
        {
            Undo.RecordObject(target, "Add Direct Effect");
            Type effectType = EFFECT_TYPES[selected - 1].type;
            EventEffect newEffect = (EventEffect)Activator.CreateInstance(effectType);
            var list = new List<EventEffect>(choice.directEffects ?? new EventEffect[0]);
            list.Add(newEffect);
            choice.directEffects = list.ToArray();
            EditorUtility.SetDirty(target);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
