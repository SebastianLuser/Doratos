using UnityEngine;

[CreateAssetMenu(menuName = "Doratos/ArenaSO")]
public class ArenaSO : ScriptableObject
{
    public float arenaSize = 20f;
    public Vector3[] spawnPoints = new Vector3[]
    {
        new Vector3(-7f, 0f, -7f),
        new Vector3(7f, 0f, -7f),
        new Vector3(-7f, 0f, 7f),
        new Vector3(7f, 0f, 7f)
    };
    public GameObject[] coverPrefabs;
}
