using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct AttackMove
{
    [Header("Animação")]
    public string animationTriggerName;
    [Range(0f, 1f)]
    public float comboWindowStart;

    [Header("Impacto")]
    public float damageMultiplier;
    public float movementImpulse;

    [Header("Game Feel (Audio)")]
    public AudioClip swingSound; // <--- NOVO: O som deste golpe específico
}

[CreateAssetMenu(fileName = "NewWeaponCombo", menuName = "Combat/Weapon Combo")]
public class WeaponComboSO : ScriptableObject
{
    [Header("Configurações do Combo")]
    public List<AttackMove> attacks;

    [Header("Punição por Erro (Spam)")]
    public float penaltyDuration = 1.0f;
}