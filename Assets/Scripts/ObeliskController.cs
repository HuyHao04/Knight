using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ObeliskController : MonoBehaviour
{
    public enum ObeliskState
    {
        Inactive = 0,
        Ready = 1,
        Activated = 2,
        Firing = 3
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform beamOrigin;
    [SerializeField] private PortalPromptUI promptUI;

    [Header("Animation")]
    [SerializeField] private string stateParameter = "State";

    [Header("Optional Audio")]
    [SerializeField] private AudioClip activationSfx;

    private ObeliskManager manager;
    private PlayerController playerInRange;
    private int playerOverlapCount;
    private ObeliskState currentState = ObeliskState.Inactive;

    public ObeliskState CurrentState => currentState;
    public Transform BeamOrigin => beamOrigin != null ? beamOrigin : transform;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        BoxCollider2D interactionTrigger = GetComponent<BoxCollider2D>();
        interactionTrigger.isTrigger = true;

        SetState(ObeliskState.Inactive);
    }

    private void Update()
    {
        if (currentState != ObeliskState.Ready
            || manager == null
            || !manager.WindowOpen
            || playerInRange == null)
        {
            return;
        }

        if (InteractionInput.TryConsumeInteract())
        {
            Activate();
        }
    }

    public void Initialize(ObeliskManager owner, PortalPromptUI sharedPrompt)
    {
        manager = owner;

        if (sharedPrompt != null)
        {
            promptUI = sharedPrompt;
        }

        RefreshPrompt();
    }

    public void SetState(ObeliskState nextState)
    {
        currentState = nextState;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetInteger(stateParameter, (int)currentState);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        RefreshPrompt();
    }

    private void Activate()
    {
        if (currentState != ObeliskState.Ready || manager == null || !manager.WindowOpen)
        {
            return;
        }

        SetState(ObeliskState.Activated);

        if (activationSfx != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(activationSfx);
        }

        manager.NotifyActivated(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || !player.CompareTag("Player"))
        {
            return;
        }

        if (playerInRange == null)
        {
            playerInRange = player;
        }

        if (player != playerInRange)
        {
            return;
        }

        playerOverlapCount++;
        RefreshPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player != playerInRange)
        {
            return;
        }

        playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);
        if (playerOverlapCount == 0)
        {
            promptUI?.Hide(this);
            playerInRange = null;
        }
    }

    private void OnDisable()
    {
        promptUI?.Hide(this);
        playerInRange = null;
        playerOverlapCount = 0;
    }

    private void RefreshPrompt()
    {
        bool shouldShow = currentState == ObeliskState.Ready
            && manager != null
            && manager.WindowOpen
            && playerInRange != null
            && playerOverlapCount > 0;

        if (shouldShow)
        {
            promptUI?.Show(this, "[E] Activate");
        }
        else
        {
            promptUI?.Hide(this);
        }
    }
}
