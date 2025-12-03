using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerLocomotion : MonoBehaviour
{
    // --- CONFIGURAÇÕES ---
    [Header("Input Config")]
    [Tooltip("Zona morta do analógico para evitar drift.")]
    public float inputThreshold = 0.2f;

    [Header("Velocidades")]
    public float walkSpeed = 2.0f;
    public float backwardsSpeed = 1.5f; // Andar para trás
    public float runSpeed = 6.0f;

    [Header("Física")]
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;

    [Header("Rotação")]
    public float rotationSpeed = 15.0f;

    [Header("Animação (Nomes)")]
    public string inputXName = "InputX";
    public string inputYName = "InputY";
    public string jumpTrigger = "Jump";
    public string dieTrigger = "Die";

    [Header("Animação (Ajustes)")]
    public float animationSmoothTime = 0.1f;

    // --- REFERÊNCIAS ---
    private CharacterController _characterController;
    private Animator _animator;
    private PlayerInput _playerInput;
    private Transform _cameraTransform;

    private InputAction _moveAction;
    private InputAction _sprintAction;
    private InputAction _jumpAction;

    // --- ESTADO ---
    private Transform _lockOnTarget;
    private Vector3 _playerVelocity;
    private bool _isGrounded;
    private bool _isDead = false;

    public bool IsLockedOn => _lockOnTarget != null;

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _playerInput = GetComponent<PlayerInput>();

        if (Camera.main != null)
            _cameraTransform = Camera.main.transform;
        else
            UnityEngine.Debug.LogError("ERRO: Nenhuma MainCamera encontrada (Tag 'MainCamera')!");

        _moveAction = _playerInput.actions.FindAction("Move");
        _sprintAction = _playerInput.actions.FindAction("Sprint");
        _jumpAction = _playerInput.actions.FindAction("Jump");
    }

    void Update()
    {
        if (_isDead) return;

        // DEBUG: Botão de morte rápida
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame) Morrer();

        HandleMovement();
        HandleGravity();
        HandleJump();
    }

    public void SetLockOnTarget(Transform target)
    {
        _lockOnTarget = target;
    }

    private void HandleMovement()
    {
        // 1. Ler Inputs
        Vector2 input = _moveAction.ReadValue<Vector2>();

        // Verifica se o botão de correr está sendo segurado (Gatilho ou Shift)
        // Usamos IsPressed() que funciona tanto para botões quanto para gatilhos analógicos pressionados fundo
        bool isSprinting = _sprintAction != null && _sprintAction.IsPressed();

        // Zona Morta
        if (input.magnitude < inputThreshold) input = Vector2.zero;
        bool hasInput = input.sqrMagnitude > 0;

        // 2. Calcular Direção
        Vector3 moveDirection = Vector3.zero;

        Vector3 camForward = _cameraTransform.forward;
        Vector3 camRight = _cameraTransform.right;
        camForward.y = 0; camRight.y = 0;
        camForward.Normalize(); camRight.Normalize();

        if (IsLockedOn)
        {
            // MODO LOCK-ON:
            // Movemos relativo à câmera, mas sem travar o movimento físico
            moveDirection = (camForward * input.y) + (camRight * input.x);

            // Apenas gira o corpo para olhar o inimigo
            HandleLockOnRotation();
        }
        else
        {
            // MODO LIVRE:
            moveDirection = (camForward * input.y) + (camRight * input.x);

            if (hasInput)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // 3. Aplica Velocidade (AQUI MUDOU)
        if (hasInput)
        {
            float targetSpeed = walkSpeed;

            // LÓGICA SIMPLIFICADA: Se apertou correr, CORRE. 
            // Não importa se está andando para trás ou travado na mira.
            if (isSprinting)
            {
                targetSpeed = runSpeed;
            }
            else if (input.y < -0.1f && !IsLockedOn)
            {
                // Só diminui velocidade se estiver andando pra trás E sem mira
                // Se estiver com mira, mantém walkSpeed normal para agilidade no combate
                targetSpeed = backwardsSpeed;
            }

            _characterController.Move(moveDirection * targetSpeed * Time.deltaTime);
        }

        // 4. Animação
        UpdateAnimation(moveDirection, isSprinting, input.magnitude);
    }

    private void HandleLockOnRotation()
    {
        if (_lockOnTarget == null) return;

        Vector3 dirToTarget = _lockOnTarget.position - transform.position;
        dirToTarget.y = 0;

        if (dirToTarget != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimation(Vector3 moveDir, bool isSprinting, float inputMag)
    {
        Vector3 localDir = transform.InverseTransformDirection(moveDir);

        // Se está correndo, intensidade vai para 1.0, se andando 0.5
        float intensity = isSprinting ? 1.0f : 0.5f;

        if (inputMag < 0.1f) intensity = 0;

        _animator.SetFloat(inputXName, localDir.x * intensity, animationSmoothTime, Time.deltaTime);
        _animator.SetFloat(inputYName, localDir.z * intensity, animationSmoothTime, Time.deltaTime);
    }

    private void HandleGravity()
    {
        _isGrounded = _characterController.isGrounded;
        if (_isGrounded && _playerVelocity.y < 0) _playerVelocity.y = -2f;

        _playerVelocity.y += gravity * Time.deltaTime;
        _characterController.Move(_playerVelocity * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (_isGrounded && _jumpAction.WasPressedThisFrame())
        {
            _playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _animator.SetTrigger(jumpTrigger);
        }
    }

    private void Morrer()
    {
        _isDead = true;
        _animator.SetTrigger(dieTrigger);
    }
}