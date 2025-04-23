using MaiMai.Implementation;
using System.Linq;
using TMPro;
using UnityEngine;

public class SessionsMenu : MonoBehaviour
{
    public ChatSessionManager sessionManager;
    public TMP_Dropdown dropdown;

    private void Start()
    {
        RefreshDropdown();
    }

    public void OnAddSessionButton()
    {
        sessionManager.CreateNewSession();
        RefreshDropdown();
    }

    public void OnDropdownChanged(int index)
    {
        var sess = sessionManager.GetSessions()[index];
        sessionManager.SwitchToSession(sess.SessionId);
    }

    private void RefreshDropdown()
    {
        dropdown.ClearOptions();
        var labels = sessionManager.GetSessions()
                       .Select(s => s.CreatedAt.ToString("g"))
                       .ToList();
        dropdown.AddOptions(labels);
        dropdown.value = labels.Count - 1;
    }
}
