using UnityEngine;
using System.Collections;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(MeshRenderer))]
public class EnemyTestDummy : MonoBehaviour
{
    [Header("Feedback Visual")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.2f;

    private MeshRenderer _meshRenderer;
    private Color _originalColor;
    private HealthComponent _healthComponent;

    void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _healthComponent = GetComponent<HealthComponent>();
        _originalColor = _meshRenderer.material.color;
    }

    void OnEnable()
    {
        _healthComponent.OnTakeDamage.AddListener(PlayDamageFeedback);
        _healthComponent.OnDeath.AddListener(KillEnemy);
    }

    void OnDisable()
    {
        _healthComponent.OnTakeDamage.RemoveListener(PlayDamageFeedback);
        _healthComponent.OnDeath.RemoveListener(KillEnemy);
    }

    void PlayDamageFeedback()
    {
        StopAllCoroutines();
        StartCoroutine(FlashColorRoutine());
    }

    void KillEnemy()
    {
        Destroy(gameObject); // Simples destruição para teste
    }

    IEnumerator FlashColorRoutine()
    {
        _meshRenderer.material.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        _meshRenderer.material.color = _originalColor;
    }
}