using System;
using System.Collections.Generic;
using TMPro;
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

    private void OnNumberClicked(int num)
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
        Debug.Log("Clicked: " + num);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        chosenNumber = num;

        if (localPlayer != null)
        {
            localPlayer.ChooseNumberServerRpc(num);
        }
        DeactivateButtons();
    }

    public void ActivateButtons(int num)
    {
        foreach (Button btn in buttons)
        {
            btn.gameObject.SetActive(true);
        }
    }

    public void DeactivateButtons()
    {
        foreach (Button btn in buttons)
        {
            btn.gameObject.SetActive(false);
        }
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

        UpdateHealthText(5);

    }
    public void UpdateHealthText(int amount)
    {
        hlthObj.GetComponentInChildren<TextMeshProUGUI>().text = amount.ToString();
    }
}
