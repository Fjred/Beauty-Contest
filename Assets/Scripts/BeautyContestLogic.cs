using Unity.Netcode;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;
public class BeautyContestLogic : NetworkBehaviour
{
    public static BeautyContestLogic Instance { get; private set; }

    private int playerCount;
    
    private double sumOfChoices;

    private double targetNumber;

    private double closestNumber = 1000;
    private double winnerNumber;

    private bool elminated1 = false;
    private bool elminated2 = false;
    private bool elminated3 = false;

    private void Awake()
    {
        Instance = this;
    }
    public void StartGame()
    {
        // Reset everything at the start of the game
        playerCount = 0;
        sumOfChoices = 0;
        closestNumber = 1000;

        GameManager.Instance.playerUI.GenerateHealthUI();

        GameManager.Instance.playerUI.GenerateButtons(); // now runs on ALL clients
    }

    void StartRound()
    {
        playerCount = 0;
        sumOfChoices = 0;
        closestNumber = 1000;
        ActivateButtonsClientRpc();
    }

    // Player.cs script runs this every time it gets information from PlayerUI that button was pressed
    public void CheckIfAllPlayersChosen()
    {
        foreach (Player p in GameManager.Instance.players)
        {
            if (!p.isNumberChosen.Value) return;
        }
        Calculate();
    }

    void Calculate()
    {
        foreach (Player p in GameManager.Instance.players)
        {
            // Revert the fact that player chose a number
            p.isNumberChosen.Value = false;

            playerCount++;
            sumOfChoices += p.chosenNumber.Value;
        }

        targetNumber = sumOfChoices / playerCount * 0.8;

        Debug.Log("Target is: " + targetNumber + " because sum of Choices is " + sumOfChoices + " and amount of people: " + playerCount);


        // Find out the closest number players
        foreach (Player p in GameManager.Instance.players)
        {
            double tempClosest; 

            tempClosest = Math.Abs(targetNumber - p.chosenNumber.Value);
            if(tempClosest < closestNumber)
            {
                closestNumber = tempClosest;
                winnerNumber = p.chosenNumber.Value;
            }
        }

        foreach (Player p in GameManager.Instance.players)
        {
            if (p.chosenNumber.Value != winnerNumber)
            {
                p.lives.Value -= 1;

                // send UI update to only that client
                var clientParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { p.OwnerClientId }
                    }
                };
                p.UpdateHealthUIClientRpc(p.lives.Value, clientParams);
            }
        }

        // Make delay so game doesnt crash or bug
        if (IsServer) StartCoroutine(NextRoundAfterDelay());


    }
    private IEnumerator NextRoundAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        StartRound();
    }

    [ClientRpc]
    void ActivateButtonsClientRpc()
    {
        GameManager.Instance.playerUI.ActivateButtons();
    }

}
