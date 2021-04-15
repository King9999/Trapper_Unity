using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script is used to create all game objects at runtime. It will also instantiate additional objects as needed.
public class GameObjectManager : MonoBehaviour
{
    //Sprite objSprite;

    public void SetupObject(GameObject obj, Sprite objSprite, Vector3 pos)
    {
        obj.AddComponent<SpriteRenderer>();
        obj.GetComponent<SpriteRenderer>().sprite = objSprite;
        obj.transform.position = pos;
    }

    //This creates prefabs
    /*public void CreateObject(GameObject obj, Vector3 pos, Quaternion rotation)
    {
        Instantiate(obj, pos, rotation);    //rotation doesn't apply so it
    }*/
}
