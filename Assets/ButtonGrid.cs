using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class ButtonGrid : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private int totalButtons = 101;

    private List<Button> buttons = new List<Button>();
    private bool generated = false;

    private int multiplier=10;

    [SerializeField] int startingX;
    [SerializeField] int startingY;


    // ✅ Call this when you want to spawn the buttons
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

            Debug.Log(x);
            Debug.Log(y);
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
    }

    private void OnNumberClicked(int num)
    {
        Debug.Log("Clicked: " + num);
        // your logic per button
    }

    public void ActivateButton(int num)
    {
        if (num >= 0 && num < buttons.Count)
            buttons[num].interactable = true;
    }

    public void DeactivateButton(int num)
    {
        if (num >= 0 && num < buttons.Count)
            buttons[num].interactable = false;
    }

    public void DestroyAllButtons()
    {
        foreach (var btn in buttons)
            Destroy(btn.gameObject);

        buttons.Clear();
        generated = false;
    }
}
