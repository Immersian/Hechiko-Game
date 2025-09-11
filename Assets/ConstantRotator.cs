using UnityEngine;

public class ConstantRotator : MonoBehaviour
{
    public float rotationSpeed = 90f;

    void Update()
    {
        // Rotate the object around its forward (Z) axis at a constant speed
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }
}