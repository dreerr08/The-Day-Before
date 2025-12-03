using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ControlePersonagem : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;
    private Transform cameraTransform;

    [Header("Configurações de Movimento")]
    public float velocidadeAndar = 2.0f;
    public float velocidadeCorrer = 6.0f;
    public float suavizacaoAnimacao = 0.1f;
    public float velocidadeGiro = 15.0f; // Velocidade para realinhar ao andar

    [Header("Física")]
    public float gravidade = -19.62f;

    [Header("Ajuste de Olhar (Coluna/Spine)")]
    public Transform boneTorco;
    public Vector3 offsetColuna;
    public float limiteBaixo = -45f;
    public float limiteCima = 45f;

    // Variáveis internas
    private Vector3 velocidadeVertical;

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        if (Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (boneTorco == null)
            UnityEngine.Debug.LogWarning("ATENÇÃO: Arraste o osso da coluna no Inspector!");
    }

    void Update()
    {
        MoverPersonagem();
    }

    void LateUpdate()
    {
        // Rotação da Coluna
        if (boneTorco != null && cameraTransform != null)
        {
            RotacionarColuna();
        }
    }

    void MoverPersonagem()
    {
        // 1. Inputs
        float x = Input.GetAxis("Horizontal"); // A/D
        float y = Input.GetAxis("Vertical");   // W/S
        bool shiftPressionado = Input.GetKey(KeyCode.LeftShift);

        // Verifica se o jogador está tentando se mover
        bool temInput = Mathf.Abs(x) > 0.1f || Mathf.Abs(y) > 0.1f;

        // 2. Rotação do Corpo (CONDICIONAL)
        // Só gira se tiver input. Se estiver parado, ignora e deixa a câmera orbitar livre.
        if (cameraTransform != null && temInput)
        {
            Vector3 olharMira = cameraTransform.forward;
            olharMira.y = 0;
            olharMira.Normalize();

            if (olharMira != Vector3.zero)
            {
                // Usamos Slerp para que, ao começar a andar, ele gire suavemente para a câmera
                // em vez de "snap" (teleporte de rotação)
                Quaternion rotacaoAlvo = Quaternion.LookRotation(olharMira);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeGiro * Time.deltaTime);
            }
        }

        // 3. Define Animação
        float multiplicador = shiftPressionado ? 1f : 0.5f;

        // Se soltar as teclas, zera
        if (!temInput) multiplicador = 0f;

        animator.SetFloat("InputX", x * multiplicador, suavizacaoAnimacao, Time.deltaTime);
        animator.SetFloat("InputY", y * multiplicador, suavizacaoAnimacao, Time.deltaTime);

        // 4. Movimento Físico
        Vector3 movimento = Vector3.zero;

        // Só calcula movimento físico se tiver input (evita micro-deslizes)
        if (cameraTransform != null && temInput)
        {
            Vector3 camFrente = cameraTransform.forward;
            Vector3 camDireita = cameraTransform.right;
            camFrente.y = 0; camDireita.y = 0;
            camFrente.Normalize(); camDireita.Normalize();

            movimento = (camFrente * y) + (camDireita * x);
        }

        float velocidadeAtual = shiftPressionado ? velocidadeCorrer : velocidadeAndar;
        if (movimento.magnitude > 0.1f)
        {
            controller.Move(movimento * velocidadeAtual * Time.deltaTime);
        }

        // 5. Gravidade
        if (controller.isGrounded && velocidadeVertical.y < 0) velocidadeVertical.y = -2f;
        velocidadeVertical.y += gravidade * Time.deltaTime;
        controller.Move(velocidadeVertical * Time.deltaTime);

        animator.SetBool("IsGrounded", controller.isGrounded);
    }

    void RotacionarColuna()
    {
        // ADICIONAL: Se você quiser que ele pare de olhar pra cima/baixo 
        // quando estiver de costas para a câmera, precisaria de uma lógica extra aqui.
        // Por enquanto, mantive o padrão: ele inclina a coluna baseada na altura da câmera.

        float anguloCamera = cameraTransform.eulerAngles.x;
        if (anguloCamera > 180) anguloCamera -= 360;

        float anguloDestino = anguloCamera;
        anguloDestino = Mathf.Clamp(anguloDestino, limiteBaixo, limiteCima);

        Quaternion rotacaoVertical = Quaternion.AngleAxis(anguloDestino, Vector3.right);
        Quaternion rotacaoOffset = Quaternion.Euler(offsetColuna);

        boneTorco.localRotation = boneTorco.localRotation * rotacaoVertical * rotacaoOffset;
    }
}