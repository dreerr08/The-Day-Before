using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SphereCollider))] // Ou BoxCollider
public class ItemPickup : MonoBehaviour
{
    [Header("Dados (Preenchidos automaticamente)")]
    public ItemData itemData;
    public int quantity = 1;

    [Header("Configuração Visual")]
    public float verticalBobSpeed = 2f;  // Velocidade de flutuar
    public float verticalBobAmount = 0.1f; // Altura do flutuar

    private SpriteRenderer _spriteRenderer;
    private Vector3 _startPosition;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        // Garante que o collider seja Trigger para não chutar o item como uma bola de futebol
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        _startPosition = transform.position;
        UpdateVisuals();
    }

    // Método chamado pelo inimigo na hora que o item nasce
    public void Initialize(ItemData data, int amount)
    {
        itemData = data;
        quantity = amount;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (itemData != null && _spriteRenderer != null)
        {
            _spriteRenderer.sprite = itemData.icon;
        }
    }

    void Update()
    {
        // Efeito visual simples de flutuar (Minecraft Style)
        float newY = _startPosition.y + (Mathf.Sin(Time.time * verticalBobSpeed) * verticalBobAmount);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        // Verifica se foi o Player que tocou
        if (other.CompareTag("Player"))
        {
            // Tenta pegar o inventário do player
            InventorySystem inventory = other.GetComponent<InventorySystem>();

            if (inventory != null)
            {
                // Tenta adicionar
                bool wasPickedUp = inventory.AddItem(itemData, quantity);

                if (wasPickedUp)
                {
                    // Toca um som aqui se quiser (ex: AudioSource.PlayClipAtPoint...)
                    UnityEngine.Debug.Log($"Pegou: {quantity}x {itemData.itemName}");
                    Destroy(gameObject);
                }
            }
        }
    }
}