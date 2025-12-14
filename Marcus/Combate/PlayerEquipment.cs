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
    public WeaponItemData EquippedWeaponData { get; private set; }
    public WeaponFeedback CurrentWeaponFeedback { get; private set; }

    private GameObject _currentWeaponModel;

    // --- ESTADO INTERNO ---
    private Animator _animator;
    private PlayerInput _playerInput;
    private InputAction _equipAction;

    private bool _isEquipped = false;
    public bool IsEquipped => _isEquipped;

    private int _equipTriggerHash;
    private int _isArmedBoolHash;

    // NOVO: Controle de Input
    private bool _inputEnabled = true;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _playerInput = GetComponent<PlayerInput>();

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

    // --- MÉTODOS PÚBLICOS ---

    public void EquipWeapon(WeaponItemData newWeaponData)
    {
        if (newWeaponData == null) return;
        if (newWeaponData == EquippedWeaponData) return;

        if (_currentWeaponModel != null) Destroy(_currentWeaponModel);

        EquippedWeaponData = newWeaponData;

        Transform targetSocket = _isEquipped ? handSocket : waistSocket;

        if (newWeaponData.weaponPrefab != null && targetSocket != null)
        {
            _currentWeaponModel = Instantiate(newWeaponData.weaponPrefab, targetSocket);
            _currentWeaponModel.transform.localPosition = Vector3.zero;
            _currentWeaponModel.transform.localRotation = Quaternion.identity;
            CurrentWeaponFeedback = _currentWeaponModel.GetComponent<WeaponFeedback>();
        }

        if (_playerCombat != null)
        {
            _playerCombat.UpdateCombatData(newWeaponData);
        }

        UnityEngine.Debug.Log($"[EQUIPAMENTO] Nova arma equipada: {newWeaponData.itemName}");
    }

    // NOVO: Chamado pelo PlayerCombat para travar/destravar o botão de sacar
    public void SetInputEnabled(bool isEnabled)
    {
        _inputEnabled = isEnabled;
    }

    // --- INPUT E ANIMAÇÃO ---

    private void OnEquipInput(InputAction.CallbackContext context)
    {
        // NOVO: Se o input estiver travado (ex: diálogo), ignora
        if (!_inputEnabled) return;

        if (EquippedWeaponData == null) return;
        if (_animator.IsInTransition(0)) return;

        _isEquipped = !_isEquipped;
        _animator.SetBool(_isArmedBoolHash, _isEquipped);
        _animator.SetTrigger(_equipTriggerHash);
    }

    public void GrabWeapon()
    {
        if (_currentWeaponModel != null && handSocket != null)
            AttachToSocket(handSocket);
    }

    public void SheathWeapon()
    {
        if (_currentWeaponModel != null && waistSocket != null)
            AttachToSocket(waistSocket);
    }

    private void AttachToSocket(Transform targetSocket)
    {
        if (_currentWeaponModel == null) return;
        _currentWeaponModel.transform.SetParent(targetSocket);
        _currentWeaponModel.transform.localPosition = Vector3.zero;
        _currentWeaponModel.transform.localRotation = Quaternion.identity;
    }
}