using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class PlayfabLogin : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text messageText;
    public Button signUpOrLoginButton;
    public TMP_Text usernameText;
    public TMP_Text emailText;
    public GameObject selection;
    public GameObject accountSettings;
    public string username = null;
    // Add this field to the class to fix CS0103
    public bool loggedIn = false;
    public static PlayfabLogin Instance;

    private void Awake()
    {   // Ensure only one instance of PlayfabLogin exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);

    }
    public void Start()
    {
        Debug.Log("This is a Debug.Log statement.");
        Debug.LogWarning("This is a Debug.LogWarning statement.");
        Debug.LogError("This is a Debug.LogError statement.");

    }

    // Subscription to scene loading
    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main Menu")
        {   // References need to be reloaded when the scene is loaded
            FindFirstObjectByType<ReferenceReloader>().ReloadPlayfabLogin();
            // If the user is logged in, skip the sign-up/login process
            if (loggedIn) OnLoginSuccess();
        }
    }
    public void SignUp()
    {
        var request = new RegisterPlayFabUserRequest
        {
            Email = emailInput.text,
            Username = usernameInput.text,
            Password = passwordInput.text,
            RequireBothUsernameAndEmail = true
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, OnSignUpSuccess, OnError);
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
        messageText.text = "Error registering user: " + error.ErrorMessage;
    }

    private void OnSignUpSuccess(RegisterPlayFabUserResult result)
    {
        SetDisplayName(usernameInput.text);
        Debug.Log("User registered successfully!");
        messageText.text = "User registered successfully!";
    }

    public void Login()
    {   // Request to login 
        var request = new LoginWithPlayFabRequest
        {
            Username = usernameInput.text,
            Password = passwordInput.text
        };
        PlayFabClientAPI.LoginWithPlayFab(request, OnLoginSuccess, OnError);
    }

    private void OnLoginSuccess(LoginResult result)
    {   // Parameterized version for login success
        loggedIn = true;
        GetPlayerInfo();
        Debug.Log("User logged in successfully!");
        selection.SetActive(false);
        accountSettings.SetActive(true);
        messageText.text = "User logged in successfully!";
    }

    private void OnLoginSuccess()
    {   //Parameterless version for UI refresh
        loggedIn = true;
        GetPlayerInfo();
        Debug.Log("User logged in successfully!");
        selection.SetActive(false);
        accountSettings.SetActive(true);
        messageText.text = "User logged in successfully!";
    }

    public void RequestLoginOrSignUp()
    {   // Same button is used for either signing up or logging in
        string buttonText = signUpOrLoginButton.GetComponentInChildren<TMP_Text>().text;
        if (buttonText == "Sign Up") SignUp();
        else if (buttonText == "Login") Login();
        else
        {   // This will NOT happen, but just in case
            Debug.LogError("Invalid button text: " + buttonText);
            messageText.text = "Invalid button text: " + buttonText;
        }

    }
    public void SetDisplayName(string displayName)
    {   // Assigns username to account
        var namerequest = new UpdateUserTitleDisplayNameRequest { DisplayName = displayName };
        PlayFabClientAPI.UpdateUserTitleDisplayName(namerequest,
            result => { Debug.Log("Display Name Set Successfully!"); },
            error => { Debug.LogError(error.GenerateErrorReport()); });
    }
    public void GetPlayerInfo()
    {   // Fetches player information
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(),
            result =>
            {   // Update UI with player information
                username = result.AccountInfo.TitleInfo.DisplayName;
                PlayerData.Instance.UpdateUsername(username);
                usernameText.text = "Username: " + username;
                emailText.text = "Email: " + result.AccountInfo.PrivateInfo?.Email;
            },
            error => Debug.LogError(error.GenerateErrorReport()));
    }
    public void SaveHighScore(int score)
    {
        int currentHighScore = GetHighScore();
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate {StatisticName = "HighScore", Value = score}
            }
        };
        //No need to try updating if the score is not higher
        if (score <= currentHighScore) return;
        PlayFabClientAPI.UpdatePlayerStatistics(request,
            result => Debug.Log($"HighScore {score} submitted successfully!"),
            error => Debug.LogError($"Error submitting HighScore: {error.GenerateErrorReport()}")
        );
    }
    public int GetHighScore()
    {
        var request = new GetPlayerStatisticsRequest { StatisticNames = new List<string> { "HighScore" } };
        var stat = new StatisticValue();
        PlayFabClientAPI.GetPlayerStatistics(request,
            result =>
            {
                stat = result.Statistics.Find(s => s.StatisticName == "HighScore");
            },
            error =>
            {
                Debug.LogError($"Error fetching high score: {error.GenerateErrorReport()}");
            });
        return stat != null ? stat.Value : 0;
    }
}
