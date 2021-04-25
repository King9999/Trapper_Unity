using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Not much to this script. Just want to make the "Return to title" pulse.
public class UI_Help : MonoBehaviour
{
    public TextMeshProUGUI returnText;
    public Animator anim;
   
    bool alphaOn = true;
    const float MOD_VALUE = 12f;
    //public Image[] backgrounds = new Image[2];

    // Update is called once per frame
    void Update()
    {
        //scroll background
        /*for (int i = 0; i < backgrounds.Length; i++)
        {
            backgrounds[i].rectTransform.position = new Vector3(backgrounds[i].rectTransform.position.x - (MOD_VALUE * Time.deltaTime), backgrounds[i].rectTransform.position.y, 0);

            //shift one of the backgrounds to the right side of the other if one of them goes off screen.
           /* if (backgrounds[i].rectTransform.position.x + (backgrounds[i].rectTransform.rect.width / 2) <= -1 * Screen.width)
            {
                backgrounds[i].rectTransform.position = new Vector3(Screen.width - 1, backgrounds[i].rectTransform.position.y, 0);
            }
        }*/

        //update the alpha
        if (alphaOn)
        {
            returnText.alpha += Time.deltaTime;
        }
        else
        {
            returnText.alpha -= Time.deltaTime;
        }

        if (returnText.alpha <= 0)
        {
            alphaOn = true;
        }
        
        if (returnText.alpha >= 1)
        {
            alphaOn = false;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //call coroutine for transition
            StartCoroutine(LoadTitle());
            
        }

    }

    IEnumerator LoadTitle()
    {
        anim.SetTrigger("Start");
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("TitleScene");
    }
}
