using UnityEngine;
using System.Reflection;
using System.Collections;

public class EnglishTestModule : MonoBehaviour
{
    private KMBombModule module;
    private bool activated;

    public void Start()
    {
        activated = false;

        module = GetComponent<KMBombModule>();
        module.OnActivate += OnActivate;

        foreach (MonoBehaviour component in findChildGameObjectByName(gameObject, "Submit Button").GetComponents<MonoBehaviour>())
        {
            if (component.GetType().FullName == "ModSelectable")
            {
                component.GetType().BaseType.GetField("ForceInteractionHighlight", BindingFlags.Public | BindingFlags.Instance).SetValue(component, true);
            }
        }
        foreach (MonoBehaviour component in findChildGameObjectByName(gameObject, "Left Button").GetComponents<MonoBehaviour>())
        {
            if (component.GetType().FullName == "ModSelectable")
            {
                component.GetType().BaseType.GetField("ForceInteractionHighlight", BindingFlags.Public | BindingFlags.Instance).SetValue(component, true);
            }
        }
        foreach (MonoBehaviour component in findChildGameObjectByName(gameObject, "Right Button").GetComponents<MonoBehaviour>())
        {
            if (component.GetType().FullName == "ModSelectable")
            {
                component.GetType().BaseType.GetField("ForceInteractionHighlight", BindingFlags.Public | BindingFlags.Instance).SetValue(component, true);
            }
        }
    }

    private void OnActivate()
    {
        activated = true;

        findChildGameObjectByName(gameObject, "Top Text").SetActive(true);
        findChildGameObjectByName(gameObject, "Bottom Text").SetActive(true);
    }

    private GameObject findChildGameObjectByName(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.gameObject.name == name)
                return child.gameObject;
            GameObject childGo = findChildGameObjectByName(child.gameObject, name);
            if (childGo != null)
                return childGo;
        }
        return null;
    }
}
