using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchManager : MonoBehaviourPunCallbacks
{
    public static MatchManager Instance;

    [SerializeField] private MatchConfigSO matchConfig;
    [SerializeField] private ArenaSO arenaSO;
    [SerializeField] private FireRing fireRing;
    [SerializeField] private Transform mapSpawnpoint;

    public bool IsInMatch { get; private set; }
    public FireRing FireRing => fireRing;

    private HashSet<int> deadActors = new HashSet<int>();
    private int totalPlayers;
    private float matchStartTime;
    private bool fireRingStarted;
    private float fireDamageTimer;

    private Dictionary<int, PlayerHealth> playerHealths = new Dictionary<int, PlayerHealth>();
    private GameObject localPlayerGO;
    private GameObject spearGO;
    private GameObject currentMapGO;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private IEnumerator Start()
    {
        Debug.Log($"[MatchManager] Start — InRoom={PhotonNetwork.InRoom}, IsMaster={PhotonNetwork.IsMasterClient}");
        if (!PhotonNetwork.InRoom) yield break;

        IsInMatch = true;
        matchStartTime = Time.time;
        Spear.ClearPlayers();

        bool hudAlreadyLoaded = SceneManager.GetSceneByName("HUD").isLoaded;
        Debug.Log($"[MatchManager] HUD ya cargado={hudAlreadyLoaded}");
        if (!hudAlreadyLoaded)
            SceneManager.LoadScene("HUD", LoadSceneMode.Additive);

        while (HUD.Instance == null)
            yield return null;

        if (PhotonNetwork.IsMasterClient)
        {
            int mapIndex = Random.Range(0, arenaSO.maps.Length);
            SpawnMap(mapIndex);
        }

        SpawnLocalPlayer();

        if (PhotonNetwork.IsMasterClient)
            SpawnSpear();
    }

    private void SpawnMap(int mapIndex)
    {
        if (arenaSO.maps == null || arenaSO.maps.Length == 0)
        {
            Debug.LogWarning("[MatchManager] No maps assigned in ArenaSO.");
            return;
        }

        mapIndex = Mathf.Clamp(mapIndex, 0, arenaSO.maps.Length - 1);
        GameObject prefab = arenaSO.maps[mapIndex];

        if (prefab == null)
        {
            Debug.LogWarning($"[MatchManager] Map prefab at index {mapIndex} is null.");
            return;
        }

        Vector3 pos = mapSpawnpoint != null ? mapSpawnpoint.position : Vector3.zero;
        Quaternion rot = mapSpawnpoint != null ? mapSpawnpoint.rotation : Quaternion.identity;

        currentMapGO = PhotonNetwork.InstantiateRoomObject("Prefabs/Maps/" + prefab.name, pos, rot);
        Debug.Log($"[MatchManager] Spawned map '{prefab.name}' via InstantiateRoomObject at {pos}");
    }

    private void Update()
    {
        if (!IsInMatch) return;

        float elapsed = Time.time - matchStartTime;
        float delay = matchConfig != null ? matchConfig.fireRingDelaySec : 60f;
        float remaining = Mathf.Max(0f, delay - elapsed);

        if (HUD.Instance != null)
            HUD.Instance.UpdateTimer(remaining, fireRingStarted);

        if (!fireRingStarted && remaining <= 0f && PhotonNetwork.IsMasterClient)
        {
            fireRingStarted = true;
            float shrinkSec = matchConfig != null ? matchConfig.fireRingShrinkSec : 30f;
            float radius = arenaSO != null ? arenaSO.arenaSize / 2f : 10f;
            photonView.RPC(nameof(RPC_StartFireRing), RpcTarget.All, radius, shrinkSec);
        }

        if (fireRingStarted && PhotonNetwork.IsMasterClient)
            CheckFireRingDamage();
    }

    private void CheckFireRingDamage()
    {
        if (fireRing == null || !fireRing.IsActive) return;

        fireDamageTimer -= Time.deltaTime;
        if (fireDamageTimer > 0f) return;
        fireDamageTimer = 1f;

        float dps = matchConfig != null ? matchConfig.fireRingDPS : 25f;

        foreach (var kvp in playerHealths)
        {
            var health = kvp.Value;
            if (health == null || health.IsDead) continue;

            float dist = new Vector2(health.transform.position.x, health.transform.position.z).magnitude;
            if (dist > fireRing.CurrentRadius)
                health.photonView.RPC(nameof(PlayerHealth.RPC_TakeDamage), RpcTarget.All, dps);
        }
    }

    [PunRPC]
    private void RPC_StartFireRing(float radius, float shrinkDuration)
    {
        fireRingStarted = true;
        if (fireRing != null)
            fireRing.Activate(radius, shrinkDuration);
    }

    private void SpawnLocalPlayer()
    {
        Debug.Log($"[MatchManager] SpawnLocalPlayer actor={PhotonNetwork.LocalPlayer.ActorNumber}");
        int index = GetSpawnIndex(PhotonNetwork.LocalPlayer.ActorNumber);
        Vector3 spawnPos = arenaSO.spawnPoints[index];
        Vector3 center = Vector3.zero;
        Quaternion spawnRot = Quaternion.LookRotation(center - spawnPos);
        localPlayerGO = PhotonNetwork.Instantiate("Prefabs/Gladiator", spawnPos, spawnRot);
        var playerGO = localPlayerGO;
        totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

        var health = playerGO.GetComponent<PlayerHealth>();
        var state = playerGO.GetComponent<PlayerState>();

        Spear.RegisterPlayer(PhotonNetwork.LocalPlayer.ActorNumber, health);
        playerHealths[PhotonNetwork.LocalPlayer.ActorNumber] = health;

        if (HUD.Instance != null)
            HUD.Instance.RegisterLocalPlayer(health, state);
    }

    private int GetSpawnIndex(int actorNumber)
    {
        // ActorNumber es único y consistente en todos los clientes — no depende del orden de PlayerList
        return (actorNumber - 1) % arenaSO.spawnPoints.Length;
    }

    private void SpawnSpear()
    {
        spearGO = PhotonNetwork.InstantiateRoomObject("Prefabs/Spear", Vector3.zero, Quaternion.identity);
        var spear = spearGO.GetComponent<Spear>();

        if (HUD.Instance != null && spear != null)
            HUD.Instance.RegisterSpear(spear);
    }

    public void RegisterRemotePlayer(PlayerHealth health, int actorNr)
    {
        Spear.RegisterPlayer(actorNr, health);
        playerHealths[actorNr] = health;

        if (HUD.Instance != null)
            HUD.Instance.RegisterEnemy(health);
        else
            StartCoroutine(RegisterEnemyWhenHUDReady(health));
    }

    private IEnumerator RegisterEnemyWhenHUDReady(PlayerHealth health)
    {
        while (HUD.Instance == null)
            yield return null;
        HUD.Instance.RegisterEnemy(health);
    }

    public void NotifyPlayerDied(int deadActorNr)
    {
        if (!IsInMatch) return;
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_PlayerDied), RpcTarget.All, deadActorNr);
    }

    [PunRPC]
    private void RPC_PlayerDied(int deadActorNr)
    {
        deadActors.Add(deadActorNr);

        if (HUD.Instance != null)
            HUD.Instance.ShowFeedback("¡Gladiador eliminado!");

        int alivePlayers = 0;
        int lastAliveActor = -1;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!deadActors.Contains(player.ActorNumber))
            {
                alivePlayers++;
                lastAliveActor = player.ActorNumber;
            }
        }

        if (alivePlayers <= 1 && PhotonNetwork.IsMasterClient)
        {
            int newKills = 0;
            bool seriesOver = false;

            if (lastAliveActor >= 0)
            {
                NetworkManager.Instance.AddSeriesKill(lastAliveActor, out newKills);
                int killLimit = NetworkManager.Instance.KillLimit;
                seriesOver = killLimit > 0 && newKills >= killLimit;
            }

            photonView.RPC(nameof(RPC_EndMatch), RpcTarget.All, lastAliveActor, "LastManStanding", seriesOver, newKills);
        }
    }
    
    public void NotifyPlayerLeft(int disconnectedActorNr)
    {
        if (!IsInMatch) return;
        if (!PhotonNetwork.IsMasterClient) return;

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            // No quedan suficientes jugadores → terminar match y volver al lobby
            photonView.RPC(nameof(RPC_EndMatch), RpcTarget.All,
                           PhotonNetwork.LocalPlayer.ActorNumber, "Disconnect", true, 0);
        }
        else
        {
            // Quedan 2+ jugadores → tratar la desconexión como muerte y continuar la ronda
            photonView.RPC(nameof(RPC_PlayerDied), RpcTarget.All, disconnectedActorNr);
        }
    }

    [PunRPC]
    private void RPC_EndMatch(int winnerActorNr, string reason, bool seriesOver, int winnerNewKills)
    {
        Debug.Log($"[MatchManager] RPC_EndMatch — winner={winnerActorNr}, reason={reason}, seriesOver={seriesOver}, local={PhotonNetwork.LocalPlayer.ActorNumber}");
        IsInMatch = false;
        bool isWinner = PhotonNetwork.LocalPlayer.ActorNumber == winnerActorNr;

        int killLimit = NetworkManager.Instance != null ? NetworkManager.Instance.KillLimit : 0;

        if (reason == "Disconnect" && HUD.Instance != null)
            HUD.Instance.ShowFeedback("Un gladiador abandonó la arena");

        if (EndScreenUI.Instance != null)
        {
            if (reason == "Disconnect")
                EndScreenUI.Instance.ShowDisconnectWin();
            else if (seriesOver && isWinner)
                EndScreenUI.Instance.ShowSeriesVictory();
            else if (seriesOver && !isWinner)
                EndScreenUI.Instance.ShowSeriesDefeat();
            else if (isWinner)
                EndScreenUI.Instance.ShowRoundVictory(winnerNewKills, killLimit);
            else
                EndScreenUI.Instance.ShowRoundDefeat(winnerNewKills, killLimit);
        }

        if (LobbyUI.Instance != null)
        {
            if (isWinner) LobbyUI.Instance.AddKill();
            else LobbyUI.Instance.AddDeath();
        }

        if (seriesOver)
            StartCoroutine(ReturnToRoomAfterDelay());
        else
            StartCoroutine(StartNextRoundAfterDelay());
    }

    private IEnumerator StartNextRoundAfterDelay()
    {
        float delay = matchConfig != null ? matchConfig.endScreenDelaySec : 3f;
        yield return new WaitForSeconds(delay);

        if (PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(RPC_LoadNextRound), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_LoadNextRound()
    {
        StartCoroutine(DestroyThenReload());
    }

    private IEnumerator DestroyThenReload()
    {
        if (localPlayerGO != null)
        {
            PhotonNetwork.Destroy(localPlayerGO);
            localPlayerGO = null;
        }
        if (PhotonNetwork.IsMasterClient && spearGO != null)
        {
            PhotonNetwork.Destroy(spearGO);
            spearGO = null;
        }
        if (PhotonNetwork.IsMasterClient && currentMapGO != null)
        {
            PhotonNetwork.Destroy(currentMapGO);
            currentMapGO = null;
        }

        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("Arena");
    }

    private IEnumerator ReturnToRoomAfterDelay()
    {
        float delay = matchConfig != null ? matchConfig.endScreenDelaySec : 3f;
        yield return new WaitForSeconds(delay);

        if (localPlayerGO != null)
        {
            PhotonNetwork.Destroy(localPlayerGO);
            localPlayerGO = null;
        }
        if (PhotonNetwork.IsMasterClient && spearGO != null)
        {
            PhotonNetwork.Destroy(spearGO);
            spearGO = null;
        }
        if (PhotonNetwork.IsMasterClient && currentMapGO != null)
        {
            PhotonNetwork.Destroy(currentMapGO);
            currentMapGO = null;
        }

        NetworkManager.Instance.ReturnToRoom();
    }

    public void ReturnToLobby()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Lobby");
    }
}