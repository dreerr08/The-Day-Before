using UnityEngine;
using UnityEngine.UI; // Importante para manipular Texto e Botões
using TMPro; // Use este se estiver usando TextMeshPro (Recomendado!)
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialoguePanel; // O Painel inteiro (Canvas)
    public TextMeshProUGUI nameText; // Nome do NPC
    public TextMeshProUGUI bodyText; // Texto principal (Fala do NPC)
    public Transform optionsContainer; // Onde os botões serão criados
    public GameObject optionButtonPrefab; // O prefab do botão
    public Button continueButton; // Botão de "Avançar" ou "Voltar" na resposta

    // Referências ao Player (Cache)
    private PlayerLocomotion _playerLocomotion;
    private PlayerTargetLock _playerTargetLock;

    // Estado Atual
    private DialogueDataSO _currentData;

    void Awake()
    {
        // Singleton simples
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Começa desligado
        dialoguePanel.SetActive(false);
    }

    public void StartDialogue(DialogueDataSO data, PlayerLocomotion locomotion, PlayerTargetLock targetLock)
    {
        _currentData = data;
        _playerLocomotion = locomotion;
        _playerTargetLock = targetLock;

        // 1. Congela o Player
        if (_playerLocomotion) _playerLocomotion.ToggleMovement(false);
        if (_playerTargetLock) _playerTargetLock.ToggleLockSystem(false);

        // 2. Abre a UI
        dialoguePanel.SetActive(true);
        nameText.text = data.npcName;

        // 3. Mostra o Hub Inicial
        ShowHub();
    }

    private void ShowHub()
    {
        // Texto de saudação
        bodyText.text = _currentData.greetingText;

        // Esconde botão de continuar (pois estamos escolhendo opção)
        if (continueButton) continueButton.gameObject.SetActive(false);

        // Limpa botões antigos
        foreach (Transform child in optionsContainer) Destroy(child.gameObject);

        // Cria novos botões
        foreach (var option in _currentData.options)
        {
            GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
            // Pega o componente de Texto dentro do botão (ajuste conforme seu prefab)
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText) btnText.text = option.buttonText;

            // Configura o clique
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnOptionSelected(option));
        }
    }

    private void OnOptionSelected(DialogueOption option)
    {
        // Mostra a resposta
        bodyText.text = option.responseText;

        // Limpa os botões de opção para focar no texto
        foreach (Transform child in optionsContainer) Destroy(child.gameObject);

        // Ativa o botão de "Continuar/Voltar"
        if (continueButton)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.onClick.RemoveAllListeners(); // Limpa cliques anteriores

            if (option.endsDialogue)
            {
                // Se for opção de saída, fecha tudo
                continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Sair";
                continueButton.onClick.AddListener(EndDialogue);
            }
            else
            {
                // Se for info, volta para o Hub
                continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Voltar";
                continueButton.onClick.AddListener(ShowHub);
            }
        }
    }

    public void EndDialogue()
    {
        dialoguePanel.SetActive(false);

        // Devolve o controle ao Player
        if (_playerLocomotion) _playerLocomotion.ToggleMovement(true);
        if (_playerTargetLock) _playerTargetLock.ToggleLockSystem(true);
    }
}