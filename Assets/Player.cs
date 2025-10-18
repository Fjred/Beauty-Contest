using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public string playerName;

    // Synced variables
    public NetworkVariable<int> chosenNumber = new NetworkVariable<int>(-10);
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false);

    public GameManager gameManager;

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
}
