using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
public class RebindAction : MonoBehaviour
{
    public InputActionReference actionRef;
    public int bindingIndex;
    public string bindName;
    private InputActionRebindingExtensions.RebindingOperation rebindOp;
    [SerializeField] private GameObject rebindPanel;
    [SerializeField] private TextMeshProUGUI rebindText;
    [SerializeField] private TextMeshProUGUI bindingLabel;
    private Image image;
    private KeybindImages kb;

    public void Awake()
    {
        image = GetComponent<Image>();
        bindName = bindingLabel.text;
    }
    private void Start()
    {   // Use instance instead of inspector as reference goes missing when scene is reloaded 
        kb = SettingsManager.Instance.GetComponent<KeybindImages>();
    }

    public void StartRebind()
    {   // Start the rebind process
        rebindPanel.SetActive(true);
        rebindText.text = "Press any key to rebind  " + bindingLabel.text + " action:";
        actionRef.action.Disable();
        if (bindingIndex == 0) PerformInteractiveRebindingKeyboard();
        else if (bindingIndex == 1) PerformInteractiveRebindingController();
        else return;
    }

    private void PerformInteractiveRebindingKeyboard()
    {   // Ignore controller inputs apart from "back" button in case of accidental rebind
        rebindOp = actionRef.action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath("<Keyboard>")   
            .WithControlsHavingToMatchPath("<Mouse>")      
            .WithControlsExcluding("Gamepad")    
            .WithCancelingThrough("<Keyboard>/escape")
            .WithCancelingThrough("<Gamepad>/Select")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => { CompleteRebind(); })
            .OnCancel(operation => { CancelRebind(); })
            .Start();
    }
    private void PerformInteractiveRebindingController()
    {   // Ignore keyboard input apart from "escape" key in case of accidental rebind
        rebindOp = actionRef.action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath("<Gamepad>")
            .WithControlsExcluding("Mouse")
            .WithControlsExcluding("Keyboard")
            .WithCancelingThrough("<Gamepad>/Select")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => { CompleteRebind(); })
            .OnCancel(operation => { CancelRebind(); })
            .Start();
    }
    public void CancelRebind()
    {   //Everything back to normal, dispose of rebind operation
        if (rebindOp == null) return;
        actionRef.action.Enable();
        rebindPanel.SetActive(false);
        rebindOp.Dispose();
    }

    private void CompleteRebind()
    {   // Update keybind image, save the setting, dispose of rebind operation
        actionRef.action.Enable();
        string bindingPath = actionRef.action.bindings[bindingIndex].effectivePath;
        kb.UpdateKeybindImage(image, bindingPath);
        SettingsManager.Instance.SetBinding(bindName, bindingPath, bindingIndex);
        rebindPanel.SetActive(false);
        rebindOp.Dispose();
    }


}