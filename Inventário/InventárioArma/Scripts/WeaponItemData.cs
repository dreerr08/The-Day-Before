using UnityEngine;

// Herança: WeaponItemData "É UM" ItemData, mas especializado.
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Inventory/Weapon Data")]
public class WeaponItemData : ItemData
{
    [Header("Visual da Arma")]
    [Tooltip("O Prefab 3D da espada que vai aparecer na mão do personagem")]
    public GameObject weaponPrefab;

    [Header("Dados de Combate")]
    [Tooltip("Link para o ScriptableObject de Combo que já criamos")]
    public WeaponComboSO weaponCombo;

    public int baseDamage = 10;
    public int maxDurability = 100;
}