using UnityEngine;

[CreateAssetMenu(fileName = "NewMemory", menuName = "Inventory/Memory Data")]
public class MemoryItemData : ItemData
{
    [Header("Conteúdo da Memória")]
    [Tooltip("Data ou Era de quando isso aconteceu. Ex: 'Ano 1024, Era das Cinzas'")]
    public string memoryDate;

    [TextArea(10, 20)] // Cria uma caixa de texto bem grande no Inspector
    [Tooltip("O texto completo da história/diário que o jogador vai ler.")]
    public string loreContent;

    [Header("Visual Especial")]
    [Tooltip("Cor de destaque para o texto na UI (Opcional)")]
    public Color fragmentColor = Color.cyan;

    private void OnEnable()
    {
        // Força configurações padrão para Memórias
        type = ItemType.Key; // Consideramos itens chave/história
        maxStack = 1;        // Memórias nunca empilham
    }
}