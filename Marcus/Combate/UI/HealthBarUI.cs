using UnityEngine;
using UnityEngine.UI; // Mantemos o using, mas vamos ser específicos lá embaixo

public class HealthBarUI : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("O script de vida que essa barra representa")]
    public HealthComponent targetHealthComponent;

    [Tooltip("A imagem que vai diminuir (Foreground)")]
    // CORREÇÃO: Usamos 'UnityEngine.UI.Image' para evitar confusão com System.Net
    public UnityEngine.UI.Image healthBarFill;

    [Header("Configuração")]
    public float hideDelay = 2.0f; // Tempo para esconder a barra se não levar dano (opcional)

    // Controle interno
    private CanvasGroup _canvasGroup;
    private float _lastHitTime;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (targetHealthComponent != null)
        {
            targetHealthComponent.OnTakeDamage.AddListener(UpdateHealthBar);
        }

        if (healthBarFill != null) healthBarFill.fillAmount = 1.0f;
    }

    void OnDisable()
    {
        if (targetHealthComponent != null)
        {
            targetHealthComponent.OnTakeDamage.RemoveListener(UpdateHealthBar);
        }
    }

    void UpdateHealthBar()
    {
        if (targetHealthComponent == null || healthBarFill == null) return;

        // Chama o método que criamos no HealthComponent
        healthBarFill.fillAmount = targetHealthComponent.GetHealthPercentage();
    }
}