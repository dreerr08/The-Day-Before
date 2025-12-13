using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // Necessário para usar List<>

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(SphereCollider))]
public class TreasureChest : MonoBehaviour
{
    [Header("Configuração de Loot")]
    [Tooltip("Arraste aqui todos os itens que podem sair deste baú.")]
    public List<ItemData> possibleLoot;

    [Header("Input")]
    public string interactActionName = "Interact";
    public GameObject interactPrompt;

    // Estado Interno
    private bool _isPlayerNearby = false;
    private InputAction _interactAction;
    private InventorySystem _playerInventory;

    void Start()
    {
        if (interactPrompt) interactPrompt.SetActive(false);

        // Garante as configurações físicas
        GetComponent<BoxCollider>().isTrigger = false;
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerNearby = true;
            _playerInventory = other.GetComponent<InventorySystem>();

            var playerInput = other.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                _interactAction = playerInput.actions.FindAction(interactActionName);
            }

            if (interactPrompt) interactPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerNearby = false;
            _playerInventory = null;
            _interactAction = null;

            if (interactPrompt) interactPrompt.SetActive(false);
        }
    }

    void Update()
    {
        if (_isPlayerNearby && _interactAction != null && _interactAction.WasPressedThisFrame())
        {
            OpenChest();
        }
    }

    private void OpenChest()
    {
        if (possibleLoot == null || possibleLoot.Count == 0)
        {
            UnityEngine.Debug.LogWarning($"[BAÚ] O baú {gameObject.name} está vazio!");
            Destroy(gameObject);
            return;
        }

        // --- CORREÇÃO AQUI ---
        // Usamos UnityEngine.Random para evitar confusão com System.Random
        int randomIndex = UnityEngine.Random.Range(0, possibleLoot.Count);
        ItemData selectedItem = possibleLoot[randomIndex];

        if (_playerInventory != null)
        {
            bool success = _playerInventory.AddItem(selectedItem, 1);

            if (success)
            {
                UnityEngine.Debug.Log($"[BAÚ] Você encontrou: {selectedItem.itemName}");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[BAÚ] Mochila cheia! O item foi perdido.");
            }
        }

        if (interactPrompt) interactPrompt.SetActive(false);
        Destroy(gameObject);
    }
}