using UnityEngine;
using UnityEngine.UI;

public class BattleMonsterItemUI : MonoBehaviour
{
    public Button button;
    public Image portraitImage;
    public Image hpFillImage;
    public Text nameText;
    public Text hpText;
    public Image intentIcon;
    public Text intentText;

    public bool ValidateReferences(bool logWarning = true)
    {
        bool ok = true;
        if (button == null) { ok = false; if (logWarning) Debug.LogWarning("[BattleMonsterItemUI] button 참조 누락"); }
        if (portraitImage == null) { ok = false; if (logWarning) Debug.LogWarning("[BattleMonsterItemUI] portraitImage 참조 누락"); }
        // name/hp/hpFill은 사용하지 않아도 동작(몬스터 이미지 전용 UI)
        return ok;
    }

    public void Bind(RuntimeMonster monster, int intentDamage, System.Action onClick)
    {
        if (monster == null || monster.data == null) return;

        if (portraitImage != null)
        {
            portraitImage.sprite = monster.data.image;
            portraitImage.enabled = monster.data.image != null;
            portraitImage.preserveAspect = true;
        }

        // 요구사항: 몬스터 HP/이름은 노출하지 않고 이미지 중심으로 표시
        if (nameText != null) nameText.gameObject.SetActive(false);
        if (hpText != null) hpText.gameObject.SetActive(false);
        if (hpFillImage != null) hpFillImage.gameObject.SetActive(false);

        if (intentIcon != null)
        {
            intentIcon.enabled = true;
            intentIcon.color = new Color(0.95f, 0.70f, 0.45f, 1f);
            intentIcon.preserveAspect = true;
        }

        if (intentText != null)
        {
            intentText.fontSize = 16;
            intentText.fontStyle = FontStyle.Bold;
            intentText.color = new Color(0.98f, 0.86f, 0.48f, 1f);
            intentText.text = $"{Mathf.Max(0, intentDamage)}";
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
        }
    }
}
