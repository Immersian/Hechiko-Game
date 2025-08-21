using TMPro;
using UnityEngine;

public class TextLineSpacing : MonoBehaviour
{
    public TMP_Text myText;
    public float lineSpacing = 0.0f; // Default is 0, negative values = less spacing

    void Start()
    {
        myText.lineSpacing = -10f; // Negative values reduce spacing
    }
}