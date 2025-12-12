using UnityEngine;
using System.Collections.Generic;

public class DialogueSoundManager : MonoBehaviour
{
    [Header("Dialogue Start Sounds")]
    [SerializeField] private AudioClip[] dialogueStartSounds;
    [SerializeField] private float startSoundVolume = 0.8f;
    [SerializeField] private float startPitchMin = 0.9f;
    [SerializeField] private float startPitchMax = 1.1f;
    [SerializeField] private bool enablePitchShift = true;
    [SerializeField] private bool randomizeStartSound = true;

    [Header("Choice Selection Sounds")]
    [SerializeField] private AudioClip[] choiceSelectSounds;
    [SerializeField] private float choiceSoundVolume = 0.7f;
    [SerializeField] private float choicePitchMin = 0.95f;
    [SerializeField] private float choicePitchMax = 1.05f;
    [SerializeField] private bool randomizeChoiceSound = true;
    [SerializeField] private bool enableChoicePitchShift = true;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }
    }

    public void PlayDialogueStartSound()
    {
        if (dialogueStartSounds == null || dialogueStartSounds.Length == 0)
        {
            Debug.LogWarning("No dialogue start sounds assigned!", this);
            return;
        }

        // Select random sound
        AudioClip clipToPlay;
        if (randomizeStartSound)
        {
            clipToPlay = dialogueStartSounds[Random.Range(0, dialogueStartSounds.Length)];
        }
        else
        {
            // Cycle through sounds
            clipToPlay = dialogueStartSounds[Random.Range(0, dialogueStartSounds.Length)];
        }

        // Apply pitch shift
        if (enablePitchShift && clipToPlay != null)
        {
            audioSource.pitch = Random.Range(startPitchMin, startPitchMax);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        // Play the sound
        audioSource.PlayOneShot(clipToPlay, startSoundVolume);
    }

    public void PlayChoiceSelectSound()
    {
        if (choiceSelectSounds == null || choiceSelectSounds.Length == 0)
        {
            Debug.LogWarning("No choice selection sounds assigned!", this);
            return;
        }

        // Select random sound
        AudioClip clipToPlay;
        if (randomizeChoiceSound)
        {
            clipToPlay = choiceSelectSounds[Random.Range(0, choiceSelectSounds.Length)];
        }
        else
        {
            // Cycle through sounds
            clipToPlay = choiceSelectSounds[Random.Range(0, choiceSelectSounds.Length)];
        }

        // Apply pitch shift
        if (enableChoicePitchShift && clipToPlay != null)
        {
            audioSource.pitch = Random.Range(choicePitchMin, choicePitchMax);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        // Play the sound
        audioSource.PlayOneShot(clipToPlay, choiceSoundVolume);
    }

    // Methods to configure sounds at runtime
    public void SetDialogueStartSounds(AudioClip[] newSounds)
    {
        dialogueStartSounds = newSounds;
    }

    public void SetChoiceSelectSounds(AudioClip[] newSounds)
    {
        choiceSelectSounds = newSounds;
    }

    public void SetStartPitchRange(float min, float max)
    {
        startPitchMin = min;
        startPitchMax = max;
    }

    public void SetChoicePitchRange(float min, float max)
    {
        choicePitchMin = min;
        choicePitchMax = max;
    }

    public void SetStartVolume(float volume)
    {
        startSoundVolume = Mathf.Clamp01(volume);
    }

    public void SetChoiceVolume(float volume)
    {
        choiceSoundVolume = Mathf.Clamp01(volume);
    }

    public void SetPitchShiftEnabled(bool startEnabled, bool choiceEnabled)
    {
        enablePitchShift = startEnabled;
        enableChoicePitchShift = choiceEnabled;
    }
}