using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerLocomotion))]
public class PlayerTargetLock : MonoBehaviour
{
    [Header("Configurações")]
    public float detectionRadius = 15.0f;
    public float breakDistance = 20.0f; // Distância para perder o lock automaticamente
    public LayerMask enemyLayer;
    public string lockOnActionName = "LockOn";

    [Header("Câmera de Combate")]
    [Tooltip("A Câmera Virtual que deve focar no inimigo.")]
    public CinemachineVirtualCamera lockOnCamera;
    public int priorityActive = 20;   // Maior que a FreeLook
    public int priorityInactive = 0;  // Menor que a FreeLook

    [Header("Visual")]
    public GameObject targetIconPrefab;
    public Vector3 iconOffset = new Vector3(0, 1.5f, 0);

    // Referências
    private PlayerInput _playerInput;
    private PlayerLocomotion _locomotion;
    private InputAction _lockAction;

    // Estado
    private Transform _currentTarget;
    private GameObject _currentIcon;
    private bool _isLocked = false;

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _locomotion = GetComponent<PlayerLocomotion>();
        _lockAction = _playerInput.actions.FindAction(lockOnActionName);
    }

    void OnEnable()
    {
        if (_lockAction != null) _lockAction.performed += OnLockInput;
    }

    void OnDisable()
    {
        if (_lockAction != null) _lockAction.performed -= OnLockInput;
        Unlock(); // Garante limpeza se o componente for desligado
    }

    void Update()
    {
        if (_isLocked)
        {
            CheckTargetStatus();
            UpdateIcon();
        }
    }

    private void OnLockInput(InputAction.CallbackContext context)
    {
        if (_isLocked) Unlock();
        else TryToLock();
    }

    private void TryToLock()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        Transform bestTarget = GetBestTarget(enemies);

        if (bestTarget != null)
        {
            Lock(bestTarget);
        }
    }

    private Transform GetBestTarget(Collider[] enemies)
    {
        Transform best = null;
        float closestDistToCenter = Mathf.Infinity;
        Camera cam = Camera.main;

        foreach (var enemy in enemies)
        {
            // Converte posição do mundo para Viewport (0 a 1)
            Vector3 viewportPos = cam.WorldToViewportPoint(enemy.transform.position);

            // Filtra: Tem que estar na frente da câmera e dentro da tela
            if (viewportPos.z > 0 && viewportPos.x > 0 && viewportPos.x < 1 && viewportPos.y > 0 && viewportPos.y < 1)
            {
                // Calcula distância até o centro da tela (0.5, 0.5)
                float distToCenter = Vector2.Distance(new Vector2(viewportPos.x, viewportPos.y), new Vector2(0.5f, 0.5f));

                if (distToCenter < closestDistToCenter)
                {
                    closestDistToCenter = distToCenter;
                    best = enemy.transform;
                }
            }
        }
        return best;
    }

    private void Lock(Transform target)
    {
        _isLocked = true;
        _currentTarget = target;

        // 1. Avisa a locomoção (para mudar o estilo de movimento)
        _locomotion.SetLockOnTarget(target);

        // 2. Configura a câmera de combate
        if (lockOnCamera != null)
        {
            lockOnCamera.LookAt = target; // A câmera foca no inimigo
            lockOnCamera.Priority = priorityActive; // Rouba o controle da FreeLook
        }

        // 3. Cria ícone visual
        if (targetIconPrefab != null)
        {
            _currentIcon = Instantiate(targetIconPrefab, target.position, Quaternion.identity);
        }
    }

    private void Unlock()
    {
        _isLocked = false;
        _currentTarget = null;

        // 1. Libera locomoção
        _locomotion.SetLockOnTarget(null);

        // 2. Libera câmera (Cinemachine volta suavemente para a FreeLook)
        if (lockOnCamera != null)
        {
            lockOnCamera.LookAt = null;
            lockOnCamera.Priority = priorityInactive;
        }

        // 3. Remove ícone
        if (_currentIcon != null)
        {
            Destroy(_currentIcon);
        }
    }

    private void CheckTargetStatus()
    {
        // Se o inimigo morreu ou sumiu
        if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy)
        {
            Unlock();
            return;
        }

        // Se ficou muito longe
        float distance = Vector3.Distance(transform.position, _currentTarget.position);
        if (distance > breakDistance)
        {
            Unlock();
        }
    }

    private void UpdateIcon()
    {
        if (_currentIcon != null && _currentTarget != null)
        {
            _currentIcon.transform.position = _currentTarget.position + iconOffset;
            _currentIcon.transform.LookAt(Camera.main.transform); // O ícone sempre olha pra câmera
        }
    }
}