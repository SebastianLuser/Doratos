using UnityEngine;

public class DummyHealth : MonoBehaviour
{
    public float CurrentHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsDead { get; private set; }
    public float HealthNormalized => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
    public int ActorNr { get; private set; }

    public void Init(float maxHealth, int actorNr)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        ActorNr = actorNr;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        CurrentHealth -= damage;
        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            Die();
        }
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        if (MatchManager.Instance != null)
            MatchManager.Instance.NotifyPlayerDied(ActorNr);
    }
}
