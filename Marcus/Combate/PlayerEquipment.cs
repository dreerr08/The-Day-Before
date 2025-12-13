using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerEquipment : MonoBehaviour
{
    [Header("Sockets (Pontos de Encaixe)")]
    public Transform handSocket;
    public Transform waistSocket;

    [Header("Animação")]
    public string equipTriggerName = "Equip";
    public string isArmedBoolName = "IsArmed";

    // --- REFERÊNCIAS ---
    [Header("Integração")]
    [Tooltip("Necessário para atualizar o dano quando trocar de arma.")]
    [SerializeField] private PlayerCombat _playerCombat;

    // --- ESTADO PÚBLICO ---
    // Qual arma (dados) estou usando agora? (Pode ser null se estiver sem nada)
    public WeaponItemData EquippedWeaponData { get; private set; }

    // Acesso ao feedback visual (Rastro/Som) da arma atual
    public WeaponFeedback CurrentWeaponFeedback { get; private set; }

    // O objeto 3D real da espada na cena (privado, nós gerenciamos ele aqui)
    private GameObject _currentWeaponModel;

    // --- ESTADO INTERNO ---
    private Animator _animator;
    private PlayerInput _playerInput;
    private InputAction _equipAction;

    // Estado lógico: True = Arma na mão (Combate), False = Arma na cintura (Pacífico)
    private bool _isEquipped = false;
    public bool IsEquipped => _isEquipped;

    private int _equipTriggerHash;
    private int _isArmedBoolHash;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _playerInput = GetComponent<PlayerInput>();

        // Tenta achar o PlayerCombat automaticamente se não tiver sido arrastado
        if (_playerCombat == null) _playerCombat = GetComponent<PlayerCombat>();

        _equipTriggerHash = Animator.StringToHash(equipTriggerName);
        _isArmedBoolHash = Animator.StringToHash(isArmedBoolName);

        _equipAction = _playerInput.actions.FindAction("Equip");
    }

    void OnEnable()
    {
        if (_equipAction != null) _equipAction.performed += OnEquipInput;
    }

    void OnDisable()
    {
        if (_equipAction != null) _equipAction.performed -= OnEquipInput;
    }

    // --- O NOVO CORAÇÃO DO SISTEMA ---
    // Este método será chamado pela UI do Inventário
    public void EquipWeapon(WeaponItemData newWeaponData)
    {
        if (newWeaponData == null) return;
        if (newWeaponData == EquippedWeaponData) return; // Já está com ela equipada?

        // 1. Limpeza: Destrói a arma antiga se existir
        if (_currentWeaponModel != null)
        {
            Destroy(_currentWeaponModel);
        }

        // 2. Atualiza os Dados
        EquippedWeaponData = newWeaponData;

        // 3. Criação (Instantiate)
        // Se estiver em modo combate (_isEquipped), nasce na mão. Senão, na cintura.
        Transform targetSocket = _isEquipped ? handSocket : waistSocket;

        if (newWeaponData.weaponPrefab != null && targetSocket != null)
        {
            _currentWeaponModel = Instantiate(newWeaponData.weaponPrefab, targetSocket);

            // Zera posição/rotação para encaixar perfeito no socket
            _currentWeaponModel.transform.localPosition = Vector3.zero;
            _currentWeaponModel.transform.localRotation = Quaternion.identity;

            // 4. Reconexão: Pega o Feedback (Rastro/Som) da nova malha
            CurrentWeaponFeedback = _currentWeaponModel.GetComponent<WeaponFeedback>();
        }

        // 5. Integração: Avisa o sistema de Combate para mudar o combo e dano
        if (_playerCombat != null)
        {
            _playerCombat.UpdateCombatData(newWeaponData);
        }

        UnityEngine.Debug.Log($"[EQUIPAMENTO] Nova arma equipada: {newWeaponData.itemName}");
    }

    // --- INPUT E ANIMAÇÃO ---

    private void OnEquipInput(InputAction.CallbackContext context)
    {
        // Só permite sacar/guardar se tiver uma arma equipada (nos dados)
        if (EquippedWeaponData == null) return;

        if (_animator.IsInTransition(0)) return;

        _isEquipped = !_isEquipped;
        _animator.SetBool(_isArmedBoolHash, _isEquipped);
        _animator.SetTrigger(_equipTriggerHash);
    }

    // Chamado pela Animação (Animation Event)
    public void GrabWeapon()
    {
        if (_currentWeaponModel != null && handSocket != null)
        {
            AttachToSocket(handSocket);
        }
    }

    // Chamado pela Animação (Animation Event)
    public void SheathWeapon()
    {
        if (_currentWeaponModel != null && waistSocket != null)
        {
            AttachToSocket(waistSocket);
        }
    }

    private void AttachToSocket(Transform targetSocket)
    {
        if (_currentWeaponModel == null) return;

        _currentWeaponModel.transform.SetParent(targetSocket);
        _currentWeaponModel.transform.localPosition = Vector3.zero;
        _currentWeaponModel.transform.localRotation = Quaternion.identity;
    }
}