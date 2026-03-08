using UnityEngine;

public static class BattleMath
{
    public static int CalcFinalDamage(float damageConst, float damagePer)
    {
        float raw = damageConst * (1f + damagePer * 0.01f);
        return Mathf.Max(0, Mathf.FloorToInt(raw));
    }
}
