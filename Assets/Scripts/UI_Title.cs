using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI_Title : MonoBehaviour
{
    //The player controls the cursor and select an option.
    public Image cursor;
    public TextMeshProUGUI start;
    public TextMeshProUGUI help;
    TextMeshProUGUI[] menu;
    const int MENU_TOTAL = 2;
    const int START = 0;
    const int HELP = 1;
    int currentOption;          //used to select menu option.

    private void Awake()
    {
        menu = new TextMeshProUGUI[MENU_TOTAL];
        menu[START] = start;
        menu[HELP] = help;

        //cursor always begins at Start
        cursor.transform.position = new Vector3(menu[START].transform.position.x - (menu[START].rectTransform.rect.width / 1.2f), cursor.transform.position.y, 0);
        currentOption = START;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentOption++;
            if (currentOption >= MENU_TOTAL)
                currentOption = START;

            cursor.transform.position = new Vector3(menu[currentOption].transform.position.x - (menu[currentOption].rectTransform.rect.width / 1.2f),
                menu[currentOption].transform.position.y, 0);
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentOption--;
            if (currentOption < 0)
                currentOption = HELP;

            cursor.transform.position = new Vector3(menu[currentOption].transform.position.x - (menu[currentOption].rectTransform.rect.width / 1.2f),
                menu[currentOption].transform.position.y, 0);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //check which menu option we're on and change scene
            if (currentOption == START)
                SceneManager.LoadScene("GameScene");
            else if (currentOption == HELP)
                SceneManager.LoadScene("HelpScene");
        }
    }
}
