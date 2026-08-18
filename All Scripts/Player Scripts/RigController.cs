//DOCUMENTED CODE
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RigController : MonoBehaviour
{
    [SerializeField] private TwoBoneIKConstraint leftHandRig;
    [SerializeField] private PlayerInputHandler playerInput;
    private void Start()
    {
        playerInput = GetComponent<PlayerInputHandler>();
    }
    private void LateUpdate()
    {
        bool isAiming = playerInput.isAiming.Value;
        float targetWeight = isAiming ? 1f : 0f;
        leftHandRig.weight = Mathf.Lerp(leftHandRig.weight, targetWeight, Time.deltaTime * 10f);
    }
}