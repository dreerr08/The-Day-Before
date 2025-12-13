using UnityEngine;
using UnityEngine.InputSystem; // Necessário para ler o teclado/controle

public class NPCInteractable : MonoBehaviour
{
    [Header("Configuração")]
    public DialogueDataSO npcData; // Arraste o ScriptableObject aqui!
    public string interactActionName = "Interact"; // Nome da ação no Input System

    [Header("Visual")]
    public GameObject interactPrompt; // Ícone "Press E" (Opcional)

    private bool _isPlayerNearby = false;
    private PlayerLocomotion _playerLocomotion;
    private PlayerTargetLock _playerLock;
    private InputAction _interactAction;

    void Start()
    {
        if (interactPrompt) interactPrompt.SetActive(false);
    }

    // Configura Input quando entra/sai da cena ou habilita/desabilita
    void OnEnable()
    {
        // Tenta achar o InputMap do Player se possível, ou usa polling no Update.
        // Como o PlayerInput está no Player, faremos a leitura no Update para simplificar
        // e não depender de referência direta complexa agora.
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerNearby = true;
            _playerLocomotion = other.GetComponent<PlayerLocomotion>();
            _playerLock = other.GetComponent<PlayerTargetLock>();

            // Tenta pegar a ação de input do player
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
            _playerLocomotion = null;
            _playerLock = null;
            _interactAction = null;

            if (interactPrompt) interactPrompt.SetActive(false);
        }
    }

    void Update()
    {
        if (_isPlayerNearby && _interactAction != null && npcData != null)
        {
            // Se apertou o botão E (ou botão Sul/Oeste)
            if (_interactAction.WasPressedThisFrame())
            {
                // Chama o Manager
                DialogueManager.Instance.StartDialogue(npcData, _playerLocomotion, _playerLock);
            }
        }
    }
}