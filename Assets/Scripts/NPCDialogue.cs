using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Manager")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private bool requireInteractKey;

    private bool hasTalked = false;
    private bool playerInRange;
    private LevelOneTutorial levelOneTutorial;

    private void Awake()
    {
        levelOneTutorial = FindAnyObjectByType<LevelOneTutorial>(
            FindObjectsInactive.Include
        );
    }

    private void Update()
    {
        if (!requireInteractKey || !playerInRange || hasTalked)
        {
            return;
        }

        if (InteractionInput.TryConsumeInteract())
        {
            BeginDialogue();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Chỉ Player mới kích hoạt dialogue
        if (!collision.CompareTag("Player"))
            return;

        if (hasTalked)
            return;

        if (requireInteractKey)
        {
            playerInRange = true;
            levelOneTutorial?.SetNpcInRange(true);
            return;
        }

        BeginDialogue();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!requireInteractKey || !collision.CompareTag("Player"))
        {
            return;
        }

        playerInRange = false;
        levelOneTutorial?.SetNpcInRange(false);
    }

    private void BeginDialogue()
    {
        if (hasTalked)
        {
            return;
        }

        if (dialogueManager == null)
        {
            Debug.LogWarning(
                "NPCDialogue: Chưa kéo DialogueManager vào Inspector!"
            );

            return;
        }

        hasTalked = true;
        playerInRange = false;
        levelOneTutorial?.NotifyNpcInteraction();

        dialogueManager.StartDialogue();
    }
}
