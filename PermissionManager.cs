using UnityEngine;
using UnityEngine.Android;

public class PermissionManager : MonoBehaviour
{
    void Start()
    {
        RequestAllPermissions();
    }

    // Request all necessary permissions
    void RequestAllPermissions()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            // Storage permissions
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
            }

            // Microphone permission
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }

            // Camera permission
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
            }

            // Location permissions
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
            {
                Permission.RequestUserPermission(Permission.CoarseLocation);
            }
        }
    }

    // Optionally, you can check if all permissions were granted
    public bool AreAllPermissionsGranted()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            return Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite) &&
                   Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead) &&
                   Permission.HasUserAuthorizedPermission(Permission.Microphone) &&
                   Permission.HasUserAuthorizedPermission(Permission.Camera) &&
                   Permission.HasUserAuthorizedPermission(Permission.FineLocation) &&
                   Permission.HasUserAuthorizedPermission(Permission.CoarseLocation);
        }
        return true; // If not Android, assume permissions are granted
    }
}
