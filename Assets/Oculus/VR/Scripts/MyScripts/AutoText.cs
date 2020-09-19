using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoText : MonoBehaviour
{
    public float letterPause = 0.2f;
    public bool isOnEnable;
    private AudioSource sfx;

    private string message;
    private Text textComp;

    private bool once = true;
 
    // Use this for initialization
    void Start () {
        
        textComp = GetComponent<Text>();
        sfx = GetComponent<AudioSource>();
        
        message = textComp.text;
        textComp.text = "";
        StartCoroutine(TypeText());
    }

    /*private void OnEnable()
    {
        message = textComp.text;
        textComp.text = "";
        StartCoroutine(TypeText());
    }*/

    IEnumerator TypeText()
    {
        for (int i = 0; i < message.Length; i++){
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

            yield return 0;
            yield return new WaitForSeconds(letterPause);
        }
    }
}
