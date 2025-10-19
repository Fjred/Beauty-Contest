using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ButtonGrid : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private int totalButtons = 100;

    private List<Button> buttons = new List<Button>();
    private bool generated = false;

    private int multiplier=10;

    // ✅ Call this when you want to spawn the buttons
    public void GenerateButtons()
    {
        if (generated) return; // prevent double spawning

        for (int i = 0; i < totalButtons; i++)
        {
            GameObject btnObj = Instantiate(buttonPrefab, transform);

            int x;
            int y;
            
            x = -125 + (i - 1) * 25;
            y = (x-1) / 9 * 25;

            Debug.Log(x);
            Debug.Log(y);
            btnObj.transform.position = new Vector2(x, y);

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
