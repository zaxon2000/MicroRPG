using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    public TextMeshProUGUI levelText;       // text showing our current level
    public TextMeshProUGUI interactText;    // text showing what we can interact with currently
    public TextMeshProUGUI inventoryText;   // text showing what the player has in their inventory
    public Image healthBarFill;             // image fill for the health bar
    public Image xpBarFill;                 // image fill for the xp bar

    private Player player;

    void Awake ()
    {
        // get the player
        player = FindObjectOfType<Player>();
    }

    // update the level text
    public void UpdateLevelText ()
    {
        levelText.text = "Lvl\n" + player.curLevel;
    }

    // update the health bar fill amount
    public void UpdateHealthBar ()
    {
        healthBarFill.fillAmount = (float)player.curHp / (float)player.maxHp;
    }

    // update the xp bar fill amount
    public void UpdateXpBar ()
    {
        xpBarFill.fillAmount = (float)player.curXp / (float)player.xpToNextLevel;
    }

    // displays the player's inventory on screen
    public void UpdateInventoryText ()
    {
        inventoryText.text = "";

        foreach(string item in player.inventory)
        {
            inventoryText.text += item + "\n";
        }
    }

    // called when we can interact with something
    public void SetInteractText (Vector3 pos, string text)
    {
        interactText.gameObject.SetActive(true);
        interactText.text = text;

        // set the text position
        interactText.transform.position = Camera.main.WorldToScreenPoint(pos + Vector3.up);
    }

    // called when we can no longer interact with something
    public void DisableInteractText ()
    {
        if(interactText.gameObject.activeInHierarchy)
            interactText.gameObject.SetActive(false);
    }
}