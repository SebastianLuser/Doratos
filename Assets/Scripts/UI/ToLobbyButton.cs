using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToLobbyButton : MonoBehaviour
{
    public void OnLobbyClicked()
    {
        if (MatchManager.Instance != null)
            MatchManager.Instance.ReturnToLobby();
    }
}
