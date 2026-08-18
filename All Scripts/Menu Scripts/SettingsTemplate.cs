using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class BindingPair
{   // Represents a single input binding
    public string action;
    public string path;
}

[System.Serializable]
public class ControlBindings
{
    public List<BindingPair> bindings = new List<BindingPair>();

    public void SetBinding(string action, string path)
    {   // Used to set a new input binding in the settings object
        action = action.ToLower();
        var pair = bindings.Find(b => b.action == action);
        if (pair != null)
            pair.path = path;
        else
            bindings.Add(new BindingPair { action = action, path = path });
    }

    public string GetBinding(string action)
    {   // Returns the input binding for a specific action
        var pair = bindings.Find(b => b.action == action);
        return pair != null ? pair.path : null;
    }
}

[System.Serializable]
public class ControlSettings
{   // Defines all default input bindings for keyboard
    public ControlBindings keyboard = new ControlBindings()
    {
        bindings = new List<BindingPair>
        {
            new BindingPair { action = "shoot",  path = "<Mouse>/leftButton" },
            new BindingPair { action = "aim",    path = "<Mouse>/rightButton" },
            new BindingPair { action = "reload", path = "<Keyboard>/r" },
            new BindingPair { action = "sprint", path = "<Keyboard>/leftShift" },
            new BindingPair { action = "crouch", path = "<Keyboard>/leftCtrl" }
        }
    };
    // Defines all default input bindings for controller
    public ControlBindings controller = new ControlBindings()
    {
        bindings = new List<BindingPair>
        {
            new BindingPair { action = "shoot",  path = "<Gamepad>/rightTrigger" },
            new BindingPair { action = "aim",    path = "<Gamepad>/leftTrigger" },
            new BindingPair { action = "reload", path = "<Gamepad>/buttonWest" },
            new BindingPair { action = "sprint", path = "<Gamepad>/leftStickPress" },
            new BindingPair { action = "crouch", path = "<Gamepad>/rightStickPress" }
        }
    };
}
[System.Serializable]
public class AudioSettings
{   // Defines all audio settings
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
}
[System.Serializable]
public class VideoSettings
{   // Defines video settings: default resolution is 1920x1080px
    public bool fullscreen = true;
    public int width = 1920;
    public int height = 1080;
}

[System.Serializable]
public class Settings
{   // Holds all game settings
    public AudioSettings audio = new AudioSettings();
    public ControlSettings controls = new ControlSettings();
    public VideoSettings video = new VideoSettings();
}
