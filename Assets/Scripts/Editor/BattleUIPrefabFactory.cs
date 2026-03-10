#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BattleUIPrefabFactory
{
    private const string PrefabDir = "Assets/Prefabs/Battle";

    [MenuItem("Tools/Battle/Create Card Prefab (Name/Desc/Image/Cost)")]
    public static void CreateCardPrefab()
    {
        EnsureDir(PrefabDir);

        GameObject root = new GameObject("BattleCardItem", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(BattleCardItemUI));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(140f, 180f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.16f, 0.13f, 0.09f, 1f);

        LayoutElement le = root.GetComponent<LayoutElement>();
        le.preferredWidth = 140f;
        le.preferredHeight = 180f;

        BattleCardItemUI ui = root.GetComponent<BattleCardItemUI>();
        ui.button = root.GetComponent<Button>();

        GameObject topBg = new GameObject("TopBarBg", typeof(RectTransform), typeof(Image));
        topBg.transform.SetParent(root.transform, false);
        RectTransform topBgRt = topBg.GetComponent<RectTransform>();
        topBgRt.anchorMin = new Vector2(0f, 1f);
        topBgRt.anchorMax = new Vector2(1f, 1f);
        topBgRt.offsetMin = new Vector2(4f, -32f);
        topBgRt.offsetMax = new Vector2(-4f, -4f);
        topBg.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 1f);

        GameObject costBg = new GameObject("CostBg", typeof(RectTransform), typeof(Image));
        costBg.transform.SetParent(root.transform, false);
        RectTransform costBgRt = costBg.GetComponent<RectTransform>();
        costBgRt.anchorMin = new Vector2(0f, 1f);
        costBgRt.anchorMax = new Vector2(0f, 1f);
        costBgRt.offsetMin = new Vector2(8f, -30f);
        costBgRt.offsetMax = new Vector2(30f, -8f);
        costBg.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.95f, 1f);

        ui.costText = CreateText(root.transform, "CostText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -30f), new Vector2(30f, -8f), TextAnchor.MiddleCenter, 12);
        ui.costText.color = Color.white;
        ui.costText.fontStyle = FontStyle.Bold;

        ui.titleText = CreateText(root.transform, "TitleText", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(62f, -30f), new Vector2(-8f, -8f), TextAnchor.MiddleCenter, 12);
        ui.titleText.fontStyle = FontStyle.Bold;

        GameObject artGo = new GameObject("ArtworkImage", typeof(RectTransform), typeof(Image));
        artGo.transform.SetParent(root.transform, false);
        RectTransform artRt = artGo.GetComponent<RectTransform>();
        artRt.anchorMin = new Vector2(0f, 0.42f);
        artRt.anchorMax = new Vector2(1f, 0.82f);
        artRt.offsetMin = new Vector2(8f, 0f);
        artRt.offsetMax = new Vector2(-8f, 0f);
        Image artImg = artGo.GetComponent<Image>();
        artImg.preserveAspect = true;
        artImg.enabled = false;
        ui.artworkImage = artImg;

        GameObject descBg = new GameObject("DescBg", typeof(RectTransform), typeof(Image));
        descBg.transform.SetParent(root.transform, false);
        RectTransform descBgRt = descBg.GetComponent<RectTransform>();
        descBgRt.anchorMin = new Vector2(0f, 0f);
        descBgRt.anchorMax = new Vector2(1f, 0.40f);
        descBgRt.offsetMin = new Vector2(4f, 4f);
        descBgRt.offsetMax = new Vector2(-4f, -4f);
        descBg.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 1f);

        ui.descText = CreateText(root.transform, "DescText", new Vector2(0f, 0f), new Vector2(1f, 0.40f), new Vector2(8f, 8f), new Vector2(-8f, -6f), TextAnchor.UpperLeft, 11);

        string path = AssetDatabase.GenerateUniqueAssetPath($"{PrefabDir}/BattleCardItem.prefab");
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleUIPrefabFactory] 카드 프리팹 생성 완료: {path}");
    }

    [MenuItem("Tools/Battle/Create Monster Prefab (Image Only)")]
    public static void CreateMonsterPrefab()
    {
        EnsureDir(PrefabDir);

        GameObject root = new GameObject("BattleMonsterItem", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(BattleMonsterItemUI));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(96f, 120f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.15f, 0.13f, 0.09f, 0.95f);

        LayoutElement le = root.GetComponent<LayoutElement>();
        le.preferredWidth = 96f;
        le.preferredHeight = 120f;

        BattleMonsterItemUI ui = root.GetComponent<BattleMonsterItemUI>();
        ui.button = root.GetComponent<Button>();

        GameObject portraitGo = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
        portraitGo.transform.SetParent(root.transform, false);
        RectTransform prt = portraitGo.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 0f);
        prt.anchorMax = new Vector2(1f, 1f);
        prt.offsetMin = new Vector2(6f, 6f);
        prt.offsetMax = new Vector2(-6f, -6f);
        Image portrait = portraitGo.GetComponent<Image>();
        portrait.preserveAspect = true;
        ui.portraitImage = portrait;

        GameObject intentIconGo = new GameObject("IntentIcon", typeof(RectTransform), typeof(Image));
        intentIconGo.transform.SetParent(root.transform, false);
        RectTransform iRt = intentIconGo.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0f, 1f);
        iRt.anchorMax = new Vector2(0f, 1f);
        iRt.pivot = new Vector2(0f, 1f);
        iRt.sizeDelta = new Vector2(18f, 18f);
        iRt.anchoredPosition = new Vector2(6f, -6f);
        Image intentIcon = intentIconGo.GetComponent<Image>();
        intentIcon.preserveAspect = true;
        ui.intentIcon = intentIcon;

        ui.intentText = CreateText(root.transform, "IntentText", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -28f), new Vector2(-6f, -6f), TextAnchor.UpperLeft, 16);
        ui.intentText.fontStyle = FontStyle.Bold;

        string path = AssetDatabase.GenerateUniqueAssetPath($"{PrefabDir}/BattleMonsterItem.prefab");
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleUIPrefabFactory] 몬스터 프리팹 생성 완료: {path}");
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAnchor align, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = align;
        t.fontSize = fontSize;
        t.color = new Color(0.93f, 0.86f, 0.65f, 1f);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string acc = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{acc}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(acc, parts[i]);
            acc = next;
        }
    }
}
#endif
