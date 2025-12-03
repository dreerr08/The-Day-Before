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
    [Tooltip("O Transform vazio na mão direita onde a espada ficará durante o combate.")]
    public Transform handSocket;

    [Tooltip("O Transform vazio na cintura/costas onde a espada fica guardada.")]
    public Transform waistSocket;

    [Header("Animação")]
    public string equipTriggerName = "Equip";
    public string isArmedBoolName = "IsArmed";

    // Referências Privadas
    private Animator _animator;
    private PlayerInput _playerInput;
    private InputAction _equipAction;

    // Estado Interno
    private bool _isEquipped = false;

    // Otimização (Hashes são mais rápidos que strings)
    private int _equipTriggerHash;
    private int _isArmedBoolHash;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _playerInput = GetComponent<PlayerInput>();

        // Configurando Hashes para performance
        _equipTriggerHash = Animator.StringToHash(equipTriggerName);
        _isArmedBoolHash = Animator.StringToHash(isArmedBoolName);

        // Configura o Input
        // Certifique-se de criar uma Action chamada "Equip" no seu Input Action Asset!
        _equipAction = _playerInput.actions.FindAction("Equip");
    }

    void OnEnable()
    {
        if (_equipAction != null)
            _equipAction.performed += OnEquipInput;
    }

    void OnDisable()
    {
        if (_equipAction != null)
            _equipAction.performed -= OnEquipInput;
    }

    private void Start()
    {
        // Garante que a espada comece no lugar certo (Cintura)
        if (weaponObject != null && waistSocket != null)
        {
            AttachToSocket(waistSocket);
        }
    }

    // 1. O INPUT: O jogador aperta o botão
    private void OnEquipInput(InputAction.CallbackContext context)
    {
        // Impede spam (opcional: checar se já está tocando animação de equipar)
        if (_animator.IsInTransition(0)) return;

        // Inverte o estado (Se está armado, desarma. Se desarmado, arma)
        _isEquipped = !_isEquipped;

        // Avisa o Animator para tocar a animação correspondente
        // O Animator deve usar o bool 'IsArmed' para saber se vai para a Blend Tree de combate ou normal
        _animator.SetBool(_isArmedBoolHash, _isEquipped);
        _animator.SetTrigger(_equipTriggerHash);
    }

    // --- MÉTODOS DE ANIMATION EVENT ---
    // Estes métodos NÃO são chamados pelo código, mas pela linha do tempo da ANIMAÇÃO na Unity.

    // Chame este evento no frame exato em que a mão toca a espada na CINTURA
    public void GrabWeapon()
    {
        if (handSocket != null)
        {
            AttachToSocket(handSocket);
        }
    }

    // Chame este evento no frame exato em que a mão guarda a espada na CINTURA (para desequipar)
    public void SheathWeapon()
    {
        if (waistSocket != null)
        {
            AttachToSocket(waistSocket);
        }
    }

    // Função auxiliar para evitar repetição de código
    private void AttachToSocket(Transform targetSocket)
    {
        if (weaponObject == null) return;

        // Muda o pai da espada para o novo socket
        weaponObject.transform.SetParent(targetSocket);

        // Zera posição e rotação local para alinhar perfeitamente com o socket
        weaponObject.transform.localPosition = Vector3.zero;
        weaponObject.transform.localRotation = Quaternion.identity;
    }
}