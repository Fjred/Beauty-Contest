using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class PlayerUI : MonoBehaviour
{ 
    [Header("Button essentials")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private int totalButtons = 101;

    private List<Button> buttons = new List<Button>();
    private bool generated = false;

    [Header("Button starting X and Y")]
    [SerializeField] int startingX;
    [SerializeField] int startingY;

    private int chosenNumber;
    [Header("Health essentials")]
    [SerializeField] private GameObject healthUIPrefab;
    private GameObject hlthObj;
    
    [Header("Score UI")]
    [SerializeField] private GameObject backgroundUIPrefab;
    [SerializeField] private GameObject playerUIPrefab;
    [SerializeField] private GameObject scoreUIPrefab;

    public float baseSpacing = 130f;

    public void GenerateButtons()
    {
        if (generated) return; // prevent double spawning

        int numX = 8;
        int numY = 0;

        for (int i = 0; i < totalButtons; i++)
        {
            GameObject btnObj = Instantiate(buttonPrefab, transform);

            int x;
            int y;
            
            if(numX == 9)
            {
                numX = 0;
                numY += 1;
            }
            else
            {
                numX++;
            }
   
            x = startingX + numX * 35;
            y = startingY - numY * 35;

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            rect.anchoredPosition = new Vector2(x, y);

            int number = i;
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = number.ToString();

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnNumberClicked(number));

            buttons.Add(btn);
        }

        generated = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }


    private void OnNumberClicked(int num) {
        Player localPlayer = null; 
        foreach (var p in FindObjectsOfType<Player>()) 
        { 
            if (p.IsOwner) { 
                localPlayer = p; 
                break; 
            } 
        }
        Debug.Log("Clicked: " + num); 
        chosenNumber = num; 
        if (localPlayer != null && localPlayer.alive.Value) { 
            localPlayer.ChooseNumberServerRpc(num); 
        } 
        DeactivateButtons(); 
    }

    public void ActivateButtons()
    {
        Player localPlayer = null;
        foreach (var p in FindObjectsOfType<Player>())
        {
            if (p.IsOwner)
            {
                localPlayer = p;
                break;
            }
        }
        if (!localPlayer.alive.Value) return;

        foreach (Button btn in buttons)
        {
            btn.gameObject.SetActive(true);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

    }

    public void DeactivateButtons()
    {
        foreach (Button btn in buttons)
        {
            btn.gameObject.SetActive(false);
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void DestroyAllButtons()
    {
        foreach (Button btn in buttons)
            Destroy(btn.gameObject);
         
        buttons.Clear();
        generated = false;
    }

    public void GenerateHealthUI()
    {
        hlthObj = Instantiate(healthUIPrefab, transform);
        RectTransform rect = hlthObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);

        rect.anchoredPosition = new Vector2(100, -50);

        UpdateHealthText(2);

    }
    public void UpdateHealthText(int amount)
    {
        hlthObj.GetComponentInChildren<TextMeshProUGUI>().text = amount.ToString();
    }

    public void GenerateScoreScreenUI()
    {
        // Make sure all players are spawned before calling this
        Debug.Log("Generating score UI...");

        // Clear old UI
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // Instantiate background
        Instantiate(backgroundUIPrefab, transform);

        int playerCount = GameManager.Instance.beautyContestPlayersIds.Count;

        for (int i = 0; i < playerCount; i++)
        {
            var objRef = GameManager.Instance.beautyContestPlayersIds[i];
            if (!objRef.TryGet(out var netObj))
            {
                Debug.LogWarning($"Failed to get network object for player {i}");
                continue;
            }

            Player player = netObj.GetComponent<Player>();
            if (player == null)
            {
                Debug.LogWarning($"Missing Player component on net object {netObj}");
                continue;
            }

            // Create the UI element
            GameObject playerObj = Instantiate(playerUIPrefab, transform);

            // Set number text
            var scoreText = playerObj.GetComponentInChildren<TextMeshProUGUI>();
            if (scoreText != null) scoreText.text = player.chosenNumber.Value.ToString();

            // Position based on total players
            RectTransform rect = playerObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(GetPlayerX(i, playerCount), 0);
        }
    }

    private float GetPlayerX(int index, int totalPlayers)
    {
        if (totalPlayers % 2 == 1) // odd count
        {
            int middle = totalPlayers / 2;
            return (index - middle) * baseSpacing;
        }
        else // even count
        {
            float middleOffset = (totalPlayers / 2 - 0.5f);
            return (index - middleOffset) * baseSpacing;
        }
    }

}
