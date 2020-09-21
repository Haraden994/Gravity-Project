using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyButton : MonoBehaviour
{
    public bool delayed;
    
    private GameManager _gm;

    // Start is called before the first frame update
    void Start()
    {
        _gm = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DelayedDisable(GameObject go)
    {
        StartCoroutine(Disable(go, 2.0f));
    }

    private IEnumerator Disable(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);

        go.SetActive(false);
        gameObject.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        _gm.StartLevel(sceneName);
    }
}
