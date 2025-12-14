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

    // Estados
    private int _currentComboIndex = 0;
    private float _lastInputTime = -1f;
    private bool _isAttacking = false;
    private bool _isPenalized = false;
    private bool _combatEnabled = true;

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

    public void UpdateCombatData(WeaponItemData weaponData)
    {
        this.currentWeaponCombo = weaponData.weaponCombo;
        this.baseDamage = weaponData.baseDamage;
        _currentComboIndex = 0;
    }

    // ATUALIZADO: Agora controla também o Equipamento
    public void SetCombatEnabled(bool isEnabled)
    {
        _combatEnabled = isEnabled;

        // NOVO: Repassa a ordem para o sistema de equipamento
        if (_equipment != null)
        {
            _equipment.SetInputEnabled(isEnabled);
        }

        if (!isEnabled)
        {
            _isAttacking = false;
            _isPenalized = false;
            StopAllCoroutines();
            _locomotion.SetMovementRestricted(false);
        }
    }

    void OnEnable() => _attackAction.performed += OnAttackInput;
    void OnDisable() => _attackAction.performed -= OnAttackInput;

    private void OnAttackInput(InputAction.CallbackContext context)
    {
        if (!_combatEnabled) return;
        if (_equipment != null && !_equipment.IsEquipped) return;
        if (_isPenalized) return;

        if (_isAttacking)
        {
            if (CanContinueCombo()) _lastInputTime = Time.time;
            else StartCoroutine(TriggerPenaltyRoutine());
        }
        else
        {
            _lastInputTime = Time.time;
        }
    }

    IEnumerator TriggerPenaltyRoutine()
    {
        _isPenalized = true;
        _lastInputTime = -1f;
        UnityEngine.Debug.Log($"<color=red><b>{gameObject.name} DESEQUILIBROU!</b></color>");
        _locomotion.SetMovementRestricted(true);
        float duration = currentWeaponCombo != null ? currentWeaponCombo.penaltyDuration : 1.0f;
        yield return new WaitForSeconds(duration);
        _isPenalized = false;
        _isAttacking = false;
        _currentComboIndex = 0;
        _locomotion.SetMovementRestricted(false);
    }

    void Update()
    {
        if (currentWeaponCombo == null || currentWeaponCombo.attacks.Count == 0) return;
        if (_isPenalized) return;
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

        StopAllCoroutines();
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

    // --- EVENTOS DE ANIMAÇÃO ---
    public void AnimEvent_StartTrail()
    {
        if (_equipment != null && _equipment.CurrentWeaponFeedback != null)
            _equipment.CurrentWeaponFeedback.SetTrailActive(true);
    }

    public void AnimEvent_EndTrail()
    {
        if (_equipment != null && _equipment.CurrentWeaponFeedback != null)
            _equipment.CurrentWeaponFeedback.SetTrailActive(false);
    }

    public void AnimEvent_PlaySwingSound()
    {
        if (_equipment != null && _equipment.CurrentWeaponFeedback != null)
        {
            AudioClip clip = currentWeaponCombo.attacks[_currentComboIndex].swingSound;
            _equipment.CurrentWeaponFeedback.PlaySlashSound(clip);
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
            if (target != null) target.TakeDamage(finalDamage);
        }
    }

    public void AnimEvent_EndAttack()
    {
        _isAttacking = false;
        _currentComboIndex = 0;
        _locomotion.SetMovementRestricted(false);
        AnimEvent_EndTrail();
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