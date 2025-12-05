using UnityEngine;
using System.Collections.Generic;

// 1. A Estrutura de um único golpe
[System.Serializable]
public struct AttackMove
{
    [Header("Animação")]
    public string animationTriggerName; // Ex: "Attack1"
    [Range(0f, 1f)]
    public float comboWindowStart;      // % da animação onde o próximo golpe é permitido (Ex: 0.6)

    [Header("Impacto")]
    public float damageMultiplier;      // Ex: 1.0, 1.2, 1.5
    public float movementImpulse;       // Força para frente (Ex: 2.0)
}

// 2. O Objeto que guarda a lista (ScriptableObject)
[CreateAssetMenu(fileName = "NewWeaponCombo", menuName = "Combat/Weapon Combo")]
public class WeaponComboSO : ScriptableObject
{
    [Header("Configurações do Combo")]
    public List<AttackMove> attacks; // Lista ordenada de golpes
}