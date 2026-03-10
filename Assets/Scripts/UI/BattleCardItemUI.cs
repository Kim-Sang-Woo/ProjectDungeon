using UnityEngine;
using UnityEngine.UI;

public class BattleCardItemUI : MonoBehaviour
{
    public Button button;
    public Image artworkImage;
    public Text titleText;
    public Text costText;
    public Text descText;

    private void EnsureReferences()
    {
        if (button == null) button = GetComponent<Button>();
        if (artworkImage == null)
        {
            Transform t = transform.Find("Artwork") ?? transform.Find("ArtworkImage");
            if (t != null) artworkImage = t.GetComponent<Image>();
        }
        if (titleText == null)
        {
            Transform t = transform.Find("Title") ?? transform.Find("TitleText");
            if (t != null) titleText = t.GetComponent<Text>();
        }
        if (costText == null)
        {
            Transform t = transform.Find("Cost") ?? transform.Find("CostText");
            if (t != null) costText = t.GetComponent<Text>();
        }
        if (descText == null)
        {
            Transform t = transform.Find("Desc") ?? transform.Find("DescText");
            if (t != null) descText = t.GetComponent<Text>();
        }
    }

    public bool ValidateReferences(bool logWarning = true)
    {
        EnsureReferences();
        bool ok = true;
        if (button == null) { ok = false; if (logWarning) Debug.LogWarning("[BattleCardItemUI] button 참조 누락"); }
        if (titleText == null) { ok = false; if (logWarning) Debug.LogWarning("[BattleCardItemUI] titleText 참조 누락"); }
        if (costText == null) { ok = false; if (logWarning) Debug.LogWarning("[BattleCardItemUI] costText 참조 누락"); }
        if (descText == null) { ok = false; if (logWarning) Debug.LogWarning("[BattleCardItemUI] descText 참조 누락"); }
        // artworkImage는 선택
        return ok;
    }

    public void Bind(string title, int cost, string desc, Sprite artwork, System.Action onClick)
    {
        EnsureReferences();
        if (titleText != null) titleText.text = title;
        if (costText != null) costText.text = cost.ToString();
        if (descText != null) descText.text = desc;

        if (artworkImage != null)
        {
            artworkImage.sprite = artwork;
            artworkImage.enabled = artwork != null;
            artworkImage.preserveAspect = true;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
        }
    }
}
