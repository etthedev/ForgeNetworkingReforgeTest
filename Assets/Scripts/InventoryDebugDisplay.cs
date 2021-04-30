using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDebugDisplay : MonoBehaviour
{
    public Text displayText;

    private void Start()
    {
        PlayerState.Instance.Inventory.onInventoryUpdated += UpdateDisplay;
        StartCoroutine(PeriodicCheck());
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.Inventory.onInventoryUpdated -= UpdateDisplay;
    }

    private void UpdateDisplay()
    {
        string text = "Inventory Items";
        IList<SavingVariable> itemList = PlayerState.Instance.Inventory.Items;
        for (int i = 0; i < itemList.Count; ++i)
        {
            text += "\n" + itemList[i].variableName;
        }

        displayText.text = text;
    }

    private IEnumerator PeriodicCheck()
    {
        while(true)
        {
            UpdateDisplay();

            yield return new WaitForSeconds(20f);
        }
    }
}
