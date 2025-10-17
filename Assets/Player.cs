using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public string playerName;

    public int chosenNumber = -10;

    public int score = 0;

    public bool isReady = false;

    public GameManager gameManager;

    private FirstPersonMovement _controller;
    private Camera _camera;
    private Animator _animator;

    public override void OnNetworkSpawn()
    {
        _controller = GetComponent<FirstPersonMovement>();
        _camera = GetComponentInChildren<Camera>();

        if (!IsOwner)
        {
            _camera.enabled = false;
            
        }
    }
    void Update()
    {
        if (!IsOwner)
        {
            return;
        }
        if (!isReady && Input.GetKeyDown(KeyCode.H))
        {
            PressReady(); // same function you used for the button
        }
    }
    public void PressReady()
    {
        isReady = true;
        Debug.Log(name + " is ready!");

        gameManager.CheckAllPlayersReady();
    }


}

