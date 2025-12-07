using UnityEngine;
using System.Collections.Generic;

namespace Systems.Dialogue
{
    [CreateAssetMenu(fileName = "NewTopic", menuName = "Dialogue/Topic")]
    public class DialogueTopicSO : ScriptableObject
    {
        [Header("Menu")]
        public string topicTitle = "Sobre o Castelo"; // O que aparece no botão

        [Header("Conteúdo")]
        [TextArea(3, 10)]
        public string[] dialogueLines; // O que o NPC fala (pode ser várias páginas)

        [Header("Regras de Acesso")]
        // Arraste aqui seus ScriptableObjects de condição (Ex: "TemItemChave")
        // Se a lista estiver vazia, o tópico aparece sempre.
        public List<DialogueCondition> requirements;

        // Função auxiliar para verificar todas as condições de uma vez
        public bool CanShowTopic(GameObject player)
        {
            foreach (var condition in requirements)
            {
                // Se UMA falhar, o tópico todo é escondido
                if (condition != null && !condition.CanAccess(player))
                {
                    return false;
                }
            }
            return true; // Passou em tudo!
        }
    }
}