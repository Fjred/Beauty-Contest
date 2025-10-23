using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs & Players")]
    public GameObject playerPrefab;
    public List<Player> players = new List<Player>();

    public bool generateButtons = false;
    public PlayerUI playerUI;

    void Awake()
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

    void Start()
    {
        // Make sure prefab is registered first
        NetworkManager.Singleton.AddNetworkPrefab(playerPrefab);

        // Spawn players on client connect 
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            if (!NetworkManager.Singleton.IsServer) return;
            SpawnPlayer(clientId);
        };
    }

    void SpawnPlayer(ulong clientId)
    {
        // Avoid duplicates
        if (players.Exists(p => p.OwnerClientId == clientId)) return;

        GameObject playerObj = Instantiate(playerPrefab);
        NetworkObject netObj = playerObj.GetComponent<NetworkObject>();

        // Assign correct ownership
        netObj.SpawnWithOwnership(clientId);

        Player playerScript = playerObj.GetComponent<Player>();
        players.Add(playerScript);
        playerScript.playerName = "Player " + clientId;
    }
    public void CheckAllPlayersReady()
    {
        foreach (Player p in players)
        {
            if (!p.isReady.Value) return;
        }
        StartGameClientRpc();
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        Debug.Log("Game Started on: " + NetworkManager.Singleton.LocalClientId);

        BeautyContestLogic.Instance.StartGame();
    }
}
