using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    private Queue<DialogueTurn> dialogueTurnsQueue;

    public bool IsDialogueActive { get; private set; }
    private bool advanceRequested = false;
    public event System.Action OnDialogueEnded;

    [SerializeField] private RectTransform dialogBox;
    [SerializeField] private Image characterPhoto;
    [SerializeField] private TextMeshProUGUI characterName;
    [SerializeField] private TextMeshProUGUI dialogArea;
    [SerializeField] private TextMeshProUGUI characterRole;
    [SerializeField] private TextMeshProUGUI dialogueProgress;
    [SerializeField] private TextMeshProUGUI advanceButtonLabel;

    private Coroutine activeDialogue;
    private int currentTurnIndex;
    private int totalTurns;
    private Button advanceButton;
private void Awake()
    {
        Instance = this;

        if (dialogBox != null)
        {
            var buttons = dialogBox.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name == "AdvanceButton")
                {
                    advanceButton = button;
                    break;
                }
            }

            if (advanceButton == null && buttons.Length > 0)
                advanceButton = buttons[0];

            characterRole = characterRole != null ? characterRole : FindTextElement("SpeakerRole");
            dialogueProgress = dialogueProgress != null ? dialogueProgress : FindTextElement("ProgressText");
            advanceButtonLabel = advanceButtonLabel != null ? advanceButtonLabel : FindTextElement("AdvanceLabel");
        }

        if (advanceButton != null)
            advanceButton.onClick.AddListener(RequestAdvance);

        HideDialogBox();
    }

    private void OnDestroy()
    {
        if (advanceButton != null)
            advanceButton.onClick.RemoveListener(RequestAdvance);

        if (Instance == this)
            Instance = null;
    }


public void StartDialogue(DialogueRound dialogue)
    {
        if (dialogue == null || dialogue.DialogueTurnsList == null || dialogue.DialogueTurnsList.Count == 0)
        {
            Debug.LogWarning("No se pudo iniciar el diálogo porque no contiene intervenciones.", this);
            return;
        }

        if (activeDialogue != null)
            StopCoroutine(activeDialogue);

        dialogueTurnsQueue = new Queue<DialogueTurn>(dialogue.DialogueTurnsList);
        totalTurns = dialogueTurnsQueue.Count;
        currentTurnIndex = 0;
        activeDialogue = StartCoroutine(DialogueCoroutine());
    }

public void RequestAdvance()
    {
        if (IsDialogueActive)
            advanceRequested = true;
    }

private IEnumerator DialogueCoroutine()
    {
        IsDialogueActive = true;
        ShowDialogBox();

        while (dialogueTurnsQueue.Count > 0)
        {
            var currentTurn = dialogueTurnsQueue.Dequeue();
            currentTurnIndex++;

            SetCharacterInfo(currentTurn.Character);
            ClearDialogArea();

            if (dialogArea != null)
                dialogArea.text = currentTurn.DialogueLine;

            UpdateNavigationState(dialogueTurnsQueue.Count == 0);

            advanceRequested = false;
            yield return new WaitUntil(() => advanceRequested);
            yield return null;
        }

        IsDialogueActive = false;
        activeDialogue = null;
        HideDialogBox();
        OnDialogueEnded?.Invoke();
    }

public void ShowDialogBox()
    {
        if (dialogBox != null)
            dialogBox.gameObject.SetActive(true);
    }

public void HideDialogBox()
    {
        if (dialogBox != null)
            dialogBox.gameObject.SetActive(false);
    }

public void SetCharacterInfo(DialogueCharacter character)
    {
        if (character == null)
            return;

        if (characterPhoto != null)
        {
            characterPhoto.sprite = character.ProfilePhoto;
            characterPhoto.enabled = character.ProfilePhoto != null;
            characterPhoto.preserveAspect = true;
        }

        if (characterName != null)
            characterName.text = character.Name;

        if (characterRole != null)
            characterRole.text = GetCharacterRole(character.Name);
    }

public void ClearDialogArea()
    {
        if (dialogArea != null)
            dialogArea.text = string.Empty;
    }


private void UpdateNavigationState(bool isLastTurn)
    {
        if (dialogueProgress != null)
            dialogueProgress.text = currentTurnIndex + " / " + totalTurns;

        if (advanceButtonLabel != null)
            advanceButtonLabel.text = isLastTurn ? "Cerrar" : "Siguiente";

        if (advanceButton != null && advanceButton.targetGraphic != null)
        {
            advanceButton.targetGraphic.color = isLastTurn
                ? new Color32(136, 242, 195, 255)
                : new Color32(153, 226, 249, 255);
        }
    }

    private TextMeshProUGUI FindTextElement(string objectName)
    {
        if (dialogBox == null)
            return null;

        var labels = dialogBox.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var label in labels)
        {
            if (label.name == objectName)
                return label;
        }

        return null;
    }

    private static string GetCharacterRole(string speakerName)
    {
        if (string.IsNullOrWhiteSpace(speakerName))
            return "Habitante del campus";

        if (speakerName.IndexOf("araña", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "UVGmon salvaje";

        if (speakerName.IndexOf("maria", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Entrenadora del campus";

        if (speakerName == "Usuario" || speakerName == "Tu")
            return "Entrenador UVGmon";

        return "Habitante del campus";
    }
}