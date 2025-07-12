using UnityEngine;
using UnityEngine.UI;

public class ParryChargeSystem : MonoBehaviour
{
    [Header("Charge Images")]
    [SerializeField] private Image[] chargeImages = new Image[5];
    [SerializeField] private Image fullChargeIndicator;

    [Header("Colors")]
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private Color fullChargeColor = Color.green;
    [SerializeField] private Color partialChargeIndicatorColor = Color.blue; // New color for partial charge state

    private int currentCharges = 0;
    private const int maxCharges = 5;

    private void Start()
    {
        ResetAllCharges();
    }

    public void AddCharge()
    {
        if (currentCharges >= maxCharges) return;

        currentCharges++;
        UpdateChargeDisplay();

        // Update full charge indicator
        fullChargeIndicator.color = (currentCharges == maxCharges) ? fullChargeColor : partialChargeIndicatorColor;
    }

    public void ResetAllCharges()
    {
        currentCharges = 0;
        UpdateChargeDisplay();
        fullChargeIndicator.color = partialChargeIndicatorColor;
    }

    private void UpdateChargeDisplay()
    {
        for (int i = 0; i < chargeImages.Length; i++)
        {
            chargeImages[i].color = (i < currentCharges) ? activeColor : inactiveColor;
        }
    }

    // For testing in editor without parrying
    [ContextMenu("Add Test Charge")]
    private void AddTestCharge()
    {
        AddCharge();
    }

    [ContextMenu("Reset Charges")]
    private void ResetTestCharges()
    {
        ResetAllCharges();
    }
}