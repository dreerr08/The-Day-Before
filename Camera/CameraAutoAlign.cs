using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CinemachineFreeLook))]
public class CameraAutoAlign : MonoBehaviour
{
    [Header("Referências")]
    public Transform playerTransform;
    public PlayerInput playerInput;

    [Header("Configurações")]
    public float timeBeforeAutoAlign = 0.5f;

    [Header("Velocidades")]
    public float forwardAlignSpeed = 20.0f;
    public float runAlignSpeed = 120.0f;

    // REMOVI o backwardAlignSpeed pois ele precisa ser ZERO para evitar o loop.

    [Header("Limites")]
    public float strafeThreshold = 0.5f;

    // Internas
    private CinemachineFreeLook _freeLook;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _sprintAction;

    private float _lastManualInputTime;

    void Awake()
    {
        _freeLook = GetComponent<CinemachineFreeLook>();
    }

    void Start()
    {
        if (playerInput == null && playerTransform != null)
            playerInput = playerTransform.GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            _moveAction = playerInput.actions.FindAction("Move");
            _lookAction = playerInput.actions.FindAction("Look");
            _sprintAction = playerInput.actions.FindAction("Sprint");
        }
    }

    void Update()
    {
        if (playerTransform == null || playerInput == null) return;
        HandleAutoAlign();
    }

    void HandleAutoAlign()
    {
        // 1. Respeita input manual
        Vector2 lookInput = _lookAction.ReadValue<Vector2>();
        if (lookInput.sqrMagnitude > 0.01f)
        {
            _lastManualInputTime = Time.time;
            return;
        }

        if (Time.time < _lastManualInputTime + timeBeforeAutoAlign) return;

        // 2. Lê movimento
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        bool isMoving = moveInput.sqrMagnitude > 0.1f;

        if (!isMoving) return;

        bool isSprinting = _sprintAction.IsPressed();
        float currentAlignSpeed = 0f;

        // --- LÓGICA ANTI-LOOP ---

        // Se estiver andando para TRÁS (Input Y negativo), PARE TUDO.
        // Isso permite o personagem correr em direção à tela sem a câmera girar loucamente.
        if (moveInput.y < -0.1f)
        {
            currentAlignSpeed = 0f; // <--- AQUI ESTÁ A CORREÇÃO (Zona Morta)
        }
        // Se estiver andando muito para os LADOS (Strafe)
        else if (Mathf.Abs(moveInput.x) > strafeThreshold)
        {
            currentAlignSpeed = 0f; // Também não gira no strafe puro
        }
        // Se estiver andando para FRENTE (Input Y Positivo)
        else
        {
            // Aqui sim aplicamos a velocidade
            currentAlignSpeed = isSprinting ? runAlignSpeed : forwardAlignSpeed;
        }

        // 3. Aplica rotação se permitido
        if (currentAlignSpeed > 0)
        {
            float targetAngle = playerTransform.eulerAngles.y;
            float currentAngle = _freeLook.m_XAxis.Value;

            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, currentAlignSpeed * Time.deltaTime);
            _freeLook.m_XAxis.Value = newAngle;
        }
    }
}