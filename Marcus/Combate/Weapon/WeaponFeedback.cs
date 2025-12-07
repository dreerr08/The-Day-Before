using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WeaponFeedback : MonoBehaviour
{
    [Header("Efeitos Visuais")]
    [Tooltip("Arraste o componente TrailRenderer da espada aqui.")]
    public TrailRenderer weaponTrail;

    private AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        // Garante que o rastro comece desligado para não ficar feio
        if (weaponTrail != null)
            weaponTrail.emitting = false;
    }

    // O erro CS1061 acontece porque o Unity não estava achando ESTE método:
    public void SetTrailActive(bool isActive)
    {
        if (weaponTrail != null)
        {
            weaponTrail.emitting = isActive;
        }
    }

    // ... e nem ESTE aqui:
    public void PlaySlashSound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            // PlayOneShot é ótimo porque toca o som sem cortar o anterior
            _audioSource.PlayOneShot(clip);
        }
    }
}