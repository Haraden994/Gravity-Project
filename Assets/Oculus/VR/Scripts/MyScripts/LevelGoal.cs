using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    public GameObject victoryPanel;
    public GameObject tutorialPanel;

    private PauseManager pauseManager;

    void Start()
    {
        pauseManager = FindObjectOfType<PauseManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(tutorialPanel.activeSelf)
            tutorialPanel.SetActive(false);
        pauseManager.isTutorial = true;
        pauseManager.Pause();
        victoryPanel.SetActive(true);
    }
}
