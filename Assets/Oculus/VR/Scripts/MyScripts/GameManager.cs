using System;
using System.Collections;
using System.Collections.Generic;
using OVR;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [SerializeField]
    private SoundFXRef mainMenuBGM;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals("TitleScreen"))
        {
            mainMenuBGM.PlaySoundAt(FindObjectOfType<Canvas>().gameObject.transform.position);
        }
        else
        {
            mainMenuBGM.StopSound();
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }
    
    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}