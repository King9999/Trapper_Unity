using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI : MonoBehaviour
{
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI stageText;

    public void SetLivesText(int lives)
    {
        livesText.text = "x " + lives;
    }

    public void SetStageText(int stage)
    {
        stageText.text = "STAGE " + stage;
    }
}
