using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public string itemName;

    // add the item to the player's inventory
    public void PickupItem ()
    {
        FindObjectOfType<Player>().AddItemToInventory(itemName);
        Destroy(gameObject);
    }
}