using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [Header("Speaker Name")]
        public string speaker;

        [Header("Dialogue Text")]
        [TextArea(2, 5)]
        public string text;
    }

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button nextButton;
    [Tooltip("Optional. Leave empty when the next button uses an icon instead of text.")]
    [SerializeField] private TextMeshProUGUI nextButtonLabel;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;

    [Header("Dialogue Content")]
    [SerializeField] private DialogueLine[] dialogueLines;

    private int currentLine = 0;
    private bool isDialogueActive = false;
    private DialogueLine[] activeDialogueLines;
    private Action dialogueCompleted;
    private bool releasePlayerOnEnd = true;
    private bool isUiReady;

    private void Awake()
    {
        if (nextButtonLabel == null && nextButton != null)
        {
            nextButtonLabel = nextButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        isUiReady = dialoguePanel != null
            && nameText != null
            && dialogueText != null
            && nextButton != null;

        if (!isUiReady)
        {
            Debug.LogError(
                "DialogueManager is missing one or more required UI references.",
                this);
            return;
        }

        dialoguePanel.SetActive(false);

        // Only manage this component's listener. Inspector or other runtime listeners remain intact.
        nextButton.onClick.RemoveListener(NextDialogue);
        nextButton.onClick.AddListener(NextDialogue);
    }

    private void OnDestroy()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextDialogue);
        }
    }

    // =========================================================
    // START DIALOGUE
    // =========================================================

    public void StartDialogue()
    {
        StartDialogue(dialogueLines, null, true);
    }

    public bool StartDialogue(
        DialogueLine[] lines,
        Action onCompleted,
        bool releaseControlWhenFinished = true)
    {
        if (!isUiReady)
        {
            Debug.LogError("DialogueManager cannot start because its UI is not ready.", this);
            return false;
        }

        if (isDialogueActive)
            return false;

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("DialogueManager: Chưa có dialogue!");
            return false;
        }

        activeDialogueLines = lines;
        dialogueCompleted = onCompleted;
        releasePlayerOnEnd = releaseControlWhenFinished;
        isDialogueActive = true;
        currentLine = 0;

        // Hiện Dialogue Panel
        dialoguePanel.SetActive(true);

        // Khóa Player
        if (playerController != null)
        {
            playerController.SetControlEnabled(false);
        }

        // Hiển thị câu đầu tiên
        ShowCurrentDialogue();
        return true;
    }

    // =========================================================
    // SHOW CURRENT DIALOGUE
    // =========================================================

    private void ShowCurrentDialogue()
    {
        if (activeDialogueLines == null
            || currentLine < 0
            || currentLine >= activeDialogueLines.Length)
            return;

        nameText.text = activeDialogueLines[currentLine].speaker;
        dialogueText.text = activeDialogueLines[currentLine].text;

        // Icon-only buttons intentionally have no label. Text buttons can still show NEXT / END.
        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = currentLine == activeDialogueLines.Length - 1
                ? "END"
                : "NEXT";
        }
    }

    // =========================================================
    // NEXT DIALOGUE
    // =========================================================

    public void NextDialogue()
    {
        if (!isDialogueActive)
            return;

        currentLine++;

        // Nếu vẫn còn câu thoại
        if (activeDialogueLines != null && currentLine < activeDialogueLines.Length)
        {
            ShowCurrentDialogue();
        }
        else
        {
            EndDialogue();
        }
    }

    // =========================================================
    // END DIALOGUE
    // =========================================================

    private void EndDialogue()
    {
        isDialogueActive = false;

        // Ẩn Dialogue Panel
        dialoguePanel.SetActive(false);

        // Cho Player hoạt động lại
        if (releasePlayerOnEnd && playerController != null)
        {
            playerController.SetControlEnabled(true);
        }

        Action completedCallback = dialogueCompleted;
        dialogueCompleted = null;
        activeDialogueLines = null;
        releasePlayerOnEnd = true;

        completedCallback?.Invoke();

        Debug.Log("Dialogue finished.");
    }

    // =========================================================
    // CHECK DIALOGUE STATUS
    // =========================================================

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}
