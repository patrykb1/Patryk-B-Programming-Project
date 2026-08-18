using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeybindImages : MonoBehaviour
{
    private Dictionary<string, Sprite> buttonSprites = new Dictionary<string, Sprite>();

    private void Awake()
    {
        // Load all sprites from the sliced sprite sheet
        Sprite[] sprites = Resources.LoadAll<Sprite>("all_input"); // path in Resources folder
        foreach (var s in sprites)
        {   // Ensure all bind names start with an underscore
            if (s.name.Length == 1)
            {   // Single character button names
                string newName = "_" + s.name;
                buttonSprites[newName] = s;
            }
            if (s.name.StartsWith("_"))
            {
                buttonSprites[s.name] = s;
            }
            // If a sprite name doesn't have an underscore, it means that it
            // is not implemented and therefore will not be used
        }
    }

    public void UpdateKeybindImage(Image icon, string bindingPath)
    {   // Update the keybind image based on the binding path
        string keyName = SanitizePath(bindingPath);
        if (buttonSprites.TryGetValue(keyName, out Sprite sprite))
        {
            icon.sprite = sprite;
        }
        else
        {   // Log a warning if no sprite is found
            Debug.LogWarning("No sprite found for: " + keyName);
        }
    }

    private string SanitizePath(string path)
    {   // Converts the path to a key name
        string keyName = path.Replace("<Keyboard>", "")
                   .Replace("<Mouse>", "")
                   .Replace("<Gamepad>", "")
                   .Replace("numpad", "")
                   .Replace("/", "_");

        HashSet<string> modifiers = new HashSet<string> { "Alt", "Shift", "Ctrl", "Control", "Meta"};
        foreach (var mod in modifiers)
        {   // Check whether the key name is in the format of "_leftModifier" or "_rightModifier"
            string leftMod = "_left" + mod;
            string rightMod = "_right" + mod;
            
            //e.g replace _leftShift with _left_shift
            if (keyName.EndsWith(leftMod, System.StringComparison.OrdinalIgnoreCase))
                return keyName.Substring(0, keyName.Length - leftMod.Length) + "_" + mod.ToLower();
            if (keyName.EndsWith(rightMod, System.StringComparison.OrdinalIgnoreCase))
                return keyName.Substring(0, keyName.Length - rightMod.Length) + "_" + mod.ToLower();
        }

        return keyName;
    }
}