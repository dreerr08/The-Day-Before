using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(PlayerLocomotion))]
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Configuração de Dados")]
    public WeaponComboSO currentWeaponCombo;

    [Header("Progressão")]
    public int maxComboUnlocked = 3;

    [Header("Ajustes Finos")]
    public float inputBufferTime = 0.2f;
    public LayerMask hitLayers;
    public Transform damageOriginPoint;
    public float attackRadius = 0.8f;
    public int baseDamage = 10;

    // --- ESTADOS INTERNOS ---
    private int _currentComboIndex = 0;
    private float _lastInputTime = -1f;
    private bool _isAttacking = false;

    // NOVO: Estado de Punição
    private bool _isPenalized = false;

    // Referências
    private Animator _animator;
    private PlayerLocomotion _locomotion;
    private PlayerEquipment _equipment;
    private PlayerInput _playerInput;
    private InputAction _attackAction;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _locomotion = GetComponent<PlayerLocomotion>();
        _equipment = GetComponent<PlayerEquipment>();
        _playerInput = GetComponent<PlayerInput>();
        _attackAction = _playerInput.actions.FindAction("Attack");
    }

    void OnEnable() => _attackAction.performed += OnAttackInput;
    void OnDisable() => _attackAction.performed -= OnAttackInput;

    // --- AQUI ESTÁ A MÁGICA DA PUNIÇÃO ---
    private void OnAttackInput(InputAction.CallbackContext context)
    {
        // 1. Regras Básicas: Sem arma ou já punido? Ignora.
        if (_equipment != null && !_equipment.IsEquipped) return;
        if (_isPenalized) return;

        // 2. Se já estamos atacando, verificamos o timing AGORA.
        if (_isAttacking)
        {
            bool canChain = CanContinueCombo();

            if (canChain)
            {
                // JOGADA CERTA: Dentro da janela -> Bufferiza o input
                _lastInputTime = Time.time;
            }
            else
            {
                // ERROU: Clicou cedo demais (Spam) -> Punição!
                StartCoroutine(TriggerPenaltyRoutine());
            }
        }
        else
        {
            // 3. Se estava parado, começa o ataque normalmente
            _lastInputTime = Time.time;
        }
    }

    // Corrotina para gerenciar o tempo de travamento
    IEnumerator TriggerPenaltyRoutine()
    {
        _isPenalized = true;
        _lastInputTime = -1f; // Limpa qualquer input salvo

        // Feedback Visual (Provisório, mas vital para testes)
        UnityEngine.Debug.Log($"<color=red><b>{gameObject.name} DESEQUILIBROU! (Spam Detectado)</b></color>");

        // Opcional: Tocar uma animação de "Hit/Stumble" aqui se tiver
        // _animator.SetTrigger("Stumble"); 

        // Trava o movimento totalmente durante a punição
        _locomotion.SetMovementRestricted(true);

        // Espera o tempo definido na Arma (ScriptableObject)
        float duration = currentWeaponCombo != null ? currentWeaponCombo.penaltyDuration : 1.0f;
        yield return new WaitForSeconds(duration);

        // Recuperação
        _isPenalized = false;
        _isAttacking = false; // Reseta o combo
        _currentComboIndex = 0;
        _locomotion.SetMovementRestricted(false); // Devolve o movimento

        UnityEngine.Debug.Log("Recuperado do desequilíbrio.");
    }

    void Update()
    {
        if (currentWeaponCombo == null || currentWeaponCombo.attacks.Count == 0) return;
        if (_isPenalized) return; // Se punido, a máquina de estados não roda

        HandleCombatState();
    }

    private void HandleCombatState()
    {
        bool hasBufferedInput = (Time.time - _lastInputTime) <= inputBufferTime;
        if (!hasBufferedInput) return;

        if (!_isAttacking)
        {
            StartComboAttack(0);
        }
        else
        {
            // Nota: A verificação de 'CanContinueCombo' já foi feita no Input,
            // mas mantemos aqui para garantir que a animação flua corretamente
            if (CanContinueCombo())
            {
                int nextIndex = _currentComboIndex + 1;
                if (nextIndex < currentWeaponCombo.attacks.Count && nextIndex < maxComboUnlocked)
                {
                    StartComboAttack(nextIndex);
                }
            }
        }
    }

    private bool CanContinueCombo()
    {
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
        if (_animator.IsInTransition(0)) return false;

        AttackMove currentMove = currentWeaponCombo.attacks[_currentComboIndex];
        return info.normalizedTime >= currentMove.comboWindowStart;
    }

    private void StartComboAttack(int index)
    {
        _lastInputTime = -1f;
        _currentComboIndex = index;
        _isAttacking = true;

        AttackMove move = currentWeaponCombo.attacks[index];

        _locomotion.SetMovementRestricted(true);
        AlignPlayerBeforeAttack();

        _animator.Play(move.animationTriggerName, 0, 0f);

        StopAllCoroutines(); // Para impulsos anteriores
        StartCoroutine(ApplyAttackImpulse(move.movementImpulse));
    }

    private void AlignPlayerBeforeAttack()
    {
        if (_locomotion.IsLockedOn && _locomotion.CurrentLockOnTarget != null)
        {
            Vector3 dir = _locomotion.CurrentLockOnTarget.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }
        else
        {
            Vector2 input = _playerInput.actions.FindAction("Move").ReadValue<Vector2>();
            if (input.sqrMagnitude > 0.1f)
            {
                Vector3 camFwd = Camera.main.transform.forward;
                Vector3 camRight = Camera.main.transform.right;
                camFwd.y = 0; camRight.y = 0;
                Vector3 moveDir = camFwd.normalized * input.y + camRight.normalized * input.x;
                if (moveDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(moveDir);
            }
        }
    }

    IEnumerator ApplyAttackImpulse(float force)
    {
        if (force <= 0.1f) yield break;
        float duration = 0.2f;
        float timer = 0f;
        CharacterController cc = GetComponent<CharacterController>();

        while (timer < duration)
        {
            timer += Time.deltaTime;
            cc.Move(transform.forward * force * (Time.deltaTime / duration));
            yield return null;
        }
    }

    public void AnimEvent_DealDamage()
    {
        float multiplier = currentWeaponCombo.attacks[_currentComboIndex].damageMultiplier;
        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

        Vector3 origin = damageOriginPoint != null ? damageOriginPoint.position : transform.position + transform.forward;
        Collider[] hits = Physics.OverlapSphere(origin, attackRadius, hitLayers);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(finalDamage);
                UnityEngine.Debug.Log($"[COMBATE] Golpe {_currentComboIndex + 1} acertou {hit.name} ({finalDamage} dmg)");
            }
        }
    }

    public void AnimEvent_EndAttack()
    {
        _isAttacking = false;
        _currentComboIndex = 0;
        _locomotion.SetMovementRestricted(false);
    }

    void LateUpdate()
    {
        if (_isAttacking)
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsTag("Locomotion") || info.IsTag("Idle"))
            {
                if (_isAttacking) AnimEvent_EndAttack();
            }
        }
    }
}