using UnityEngine;
using System.Collections;

public class RotationManager : MonoBehaviour
{
    private GameObject bombGameObject;
    private Vector3 eulerAnglesDelta;

    public void Start()
    {
        bombGameObject = transform.GetChild(0).gameObject;
        bombGameObject.transform.parent = null;
        eulerAnglesDelta = new Vector3(0, 0, 0);
    }

    public void Update()
    {
        Vector3 calculatedEulerAngles = gameObject.transform.eulerAngles + eulerAnglesDelta;
        if (calculatedEulerAngles.x > 270 + 45)
        {
            eulerAnglesDelta -= new Vector3(90, 0, 0);
        }
        if (calculatedEulerAngles.x < 270 - 45)
        {
            eulerAnglesDelta += new Vector3(90, 0, 0);
        }
        bombGameObject.transform.position = transform.position;
        bombGameObject.transform.eulerAngles = transform.eulerAngles + eulerAnglesDelta;
    }
}
