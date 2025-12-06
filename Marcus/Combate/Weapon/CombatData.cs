using UnityEngine;
using System.Collections.Generic;

// [CombatData.cs]
// Mantivemos o AttackMove igual, mexemos apenas no SO principal.

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
}

[CreateAssetMenu(fileName = "NewWeaponCombo", menuName = "Combat/Weapon Combo")]
public class WeaponComboSO : ScriptableObject
{
    [Header("Configurações do Combo")]
    public List<AttackMove> attacks;

    [Header("Punição por Erro (Spam)")]
    [Tooltip("Tempo em segundos que o jogador fica travado se clicar cedo demais.")]
    public float penaltyDuration = 1.0f; // Novo campo!
}