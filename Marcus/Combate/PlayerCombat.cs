using UnityEngine;
using UnityEngine.InputSystem;
// NÃO adicione 'using System.Diagnostics;' aqui

[RequireComponent(typeof(PlayerLocomotion))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Debug")]
    public bool showDebugLogs = true;

    [Header("Configurações")]
    public string attackActionName = "Attack";
    public string attackTriggerName = "Attack";

    [Header("Combate")]
    public int attackDamage = 10;
    public float attackRange = 1.5f;
    public float attackRadius = 0.8f;
    public LayerMask hitLayers;

    [Header("Referências Visuais")]
    public Transform damageOriginPoint;

    // Referências
    private PlayerLocomotion _locomotion;
    private PlayerEquipment _equipment;
    private Animator _animator;
    private PlayerInput _playerInput;
    private InputAction _attackAction;

    // Estado Interno
    private bool _isAttacking = false;

    // Hashes
    private int _attackTriggerHash;

    void Awake()
    {
        _locomotion = GetComponent<PlayerLocomotion>();
        _equipment = GetComponent<PlayerEquipment>();
        _animator = GetComponent<Animator>();
        _playerInput = GetComponent<PlayerInput>();

        _attackTriggerHash = Animator.StringToHash(attackTriggerName);

        if (_playerInput.actions != null)
            _attackAction = _playerInput.actions.FindAction(attackActionName);

        if (_attackAction == null)
            UnityEngine.Debug.LogError($"[PlayerCombat] Action '{attackActionName}' não encontrada!");
    }

    void OnEnable()
    {
        if (_attackAction != null) _attackAction.performed += OnAttackInput;
    }

    void OnDisable()
    {
        if (_attackAction != null) _attackAction.performed -= OnAttackInput;
    }

    private void Update()
    {
        // WATCHDOG SIMPLES (Cão de Guarda)
        if (_isAttacking && !_animator.IsInTransition(0))
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            // Se já passamos do ponto ou não estamos mais no ataque
            if (!info.IsTag("Attack") && info.normalizedTime > 1.0f)
            {
                if (showDebugLogs) UnityEngine.Debug.Log("[COMBATE] Watchdog: Forçando fim do ataque.");
                EndCombatState();
            }
        }
    }

    private void OnAttackInput(InputAction.CallbackContext context)
    {
        // 1. GATEKEEPER
        if (_isAttacking) return;

        // 2. EQUIPAMENTO
        if (_equipment != null && !_equipment.IsEquipped)
        {
            if (showDebugLogs) UnityEngine.Debug.Log("[COMBATE] Falha: Espada não equipada.");
            return;
        }

        PerformAttack();
    }

    private void PerformAttack()
    {
        _isAttacking = true;

        // Trava movimento
        _locomotion.SetMovementRestricted(true);

        // Rotação (Apenas se tiver Lock-on)
        HandleAttackRotation();

        // Toca animação
        _animator.SetTrigger(_attackTriggerHash);

        if (showDebugLogs) UnityEngine.Debug.Log("[COMBATE] Ataque Iniciado!");
    }

    private void HandleAttackRotation()
    {
        if (_locomotion.IsLockedOn && _locomotion.CurrentLockOnTarget != null)
        {
            Vector3 dirToTarget = _locomotion.CurrentLockOnTarget.position - transform.position;
            dirToTarget.y = 0;
            if (dirToTarget != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dirToTarget);
            }
        }
    }

    // --- EVENTOS DE ANIMAÇÃO ---

    public void AnimEvent_DealDamage()
    {
        Vector3 origin = damageOriginPoint != null ? damageOriginPoint.position : transform.position + transform.forward * attackRange;
        Collider[] hits = Physics.OverlapSphere(origin, attackRadius, hitLayers);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (showDebugLogs) UnityEngine.Debug.Log($"[COMBATE] Acertei: {hit.name}!");
        }
    }

    public void AnimEvent_EndAttack()
    {
        if (showDebugLogs) UnityEngine.Debug.Log("[COMBATE] Fim da Animação recebido via Evento.");
        EndCombatState();
    }

    private void EndCombatState()
    {
        _isAttacking = false;
        _locomotion.SetMovementRestricted(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (damageOriginPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(damageOriginPoint.position, attackRadius);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Vector3 previewPos = transform.position + transform.forward * attackRange;
            Gizmos.DrawWireSphere(previewPos, attackRadius);
        }
    }
}