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
    public bool isTyping = false;
    private bool isDisplayingChoices = false;

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

        // Reset input state to prevent immediate skipping
        ResetInputState();

        ContinueStory();
    }

    private void ResetInputState()
    {
        // Clear any existing input that might cause immediate skipping
        canContinueToNextLine = false;
        isTyping = false;
        isDisplayingChoices = false;

        // Reset input system
        InputManager.instance.inputControl.Dialogue.Interact.Reset();
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            if (displayLineCoroutine != null)
            {
                StopCoroutine(displayLineCoroutine);
            }

            // Reset states for new line
            canContinueToNextLine = false;
            isTyping = false;
            isDisplayingChoices = false;
            HideChoices();

            displayLineCoroutine = StartCoroutine(DisplayLine(currentStory.Continue()));
        }
        else if (currentStory.currentChoices.Count > 0)
        {
            DisplayChoices();
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
        isTyping = true;
        HideChoices();

        string fullText = line;
        bool skipped = false;

        // Small delay before starting to type to prevent immediate skip
        yield return new WaitForSeconds(0.05f);

        // Type out the text character by character
        foreach (char letter in fullText.ToCharArray())
        {
            // Check if the player wants to skip the typing
            if (InputManager.instance.inputControl.Dialogue.Interact.WasPressedThisFrame() && !skipped)
            {
                currentDialogueText.text = fullText;
                skipped = true;
                break;
            }

            currentDialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        // If skipped, wait a tiny moment before proceeding
        if (skipped)
        {
            yield return new WaitForSeconds(0.1f);
        }

        isTyping = false;

        // Check if we have choices after this line
        if (currentStory.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
        else
        {
            canContinueToNextLine = true;
        }
    }

    private void HandleContinueClick()
    {
        if (InputManager.instance.inputControl.Dialogue.Interact.WasPressedThisFrame())
        {
            if (isTyping)
            {
                // If currently typing, skip to the end of the current line
                SkipTyping();
            }
            else if (canContinueToNextLine && !isDisplayingChoices)
            {
                // If not typing and can continue, go to next line
                ContinueStory();
            }
        }
    }

    private void SkipTyping()
    {
        if (displayLineCoroutine != null)
        {
            StopCoroutine(displayLineCoroutine);
            isTyping = false;

            // Display the full text immediately
            string currentLine = currentStory.currentText;
            currentDialogueText.text = currentLine;

            // Check if we have choices after this line
            if (currentStory.currentChoices.Count > 0)
            {
                DisplayChoices();
            }
            else
            {
                canContinueToNextLine = true;
            }
        }
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        if (currentChoices.Count > 0)
        {
            isDisplayingChoices = true;
            canContinueToNextLine = false;

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
            canContinueToNextLine = true;
            isDisplayingChoices = false;
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
        if (isDisplayingChoices)
        {
            currentStory.ChooseChoiceIndex(choiceIndex);
            isDisplayingChoices = false;
            HideChoices();

            // Add a small delay to prevent instant skipping
            StartCoroutine(ContinueAfterChoice());
        }
    }

    private IEnumerator ContinueAfterChoice()
    {
        // Reset input to prevent the choice selection from triggering immediate skip
        InputManager.instance.inputControl.Dialogue.Interact.Reset();

        // Wait one frame to ensure input is cleared
        yield return null;

        ContinueStory();
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