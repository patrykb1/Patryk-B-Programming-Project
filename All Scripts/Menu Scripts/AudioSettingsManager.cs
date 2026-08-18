//DOCUMENTED CODE
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VolumeUI : MonoBehaviour
{
    [Header("Master")]
    public Slider masterSlider;
    public TMP_Text masterLabel;

    [Header("Music")]
    public Slider musicSlider;
    public TMP_Text musicLabel;

    [Header("SFX")]
    public Slider sfxSlider;
    public TMP_Text sfxLabel;

    // Subscribe to scene loaded event to setup the UI when the main menu is loaded
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
        {   // Reload references to volume UI element
            FindFirstObjectByType<ReferenceReloader>().ReloadVolumeUI();
            Setup();
        }
    }
    public void Setup()
    {   // Load correct slider values and update labels
        masterSlider.value = SettingsManager.Instance.newSettings.audio.masterVolume;
        musicSlider.value = SettingsManager.Instance.newSettings.audio.musicVolume;
        sfxSlider.value = SettingsManager.Instance.newSettings.audio.sfxVolume;
        UpdateAll();
    }


    public void OnMasterChanged(float value)
    {   // Update master volume label and save new value to settings manager
        UpdateLabel(masterLabel, value);
        SettingsManager.Instance.SetMasterVolume(value);
    }

    public void OnMusicChanged(float value)
    {   // Update music volume label and save new value to settings manager
        UpdateLabel(musicLabel, value);
        SettingsManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXChanged(float value)
    {   // Update sfx volume label and save new value to settings manager
        UpdateLabel(sfxLabel, value);
        SettingsManager.Instance.SetSFXVolume(value);
    }
    public void UpdateAll()
    {   // Update all labels to match current slider values
        UpdateLabel(masterLabel, masterSlider.value);
        UpdateLabel(musicLabel, musicSlider.value);
        UpdateLabel(sfxLabel, sfxSlider.value);
    }

    private void UpdateLabel(TMP_Text label, float value)
    {   // Update the given label to show the percentage value of the slider
        label.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
}