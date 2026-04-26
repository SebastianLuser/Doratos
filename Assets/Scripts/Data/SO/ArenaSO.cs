using UnityEngine;

[CreateAssetMenu(menuName = "Doratos/ArenaSO")]
public class ArenaSO : ScriptableObject
{
    public float arenaSize = 50;
    public Vector3[] spawnPoints = new Vector3[]
    {
        new Vector3(-7f, 0f, -7f),
        new Vector3(7f, 0f, -7f),
        new Vector3(-7f, 0f, 7f),
        new Vector3(7f, 0f, 7f)
    };
    
    public GameObject[] maps;

    public GameObject GetRandomMap()
    {
        if (maps == null || maps.Length == 0)
        {
            Debug.LogWarning("No maps assigned in ArenaSO.");
            return null;
        }

        return maps[Random.Range(0, maps.Length)];
    }
}
