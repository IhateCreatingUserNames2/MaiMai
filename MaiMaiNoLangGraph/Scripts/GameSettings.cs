// GameSettings.cs
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    public bool isVoiceEnabled = false;

    private void Awake()
    {
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

    public void SetVoiceEnabled(bool isEnabled)
    {
        isVoiceEnabled = isEnabled;
        Debug.Log($"Voice Enabled set to: {isVoiceEnabled}");
    }
}
