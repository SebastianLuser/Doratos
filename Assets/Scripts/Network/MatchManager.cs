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
    [SerializeField] private GameObject dummyGladiatorPrefab;
    [SerializeField] private FireRing fireRing;

    public bool IsInMatch { get; private set; }

    private HashSet<int> deadActors = new HashSet<int>();
    private int totalPlayers;
    private float matchStartTime;
    private bool fireRingStarted;
    private float fireDamageTimer;

    private Dictionary<int, PlayerHealth> playerHealths = new Dictionary<int, PlayerHealth>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (!PhotonNetwork.InRoom) return;

        IsInMatch = true;
        matchStartTime = Time.time;

        SceneManager.LoadScene("HUD", LoadSceneMode.Additive);

        SpawnLocalPlayer();

        if (PhotonNetwork.IsMasterClient)
            SpawnSpear();

        if (PhotonNetwork.OfflineMode)
            StartCoroutine(SpawnDummies());
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
        int index = GetSpawnIndex(PhotonNetwork.LocalPlayer.ActorNumber);
        Vector3 spawnPos = arenaSO.spawnPoints[index];
        Vector3 center = Vector3.zero;
        Quaternion spawnRot = Quaternion.LookRotation(center - spawnPos);
        var playerGO = PhotonNetwork.Instantiate("Prefabs/Gladiator", spawnPos, spawnRot);
        totalPlayers = PhotonNetwork.OfflineMode ? 4 : PhotonNetwork.CurrentRoom.PlayerCount;

        var health = playerGO.GetComponent<PlayerHealth>();
        var state = playerGO.GetComponent<PlayerState>();

        Spear.RegisterPlayer(PhotonNetwork.LocalPlayer.ActorNumber, health);
        playerHealths[PhotonNetwork.LocalPlayer.ActorNumber] = health;

        if (HUD.Instance != null)
            HUD.Instance.RegisterLocalPlayer(health, state);
    }

    private int GetSpawnIndex(int actorNumber)
    {
        int index = 0;
        var players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].ActorNumber == actorNumber)
            {
                index = i;
                break;
            }
        }
        return Mathf.Clamp(index, 0, arenaSO.spawnPoints.Length - 1);
    }

    private IEnumerator SpawnDummies()
    {
        yield return new WaitForSeconds(0.2f);

        var dummyPrefab = dummyGladiatorPrefab;
        if (dummyPrefab == null)
        {
            dummyPrefab = Resources.Load<GameObject>("Prefabs/Gladiator");
        }

        if (dummyPrefab == null)
        {
            Debug.LogError("No se encontró prefab para dummies");
            yield break;
        }

        for (int i = 1; i < 4; i++)
        {
            Vector3 pos = arenaSO.spawnPoints[i];
            Quaternion rot = Quaternion.LookRotation(Vector3.zero - pos);
            var dummy = Instantiate(dummyPrefab, pos, rot);

            var spearSocket = dummy.transform.Find("SpearSocket");
            if (spearSocket != null)
            {
                foreach (Transform child in spearSocket)
                    Destroy(child.gameObject);
            }

            var aimIndicator = dummy.transform.Find("AimIndicator");
            if (aimIndicator != null)
            {
                var r = aimIndicator.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }

            var controller = dummy.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;

            var state = dummy.GetComponent<PlayerState>();
            if (state != null) state.enabled = false;

            var netSync = dummy.GetComponent<PlayerNetworkSync>();
            if (netSync != null) netSync.enabled = false;

            var photonView = dummy.GetComponent<PhotonView>();
            if (photonView != null) Destroy(photonView);

            var transformView = dummy.GetComponent<PhotonTransformView>();
            if (transformView != null) Destroy(transformView);

            var playerHealth = dummy.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                var dh = dummy.AddComponent<DummyHealth>();
                dh.Init(playerHealth.MaxHealth, 1000 + i);
                Destroy(playerHealth);

                if (HUD.Instance != null)
                    HUD.Instance.RegisterDummy(dh);
            }
        }
    }

    private void SpawnSpear()
    {
        var spearGO = PhotonNetwork.InstantiateRoomObject("Prefabs/Spear", Vector3.zero, Quaternion.identity);
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
    }

    public void NotifyPlayerDied(int deadActorNr)
    {
        if (!IsInMatch) return;

        if (PhotonNetwork.OfflineMode)
        {
            deadActors.Add(deadActorNr);
            int alive = totalPlayers - deadActors.Count;
            if (alive <= 1)
            {
                IsInMatch = false;
                bool isWinner = !deadActors.Contains(PhotonNetwork.LocalPlayer.ActorNumber);
                if (isWinner && EndScreenUI.Instance != null)
                    EndScreenUI.Instance.ShowVictory();
                else if (EndScreenUI.Instance != null)
                    EndScreenUI.Instance.ShowDefeat();
                StartCoroutine(ReturnToLobbyAfterDelay());
            }
            return;
        }

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
            photonView.RPC(nameof(RPC_EndMatch), RpcTarget.All, lastAliveActor, "LastManStanding");
        }
    }

    public void EndMatchByDisconnect()
    {
        if (!IsInMatch) return;
        IsInMatch = false;

        if (HUD.Instance != null)
            HUD.Instance.ShowFeedback("Un gladiador abandonó la arena");

        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        RPC_EndMatch(localActor, "Disconnect");
    }

    [PunRPC]
    private void RPC_EndMatch(int winnerActorNr, string reason)
    {
        IsInMatch = false;
        bool isWinner = PhotonNetwork.LocalPlayer.ActorNumber == winnerActorNr;

        if (EndScreenUI.Instance != null)
        {
            if (reason == "Disconnect")
                EndScreenUI.Instance.ShowDisconnectWin();
            else if (isWinner)
                EndScreenUI.Instance.ShowVictory();
            else
                EndScreenUI.Instance.ShowDefeat();
        }

        if (LobbyUI.Instance != null)
        {
            if (isWinner)
                LobbyUI.Instance.AddKill();
            else
                LobbyUI.Instance.AddDeath();
        }

        StartCoroutine(ReturnToLobbyAfterDelay());
    }

    private IEnumerator ReturnToLobbyAfterDelay()
    {
        float delay = matchConfig != null ? matchConfig.endScreenDelaySec : 3f;
        yield return new WaitForSeconds(delay);
        ReturnToLobby();
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
