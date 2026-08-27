using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BossIntroTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private NecromancerBoss necromancerBoss;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float combatStartDelay = 0.35f;

    private bool hasTriggered;
    private bool introCompleted;
    private PlayerController player;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || introCompleted)
        {
            return;
        }

        PlayerController enteringPlayer = other.GetComponentInParent<PlayerController>();
        if (enteringPlayer == null || !enteringPlayer.CompareTag("Player"))
        {
            return;
        }

        if (dialogueManager == null || necromancerBoss == null)
        {
            Debug.LogError("BossIntroTrigger is missing DialogueManager or NecromancerBoss.", this);
            return;
        }

        hasTriggered = true;
        player = enteringPlayer;
        player.SetControlEnabled(false);
        player.FaceTowards(necromancerBoss.transform.position.x);

        bool started = dialogueManager.StartDialogue(
            CreateDialogueLines(),
            OnDialogueCompleted,
            false);

        if (!started)
        {
            hasTriggered = false;
            player.SetControlEnabled(true);
            player = null;
        }
    }

    private void OnDialogueCompleted()
    {
        if (!introCompleted)
        {
            StartCoroutine(StartCombatRoutine());
        }
    }

    private IEnumerator StartCombatRoutine()
    {
        yield return new WaitForSecondsRealtime(combatStartDelay);

        introCompleted = true;
        necromancerBoss.StartCombat();

        if (player != null)
        {
            player.SetControlEnabled(true);
        }
    }

    private static DialogueManager.DialogueLine[] CreateDialogueLines()
    {
        return new[]
        {
            CreateLine(
                "Malakor",
                "A knight? Sent by those fools in the valley? You've cut through my servants only to walk willingly into my sanctum."),
            CreateLine(
                "Knight",
                "Your servants? Those corpses outside were once people. And now you're murdering innocent villagers for your twisted ritual."),
            CreateLine(
                "Malakor",
                "Murdering? No... I am transcending death itself. The dead serve me, and the living provide the essence I need. Why should such power be wasted beneath the earth?"),
            CreateLine(
                "Knight",
                "You haven't conquered death, Malakor. You've surrounded yourself with it because you're terrified of joining the dead."),
            CreateLine(
                "Malakor",
                "Fear? I command death! Flesh withers, kingdoms fall, but my necromancy will endure. And when I take your life force, Knight, I will be one step closer to eternity!"),
            CreateLine(
                "Knight",
                "Then let's see if your immortality survives my blade.")
        };
    }

    private static DialogueManager.DialogueLine CreateLine(string speaker, string text)
    {
        return new DialogueManager.DialogueLine
        {
            speaker = speaker,
            text = text
        };
    }
}
