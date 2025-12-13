using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int quantity;
    public int currentDurability = -1;

    public InventorySlot(ItemData item, int qty)
    {
        itemData = item;
        quantity = qty;

        if (item is WeaponItemData weaponData)
        {
            currentDurability = weaponData.maxDurability;
        }
    }

    public void AddQuantity(int amount) => quantity += amount;
}

public class InventorySystem : MonoBehaviour
{
    [Header("Configurações de Capacidade")]
    public int adventureBagSize = 20; // Limite para itens gerais
    public int weaponBagSize = 10;    // Limite para armas (geralmente menor)
    // Memórias geralmente não têm limite, pois são história

    [Header("Referências")]
    [SerializeField] private PlayerEquipment playerEquipment;

    [Header("Compartimentos (Listas Separadas)")]
    // Cada lista é independente. Encher uma não trava a outra.
    [SerializeField] private List<InventorySlot> adventureInventory = new List<InventorySlot>();
    [SerializeField] private List<InventorySlot> weaponInventory = new List<InventorySlot>();
    [SerializeField] private List<InventorySlot> memoryInventory = new List<InventorySlot>();

    private void Awake()
    {
        if (playerEquipment == null)
            playerEquipment = GetComponent<PlayerEquipment>();
    }

    private void Start()
    {
        CheckForDefaultWeapon();
    }

    // Procura apenas no bolso de armas
    private void CheckForDefaultWeapon()
    {
        foreach (var slot in weaponInventory)
        {
            if (slot.itemData is WeaponItemData weaponFound)
            {
                if (playerEquipment != null)
                {
                    playerEquipment.EquipWeapon(weaponFound);
                    break;
                }
            }
        }
    }

    // O "Porteiro" inteligente que separa os itens
    public bool AddItem(ItemData newItem, int amount = 1)
    {
        // 1. Roteamento: É uma ARMA?
        if (newItem is WeaponItemData)
        {
            return AddToWeaponInventory(newItem, amount);
        }
        // 2. Roteamento: É uma MEMÓRIA?
        else if (newItem is MemoryItemData)
        {
            return AddToMemoryInventory(newItem);
        }
        // 3. Roteamento: É item de AVENTURA (Padrão)?
        else
        {
            return AddToAdventureInventory(newItem, amount);
        }
    }

    // --- LÓGICA DO BOLSO DE ARMAS ---
    private bool AddToWeaponInventory(ItemData newItem, int amount)
    {
        // Armas geralmente não empilham (cada espada é uma espada única com durabilidade)
        // Mas se quiser empilhar armas iguais, a lógica seria similar ao Adventure.
        // Aqui vou assumir que cada arma ocupa 1 slot (Dark Cloud Style).

        for (int i = 0; i < amount; i++)
        {
            if (weaponInventory.Count < weaponBagSize)
            {
                weaponInventory.Add(new InventorySlot(newItem, 1));
                UnityEngine.Debug.Log($"[INVENTÁRIO] Arma guardada: {newItem.itemName}");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[INVENTÁRIO] Bolso de Armas Cheio!");
                return false; // Paramos assim que encher
            }
        }
        return true;
    }

    // --- LÓGICA DO BOLSO DE MEMÓRIAS ---
    private bool AddToMemoryInventory(ItemData newItem)
    {
        // Verifica unicidade
        foreach (var slot in memoryInventory)
        {
            if (slot.itemData == newItem)
            {
                UnityEngine.Debug.Log($"[INVENTÁRIO] Memória já conhecida: {newItem.itemName}");
                return false;
            }
        }

        // Adiciona (sem limite de tamanho)
        memoryInventory.Add(new InventorySlot(newItem, 1));
        UnityEngine.Debug.Log($"[INVENTÁRIO] Nova memória desbloqueada: {newItem.itemName}");
        return true;
    }

    // --- LÓGICA DO BOLSO DE AVENTURA ---
    private bool AddToAdventureInventory(ItemData newItem, int amount)
    {
        // 1. Tenta empilhar
        if (newItem.maxStack > 1)
        {
            foreach (InventorySlot slot in adventureInventory)
            {
                if (slot.itemData == newItem && slot.quantity < newItem.maxStack)
                {
                    int spaceLeft = newItem.maxStack - slot.quantity;

                    if (amount <= spaceLeft)
                    {
                        slot.AddQuantity(amount);
                        UnityEngine.Debug.Log($"[INVENTÁRIO] +{amount} {newItem.itemName} (Empilhado)");
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

        // 2. Cria novo slot se sobrou item
        if (amount > 0)
        {
            if (adventureInventory.Count < adventureBagSize)
            {
                adventureInventory.Add(new InventorySlot(newItem, amount));
                UnityEngine.Debug.Log($"[INVENTÁRIO] Novo item de aventura: {newItem.itemName}");
                return true;
            }
            else
            {
                UnityEngine.Debug.LogWarning("[INVENTÁRIO] Bolso de Aventura Cheio!");
                return false;
            }
        }

        return true;
    }

    // --- MÉTODOS DE CONSULTA (Para UI futura) ---

    // Agora precisamos especificar qual lista queremos ver
    public List<InventorySlot> GetAdventureList() => adventureInventory;
    public List<InventorySlot> GetWeaponList() => weaponInventory;
    public List<InventorySlot> GetMemoryList() => memoryInventory;

    public bool HasItem(ItemData itemToCheck)
    {
        // Precisamos procurar na lista certa baseada no tipo
        List<InventorySlot> targetList;

        if (itemToCheck is WeaponItemData) targetList = weaponInventory;
        else if (itemToCheck is MemoryItemData) targetList = memoryInventory;
        else targetList = adventureInventory;

        foreach (var slot in targetList)
        {
            if (slot.itemData == itemToCheck) return true;
        }
        return false;
    }
}