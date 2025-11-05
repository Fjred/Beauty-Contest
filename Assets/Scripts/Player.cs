using System.Collections.Generic;
using System.Diagnostics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Collections;
public class Player : NetworkBehaviour
{
    public NetworkVariable<FixedString128Bytes> playerName = new NetworkVariable<FixedString128Bytes>();

    // Synced variables
    public NetworkVariable<int> chosenNumber = new NetworkVariable<int>(-10);
    public NetworkVariable<int> lives = new NetworkVariable<int>(2);
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isNumberChosen = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> alive = new NetworkVariable<bool>(true);
    public NetworkVariable<bool> isDuplicate = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> validChoice = new NetworkVariable<bool>(true);
    public override void OnNetworkSpawn()
    {
        // --- Components ---
        FirstPersonMovement controller = GetComponent<FirstPersonMovement>();
        Camera cam = GetComponentInChildren<Camera>();
        FirstPersonAudio audio = GetComponentInChildren<FirstPersonAudio>();
        GroundCheck ground = GetComponentInChildren<GroundCheck>();
        AudioListener listener = GetComponentInChildren<AudioListener>();

        // --- Cursor setup ---
        if (IsOwner)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // --- Enable only for owner ---
        if (!IsOwner)
        {
            if (cam != null) cam.enabled = false;
            if (controller != null) controller.enabled = false;
            if (audio != null) audio.enabled = false;
            if (ground != null) ground.enabled = false;
            if (listener != null) listener.enabled = false;
        }

        // --- NetworkVariable setup ---
        if (IsServer)
        {
            playerName.Value = new FixedString128Bytes("Player " + OwnerClientId);
        }

    }

    void Update()
    {
        if (!IsOwner) return; // only the local player can press keys

        if (!isReady.Value && Input.GetKeyDown(KeyCode.H))
        {
            PressReady();
        }
    }

    public void PressReady()
    {
        if (!IsOwner) return;
        PressReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = true)]
    void PressReadyServerRpc()
    {
        isReady.Value = true;
        print(playerName + " is ready!");

        GameManager.Instance.beautyContestPlayers.Add(this);
        GameManager.Instance.beautyContestPlayersIds.Add(this.NetworkObject);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckAllPlayersReady();
        }
    }

    [ServerRpc(RequireOwnership = true)]
    public void ChooseNumberServerRpc(int num)
    {
        chosenNumber.Value = num;
        isNumberChosen.Value = true;
        if (BeautyContestLogic.Instance != null)
        {
            BeautyContestLogic.Instance.CheckIfAllPlayersChosen();
        }
    }

    [ClientRpc]
    public void UpdateHealthUIClientRpc(int newHealth, ClientRpcParams clientRpcParams = default)
    {
        // Only runs on the target client
        GameManager.Instance.playerUI.UpdateHealthText(newHealth);
    }
}
