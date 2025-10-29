using Unity.Netcode;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;
using Unity.VisualScripting;
public class BeautyContestLogic : NetworkBehaviour
{
    public static BeautyContestLogic Instance { get; private set; }

    private int playerCount;
    
    private double sumOfChoices;

    private double targetNumber;

    private double closestNumber = 1000;
    private double winnerNumber;

    private int deadPlayers = 0;

    private bool rule1Active = false;
    private bool rule2Active = false;
    private bool rule3Active = false;

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
        CheckForNewRulesUpdate();
        ActivateButtonsClientRpc();
    }

    void CheckForNewRulesUpdate()
    {
        if (deadPlayers >= 1) rule1Active = true; 
        if (deadPlayers >= 2) rule2Active = true; 
        if (deadPlayers >= 3) rule3Active = true; 
    }

    //Rule 1 checks if there are multiple players who chose the same number. If there are, disqualify them from this round and the left players continue to play by standart rules
    void Rule1()
    {
        if (!rule1Active) return;

        Debug.Log("Rule 1 apllied");

        for (int i = 0; i < playerCount; i++)
        {
            for (int j = i + 1; j < playerCount; j++)
            {
                var p1 = GameManager.Instance.players[i];
                var p2 = GameManager.Instance.players[j];

                if (p1.chosenNumber.Value == p2.chosenNumber.Value)
                {
                    p1.isDuplicate.Value = true;
                    p2.isDuplicate.Value = true;
                }
            }
        }

        // Apply penalties + invalidate choices
        foreach (var p in GameManager.Instance.players)
        {
            if (p.isDuplicate.Value)
            {
                UpdateHealth(p, 1);
                p.validChoice.Value = false;
            }
        }


    }
    void Rule2()
    {
        // Return if rule isnt active
        if (!rule2Active) return;
    }
    void Rule3()
    {
        // Return if rule isnt active
        if (!rule3Active) return;
    }

    void CheckForDeath()
    {
        deadPlayers = 0;

        foreach (Player p in GameManager.Instance.players)
        {
            if (p.lives.Value <= 0)
            {
                p.alive.Value = false;
            }

            if (!p.alive.Value) deadPlayers++;
        }

    }
    // Player.cs script runs this every time it gets information from PlayerUI that button was pressed
    public void CheckIfAllPlayersChosen()
    {
        foreach (Player p in GameManager.Instance.players)
        {
            if (!p.isNumberChosen.Value && p.alive.Value) return; // Check if player has chosen a number while he is alive
        }
        Calculate();
    }

    void Calculate()
    {
        foreach (Player p in GameManager.Instance.players)
        {
            if (p.alive.Value)
            {
                playerCount++;
            }
        }
        Debug.Log("Current player count: " + playerCount);
        Rule1();

        playerCount = 0;

        foreach (Player p in GameManager.Instance.players)
        {
            if (p.alive.Value)
            {
                // Revert the fact that player chose a number
                p.isNumberChosen.Value = false;
                if (p.validChoice.Value)
                {
                    playerCount++;
                    sumOfChoices += p.chosenNumber.Value;
                }
            }
        }

        targetNumber = sumOfChoices / playerCount * 0.8;

        Debug.Log("Target is: " + targetNumber + " because sum of Choices is " + sumOfChoices + " and amount of people: " + playerCount);


        // Find out the closest number players
        foreach (Player p in GameManager.Instance.players)
        {
            if (p.alive.Value && p.validChoice.Value)
            {
                double tempClosest;

                tempClosest = Math.Abs(targetNumber - p.chosenNumber.Value);
                if (tempClosest < closestNumber)
                {
                    closestNumber = tempClosest;
                    winnerNumber = p.chosenNumber.Value;
                }
            }
        }

        foreach (Player p in GameManager.Instance.players)
        {
            if (p.chosenNumber.Value != winnerNumber && p.alive.Value && p.validChoice.Value)
            {
                UpdateHealth(p, 1);
            }
            p.validChoice.Value = true;
            p.isDuplicate.Value = false;
        }

        CheckForDeath();

        // Make delay so game doesnt crash or bug
        if (IsServer) StartCoroutine(NextRoundAfterDelay());


    }

    private void UpdateHealth(Player p, int amount)
    {
        p.lives.Value -= amount;

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
