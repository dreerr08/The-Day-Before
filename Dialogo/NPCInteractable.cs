using UnityEngine;
using UnityEngine.InputSystem;

public class NPCInteractable : MonoBehaviour
{
    [Header("Configuração")]
    public DialogueDataSO npcData;
    public string interactActionName = "Interact";

    [Header("Visual")]
    public GameObject interactPrompt;

    private bool _isPlayerNearby = false;
    private PlayerLocomotion _playerLocomotion;
    private PlayerTargetLock _playerLock;
    private PlayerCombat _playerCombat;

    // Armazenamos o PlayerInput para passar ao Manager
    private PlayerInput _playerInput;
    private InputAction _interactAction;

    void Start()
    {
        if (interactPrompt) interactPrompt.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerNearby = true;
            _playerLocomotion = other.GetComponent<PlayerLocomotion>();
            _playerLock = other.GetComponent<PlayerTargetLock>();
            _playerCombat = other.GetComponent<PlayerCombat>();

            // PEGA O INPUT COMPONENT AQUI
            _playerInput = other.GetComponent<PlayerInput>();

            if (_playerInput != null)
            {
                _interactAction = _playerInput.actions.FindAction(interactActionName);
            }

            if (interactPrompt) interactPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerNearby = false;
            _playerLocomotion = null;
            _playerLock = null;
            _playerCombat = null;
            _playerInput = null; // Limpa referência
            _interactAction = null;

            if (interactPrompt) interactPrompt.SetActive(false);
        }
    }

    void Update()
    {
        if (_isPlayerNearby && _interactAction != null && npcData != null)
        {
            if (_interactAction.WasPressedThisFrame())
            {
                // ATUALIZADO: Passa _playerInput como último argumento
                DialogueManager.Instance.StartDialogue(
                    npcData,
                    _playerLocomotion,
                    _playerLock,
                    _playerCombat,
                    this.transform,
                    _playerInput
                );
            }
        }
    }
}