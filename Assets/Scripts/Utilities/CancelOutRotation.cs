using UnityEngine;
using UnityEngine.Serialization;

public class CancelOutRotation : MonoBehaviour
{
    [SerializeField] private Quaternion newRotation;
    
    private void LateUpdate() => transform.rotation = newRotation;
}
