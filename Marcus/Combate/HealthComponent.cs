using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour, IDamageable
{
    [Header("Configurações")]
    public int maxHealth = 100;

    [Header("Debug (Apenas Leitura)")]
    [SerializeField] private int _currentHealth;

    [Header("Eventos")]
    public UnityEvent OnTakeDamage; // Dispara quando leva hit
    public UnityEvent OnDeath;      // Dispara quando a vida zera

    private bool _isDead = false;

    void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        if (_isDead) return;

        _currentHealth -= damageAmount;

        // CORREÇÃO: UnityEngine.Debug para evitar conflito
        UnityEngine.Debug.Log($"{gameObject.name} tomou {damageAmount} de dano. Vida restante: {_currentHealth}");

        OnTakeDamage?.Invoke();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;

        // CORREÇÃO: UnityEngine.Debug aqui também
        UnityEngine.Debug.Log($"{gameObject.name} Morreu!");

        OnDeath?.Invoke();
    }

    public float GetHealthPercentage()
    {
        return (float)_currentHealth / maxHealth;
    }
}