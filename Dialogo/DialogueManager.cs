using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // <--- NECESSÁRIO PARA O CONTROLE FUNCIONAR

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI bodyText;
    public Transform optionsContainer;
    public GameObject optionButtonPrefab;

    public Button closeButton;

    [Header("Camera System")]
    public CinemachineVirtualCamera dialogueCamera;
    public float baseCameraDistance = 2.5f;
    public float baseCameraHeight = 1.6f;

    [Header("Settings")]
    public float typeSpeed = 0.04f;

    // --- ESTADOS DO DIÁLOGO ---
    private enum DialogueState
    {
        Typing,
        WaitingForNext,
        ChoosingOption
    }
    private DialogueState _currentState;

    // --- REFERÊNCIAS ---
    private PlayerLocomotion _playerLocomotion;
    private PlayerTargetLock _playerTargetLock;
    private PlayerCombat _playerCombat;
    private PlayerInput _playerInput;

    // --- INPUT ACTIONS ---
    private InputAction _nextAction;
    private InputAction _quitAction;

    // --- DADOS INTERNOS ---
    private DialogueDataSO _currentData;
    private string _fullTextTarget;
    private Coroutine _typingCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        dialoguePanel.SetActive(false);
        if (closeButton) closeButton.onClick.AddListener(EndDialogue);
    }

    public void StartDialogue(DialogueDataSO data, PlayerLocomotion locomotion, PlayerTargetLock targetLock, PlayerCombat combat, Transform npcTransform, PlayerInput input)
    {
        _currentData = data;
        _playerLocomotion = locomotion;
        _playerTargetLock = targetLock;
        _playerCombat = combat;
        _playerInput = input;

        // 1. Configurar Inputs
        if (_playerInput != null)
        {
            _nextAction = _playerInput.actions.FindAction("NextDialogue");
            _quitAction = _playerInput.actions.FindAction("QuitDialogue");

            if (_nextAction == null) UnityEngine.Debug.LogError("Ação 'NextDialogue' não encontrada no Input System!");
            if (_quitAction == null) UnityEngine.Debug.LogError("Ação 'QuitDialogue' não encontrada no Input System!");
        }

        // 2. Congelar Player
        if (_playerLocomotion) _playerLocomotion.ToggleMovement(false);
        if (_playerTargetLock) _playerTargetLock.ToggleLockSystem(false);
        if (_playerCombat) _playerCombat.SetCombatEnabled(false);

        // 3. Posicionar Câmera
        if (dialogueCamera != null && npcTransform != null)
        {
            float scaleFactor = npcTransform.localScale.y;
            Vector3 finalPos = npcTransform.position
                               + (npcTransform.forward * (baseCameraDistance * scaleFactor))
                               + (Vector3.up * (baseCameraHeight * scaleFactor));

            dialogueCamera.Follow = null;
            dialogueCamera.LookAt = null;
            dialogueCamera.transform.position = finalPos;
            Vector3 lookTarget = npcTransform.position + (Vector3.up * (baseCameraHeight * scaleFactor));
            dialogueCamera.transform.LookAt(lookTarget);
            dialogueCamera.Priority = 100;
        }

        // 4. Iniciar UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        dialoguePanel.SetActive(true);

        // 5. Começar com a Saudação
        StartTyping(data.greetingText);
    }

    void Update()
    {
        if (!dialoguePanel.activeSelf) return;

        // --- INPUT: QUIT ---
        if (_quitAction != null && _quitAction.WasPressedThisFrame())
        {
            EndDialogue();
            return;
        }

        // --- INPUT: NEXT ---
        if (_nextAction != null && _nextAction.WasPressedThisFrame())
        {
            HandleNextInput();
        }
    }

    private void HandleNextInput()
    {
        switch (_currentState)
        {
            case DialogueState.Typing:
                CompleteTextImmediately();
                break;

            case DialogueState.WaitingForNext:
                ShowOptions();
                break;

            case DialogueState.ChoosingOption:
                // DEIXE VAZIO: O Input System UI Module vai cuidar do clique 
                // quando apertar o botão de confirmação do controle (A ou X)
                break;
        }
    }

    // --- TYPEWRITER ---

    private void StartTyping(string text)
    {
        foreach (Transform child in optionsContainer) Destroy(child.gameObject);

        _fullTextTarget = text;
        bodyText.text = "";
        _currentState = DialogueState.Typing;

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypewriterRoutine());
    }

    private IEnumerator TypewriterRoutine()
    {
        foreach (char c in _fullTextTarget)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        OnTypingFinished();
    }

    private void CompleteTextImmediately()
    {
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        bodyText.text = _fullTextTarget;
        OnTypingFinished();
    }

    private void OnTypingFinished()
    {
        _currentState = DialogueState.WaitingForNext;
    }

    // --- OPÇÕES (CORRIGIDO PARA CONTROLE) ---

    private void ShowOptions()
    {
        _currentState = DialogueState.ChoosingOption;

        // Limpa botões antigos
        foreach (Transform child in optionsContainer) Destroy(child.gameObject);

        GameObject firstButton = null; // Armazena o primeiro botão criado

        foreach (var option in _currentData.options)
        {
            GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText) btnText.text = option.buttonText;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnOptionSelected(option));

            // Salva referência se for o primeiro
            if (firstButton == null) firstButton = btnObj;
        }

        // CORREÇÃO PARA O CONTROLE XBOX:
        // Força o sistema de eventos a selecionar o primeiro botão
        if (firstButton != null)
        {
            // Limpa a seleção atual para garantir que o "highlight" visual atualize
            EventSystem.current.SetSelectedGameObject(null);
            // Seleciona o novo botão
            EventSystem.current.SetSelectedGameObject(firstButton);
        }
    }

    private void OnOptionSelected(DialogueOption option)
    {
        // Ao selecionar, tiramos o foco dos botões para evitar cliques duplos
        EventSystem.current.SetSelectedGameObject(null);

        if (option.endsDialogue)
        {
            StartCoroutine(CloseAfterReading(option.responseText));
        }
        else
        {
            StartTyping(option.responseText);
        }
    }

    private IEnumerator CloseAfterReading(string text)
    {
        StartTyping(text);
        while (_currentState == DialogueState.Typing) yield return null;
        while (!_nextAction.WasPressedThisFrame()) yield return null;
        EndDialogue();
    }

    public void EndDialogue()
    {
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

        dialoguePanel.SetActive(false);

        if (_playerLocomotion) _playerLocomotion.ToggleMovement(true);
        if (_playerTargetLock) _playerTargetLock.ToggleLockSystem(true);
        if (_playerCombat) _playerCombat.SetCombatEnabled(true);

        if (dialogueCamera != null) dialogueCamera.Priority = 0;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}