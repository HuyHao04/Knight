using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Player")]
    [SerializeField] private PlayerController playerController;

    [Header("Dialogue Content")]
    [SerializeField] private DialogueLine[] dialogueLines;

    private int currentLine = 0;
    private bool isDialogueActive = false;

    private void Start()
    {
        // Ẩn dialogue khi bắt đầu game
        dialoguePanel.SetActive(false);

        // Đảm bảo Button không bị đăng ký nhiều lần
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(NextDialogue);
    }

    // =========================================================
    // START DIALOGUE
    // =========================================================

    public void StartDialogue()
    {
        if (isDialogueActive)
            return;

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning("DialogueManager: Chưa có dialogue!");
            return;
        }

        isDialogueActive = true;
        currentLine = 0;

        // Hiện Dialogue Panel
        dialoguePanel.SetActive(true);

        // Khóa Player
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Hiển thị câu đầu tiên
        ShowCurrentDialogue();
    }

    // =========================================================
    // SHOW CURRENT DIALOGUE
    // =========================================================

    private void ShowCurrentDialogue()
    {
        if (currentLine < 0 || currentLine >= dialogueLines.Length)
            return;

        nameText.text = dialogueLines[currentLine].speaker;
        dialogueText.text = dialogueLines[currentLine].text;

        // Nếu là câu cuối thì có thể đổi chữ Button
        if (currentLine == dialogueLines.Length - 1)
        {
            nextButton.GetComponentInChildren<TextMeshProUGUI>().text = "END";
        }
        else
        {
            nextButton.GetComponentInChildren<TextMeshProUGUI>().text = "NEXT";
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
        if (currentLine < dialogueLines.Length)
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
        if (playerController != null)
        {
            playerController.enabled = true;
        }

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