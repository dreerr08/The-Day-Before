using UnityEngine;

public class CameraSeguidora : MonoBehaviour
{
    public Transform alvo; // O Personagem

    [Header("Configurações de Mouse")]
    public float sensibilidadeMouse = 3.0f;
    public Vector2 limitesAngulo = new Vector2(-40, 85);

    [Header("Posicionamento")]
    // X=Lado, Y=Altura, Z=Distância
    public Vector3 offsetPadrao = new Vector3(0.5f, 1.8f, -3.0f);
    public float suavizacao = 10.0f;

    [Header("Colisão (Anti-Parede)")]
    public LayerMask camadasColisao; // Marque 'Default', 'Cenario', etc.
    public float raioColisao = 0.2f; // Tamanho da bolinha que checa paredes

    // Variáveis internas
    private float rotacaoX = 0.0f;
    private float rotacaoY = 0.0f;
    private Vector3 offsetAtual;

    void Start()
    {
        // Trava o mouse no centro
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        offsetAtual = offsetPadrao;

        // Configura camadas padrão se você esquecer
        if (camadasColisao == 0)
            camadasColisao = LayerMask.GetMask("Default", "Terrain", "Cenario");
    }

    void LateUpdate()
    {
        if (!alvo) return;

        // 1. Input do Mouse
        rotacaoX += Input.GetAxis("Mouse X") * sensibilidadeMouse;
        rotacaoY -= Input.GetAxis("Mouse Y") * sensibilidadeMouse;
        rotacaoY = Mathf.Clamp(rotacaoY, limitesAngulo.x, limitesAngulo.y);

        // 2. Calcula Rotação da Câmera
        Quaternion rotacaoFinal = Quaternion.Euler(rotacaoY, rotacaoX, 0);

        // 3. Calcula onde a câmera QUER estar (sem paredes)
        // Usamos um pivô no ombro do personagem para o raio não sair do pé
        Vector3 pivoPersonagem = alvo.position + Vector3.up * 1.5f;
        Vector3 posicaoDesejada = pivoPersonagem + (rotacaoFinal * offsetPadrao);

        // 4. Sistema de Colisão (SphereCast)
        Vector3 posicaoFinal = posicaoDesejada;
        Vector3 direcao = posicaoDesejada - pivoPersonagem;
        float distancia = direcao.magnitude;

        // Lança um raio do personagem em direção à câmera
        if (Physics.SphereCast(pivoPersonagem, raioColisao, direcao.normalized, out RaycastHit hit, distancia, camadasColisao))
        {
            // Se bateu na parede, puxa a câmera para o ponto de impacto
            posicaoFinal = pivoPersonagem + (direcao.normalized * hit.distance);
        }

        // 5. Aplica movimento suave
        transform.position = Vector3.Lerp(transform.position, posicaoFinal, Time.deltaTime * suavizacao);

        // 6. Faz a câmera olhar para frente do pivô (mantendo o personagem em foco)
        transform.LookAt(pivoPersonagem + (rotacaoFinal * Vector3.forward * 5f));
    }
}