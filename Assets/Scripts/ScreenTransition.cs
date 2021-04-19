using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script is used to fade the sceen to black at different points of the game.
public class ScreenTransition : MonoBehaviour
{
    public Animator transition;

    // Update is called once per frame
    void Update()
    {
        
    }


    public IEnumerator FadeToBlack()
    {
        transition.SetTrigger("Start");

        yield return new WaitForSeconds(1);

        //reset level or change level
    }

}
