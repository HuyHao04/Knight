using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelOneTutorial : MonoBehaviour
{
    private enum TutorialStep
    {
        Move,
        Jump,
        Coin,
        Attack,
        Talk,
        Complete
    }

    [Header("Scene References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform tutorialEnemy;

    [Header("Progress")]
    [SerializeField, Min(0.5f)] private float movementDistance = 2f;
    [SerializeField, Min(1f)] private float attackHintDistance = 5f;

    private TutorialStep step;
    private CanvasGroup promptGroup;
    private TextMeshProUGUI promptText;
    private Vector3 startPosition;
    private int initialCoinCount;
    private bool countdownStarted;
    private bool jumped;
    private bool attacked;
    private bool playerNearNpc;
    private bool initialized;
    private float targetAlpha;

    public bool CountdownStarted => countdownStarted;

    private void Start()
    {
        StartCoroutine(InitializeAfterSceneStart());
    }

    private IEnumerator InitializeAfterSceneStart()
    {
        yield return null;

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        CreatePromptUi();

        if (player == null)
        {
            Debug.LogWarning("LevelOneTutorial could not find the Player.", this);
            HidePrompt(true);
            countdownStarted = true;
            yield break;
        }

        if (CheckpointManager.Instance.PlayerSpawnRestoredFromReload)
        {
            CompleteImmediately();
            yield break;
        }

        startPosition = player.transform.position;
        initialCoinCount = ScoreManager.Instance.CoinCount;
        step = TutorialStep.Move;
        initialized = true;
        ShowPrompt("<b><color=#FFD45A>A / D</color></b>   MOVE");
    }

    private void Update()
    {
        UpdatePromptFade();

        if (!initialized || player == null || step == TutorialStep.Complete)
        {
            return;
        }

        bool movedThisFrame = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);
        if (movedThisFrame)
        {
            countdownStarted = true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumped = true;
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            attacked = true;
        }

        switch (step)
        {
            case TutorialStep.Move:
                if (countdownStarted
                    && Mathf.Abs(player.transform.position.x - startPosition.x) >= movementDistance)
                {
                    step = TutorialStep.Jump;
                    ShowPrompt("<b><color=#FFD45A>SPACE</color></b>   JUMP");
                }
                break;

            case TutorialStep.Jump:
                if (jumped)
                {
                    step = TutorialStep.Coin;
                    ShowPrompt("FOLLOW THE COINS");
                }
                break;

            case TutorialStep.Coin:
                if (ScoreManager.Instance.CoinCount > initialCoinCount)
                {
                    step = TutorialStep.Attack;
                    HidePrompt(false);
                }
                break;

            case TutorialStep.Attack:
                if (attacked)
                {
                    step = TutorialStep.Talk;
                    HidePrompt(false);
                }
                else if (tutorialEnemy != null
                    && Mathf.Abs(player.transform.position.x - tutorialEnemy.position.x)
                    <= attackHintDistance)
                {
                    ShowPrompt("<b><color=#FFD45A>J</color></b>   ATTACK");
                }
                break;

            case TutorialStep.Talk:
                if (playerNearNpc)
                {
                    ShowPrompt("<b><color=#FFD45A>E</color></b>   TALK");
                }
                break;
        }
    }

    public void SetNpcInRange(bool inRange)
    {
        playerNearNpc = inRange;

        if (!initialized || step == TutorialStep.Complete)
        {
            return;
        }

        if (inRange)
        {
            step = TutorialStep.Talk;
            ShowPrompt("<b><color=#FFD45A>E</color></b>   TALK");
        }
        else if (step == TutorialStep.Talk)
        {
            HidePrompt(false);
        }
    }

    public void NotifyNpcInteraction()
    {
        if (!initialized)
        {
            return;
        }

        step = TutorialStep.Complete;
        HidePrompt(false);
    }

    private void CompleteImmediately()
    {
        countdownStarted = true;
        step = TutorialStep.Complete;
        initialized = true;
        HidePrompt(true);
    }

    private void CreatePromptUi()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject promptObject = new GameObject(
            "Level1TutorialPrompt",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        promptObject.transform.SetParent(canvas.transform, false);

        RectTransform promptRect = promptObject.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 70f);
        promptRect.sizeDelta = new Vector2(520f, 72f);

        Image background = promptObject.GetComponent<Image>();
        background.color = new Color(0.035f, 0.045f, 0.065f, 0.88f);
        background.raycastTarget = false;

        promptGroup = promptObject.GetComponent<CanvasGroup>();
        promptGroup.alpha = 0f;
        promptGroup.interactable = false;
        promptGroup.blocksRaycasts = false;

        GameObject textObject = new GameObject(
            "PromptText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(promptObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(22f, 8f);
        textRect.offsetMax = new Vector2(-22f, -8f);

        promptText = textObject.GetComponent<TextMeshProUGUI>();
        promptText.font = TMP_Settings.defaultFontAsset;
        promptText.fontSize = 28f;
        promptText.enableAutoSizing = true;
        promptText.fontSizeMin = 18f;
        promptText.fontSizeMax = 28f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.raycastTarget = false;
    }

    private void ShowPrompt(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
        }

        targetAlpha = 1f;
    }

    private void HidePrompt(bool immediately)
    {
        targetAlpha = 0f;
        if (immediately && promptGroup != null)
        {
            promptGroup.alpha = 0f;
        }
    }

    private void UpdatePromptFade()
    {
        if (promptGroup == null)
        {
            return;
        }

        promptGroup.alpha = Mathf.MoveTowards(
            promptGroup.alpha,
            targetAlpha,
            Time.unscaledDeltaTime * 6f);
    }
}
