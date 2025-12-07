using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerEquipment : MonoBehaviour
{
    [Header("Configurações da Arma")]
    [Tooltip("O objeto visual da espada (arraste a espada aqui).")]
    public GameObject weaponObject;

    [Header("Sockets (Pontos de Encaixe)")]
    public Transform handSocket;
    public Transform waistSocket;

    [Header("Animação")]
    public string equipTriggerName = "Equip";
    public string isArmedBoolName = "IsArmed";

    // --- NOVO: Acesso rápido ao feedback da arma atual ---
    public WeaponFeedback CurrentWeaponFeedback { get; private set; }

    private Animator _animator;
    private PlayerInput _playerInput;
    private InputAction _equipAction;
    private bool _isEquipped = false;
    public bool IsEquipped => _isEquipped;
    private int _equipTriggerHash;
    private int _isArmedBoolHash;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _playerInput = GetComponent<PlayerInput>();

        _equipTriggerHash = Animator.StringToHash(equipTriggerName);
        _isArmedBoolHash = Animator.StringToHash(isArmedBoolName);

        _equipAction = _playerInput.actions.FindAction("Equip");
    }

    private void Start()
    {
        // Inicializa buscando o script de feedback na espada
        if (weaponObject != null)
        {
            CurrentWeaponFeedback = weaponObject.GetComponent<WeaponFeedback>();

            if (waistSocket != null) AttachToSocket(waistSocket);
        }
    }

    void OnEnable()
    {
        if (_equipAction != null) _equipAction.performed += OnEquipInput;
    }

    void OnDisable()
    {
        if (_equipAction != null) _equipAction.performed -= OnEquipInput;
    }

    private void OnEquipInput(InputAction.CallbackContext context)
    {
        if (_animator.IsInTransition(0)) return;
        _isEquipped = !_isEquipped;
        _animator.SetBool(_isArmedBoolHash, _isEquipped);
        _animator.SetTrigger(_equipTriggerHash);
    }

    public void GrabWeapon()
    {
        if (handSocket != null) AttachToSocket(handSocket);
    }

    public void SheathWeapon()
    {
        if (waistSocket != null) AttachToSocket(waistSocket);
    }

    private void AttachToSocket(Transform targetSocket)
    {
        if (weaponObject == null) return;
        weaponObject.transform.SetParent(targetSocket);
        weaponObject.transform.localPosition = Vector3.zero;
        weaponObject.transform.localRotation = Quaternion.identity;
    }
}