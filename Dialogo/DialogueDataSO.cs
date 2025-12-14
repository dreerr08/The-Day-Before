using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewNPCData", menuName = "Dialogue/Hub Data")]
public class DialogueDataSO : ScriptableObject
{
    // REMOVIDO: public string npcName;

    [Header("Texto Inicial")]
    [TextArea(3, 5)]
    public string greetingText = "Olá, viajante. O que deseja saber?";

    [Header("Opções de Resposta")]
    public List<DialogueOption> options;
}

[System.Serializable]
public class DialogueOption
{
    [Tooltip("Texto curto que vai no botão (Ex: 'Onde estou?')")]
    public string buttonText;

    [Tooltip("O que o NPC responde quando clica no botão.")]
    [TextArea(3, 10)]
    public string responseText;

    [Tooltip("Se marcado, o diálogo fecha após essa resposta.")]
    public bool endsDialogue;
}