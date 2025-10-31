using Unity.Netcode;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;
using Unity.VisualScripting;
public class BeautyContestLogic : NetworkBehaviour
{
    public static BeautyContestLogic Instance { get; private set; }

    private int _playerCount;
    
    private double _sumOfChoices;

    private double _targetNumber;

    private double _closestNumber = 1000;
    private double _winnerNumber;

    private int _deadPlayers = 0;

    private bool _rule1Active = false;
    private bool _rule2Active = false;
    private bool _rule3Active = false;

    private void Awake()
    {
        Instance = this;
    }
    public void StartGame()
    {
        // Reset everything at the start of the game
        _playerCount = 0;
        _sumOfChoices = 0;
        _closestNumber = 1000;

        GameManager.Instance.playerUI.GenerateHealthUI();

        GameManager.Instance.playerUI.GenerateButtons(); // now runs on ALL clients
    }

    void StartRound()
    {
        _playerCount = 0;
        _sumOfChoices = 0;
        _closestNumber = 1000;
        CheckForNewRulesUpdate();
        ActivateButtonsClientRpc();
    }

    void CheckForNewRulesUpdate()
    {
        if (_deadPlayers >= 1) _rule1Active = true; 
        if (_deadPlayers >= 2) _rule2Active = true; 
        if (_deadPlayers >= 3) _rule3Active = true; 
    }

    //Rule 1 checks if there are multiple players who chose the same number. If there are, disqualify them from this round and the left players continue to play by standart rules
    void Rule1()
    {
        if (!_rule1Active) return;

        Debug.Log("Rule 1 apllied");

        for (int i = 0; i < _playerCount; i++)
        {
            for (int j = i + 1; j < _playerCount; j++)
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
    // Rule 2 checks if any player chose the exact number, and if they add, all the other players get -2
    void Rule2(double target)
    {
        // Return if rule isnt active
        if (!_rule2Active) return;

        Debug.Log("Rule 2 apllied");
        
        int roundedTarget = (int)Math.Round(target, MidpointRounding.AwayFromZero);


        foreach (Player p in GameManager.Instance.players)
        {
            if (p.chosenNumber.Value == roundedTarget)
            {
                Debug.Log("Player with exact match found");
                foreach(Player k in GameManager.Instance.players)
                {
                    if(k.OwnerClientId != p.OwnerClientId && k.alive.Value) UpdateHealth(k, 1);
                    Debug.Log("Health updated");
                }
            }
        }
    }
    void Rule3()
    {
        // Return if rule isnt active
        if (!_rule3Active) return;
    }

    void CheckForDeath()
    {
        _deadPlayers = 0;

        foreach (Player p in GameManager.Instance.players)
        {
            if (p.lives.Value <= 0)
            {
                p.alive.Value = false;
            }

            if (!p.alive.Value) _deadPlayers++;
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
                _playerCount++;
                p.validChoice.Value = true;
            }
        }
        Debug.Log("Current player count: " + _playerCount);

        Rule1();

        _playerCount = 0;

        foreach (Player p in GameManager.Instance.players)
        {
            if (p.alive.Value)
            {
                // Revert the fact that player chose a number
                p.isNumberChosen.Value = false;
                if (p.validChoice.Value)
                {
                    _playerCount++;
                    _sumOfChoices += p.chosenNumber.Value;
                }
            }
        }

        _targetNumber = _sumOfChoices / _playerCount * 0.8;

        Debug.Log("Target is: " + _targetNumber + " because sum of Choices is " + _sumOfChoices + " and amount of people: " + _playerCount);

        Rule2(_targetNumber);

        // Find out the closest number players
        foreach (Player p in GameManager.Instance.players)
        {
            if (p.alive.Value && p.validChoice.Value)
            {
                double tempClosest;

                tempClosest = Math.Abs(_targetNumber - p.chosenNumber.Value);
                if (tempClosest < _closestNumber)
                {
                    _closestNumber = tempClosest;
                    _winnerNumber = p.chosenNumber.Value;
                }
            }
        }

        foreach (Player p in GameManager.Instance.players)
        {
            if (p.chosenNumber.Value != _winnerNumber && p.alive.Value && p.validChoice.Value)
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
