using UnityEngine;

// Enum para sabermos o comportamento do item
public enum ItemType
{
    Consumable, // Poção, Comida (Cura)
    Material,   // Pó de Reparo (Conserta arma)
    Key,        // Chave (Abre portas)
    Equipment   // Espada, Escudo (Não empilha)
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identidade")]
    public string itemID;      // Ex: "hp_potion_small"
    public string itemName;    // Ex: "Pão Mofado"
    [TextArea] public string description;
    public Sprite icon;        // Imagem para a UI

    [Header("Comportamento")]
    public ItemType type;
    public int maxStack = 20;  // Quantos cabem num slot? (Equipamento = 1)

    [Header("Valores")]
    [Tooltip("Se for cura, recupera HP. Se for reparo, recupera Durabilidade.")]
    public int effectValue = 20;
}