using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasDebugger : MonoBehaviour
{
    public Text[] debugTextList;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateAcceleration(Vector3 acceleration)
    {
        debugTextList[1].text = acceleration.ToString();
    }
    
    public void ShowVectorsInCanvas(Vector3[] vectorsToShow)
    {
        for (int i = 0; i < debugTextList.Length; i++)
        {
            debugTextList[i].text = vectorsToShow[i].ToString();
        }
    }
}
