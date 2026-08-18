using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    [SerializeField] GameObject outerLines;
    [SerializeField] PlayerInputHandler inputHandler;
    [SerializeField] PlayerController playerController;
    [SerializeField] CrosshairBloom crosshairBloom;
    public GameObject hitMarker;
    public readonly float baseBloom = 30f;

    void Update() => UpdateCrosshair();

    public void ShowHitMarker()
    {
        hitMarker.SetActive(true);
        Invoke(nameof(HideHitMarker), 0.2f);
    }
    void HideHitMarker() => hitMarker.SetActive(false);

    void UpdateCrosshair()
    {
        if (inputHandler.isAiming.Value)
        {
            crosshairBloom.AddBloom(0);
            outerLines.SetActive(false);
            return;
        }

        float bloom = baseBloom + playerController.currentState switch
        {
            PlayerController.MovementState.walking => 20,
            PlayerController.MovementState.crouchIdle => -5,
            PlayerController.MovementState.crouchWalking => 10,
            PlayerController.MovementState.sprinting => 40,
            PlayerController.MovementState.air => 70,
            _ => 0
        };
        var state = playerController.currentState;
        bool hide = state == PlayerController.MovementState.sprinting ||
                    ( state == PlayerController.MovementState.air &&
                      playerController.IsSprinting() );

        outerLines.SetActive(!hide);
        crosshairBloom.AddBloom(bloom);
    }
}