using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public float parallaxFactor;

    public void Move(float delta)
    {
        // Prevent extreme shifts that might occur during initialization
        if (Mathf.Abs(delta) > 100f) return;

        Vector3 newPos = transform.localPosition;
        newPos.x -= delta * parallaxFactor;

        transform.localPosition = newPos;
    }
}