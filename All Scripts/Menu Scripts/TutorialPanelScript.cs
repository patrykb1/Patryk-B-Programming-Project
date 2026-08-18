using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPanelScript : MonoBehaviour
{
    [SerializeField] private TMP_Text tutorialText;
    private int currentIndex = 0;

    private readonly List<String> tutorialTexts = new List<string>
    {
        "Welcome to Luck of the Draw! Your objective is to defend the laptop from hoardes of enemies for as long as you can.",
        "Use the WASD keys or the left-stick on controller to move around.\nUse the mouse or the right stick on controller to look around",
        "Press left click or the right trigger to shoot your weapon.\nHolding down right click or the left trigger allows you to aim in",
        "Space/A button to jump\nLeft Shift or pressing down the left stick allows you to sprint\nR key/X button to reload",
        "Defeat enemies and survive as long as you can!"
    };
    private void Start()
    {
        HideTutorial();
        UpdateTutorialText(currentIndex);
    }
    private void UpdateTutorialText(int index) => tutorialText.text = tutorialTexts[index];
    public void OnNextPressed() 
    { 
        currentIndex++;    
        if (currentIndex < tutorialTexts.Count) UpdateTutorialText(currentIndex);
        else HideTutorial();
    }
    public void OnBackPressed()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateTutorialText(currentIndex);
        }
    }
    public void ToggleTutorialState()
    {
        if (GetComponent<CanvasGroup>().alpha == 0f) ShowTutorial();
        else HideTutorial();
    }

    private void HideTutorial()
    {   // Hides the panel
        currentIndex = 0;
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f; //transparent
            cg.interactable = false;
            cg.blocksRaycasts = false; 
        }
    }

    private void ShowTutorial()
    {
        currentIndex = 0;
        UpdateTutorialText(currentIndex);
        CanvasGroup cg = GetComponent<CanvasGroup>();
         if (cg != null)
         {
              cg.alpha = 1f; //opaque
              cg.interactable = true;
              cg.blocksRaycasts = true;
        }
    }

}
