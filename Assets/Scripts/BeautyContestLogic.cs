using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class BeautyContestLogic : NetworkBehaviour
{
    public static BeautyContestLogic Instance { get; private set; }

    private int _playerCount;
    
    private double _targetNumber;

    public int _deadPlayers;

    private bool _rule1Active = false;
    private bool _rule2Active = false;
    private bool _rule3Active = false;

    private Player _winner;
    private void Awake()
    {
        Instance = this;
    }
    public void StartGame()
    {
        // Reset everything at the start of the game
        _playerCount = 0;
        _deadPlayers = 0;

        foreach(Player p in GameManager.Instance.beautyContestPlayers)
        {
            p.lives.Value = 2;
            p.alive.Value = true;
            p.validChoice.Value = true;
            p.isDuplicate.Value = false;
            _playerCount++;
        }

        GameManager.Instance.playerUI.GenerateHealthUI();
        GameManager.Instance.playerUI.GenerateButtons();
    }

    void StartRound()
    {
        CheckForNewRulesUpdate();
        ActivateButtonsClientRpc();
    }

    void CheckForNewRulesUpdate()
    {
        if (_deadPlayers >= 1) _rule1Active = true; 
        if (_deadPlayers >= 2) _rule2Active = true; 
        if (_playerCount == 2) _rule3Active = true; 
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
                var p1 = GameManager.Instance.beautyContestPlayers[i];
                var p2 = GameManager.Instance.beautyContestPlayers[j];

                if (p1.chosenNumber.Value != p2.chosenNumber.Value) continue;
                
                p1.isDuplicate.Value = true;
                p2.isDuplicate.Value = true;
            }
        }

        // Apply penalties + invalidate choices
        foreach (var p in GameManager.Instance.beautyContestPlayers)
        {
            if (p.isDuplicate.Value)
            {
                UpdateHealth(p, 1);
                p.validChoice.Value = false;
            }
        }
    }
    // Rule 2 checks if any player chose the exact number, and if they do, all the other players get -2
    void Rule2(double target)
    {
        // Return if rule isnt active
        if (!_rule2Active) return;

        Debug.Log("Rule 2 applied");
        
        int roundedTarget = (int)Math.Round(target, MidpointRounding.AwayFromZero);

        var players = GameManager.Instance.beautyContestPlayers;

        foreach (var winner in players)
        {
            if (winner.chosenNumber.Value != roundedTarget) continue;

            Debug.Log($"Player {winner.OwnerClientId} matched target {roundedTarget}");

            foreach (var other in players)
            {
                if (other.OwnerClientId == winner.OwnerClientId || !other.alive.Value) continue;

                UpdateHealth(other, 1);
                Debug.Log($"Health updated for {other.OwnerClientId}");
            }

            break; // optional: only 1 winner possible, so exit outer loop
        }
    }
    void Rule3()
    {
        // Return if rule isnt active
        if (!_rule3Active) return;

        if (_playerCount != 2) return;

        var p1 = GameManager.Instance.beautyContestPlayers[0];
        var p2 = GameManager.Instance.beautyContestPlayers[1];

        int v1 = p1.chosenNumber.Value;
        int v2 = p2.chosenNumber.Value;

        if (!((v1 == 100 || v2 == 100) && (v1 == 0 || v2 == 0))) return;

        if (v1 == 0) UpdateHealth(p1, 1);
        else if (v2 == 0) UpdateHealth(p2, 1);

        p1.validChoice.Value = false;
        p2.validChoice.Value = false;
    }

    void CheckForDeath()
    { 
        // Collect players to remove first
        List<Player> toRemove = new List<Player>();

        foreach (Player p in GameManager.Instance.beautyContestPlayers)
        {
            if (p.lives.Value <= 0)
            {
                p.alive.Value = false;
                toRemove.Add(p);
                _deadPlayers++;
                _playerCount--;
            }
        }

        // Remove AFTER enumeration
        foreach (Player p in toRemove)
        {
            GameManager.Instance.beautyContestPlayers.Remove(p);
            GameManager.Instance.beautyContestPlayersIds.Remove(p.NetworkObject);
        }
    }

    void CheckForWinner()
    {
        if (_playerCount == 1)
        {
            _winner = GameManager.Instance.beautyContestPlayers[0];
        }
        else if(_playerCount == 0)
        {
            _winner = null;
        }
    }

    // Player.cs script runs this every time it gets information from PlayerUI that button was pressed
    public void CheckIfAllPlayersChosen()
    {
        foreach (Player p in GameManager.Instance.beautyContestPlayers)
        {
            if (!p.isNumberChosen.Value && p.alive.Value) return; // Check if player has chosen a number while he is alive
        }
        Calculate();
    }

    void Calculate()
    {
        double sumOfChoices = 0;
        double closestNumber = double.MaxValue;
        double winnerNumber = 0;

        Debug.Log($"Current player count: {_playerCount}");

        Rule1();

        foreach (Player p in GameManager.Instance.beautyContestPlayers)
        {
            if (!p.alive.Value) continue;

            p.isNumberChosen.Value = false;

            if (!p.validChoice.Value) continue;

            sumOfChoices += p.chosenNumber.Value;
        }

        _targetNumber = sumOfChoices / _playerCount * 0.8;

        Debug.Log("Target is: " + _targetNumber + " because sum of Choices is " + sumOfChoices + " and amount of people: " + _playerCount);

        Rule2(_targetNumber);
        Rule3();

        // Find out the closest number players
        foreach (Player p in GameManager.Instance.beautyContestPlayers)
        {
            if (p.alive.Value && p.validChoice.Value)
            {
                double tempClosest;

                tempClosest = Math.Abs(_targetNumber - p.chosenNumber.Value);
                if (tempClosest < closestNumber)
                {
                    closestNumber = tempClosest;
                    winnerNumber = p.chosenNumber.Value;
                }
            }
        }

        foreach (Player p in GameManager.Instance.beautyContestPlayers)
        {
            if (p.chosenNumber.Value != winnerNumber && p.alive.Value && p.validChoice.Value)
            {
                UpdateHealth(p, 1);
            }
            p.validChoice.Value = true;
            p.isDuplicate.Value = false;
        }

        CheckForDeath();
        CheckForWinner();

        GenerateScoreScreenUIClientRpc();

        // Make delay so game doesnt crash or bug
        if (IsServer && _playerCount >= 2) StartCoroutine(NextRoundAfterDelay());
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
    [ClientRpc]
    void GenerateScoreScreenUIClientRpc()
    {
        // ADD DELAYYYYYYYYY 
        GameManager.Instance.playerUI.GenerateScoreScreenUI();
    }

}
