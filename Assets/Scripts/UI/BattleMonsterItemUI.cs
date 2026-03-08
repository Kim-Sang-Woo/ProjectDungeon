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
        if (nameText == null) { ok = false; if (logWarning) Debug.LogWarning("[BattleMonsterItemUI] nameText 참조 누락"); }
        if (hpText == null) { ok = false; if (logWarning) Debug.LogWarning("[BattleMonsterItemUI] hpText 참조 누락"); }
        // intentText는 선택(없으면 런타임 fallback 생성)
        return ok;
    }

    public void Bind(RuntimeMonster monster, int intentDamage, System.Action onClick)
    {
        if (monster == null || monster.data == null) return;

        if (portraitImage != null)
        {
            portraitImage.sprite = monster.data.image;
            portraitImage.enabled = monster.data.image != null;
        }

        if (nameText != null)
            nameText.text = monster.data.monsterName;

        int maxHp = Mathf.Max(1, monster.data.maxHP);
        int curHp = Mathf.Max(0, monster.currentHP);

        if (hpText != null)
            hpText.text = $"HP {curHp}/{maxHp}";

        if (hpFillImage != null)
            hpFillImage.fillAmount = Mathf.Clamp01((float)curHp / maxHp);

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
