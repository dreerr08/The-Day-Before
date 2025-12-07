using UnityEngine;
using System.Collections.Generic;

// Classe "gaveta" (Pura C#, não precisa de UnityEngine.Debug)
[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int quantity;

    public InventorySlot(ItemData item, int qty)
    {
        itemData = item;
        quantity = qty;
    }

    public void AddQuantity(int amount) => quantity += amount;
}

// O Gerente da Mochila
public class InventorySystem : MonoBehaviour
{
    [Header("Configurações")]
    public int bagSize = 20;

    [SerializeField] private List<InventorySlot> inventory = new List<InventorySlot>();

    public bool AddItem(ItemData newItem, int amount = 1)
    {
        // 1. Tenta empilhar
        if (newItem.maxStack > 1)
        {
            foreach (InventorySlot slot in inventory)
            {
                if (slot.itemData == newItem && slot.quantity < newItem.maxStack)
                {
                    int spaceLeft = newItem.maxStack - slot.quantity;

                    if (amount <= spaceLeft)
                    {
                        slot.AddQuantity(amount);
                        // CORREÇÃO: UnityEngine.Debug
                        UnityEngine.Debug.Log($"[INVENTÁRIO] Adicionado +{amount} {newItem.itemName} ao slot existente.");
                        return true;
                    }
                    else
                    {
                        slot.AddQuantity(spaceLeft);
                        amount -= spaceLeft;
                    }
                }
            }
        }

        // 2. Cria novo slot
        if (amount > 0)
        {
            if (inventory.Count < bagSize)
            {
                inventory.Add(new InventorySlot(newItem, amount));
                // CORREÇÃO: UnityEngine.Debug
                UnityEngine.Debug.Log($"[INVENTÁRIO] Novo slot criado: {amount}x {newItem.itemName}.");
                return true;
            }
            else
            {
                // CORREÇÃO: UnityEngine.Debug
                UnityEngine.Debug.LogWarning("[INVENTÁRIO] Bolsa Cheia! Item perdido.");
                return false;
            }
        }

        return true;
    }

    public bool HasItem(ItemData itemToCheck)
    {
        foreach (var slot in inventory)
        {
            if (slot.itemData == itemToCheck) return true;
        }
        return false;
    }
}