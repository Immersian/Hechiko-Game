using Ink.Runtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using SupanthaPaul;

public class DialogueManager : MonoBehaviour
{
    [Header("Left Dialogue UI")]
    [SerializeField] private GameObject leftDialogueCanvas;
    [SerializeField] private TextMeshProUGUI leftDialogueText;
    [SerializeField] private GameObject[] leftChoiceButtons;

    [Header("Right Dialogue UI")]
    [SerializeField] private GameObject rightDialogueCanvas;
    [SerializeField] private TextMeshProUGUI rightDialogueText;
    [SerializeField] private GameObject[] rightChoiceButtons;

    [Header("Params")]
    [SerializeField] private float typingSpeed = 0.04f;

    private Story currentStory;
    public bool dialogueIsPlaying { get; private set; }
    private bool canContinueToNextLine = false;
    private Coroutine displayLineCoroutine;
    private Dictionary<string, int> interactionCounts = new Dictionary<string, int>();

    // Current active UI elements
    private GameObject currentDialogueCanvas;
    private TextMeshProUGUI currentDialogueText;
    private GameObject[] currentChoiceButtons;
    private TextMeshProUGUI[] currentChoicesText;

    private void Awake()
    {
        // Initialize choices text arrays
        InitializeChoiceButtons(leftChoiceButtons, out _);
        InitializeChoiceButtons(rightChoiceButtons, out _);

        // Ensure canvases are disabled at start
        leftDialogueCanvas?.SetActive(false);
        rightDialogueCanvas?.SetActive(false);
    }

    private void InitializeChoiceButtons(GameObject[] buttons, out TextMeshProUGUI[] choicesText)
    {
        choicesText = new TextMeshProUGUI[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            choicesText[i] = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
        }
    }
    private void Update()
    {
        // Only handle input when dialogue is playing
        if (dialogueIsPlaying)
        {
            HandleContinueClick();
        }
    }
    public void StartDialogue(TextAsset inkJSON, string npcID, bool playerIsLeft)
    {
        // Set up the appropriate UI based on player position
        if (playerIsLeft)
        {
            currentDialogueCanvas = rightDialogueCanvas;
            currentDialogueText = rightDialogueText;
            currentChoiceButtons = rightChoiceButtons;
        }
        else
        {
            currentDialogueCanvas = leftDialogueCanvas;
            currentDialogueText = leftDialogueText;
            currentChoiceButtons = leftChoiceButtons;
        }

        // Initialize choices text
        currentChoicesText = new TextMeshProUGUI[currentChoiceButtons.Length];
        for (int i = 0; i < currentChoiceButtons.Length; i++)
        {
            currentChoicesText[i] = currentChoiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        }

        // Initialize interaction count
        if (!interactionCounts.ContainsKey(npcID))
        {
            interactionCounts[npcID] = 0;
        }
        interactionCounts[npcID]++;

        // Disable player movement
        PlayerController player = FindObjectOfType<PlayerController>();
        player?.DisableMovement();

        InputManager.instance.SetGameplayInputEnabled(false);

        // Start the story
        currentStory = new Story(inkJSON.text);
        currentStory.variablesState["interaction_count"] = interactionCounts[npcID];

        dialogueIsPlaying = true;
        currentDialogueCanvas.SetActive(true);
        ContinueStory();
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            if (displayLineCoroutine != null)
            {
                StopCoroutine(displayLineCoroutine);
            }

            displayLineCoroutine = StartCoroutine(DisplayLine(currentStory.Continue()));
        }
        else
        {
            ExitDialogueMode();
        }
    }

    private IEnumerator DisplayLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            ContinueStory();
            yield break;
        }

        currentDialogueText.text = "";
        canContinueToNextLine = false;
        HideChoices();

        foreach (char letter in line.ToCharArray())
        {
            currentDialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        DisplayChoices();
        canContinueToNextLine = true;
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        if (currentChoices.Count > 0)
        {
            InputManager.instance.SetDialogueInputEnabled(false);

            for (int i = 0; i < currentChoices.Count; i++)
            {
                if (i < currentChoiceButtons.Length)
                {
                    currentChoiceButtons[i].SetActive(true);
                    currentChoicesText[i].text = currentChoices[i].text;
                }
                else
                {
                    Debug.LogWarning("More choices were given in the Ink story than UI buttons available");
                    break;
                }
            }

            for (int i = currentChoices.Count; i < currentChoiceButtons.Length; i++)
            {
                currentChoiceButtons[i].SetActive(false);
            }

            StartCoroutine(SelectFirstChoice());
        }
        else
        {
            HideChoices();
            InputManager.instance.SetDialogueInputEnabled(true);

            // Enable continue-on-click when there are no choices
            canContinueToNextLine = true;
        }
    }

    private IEnumerator SelectFirstChoice()
    {
        yield return null;
        if (currentChoiceButtons.Length > 0 && currentChoiceButtons[0].activeInHierarchy)
        {
            currentChoiceButtons[0].GetComponent<Button>().Select();
        }
    }

    private void HideChoices()
    {
        foreach (GameObject choiceButton in currentChoiceButtons)
        {
            choiceButton?.SetActive(false);
        }
    }

    public void MakeChoice(int choiceIndex)
    {
        if (canContinueToNextLine)
        {
            currentStory.ChooseChoiceIndex(choiceIndex);
            ContinueStory();
        }
    }
    private void HandleContinueClick()
    {
        if (InputManager.instance.inputControl.Dialogue.Interact.WasPressedThisFrame() &&
            canContinueToNextLine &&
            currentStory.currentChoices.Count == 0)
        {
            ContinueStory();
        }
    }
    private void ExitDialogueMode()
    {
        dialogueIsPlaying = false;
        currentDialogueCanvas?.SetActive(false);
        currentDialogueText.text = "";
        InputManager.instance.SetGameplayInputEnabled(true);

        PlayerController player = FindObjectOfType<PlayerController>();
        player?.EnableMovement();
    }
}