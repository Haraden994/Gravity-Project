using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoText : MonoBehaviour
{
    public float letterPause = 0.2f;
    public GameObject skipButton;
    public GameObject trueButton;
    
    private AudioSource sfx;

    private string message;
    private Text textComp;

    private bool once = true;
    private bool skipped;

    // Use this for initialization
    void OnEnable () {
        
        textComp = GetComponent<Text>();
        sfx = GetComponent<AudioSource>();
        
        message = textComp.text;
        textComp.text = "";
        StartCoroutine(TypeText());
    }

    private void OnDisable()
    {
        textComp.text = message;
    }

    IEnumerator TypeText()
    {
        for (int i = 0; i < message.Length; i++)
        {
            if (skipped)
                break;
            
            textComp.text += message[i];
            if (sfx)
            {
                if (once)
                {
                    sfx.Play();
                }
                if (i == message.Length - 1)
                    sfx.Stop();
            }
            
            if(skipButton && trueButton)
                Check();
            
            yield return new WaitForSeconds(letterPause);
        }
    }

    public void Skip()
    {
        skipButton.SetActive(false);
        if(trueButton)
            trueButton.SetActive(true);
        
        skipped = true;
        textComp.text = message;
    }

    void Check()
    {
        if (textComp.text.Equals(message))
        {
            skipButton.SetActive(false);
            trueButton.SetActive(true);
        }
    }
}
