using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    // Changed from private to public so the spawner can access it
    public float damageReduction;

    public int maxHp;
    public int currentHp;

    public void Initialize(int hp, float armor)
    {
        maxHp = hp;
        currentHp = maxHp;
        damageReduction = armor;
    }

    public void TakeDamage(int amount)
    {
        int finalDamage = Mathf.RoundToInt(amount * (1f - damageReduction));
        currentHp -= Mathf.Max(1, finalDamage);

        if (currentHp <= 0)
        {
            Destroy(gameObject);
        }
    }
}