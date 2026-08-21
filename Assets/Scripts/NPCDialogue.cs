using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Manager")]
    [SerializeField] private DialogueManager dialogueManager;

    private bool hasTalked = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Chỉ Player mới kích hoạt dialogue
        if (!collision.CompareTag("Player"))
            return;

        // NPC chỉ nói một lần
        if (hasTalked)
            return;

        // Kiểm tra DialogueManager
        if (dialogueManager == null)
        {
            Debug.LogWarning(
                "NPCDialogue: Chưa kéo DialogueManager vào Inspector!"
            );

            return;
        }

        hasTalked = true;

        // Bắt đầu dialogue
        dialogueManager.StartDialogue();
    }
}