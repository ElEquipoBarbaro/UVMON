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

    [SerializeField] private RectTransform dialogBox;
    [SerializeField] private Image characterPhoto;
    [SerializeField] private TextMeshProUGUI characterName;
    [SerializeField] private TextMeshProUGUI dialogArea;

    private void Awake()
    {
        Instance = this;
        HideDialogBox();
    }

    public void StartDialogue(DialogueRound dialogue)
    {
        dialogueTurnsQueue = new Queue<DialogueTurn>(dialogue.DialogueTurnsList);
        StartCoroutine(DialogueCoroutine());
    }

    public void RequestAdvance()
    {
        advanceRequested = true;
    }

    private IEnumerator DialogueCoroutine()
    {
        IsDialogueActive = true;
        ShowDialogBox();

        while (dialogueTurnsQueue.Count > 0)
        {
            var currentTurn = dialogueTurnsQueue.Dequeue();
            SetCharacterInfo(currentTurn.Character);
            ClearDialogArea();
            dialogArea.text = currentTurn.DialogueLine;

            advanceRequested = false;
            yield return new WaitUntil(() => advanceRequested);
            yield return null;
        }

        IsDialogueActive = false;
        HideDialogBox();
    }

    public void ShowDialogBox()
    {
        dialogBox.gameObject.SetActive(true);
    }

    public void HideDialogBox()
    {
        dialogBox.gameObject.SetActive(false);
    }

    public void SetCharacterInfo(DialogueCharacter character)
    {
        if (character == null) return;
        characterPhoto.sprite = character.ProfilePhoto;
        characterName.text = character.Name;
    }

    public void ClearDialogArea()
    {
        dialogArea.text = string.Empty;
    }
}