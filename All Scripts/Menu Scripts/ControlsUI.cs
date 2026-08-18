using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ControlsUI : MonoBehaviour
{
    public List<GameObject> buttonImages = new();
    public KeybindImages kb;
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main Menu")
        {   // Ensures controls UI is setup when the scene is reloaded
            FindFirstObjectByType<ReferenceReloader>().ReloadControlsUI();
            SetupKeybinds();
        }
    }

    private void Start()
    {
        SetupKeybinds();
    }
    private void SetupKeybinds()
    {
        kb = GetComponent<KeybindImages>();
        foreach (GameObject button in buttonImages)
        {
            RebindAction ra = button.GetComponent<RebindAction>();
            string bindName = ra.bindName.ToLower();
            int bindingIndex = ra.bindingIndex;
            var controls = SettingsManager.Instance.newSettings.controls;
            string bindingPath = bindingIndex switch
            {
                0 => controls.keyboard.GetBinding(bindName),
                1 => controls.controller.GetBinding(bindName),
                _ => ""
            };
            Image icon = button.GetComponent<Image>();
            kb.UpdateKeybindImage(icon, bindingPath);
        }
        MenuManager.Instance.OnKeybindsLoaded();
    }
}
