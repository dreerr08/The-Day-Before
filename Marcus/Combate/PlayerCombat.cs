using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Necessário para Coroutines

[RequireComponent(typeof(PlayerLocomotion))]
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Configuração de Dados")]
    [Tooltip("Arraste aqui o arquivo de Combo que criamos (WeaponComboSO)")]
    public WeaponComboSO currentWeaponCombo;

    [Header("Progressão (RPG)")]
    [Tooltip("Quantos golpes deste combo o jogador desbloqueou?")]
    public int maxComboUnlocked = 3;

    [Header("Ajustes Finos")]
    public float inputBufferTime = 0.2f; // Tempo que o clique "espera" na fila
    public LayerMask hitLayers;
    public Transform damageOriginPoint;
    public float attackRadius = 0.8f;
    public int baseDamage = 10;

    // --- ESTADOS INTERNOS ---
    private int _currentComboIndex = 0;
    private float _lastInputTime = -1f;
    private bool _isAttacking = false;

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

        // Configura Input
        _attackAction = _playerInput.actions.FindAction("Attack");
    }

    void OnEnable() => _attackAction.performed += OnAttackInput;
    void OnDisable() => _attackAction.performed -= OnAttackInput;

    // 1. O INPUT: Apenas registra a intenção (Buffer)
    private void OnAttackInput(InputAction.CallbackContext context)
    {
        // Só aceita input se tiver arma equipada
        if (_equipment != null && !_equipment.IsEquipped) return;

        _lastInputTime = Time.time;
    }

    void Update()
    {
        // Se não tiver combo configurado, não faz nada
        if (currentWeaponCombo == null || currentWeaponCombo.attacks.Count == 0) return;

        HandleCombatState();
    }

    // 2. A MÁQUINA DE ESTADOS (Loop Principal)
    private void HandleCombatState()
    {
        // Verifica se o Buffer ainda é válido (input recente)
        bool hasBufferedInput = (Time.time - _lastInputTime) <= inputBufferTime;

        if (!hasBufferedInput) return;

        // CENÁRIO A: Começar do Zero (Idle -> Ataque 1)
        if (!_isAttacking)
        {
            StartComboAttack(0);
        }
        // CENÁRIO B: Continuar Combo (Ataque 1 -> Ataque 2)
        else
        {
            // Verifica se estamos na janela permitida do ataque ATUAL
            if (CanContinueCombo())
            {
                int nextIndex = _currentComboIndex + 1;

                // Verifica barreiras: Existe próximo golpe? O jogador desbloqueou?
                if (nextIndex < currentWeaponCombo.attacks.Count && nextIndex < maxComboUnlocked)
                {
                    StartComboAttack(nextIndex);
                }
            }
        }
    }

    // Verifica se a animação atual já passou do ponto de "ComboWindowStart"
    private bool CanContinueCombo()
    {
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);

        // Proteção: Se estiver em transição, não deixa clicar ainda para evitar bugs visuais
        if (_animator.IsInTransition(0)) return false;

        // Pega os dados do ataque que está tocando AGORA
        AttackMove currentMove = currentWeaponCombo.attacks[_currentComboIndex];

        // Verifica se o tempo normalizado da animação passou da janela definida no ScriptableObject
        return info.normalizedTime >= currentMove.comboWindowStart;
    }

    // 3. EXECUÇÃO DO ATAQUE
    private void StartComboAttack(int index)
    {
        // Consome o input do buffer
        _lastInputTime = -1f;

        _currentComboIndex = index;
        _isAttacking = true;

        // Pega dados do novo ataque
        AttackMove move = currentWeaponCombo.attacks[index];

        // A. Trava movimentação normal, mas permite gravidade
        _locomotion.SetMovementRestricted(true);

        // B. Rotação (Game Feel): Vira para o inimigo OU para o input
        AlignPlayerBeforeAttack();

        // C. Animação
        _animator.Play(move.animationTriggerName, 0, 0f); // Toca do início (Play imediato é melhor que Trigger para combos rápidos)

        // D. Impulso (Juice): Empurra o boneco levemente
        StopAllCoroutines();
        StartCoroutine(ApplyAttackImpulse(move.movementImpulse));
    }

    private void AlignPlayerBeforeAttack()
    {
        // Prioridade: Lock-On Target
        if (_locomotion.IsLockedOn && _locomotion.CurrentLockOnTarget != null)
        {
            Vector3 dir = _locomotion.CurrentLockOnTarget.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }
        // Secundário: Input de Movimento (Stick Esquerdo)
        else
        {
            Vector2 input = _playerInput.actions.FindAction("Move").ReadValue<Vector2>();
            if (input.sqrMagnitude > 0.1f)
            {
                // Converte input 2D para direção 3D relativa à câmera
                Vector3 camFwd = Camera.main.transform.forward;
                Vector3 camRight = Camera.main.transform.right;
                camFwd.y = 0; camRight.y = 0;

                Vector3 moveDir = camFwd.normalized * input.y + camRight.normalized * input.x;
                if (moveDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(moveDir);
            }
        }
    }

    // Coroutine para empurrar o personagem suavemente
    IEnumerator ApplyAttackImpulse(float force)
    {
        if (force <= 0.1f) yield break;

        float duration = 0.2f; // Impulso rápido
        float timer = 0f;
        CharacterController cc = GetComponent<CharacterController>();

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // Move para frente RELATIVO ao personagem
            cc.Move(transform.forward * force * (Time.deltaTime / duration));
            yield return null;
        }
    }

    // --- EVENTOS DE ANIMAÇÃO (Hitbox) ---
    // Mantenha seus Animation Events na Timeline da Animação para ligar o dano
    public void AnimEvent_DealDamage()
    {
        // Calcula dano baseado no multiplicador do golpe atual
        float multiplier = currentWeaponCombo.attacks[_currentComboIndex].damageMultiplier;
        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

        // Detecta colisão
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

    // IMPORTANTE: Adicione este evento no FINAL de cada animação de ataque
    public void AnimEvent_EndAttack()
    {
        // Se a animação acabou e não iniciamos outro ataque, volta pra Idle
        _isAttacking = false;
        _currentComboIndex = 0;
        _locomotion.SetMovementRestricted(false); // Libera WASD
    }

    // Segurança: Se algo der errado, reseta ao sair do estado de ataque
    void LateUpdate()
    {
        if (_isAttacking)
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            // Se a animação mudou para "Locomotion" ou "Idle" sem passar pelo evento, reseta
            if (info.IsTag("Locomotion") || info.IsTag("Idle"))
            {
                if (_isAttacking) AnimEvent_EndAttack();
            }
        }
    }
}