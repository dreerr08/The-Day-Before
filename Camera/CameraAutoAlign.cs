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

    [Tooltip("Velocidade de alinhamento após esperar os 5 segundos andando para trás.")]
    public float backwardAlignSpeed = 15.0f; // Um pouco mais lento para não enjoar

    [Header("Delay Inteligente")]
    [Tooltip("Tempo (segundos) andando para trás antes da câmera decidir girar.")]
    public float backwardAlignDelay = 5.0f; // <--- O PEDIDO DO MENTOR: 5 Segundos!

    [Header("Limites")]
    public float strafeThreshold = 0.5f;

    // Internas
    private CinemachineFreeLook _freeLook;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _sprintAction;

    private float _lastManualInputTime;
    private float _backwardMovementTimer; // Contador para o delay

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
        // 1. Respeita input manual do mouse/analógico direito
        Vector2 lookInput = _lookAction.ReadValue<Vector2>();
        if (lookInput.sqrMagnitude > 0.01f)
        {
            _lastManualInputTime = Time.time;
            _backwardMovementTimer = 0f; // Reseta timer se mexer a câmera manualmente
            return;
        }

        if (Time.time < _lastManualInputTime + timeBeforeAutoAlign) return;

        // 2. Lê movimento
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        bool isMoving = moveInput.sqrMagnitude > 0.1f;

        if (!isMoving)
        {
            _backwardMovementTimer = 0f; // Reseta se parar
            return;
        }

        bool isSprinting = _sprintAction.IsPressed();
        float currentAlignSpeed = 0f;

        // --- LÓGICA DE ALINHAMENTO ---

        // CASO 1: Andando para TRÁS (Input Y Negativo)
        if (moveInput.y < -0.1f)
        {
            // Começa a contar o tempo
            _backwardMovementTimer += Time.deltaTime;

            if (_backwardMovementTimer > backwardAlignDelay)
            {
                // Já passou dos 5 segundos, libera o giro!
                currentAlignSpeed = backwardAlignSpeed;
            }
            else
            {
                // Ainda não deu o tempo, mantenha a câmera parada (zona morta de rotação)
                currentAlignSpeed = 0f;
            }
        }
        // CASO 2: Andando muito para os LADOS (Strafe)
        else if (Mathf.Abs(moveInput.x) > strafeThreshold)
        {
            _backwardMovementTimer = 0f; // Mudou a intenção, reseta
            currentAlignSpeed = 0f;      // Não gira no strafe puro
        }
        // CASO 3: Andando para FRENTE (Input Y Positivo)
        else
        {
            _backwardMovementTimer = 0f; // Mudou a intenção, reseta
            // Aplica a velocidade normal
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