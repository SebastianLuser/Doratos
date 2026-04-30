using Photon.Pun;
using TMPro;
using UnityEngine;

public class PlayerNameplate : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;

    private Transform cam;

    private void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;

        var pv = GetComponentInParent<PhotonView>();
        string nick = pv?.Owner?.NickName;
        nameText.text = string.IsNullOrEmpty(nick) ? "Player" : nick;
    }

    private void LateUpdate()
    {
        if (cam == null) return;
        transform.rotation = cam.rotation;
    }
}
