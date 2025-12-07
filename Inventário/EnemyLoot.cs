using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class EnemyLoot : MonoBehaviour
{
    [Header("O Que Dropar")]
    public GameObject lootPrefab; // <--- O PREFAB GENÉRICO DO ITEM (Arraste aqui)
    public ItemData itemToDrop;

    [Header("Probabilidade")]
    [Range(0f, 100f)]
    public float dropChance = 50.0f;
    [Range(1, 5)]
    public int dropAmount = 1;

    [Header("Física do Drop")]
    public float dropUpForce = 3.0f; // Pulinho para cima ao spawnar

    private HealthComponent _healthComponent;
    private bool _hasDropped = false;

    void Awake()
    {
        _healthComponent = GetComponent<HealthComponent>();
    }

    void OnEnable()
    {
        if (_healthComponent != null)
            _healthComponent.OnDeath.AddListener(TryDropLoot);
    }

    void OnDisable()
    {
        if (_healthComponent != null)
            _healthComponent.OnDeath.RemoveListener(TryDropLoot);
    }

    void TryDropLoot()
    {
        if (_hasDropped) return;
        _hasDropped = true;

        if (itemToDrop == null || lootPrefab == null) return;

        float roll = UnityEngine.Random.Range(0f, 100f);

        if (roll <= dropChance)
        {
            SpawnItem();
        }
    }

    void SpawnItem()
    {
        // ANTES:
        // Vector3 spawnPos = transform.position + Vector3.up * 1.0f; 

        // DEPOIS (Correção): 
        // Usamos 0.2f (20cm) apenas para garantir que não atravesse o chão, 
        // mas fique visualmente "no pé" do inimigo.
        Vector3 spawnPos = transform.position + Vector3.up * 0.05f;

        GameObject droppedItem = Instantiate(lootPrefab, spawnPos, Quaternion.identity);

        // ... o resto continua igual ...
        ItemPickup pickupScript = droppedItem.GetComponent<ItemPickup>();
        if (pickupScript != null)
        {
            pickupScript.Initialize(itemToDrop, dropAmount);
        }
    }
}