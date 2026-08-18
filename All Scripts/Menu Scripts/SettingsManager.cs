using UnityEngine;
using System.IO;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;
    private static string path => Path.Combine(Application.persistentDataPath, "settings.json");
    public Settings newSettings;
    public AudioMixer mixer; 
    public VolumeUI volumeUI;

    private void Awake()
    {   // Ensures one instance is active
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {   // Load settings and setup UI
        newSettings = Load();
        volumeUI.Setup();
    }

    public static void Save(Settings settings)
    {   // Save settings to file
        string json = JsonUtility.ToJson(settings,true);
        File.WriteAllText(path,json);
    }
    public static Settings Load()
    {   // Load settings from file
        if (!File.Exists(path)) return new Settings(); // return default settings if file doesn't exist
        string json = File.ReadAllText(path);   // Read the JSON file
        Settings loadedSettings = JsonUtility.FromJson<Settings>(json); // Load settings from the JSON file
        return loadedSettings;
    }
    public void SaveChanges() => Save(newSettings);
    
    // Volume settings
    public void SetMasterVolume(float volume)
    {   // Set master volume
        mixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        newSettings.audio.masterVolume = volume;
    }
    public void SetMusicVolume(float volume)
    {   // Set music volume
        mixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
        newSettings.audio.musicVolume = volume;
    }
    public void SetSFXVolume(float volume)
    {   // Set SFX volume
        mixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
        newSettings.audio.sfxVolume = volume;
    }

    public void SetFullscreen(bool isFullscreen)
    {   // Set fullscreen mode
        if (isFullscreen) { Screen.fullScreenMode = FullScreenMode.FullScreenWindow; }
        else { Screen.fullScreenMode = FullScreenMode.Windowed; }
    }

    public void SetBinding(string bindName, string bindingPath, int bindingIndex)
    {   // Set the binding for the specified control scheme
      if (bindingIndex == 0) // Keyboard
      {
        newSettings.controls.keyboard.SetBinding(bindName,bindingPath);
      }
      if (bindingIndex == 1) // Controller
      {
        newSettings.controls.controller.SetBinding(bindName,bindingPath);
      }
    }

    public void ApplyBindings(PlayerInput playerInput)
    {
        if (playerInput == null) return;
        var actions = playerInput.actions;
        foreach (var bind in newSettings.controls.keyboard.bindings)
        {   // Keyboard bindings
            var action = actions[bind.action];
            if (action == null) continue;

            for (int i = 0; i < action.bindings.Count; i++)
            {   // Loop through every binding
                if (action.bindings[i].groups.Contains("Keyboard"))
                {   // Override keyboard bindings
                    action.ApplyBindingOverride(i, bind.path);
                    break;
                }
            }
        }
        foreach (var bind in newSettings.controls.controller.bindings)
        {   // Controller bindings
            var action = actions[bind.action];
            if (action == null) continue;

            for (int i = 0; i < action.bindings.Count; i++)
            {   // Loop through every binding
                if (action.bindings[i].groups.Contains("Gamepad"))
                {   // Override controller bindings
                    action.ApplyBindingOverride(i, bind.path);
                    break;
                }
            }
        }
        actions.Enable();
    }
}
