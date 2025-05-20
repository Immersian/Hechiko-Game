using Ink.Runtime;
using SupanthaPaul;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialoguetext;
    [SerializeField] private GameObject interactIcon;

    [Header("Choices UI")]
    [SerializeField] private GameObject[] choiceButtons;
    private TextMeshProUGUI[] choicesText;

    [Header("Params")]
    [SerializeField] private float typingSpeed = 0.04f;

    private Story currentStory;
    public bool dialogueIsPlaying { get; private set; }
    private bool canContinueToNextLine = false;
    private Coroutine displayLineCoroutine;
    private static DialogueManager instance;
    
    // Track interaction count per NPC
    private Dictionary<string, int> interactionCounts = new Dictionary<string, int>();

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Found more than one Dialogue Manager in the scene");
        }
        instance = this;

        // Initialize choices UI
        choicesText = new TextMeshProUGUI[choiceButtons.Length];
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choicesText[i] = choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        HideChoices();
    }

    private void Update()
    {
        if (!dialogueIsPlaying)
        {
            return;
        }

        if (InputManager.instance.inputControl.Dialogue.Interact.WasPressedThisFrame() && 
            currentStory.currentChoices.Count == 0 && canContinueToNextLine)
        {
            ContinueStory();
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON, string npcID)
    {
        if (inkJSON == null)
        {
            Debug.LogError("Ink JSON file is null!");
            return;
        }

        // Get or create interaction count for this NPC
        if (!interactionCounts.ContainsKey(npcID))
        {
            interactionCounts[npcID] = 0;
        }
        interactionCounts[npcID]++;

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.DisableMovement();
        }

        InputManager.instance.SetGameplayInputEnabled(false);

        currentStory = new Story(inkJSON.text);
        
        // Set the interaction count in Ink
        currentStory.variablesState["interaction_count"] = interactionCounts[npcID];
        
        dialogueIsPlaying = true;
        dialoguePanel.SetActive(true);
        ContinueStory();
    }

    private void ExitDialogueMode()
    {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        dialoguetext.text = "";
        InputManager.instance.SetGameplayInputEnabled(true);
        
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.EnableMovement();
        }
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
        // Skip empty lines
        if (string.IsNullOrWhiteSpace(line))
        {
            ContinueStory();
            yield break;
        }

        dialoguetext.text = "";
        canContinueToNextLine = false;
        HideChoices();

        foreach (char letter in line.ToCharArray())
        {
            dialoguetext.text += letter;
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
            interactIcon.SetActive(false);
            InputManager.instance.SetDialogueInputEnabled(false);
            
            for (int i = 0; i < currentChoices.Count; i++)
            {
                if (i < choiceButtons.Length)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    choicesText[i].text = currentChoices[i].text;
                }
                else
                {
                    Debug.LogWarning("More choices were given in the Ink story than UI buttons available");
                    break;
                }
            }

            for (int i = currentChoices.Count; i < choiceButtons.Length; i++)
            {
                choiceButtons[i].gameObject.SetActive(false);
            }

            StartCoroutine(SelectFirstChoice());
        }
        else
        {
            interactIcon.SetActive(true);
            HideChoices();
            InputManager.instance.SetDialogueInputEnabled(true);
        }
    }

    private IEnumerator SelectFirstChoice()
    {
        yield return null;
        if (choiceButtons.Length > 0 && choiceButtons[0].activeInHierarchy)
        {
            choiceButtons[0].GetComponent<Button>().Select();
        }
    }

    private void HideChoices()
    {
        foreach (GameObject choiceButton in choiceButtons)
        {
            choiceButton.SetActive(false);
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
}