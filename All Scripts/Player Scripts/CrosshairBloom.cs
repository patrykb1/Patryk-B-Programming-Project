using UnityEngine;

public class CrosshairBloom : MonoBehaviour
{
    public RectTransform UpLine;
    public RectTransform DownLine;
    public RectTransform LeftLine;
    public RectTransform RightLine;

    public void AddBloom(float spread)
    {   // Change position of inidicators based on spread
        UpLine.anchoredPosition = new Vector2(0, spread);
        DownLine.anchoredPosition = new Vector2(0, -spread);
        LeftLine.anchoredPosition = new Vector2(-spread, 0);
        RightLine.anchoredPosition = new Vector2(spread, 0);

    }
    private void Start()
    {   // Set initial bloom
        AddBloom(30f);
    }
}
