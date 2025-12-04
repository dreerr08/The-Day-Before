using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerLocomotion : MonoBehaviour
{
    // --- DEBUG ---
    [Header("Debug")]
    public bool showDebugLogs = false;

    // --- CONFIGURAÇÕES ---
    [Header("Velocidades")]
    public float walkSpeed = 2.0f;
    [Tooltip("Velocidade ao andar para trás")]
    public float backwardsSpeed = 1.5f;
    public float runSpeed = 6.0f;

    [Header("Combate / Restrição")] // <--- NOVO
    [Tooltip("Velocidade permitida enquanto ataca (Ex: 0.1 para deslizar levemente)")]
    public float attackingSpeed = 0.1f;

    [Header("Correção de Lock-on")]
    [Tooltip("Força que puxa o player para o inimigo ao girar.")]
    public float orbitCorrectionStrength = 2.0f;

    public float gravity = -9.81f;

    [Header("Pulo")]
    public float jumpHeight = 1.2f;

    [Header("Rotacao e Turn In Place")]
    public float rotationSpeed = 15.0f;
    public float turnThreshold = 45.0f;
    public float turnInPlaceSpeed = 5.0f;

    [Header("Animacao")]
    public string inputXName = "InputX";
    public string inputYName = "InputY";
    public string deathTriggerName = "Die";
    public string jumpTriggerName = "Jump";

    public float walkIntensity = 0.5f;
    public float runIntensity = 1.0f;
    public float animationSmoothTime = 0.08f;

    // --- REFERÊNCIAS ---
    private CharacterController _characterController;
    private Animator _animator;
    private PlayerInput _playerInput;
    private Transform _cameraTransform;

    private InputAction _moveAction;
    private InputAction _sprintAction;
    private InputAction _jumpAction;

    // --- ESTADOS INTERNOS ---
    private Transform _currentTarget;
    private Vector3 _playerVelocity;
    private bool _isGrounded;
    private bool _isDead = false;

    // ESTADO DE RESTRIÇÃO
    private bool _isMovementRestricted = false;

    // --- PROPRIEDADES PÚBLICAS ---
    public bool IsLockedOn => _currentTarget != null;
    public Transform CurrentLockOnTarget => _currentTarget;

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _playerInput = GetComponent<PlayerInput>();

        if (Camera.main != null) _cameraTransform = Camera.main.transform;
        else UnityEngine.Debug.LogError("[PlayerLocomotion] MainCamera nao encontrada!");

        _moveAction = _playerInput.actions.FindAction("Move");
        _sprintAction = _playerInput.actions.FindAction("Sprint");
        _jumpAction = _playerInput.actions.FindAction("Jump");
    }

    void Update()
    {
        if (!_isDead && Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            Morrer();
        }

        if (_isDead) return;

        HandleMovementAndRotation();
        HandleJump();
        ApplyGravity();
    }

    public void SetMovementRestricted(bool restricted)
    {
        _isMovementRestricted = restricted;
        // Não zeramos mais a animação aqui forçadamente, deixamos o input ditar
    }

    public void SetLockOnTarget(Transform target)
    {
        _currentTarget = target;
    }

    void Morrer()
    {
        _isDead = true;
        _animator.SetTrigger(deathTriggerName);
    }

    void HandleJump()
    {
        if (_isMovementRestricted) return; // Não pula atacando

        if (_isGrounded)
        {
            bool jumpPressed = _jumpAction != null && _jumpAction.WasPressedThisFrame();
            Vector2 inputVector = _moveAction.ReadValue<Vector2>();
            bool isStandingStill = inputVector.sqrMagnitude == 0;

            if (jumpPressed && isStandingStill)
            {
                _playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _animator.SetTrigger(jumpTriggerName);
            }
        }
    }

    void HandleMovementAndRotation()
    {
        // 1. LEITURA DE INPUT
        Vector2 inputVector = _moveAction.ReadValue<Vector2>();
        if (inputVector.sqrMagnitude > 1) inputVector.Normalize();

        bool isMoving = inputVector.sqrMagnitude > 0;
        bool isSprinting = _sprintAction != null && _sprintAction.IsPressed();
        bool isMovingBackwards = inputVector.y < -0.1f;

        // 2. CÁLCULO DE DIREÇÃO DA CÂMERA
        Vector3 cameraForward = _cameraTransform.forward;
        Vector3 cameraRight = _cameraTransform.right;
        cameraForward.y = 0; cameraRight.y = 0;
        cameraForward.Normalize(); cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * inputVector.y + cameraRight * inputVector.x;

        // 3. LÓGICA DE VELOCIDADE (AQUI MUDOU!)
        float currentSpeed = 0f;

        if (_isMovementRestricted)
        {
            // MODO ATAQUE: Velocidade fixa bem baixa (0.1)
            currentSpeed = isMoving ? attackingSpeed : 0f;
        }
        else
        {
            // MODO NORMAL
            if (isMoving)
            {
                if (isSprinting) currentSpeed = runSpeed;
                else if (isMovingBackwards) currentSpeed = backwardsSpeed;
                else currentSpeed = walkSpeed;
            }
        }

        // 4. ROTAÇÃO E MOVIMENTO
        if (isMoving || IsLockedOn)
        {
            // --- ROTAÇÃO ---
            // Só giramos via input se NÃO estivermos atacando (Restricted)
            // Se estiver atacando, o Combat script cuida da rotação (LockOn) ou mantém fixo.
            if (!_isMovementRestricted)
            {
                if (IsLockedOn)
                {
                    Vector3 directionToTarget = _currentTarget.position - transform.position;
                    directionToTarget.y = 0;
                    if (directionToTarget != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    }

                    // Correção Orbital no LockOn
                    if (isMoving && Mathf.Abs(inputVector.x) > 0.1f)
                    {
                        Vector3 pullDirection = directionToTarget.normalized;
                        moveDirection += pullDirection * orbitCorrectionStrength * Time.deltaTime;
                    }
                }
                else if (cameraForward != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }

            // --- APLICA O MOVIMENTO ---
            if (currentSpeed > 0)
            {
                _characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
            }

            // --- ANIMAÇÃO ---
            // Se estiver atacando, talvez você queira manter os pés parados visualmente no Animator
            // ou deixar mover um pouco. Vou deixar mover um pouco.
            Vector3 localMoveDirection = transform.InverseTransformDirection(moveDirection);
            float targetIntensity = isSprinting ? runIntensity : walkIntensity;

            // Se estiver restrito, reduzimos a intensidade da animação de pernas também
            if (_isMovementRestricted) targetIntensity = 0.1f;

            float inputMagnitude = inputVector.magnitude;
            _animator.SetFloat(inputXName, localMoveDirection.x * targetIntensity * inputMagnitude, animationSmoothTime, Time.deltaTime);
            _animator.SetFloat(inputYName, localMoveDirection.z * targetIntensity * inputMagnitude, animationSmoothTime, Time.deltaTime);
        }
        else
        {
            // Parado
            _animator.SetFloat(inputXName, 0, animationSmoothTime, Time.deltaTime);
            _animator.SetFloat(inputYName, 0, animationSmoothTime, Time.deltaTime);
        }
    }

    void ApplyGravity()
    {
        _isGrounded = _characterController.isGrounded;
        if (_isGrounded && _playerVelocity.y < 0) _playerVelocity.y = -2f;
        _playerVelocity.y += gravity * Time.deltaTime;
        _characterController.Move(_playerVelocity * Time.deltaTime);
    }
}