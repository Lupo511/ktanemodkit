using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VennWires2Module : MonoBehaviour
{
    public WireInfo[] wires;
    private bool activated;
    private List<Color> colors = new List<Color>() { Color.black, Color.white, Color.red, Color.blue, Color.green, Color.yellow };

    [System.Serializable]
    public struct WireInfo
    {
        public KMSelectable SelectableOjbect;
        public GameObject WireUnsnippedObject;
        public GameObject WireSnippedObject;
        public GameObject LedONObject;
        public GameObject LedOFFObject;
        public TextMesh TextMeshGameObject;
    }

    public void Start()
    {
        foreach (WireInfo info in wires)
        {
            bool ledOn = Random.Range(0, 2) == 1;
            info.LedONObject.SetActive(ledOn);
            info.LedOFFObject.SetActive(!ledOn);
            int text = Random.Range(0, 3);
            if (text < 1)
                info.TextMeshGameObject.text = "";
            else if (text == 1)
                info.TextMeshGameObject.text = "A";
            else
                info.TextMeshGameObject.text = "B";
            info.WireUnsnippedObject.GetComponent<Renderer>().material.color = colors[Random.Range(0, colors.Count)];
        }
    }

    public void OnActivate()
    {
        activated = true;
    }

    public void Update()
    {

    }

    public void OnDeactivate()
    {
        activated = false;
    }
}
