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
    [Header("Input Config")]
    [Range(0f, 0.5f)]
    [Tooltip("Zona morta do analógico.")]
    public float inputThreshold = 0.2f;

    [Header("Velocidades")]
    public float walkSpeed = 2.0f;
    [Tooltip("Velocidade ao andar para trás")]
    public float backwardsSpeed = 1.5f;
    public float runSpeed = 6.0f;

    [Header("Correção de Lock-on")]
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

    // --- LOCK-ON ---
    private Transform _currentTarget;
    public bool IsLockedOn => _currentTarget != null;

    // --- CÁLCULOS ---
    private Vector3 _playerVelocity;
    private bool _isGrounded;

    // --- NOVO: DIREÇÃO TRAVADA ---
    private Vector3 _lockedCameraForward;
    private Vector3 _lockedCameraRight;
    private bool _isMoveLocked = false;

    // Hashes
    private int _inputXHash;
    private int _inputYHash;
    private int _turnLeftHash;
    private int _turnRightHash;
    private int _dieHash;
    private int _jumpHash;

    // ESTADOS
    private bool _isTurningInPlace = false;
    private bool _isDead = false;

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _playerInput = GetComponent<PlayerInput>();

        if (Camera.main != null) _cameraTransform = Camera.main.transform;
        else UnityEngine.Debug.LogError("[ERRO] MainCamera nao encontrada!");

        // Hashes
        _inputXHash = Animator.StringToHash(inputXName);
        _inputYHash = Animator.StringToHash(inputYName);
        _turnLeftHash = Animator.StringToHash("TurnLeft");
        _turnRightHash = Animator.StringToHash("TurnRight");
        _dieHash = Animator.StringToHash(deathTriggerName);
        _jumpHash = Animator.StringToHash(jumpTriggerName);

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

    public void SetLockOnTarget(Transform target)
    {
        _currentTarget = target;
    }

    void Morrer()
    {
        _isDead = true;
        _animator.SetTrigger(_dieHash);
    }

    void HandleJump()
    {
        if (_isGrounded)
        {
            bool jumpPressed = _jumpAction != null && _jumpAction.WasPressedThisFrame();
            Vector2 inputVector = _moveAction.ReadValue<Vector2>();

            if (inputVector.magnitude < inputThreshold) inputVector = Vector2.zero;

            bool isStandingStill = inputVector.sqrMagnitude == 0;

            if (jumpPressed && isStandingStill)
            {
                _playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _animator.SetTrigger(_jumpHash);
            }
            else
            {
                _animator.ResetTrigger(_jumpHash);
            }
        }
    }

    void HandleMovementAndRotation()
    {
        Vector2 rawInput = _moveAction.ReadValue<Vector2>();
        Vector2 inputVector = rawInput.magnitude > inputThreshold ? rawInput : Vector2.zero;

        if (inputVector.sqrMagnitude > 1) inputVector.Normalize();

        bool isMoving = inputVector.sqrMagnitude > 0;
        bool isSprinting = _sprintAction != null && _sprintAction.IsPressed();

        // Verifica se está andando para trás (zona negativa do Y)
        bool isMovingBackwards = inputVector.y < -0.1f;

        // --- LÓGICA DE TRAVA DE DIREÇÃO (Input Frame Locking) ---
        // Se começarmos a andar para trás e não estivermos travados, salvamos a direção atual.
        if (isMovingBackwards && !_isMoveLocked && !IsLockedOn)
        {
            _isMoveLocked = true;
            _lockedCameraForward = _cameraTransform.forward;
            _lockedCameraRight = _cameraTransform.right;

            // Zera Y para manter plano horizontal
            _lockedCameraForward.y = 0;
            _lockedCameraRight.y = 0;
            _lockedCameraForward.Normalize();
            _lockedCameraRight.Normalize();
        }
        // Se pararmos de andar para trás (ou pararmos de mover), solta a trava.
        else if (!isMovingBackwards)
        {
            _isMoveLocked = false;
        }

        // CÁLCULO DE DIREÇÃO
        Vector3 moveDirection = Vector3.zero;

        if (IsLockedOn)
        {
            // No Lock-on, sempre relativo ao alvo, ignoramos a trava de câmera
            Vector3 cameraForward = _cameraTransform.forward;
            Vector3 cameraRight = _cameraTransform.right;
            cameraForward.y = 0; cameraRight.y = 0;
            cameraForward.Normalize(); cameraRight.Normalize();

            moveDirection = cameraForward * inputVector.y + cameraRight * inputVector.x;
        }
        else if (_isMoveLocked)
        {
            // --- AQUI ESTÁ A MÁGICA ---
            // Usamos os vetores salvos (_locked) em vez dos atuais da câmera
            moveDirection = _lockedCameraForward * inputVector.y + _lockedCameraRight * inputVector.x;
        }
        else
        {
            // Movimento Normal (frente/lados) segue a câmera em tempo real
            Vector3 cameraForward = _cameraTransform.forward;
            Vector3 cameraRight = _cameraTransform.right;
            cameraForward.y = 0; cameraRight.y = 0;
            cameraForward.Normalize(); cameraRight.Normalize();

            moveDirection = cameraForward * inputVector.y + cameraRight * inputVector.x;
        }

        if (isMoving || IsLockedOn)
        {
            _isTurningInPlace = false;

            if (IsLockedOn)
            {
                // ... (Mantém lógica de LockOn inalterada) ...
                Vector3 directionToTarget = _currentTarget.position - transform.position;
                directionToTarget.y = 0;

                if (directionToTarget != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                if (isMoving && Mathf.Abs(inputVector.x) > 0.1f && Mathf.Abs(inputVector.y) < 0.5f)
                {
                    Vector3 pullDirection = directionToTarget.normalized;
                    moveDirection += pullDirection * orbitCorrectionStrength * Time.deltaTime;
                }
            }
            else if (isMoving && moveDirection != Vector3.zero)
            {
                // Rotação Padrão (agora usando o moveDirection travado quando recua)
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // --- MOVIMENTO FÍSICO ---
            if (isMoving)
            {
                float currentSpeed = walkSpeed;
                if (isSprinting) currentSpeed = runSpeed;
                else if (isMovingBackwards) currentSpeed = backwardsSpeed;

                _characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
            }

            // --- ANIMAÇÃO ---
            Vector3 localMoveDirection = transform.InverseTransformDirection(moveDirection);

            float targetIntensity = isSprinting ? runIntensity : walkIntensity;
            float inputMagnitude = inputVector.magnitude;

            _animator.SetFloat(_inputXHash, localMoveDirection.x * targetIntensity * inputMagnitude, animationSmoothTime, Time.deltaTime);
            _animator.SetFloat(_inputYHash, localMoveDirection.z * targetIntensity * inputMagnitude, animationSmoothTime, Time.deltaTime);
        }
        else
        {
            _animator.SetFloat(_inputXHash, 0, animationSmoothTime, Time.deltaTime);
            _animator.SetFloat(_inputYHash, 0, animationSmoothTime, Time.deltaTime);

            if (_isGrounded && !IsLockedOn)
            {
                // Nota: TurnInPlace usa cameraForward atual, pode girar o char parado. 
                // Se quiser travar isso também, avise!
                Vector3 currentCamFwd = _cameraTransform.forward;
                currentCamFwd.y = 0;
                currentCamFwd.Normalize();
                HandleTurnInPlace(currentCamFwd);
            }
        }
    }

    void HandleTurnInPlace(Vector3 cameraForward)
    {
        // ... (Mantém inalterado) ...
        if (_animator.IsInTransition(0)) return;

        float angleDifference = Vector3.SignedAngle(transform.forward, cameraForward, Vector3.up);

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        bool isPlayingTurnAnim = stateInfo.IsTag("Turn");

        if (Mathf.Abs(angleDifference) > turnThreshold && !isPlayingTurnAnim)
        {
            _animator.ResetTrigger(_turnLeftHash);
            _animator.ResetTrigger(_turnRightHash);

            if (angleDifference > 0) _animator.SetTrigger(_turnRightHash);
            else _animator.SetTrigger(_turnLeftHash);
        }

        if (isPlayingTurnAnim)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnInPlaceSpeed * Time.deltaTime);
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