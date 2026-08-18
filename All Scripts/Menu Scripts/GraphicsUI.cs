using UnityEngine;

public class GraphicsUI : MonoBehaviour
{
    public void OnFullscreenToggle(bool isFullscreen)
    {
        SettingsManager.Instance.SetFullscreen(isFullscreen);
    }
}
