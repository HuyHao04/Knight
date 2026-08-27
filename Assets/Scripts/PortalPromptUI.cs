using TMPro;
using UnityEngine;

public class PortalPromptUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup promptGroup;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string message = "[E] Enter";

    private UnityEngine.Object currentOwner;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        Show(this, message);
    }

    public void Show(UnityEngine.Object owner, string overrideMessage)
    {
        currentOwner = owner;

        if (promptText != null)
        {
            promptText.text = string.IsNullOrWhiteSpace(overrideMessage)
                ? message
                : overrideMessage;
        }

        SetVisible(true);
    }

    public void Hide()
    {
        currentOwner = null;
        SetVisible(false);
    }

    public void Hide(UnityEngine.Object owner)
    {
        if (currentOwner != null && currentOwner != owner)
        {
            return;
        }

        Hide();
    }

    private void SetVisible(bool visible)
    {
        if (promptGroup == null)
        {
            return;
        }

        promptGroup.alpha = visible ? 1f : 0f;
        promptGroup.interactable = false;
        promptGroup.blocksRaycasts = false;
    }
}
