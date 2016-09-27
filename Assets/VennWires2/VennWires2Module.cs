using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Reflection;

public class VennWires2Module : MonoBehaviour
{
    private WireInfo[] wires;
    private bool activated;
    private List<int> leds;
    private List<Color> colors = new List<Color>() { Color.black, Color.white, Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };
    private KMBombModule module;
    private KMBombInfo bombInfo;
    private KMAudio kmAudio;

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
        wires = new WireInfo[6];
        for (int i = 0; i < wires.Length; i++)
        {
            WireInfo info = new WireInfo();
            i += 1;
            info.SelectableOjbect = FindChildrenGO(gameObject, "VennWire" + i).GetComponent<KMSelectable>();
            info.WireUnsnippedObject = FindChildrenGO(gameObject, "Wires_Unsnipped_" + i);
            info.WireSnippedObject = FindChildrenGO(gameObject, "Wires_Snipped_" + i);
            info.LedONObject = FindChildrenGO(gameObject, "LED_" + i + "_ON");
            info.LedOFFObject = FindChildrenGO(gameObject, "LED_" + i);
            info.TextMeshGameObject = FindChildrenGO(gameObject, "Text" + i).GetComponent<TextMesh>();
            i -= 1;
            wires[i] = info;
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
            //Much hax
            foreach (MonoBehaviour component in info.SelectableOjbect.GetComponents<MonoBehaviour>())
            {
                if (component.GetType().FullName == "ModSelectable")
                {
                    component.GetType().BaseType.GetField("ForceInteractionHighlight", BindingFlags.Public | BindingFlags.Instance).SetValue(component, true);
                }
            }
        }
        Module.OnActivate += OnActivate;
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
        if (!CheckWireCut(info))
            Module.HandleStrike();
        //Hacky way to update the highlight
        info.WireUnsnippedObject.GetComponent<MeshFilter>().mesh = info.WireSnippedObject.GetComponent<MeshFilter>().mesh;
        info.WireUnsnippedObject.GetComponent<MeshRenderer>().materials = info.WireSnippedObject.GetComponent<MeshRenderer>().materials;
        info.WireUnsnippedObject.transform.position = info.WireSnippedObject.transform.position;
        info.WireUnsnippedObject.transform.rotation = info.WireSnippedObject.transform.rotation;
        BoxCollider oldCollider = info.WireUnsnippedObject.GetComponent<BoxCollider>();
        BoxCollider newCollider = info.WireUnsnippedObject.AddComponent<BoxCollider>();
        oldCollider.center = newCollider.center;
        oldCollider.size = newCollider.size;
        Destroy(newCollider);
        info.WireUnsnippedObject.transform.GetChild(0).GetComponent<MeshFilter>().mesh = info.WireSnippedObject.GetComponent<MeshFilter>().mesh;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, info.SelectableOjbect.transform);
        foreach (WireInfo wire in wires)
            if (CheckWireCut(wire) && wire.WireUnsnippedObject.transform.position != wire.WireSnippedObject.transform.position)
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

    public GameObject FindChildrenGO(GameObject parent, string childrenName)
    {
        foreach (Transform children in parent.transform)
        {
            if (children.gameObject.name == childrenName)
                return children.gameObject;
            GameObject go = FindChildrenGO(children.gameObject, childrenName);
            if (go != null)
                return go;
        }
        return null;
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

    public KMAudio Audio
    {
        get
        {
            if (kmAudio == null)
                kmAudio = GetComponent<KMAudio>();
            return kmAudio;
        }
    }
}
