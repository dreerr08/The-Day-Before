using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using System.Collections;
using System.Diagnostics;

[RequireComponent(typeof(PlayerInput))]
public class PlayerTargetLock : MonoBehaviour
{
    [Header("Configurações de Busca")]
    public float detectionRadius = 15.0f;
    public float breakDistance = 20.0f;
    public LayerMask enemyLayer;
    public string lockOnInputName = "LockOn";

    [Header("Câmeras Cinemachine")]
    [Tooltip("Sua câmera de exploração padrão (FreeLook)")]
    public CinemachineFreeLook freeLookCamera; // <--- NOVO CAMPO
    [Tooltip("Sua câmera virtual de combate")]
    public CinemachineVirtualCamera lockOnCamera;

    public int priorityLocked = 20;
    public int priorityUnlocked = 0;

    [Header("UI Feedback")]
    public GameObject targetIconPrefab;
    public Vector3 iconOffset = new Vector3(0, 1.5f, 0);

    // Referências Privadas
    private PlayerInput _playerInput;
    private InputAction _lockAction;
    private PlayerLocomotion _locomotion;

    // Estado
    private Transform _currentTarget;
    private GameObject _currentIcon;
    private bool _isLocked = false;

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _locomotion = GetComponent<PlayerLocomotion>();

        _lockAction = _playerInput.actions.FindAction(lockOnInputName);
        if (_lockAction == null) UnityEngine.Debug.LogError("Ação 'LockOn' não encontrada!");
    }

    void OnEnable()
    {
        if (_lockAction != null) _lockAction.performed += OnLockOnPressed;
    }

    void OnDisable()
    {
        if (_lockAction != null) _lockAction.performed -= OnLockOnPressed;
        Unlock();
    }

    void Update()
    {
        if (_isLocked)
        {
            CheckTargetStatus();
            UpdateIconPosition();
        }
    }

    private void OnLockOnPressed(InputAction.CallbackContext context)
    {
        if (_isLocked) Unlock();
        else TryLockTarget();
    }

    private void TryLockTarget()
    {
        // 1. Busca todos os colliders na esfera (Isso continua igual, filtra por distância bruta)
        Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

        Transform bestTarget = null;

        // Vamos procurar o inimigo que está mais perto do CENTRO da tela (0.5, 0.5)
        float closestDistanceToCenter = Mathf.Infinity;

        // Referência da Câmera para cálculos
        Camera cam = Camera.main;

        foreach (var enemy in enemies)
        {
            // --- PASSO A: Converter Posição do Mundo (3D) para Posição da Tela (2D) ---
            // O ViewportPoint retorna:
            // x: 0 (esquerda) a 1 (direita)
            // y: 0 (baixo) a 1 (cima)
            // z: distância da câmera até o objeto (profundidade)
            Vector3 viewportPos = cam.WorldToViewportPoint(enemy.transform.position);

            // --- PASSO B: Filtrar quem está atrás da câmera ou fora da tela ---
            bool isInFrontOfCamera = viewportPos.z > 0;
            bool isOnScreenX = viewportPos.x > 0 && viewportPos.x < 1;
            bool isOnScreenY = viewportPos.y > 0 && viewportPos.y < 1;

            if (isInFrontOfCamera && isOnScreenX && isOnScreenY)
            {
                // --- PASSO C: Calcular distância até o centro da tela (0.5, 0.5) ---
                Vector2 screenCenter = new Vector2(0.5f, 0.5f);
                Vector2 enemyScreenPos = new Vector2(viewportPos.x, viewportPos.y);

                float distToCenter = Vector2.Distance(screenCenter, enemyScreenPos);

                // Se esse inimigo estiver mais centralizado que o anterior, ele ganha
                if (distToCenter < closestDistanceToCenter)
                {
                    closestDistanceToCenter = distToCenter;
                    bestTarget = enemy.transform;
                }
            }
        }

        if (bestTarget != null)
        {
            Lock(bestTarget);
        }
        else
        {
            // Opcional: Feedback visual ou sonoro de "Nenhum alvo na tela"
            UnityEngine.Debug.Log("Nenhum inimigo na visão da câmera!");
        }
    }

    private void Lock(Transform target)
    {
        _currentTarget = target;
        _isLocked = true;

        _locomotion.SetLockOnTarget(_currentTarget);

        if (lockOnCamera != null)
        {
            lockOnCamera.LookAt = _currentTarget;
            lockOnCamera.Priority = priorityLocked;
        }

        if (targetIconPrefab != null)
        {
            _currentIcon = Instantiate(targetIconPrefab, target.position, Quaternion.identity);
        }
    }

    private void Unlock()
    {
        _isLocked = false;
        _currentTarget = null;

        _locomotion.SetLockOnTarget(null);

        // --- AQUI ESTÁ A MÁGICA DE SINCRONIZAÇÃO ---
        // Antes de devolver o controle, alinhamos a FreeLook com a direção atual
        if (freeLookCamera != null && Camera.main != null)
        {
            // Pega a rotação Y (Horizontal) da câmera atual
            float currentYRotation = Camera.main.transform.eulerAngles.y;

            // Força a FreeLook a assumir esse ângulo
            freeLookCamera.m_XAxis.Value = currentYRotation;

            // Opcional: Resetar a altura (Y Axis) para o centro (0.5) ou manter onde estava
            // freeLookCamera.m_YAxis.Value = 0.5f; 
        }

        // Reseta prioridades
        if (lockOnCamera != null)
        {
            lockOnCamera.LookAt = null;
            lockOnCamera.Priority = priorityUnlocked;
        }

        if (_currentIcon != null) Destroy(_currentIcon);
    }

    private void CheckTargetStatus()
    {
        if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy)
        {
            Unlock();
            return;
        }

        float distance = Vector3.Distance(transform.position, _currentTarget.position);
        if (distance > breakDistance) Unlock();
    }

    private void UpdateIconPosition()
    {
        if (_currentIcon != null && _currentTarget != null)
        {
            _currentIcon.transform.position = _currentTarget.position + iconOffset;
            _currentIcon.transform.LookAt(Camera.main.transform);
        }
    }
}