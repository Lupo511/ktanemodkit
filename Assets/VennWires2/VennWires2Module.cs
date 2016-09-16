using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class VennWires2Module : MonoBehaviour
{
    public WireInfo[] wires;
    private bool activated;
    private List<int> leds;
    private List<Color> colors = new List<Color>() { Color.black, Color.white, Color.red, Color.blue, Color.green, Color.yellow };
    private KMBombModule module;
    private KMBombInfo bombInfo;

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
        leds = new List<int>();
        Module.OnActivate += OnActivate;
        for (int i = 0; i < wires.Length; i++)
        {
            WireInfo info = wires[i];
            if (Random.Range(0, 2) == 1)
                leds.Add(i);
            int text = Random.Range(0, 3);
            if (text == 0)
                info.TextMeshGameObject.text = "";
            else if (text == 1)
                info.TextMeshGameObject.text = "A";
            else
                info.TextMeshGameObject.text = "B";
            Color color = colors[Random.Range(0, colors.Count)];
            info.WireUnsnippedObject.GetComponent<Renderer>().material.color = color;
            foreach (Renderer rend in info.WireSnippedObject.GetComponentsInChildren<Renderer>())
            {
                foreach (Material mat in rend.materials)
                {
                    if (mat.name.StartsWith("WiresColor"))
                        mat.color = color;
                }
            }
            info.SelectableOjbect.OnInteract += delegate ()
            {
                return OnWireInteract(info);
            };
        }
    }

    public void OnActivate()
    {
        activated = true;
        foreach (int i in leds)
        {
            wires[i].LedOFFObject.SetActive(false);
            wires[i].LedONObject.SetActive(true);
        }
    }

    public bool OnWireInteract(WireInfo info)
    {
        if (!activated)
            return false;
        if (!CheckWireCut(info))
            Module.HandleStrike();
        info.SelectableOjbect.Highlight = info.WireSnippedObject.GetComponent<KMHighlightable>();
        info.WireUnsnippedObject.SetActive(false);
        info.WireSnippedObject.SetActive(true);
        foreach (WireInfo wire in wires)
            if (CheckWireCut(wire) && wire.WireUnsnippedObject.activeSelf == true)
                return false;
        Module.HandlePass();
        return false;
    }

    public void Update()
    {

    }

    public void OnDeactivate()
    {
        activated = false;
    }

    public bool CheckWireCut(WireInfo index)
    {
        bool cut = false;
        if (MustInvert())
            cut = !cut;
        return cut;
    }

    public bool MustInvert()
    {
        foreach (string ports in BombInfo.QueryWidgets(KMBombInfo.QUERYKEY_GET_PORTS, null))
        {
            Dictionary<string, List<string>> portInfo = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(ports);
            foreach (string port in portInfo["presentPorts"])
            {
                if (port == "StereoRCA")
                {
                    return true;
                }
            }
        }
        return false;
    }

    public KMBombModule Module
    {
        get
        {
            if (module == null)
                module = GetComponent<KMBombModule>();
            return module;
        }
    }

    public KMBombInfo BombInfo
    {
        get
        {
            if (bombInfo == null)
                bombInfo = GetComponent<KMBombInfo>();
            return bombInfo;
        }
    }
}
