using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerMaterialSetup : MonoBehaviourPun
{
    [SerializeField] private PlayerColorsSO playerColors;
    [SerializeField] private List<SkinnedMeshRenderer> renderers;

    [PunRPC]
    public void RPC_SetColors(int index)
    {
        foreach (var renderer in renderers)
        {
            renderer.material.SetColor("_BaseColor", playerColors.SlotColors[index]);
        }
    }
}