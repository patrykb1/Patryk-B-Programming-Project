using UnityEngine;
using UnityEngine.InputSystem;

public class InputBindingLoader : MonoBehaviour
{
    public PlayerInput playerInput; // assign in inspector
    private Settings settings;

    void Start()
    {
        settings = SettingsManager.Load();
        ApplyBindings();
    }
    public void ApplyBindings()
    {   // Override input bindings
        if (playerInput == null || settings == null) return;
        // In the PlayerInput file, 0 = keyboard, 1 = controller
        // Apply keyboard bindings
        foreach (var binding in settings.controls.keyboard.bindings)
        { 
            var action = playerInput.actions[binding.action];
            action?.ApplyBindingOverride(0, binding.path);
        }

        // Apply controller bindings
        foreach (var binding in settings.controls.controller.bindings)
        {
            var action = playerInput.actions[binding.action];
            action?.ApplyBindingOverride(1, binding.path);
 
        }
    }
}
