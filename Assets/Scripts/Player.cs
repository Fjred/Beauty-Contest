using System.Diagnostics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public string playerName;

    // Synced variables
    public NetworkVariable<int> chosenNumber = new NetworkVariable<int>(-10);
    public NetworkVariable<int> lives = new NetworkVariable<int>(2);
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isNumberChosen = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        FirstPersonMovement controller = GetComponent<FirstPersonMovement>();
        Camera cam = GetComponentInChildren<Camera>();
        FirstPersonAudio audio = GetComponentInChildren<FirstPersonAudio>();
        GroundCheck ground = GetComponentInChildren<GroundCheck>();
        AudioListener listener = GetComponentInChildren<AudioListener>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (!IsOwner)
        {
            if (cam != null) cam.enabled = false; // only enable for owner
            if (controller != null) controller.enabled = false; // only enable for owner
            if (audio != null) audio.enabled = false; // only enable for owner
            if (ground != null) ground.enabled = false; // only enable for owner
            if (listener != null) listener.enabled = false; // only enable for owner
        }
        print($"Player spawned. Owner: {IsOwner}, ClientId: {OwnerClientId}");
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
