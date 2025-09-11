using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Credits : MonoBehaviour
{
    public float scrollSpeed = 30f;
    public RectTransform creditsTransform; // Changed from 'transform' to 'creditsTransform'

    // Start is called before the first frame update
    void Start()
    {
        creditsTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        creditsTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);
    }
}