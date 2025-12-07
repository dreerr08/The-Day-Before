using UnityEngine;

namespace Systems.Dialogue
{
    // "abstract" significa que esta classe não pode ser usada diretamente.
    // Ela serve apenas de modelo para outras (Herança).
    public abstract class DialogueCondition : ScriptableObject
    {
        [TextArea]
        public string developerDescription; // Apenas para você lembrar o que isso faz no Inspector

        // Este é o método mágico. Cada condição vai implementar sua própria lógica aqui.
        // Retorna TRUE se o requisito for cumprido.
        public abstract bool CanAccess(GameObject player);
    }
}