using Unity.Netcode;
using UnityEngine;
using System;
using UnityEngine.UI;
public class BeautyContestLogic : NetworkBehaviour
{
    public static BeautyContestLogic Instance { get; private set; }

    private int playerCount;
    
    private double sumOfChoices;

    private double targetNumber;

    private double closestNumber = 1000;
    private double winnerNumber;
    private void Awake()
    {
        Instance = this;
    }
    public void StartGame()
    {
        // Reset everything at the start of a new round
        playerCount = 0;
        sumOfChoices = 0;
        closestNumber = 1000;

        GameManager.Instance.playerUI.GenerateHealthUI();

        GameManager.Instance.playerUI.GenerateButtons(); // now runs on ALL clients
    }
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
            playerCount++;
            sumOfChoices += p.chosenNumber.Value;
        }

        targetNumber = sumOfChoices / playerCount * 0.8;

        Debug.Log("Target is: " + targetNumber + " because sum of Choices is " + sumOfChoices + " and amount of people: " + playerCount);



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
    }
}
