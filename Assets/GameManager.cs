using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public List<Player> players;

    int roundNumber = 0;

    bool allowToPick = false; // allows player to choose number on his screen

    bool allReady = true;

    bool startGame = true;
    public void CheckAllPlayersReady()
    {
        foreach (Player p in players)
        {
            if (!p.isReady) return; // someone not ready yet
        }
        StartGame();
    }

    void StartGame()
    {
        print("AAAAAA");
        allowToPick = true;
    }
}
