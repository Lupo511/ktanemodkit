/*using Assets.Scripts.Platform.PC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Assets.Scripts.Input;
using Assets.Scripts.Platform;
using Assets.Scripts.VR;
using Assets.Scripts.VR.Null;

namespace MultipleBombsAssembly
{
    public class VRTester
    {
        public static void SetMotionControls()
        {
            CustomPlatformUtil.ReplacePlatformUtil();

            if (KTInputManager.Instance.MotionControlsPrefab != null)
            {
                MotionControls motionControls = UnityEngine.Object.Instantiate<GameObject>(KTInputManager.Instance.MotionControlsPrefab).GetComponent<MotionControls>();
                typeof(KTInputManager).GetMethod("set_MotionControls", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(KTInputManager.Instance, new object[] { motionControls });
            }

            KTInputManager.Instance.SetControlType(ControlType.Motion, true);

            CustomVRController.CreateController();
        }
    }

    public class CustomPlatformUtil : PCPlatformUtil
    {
        public static void ReplacePlatformUtil()
        {
            GameObject newPlatformUtilGameObject = new GameObject("CustomPlatformUtil");
            DontDestroyOnLoad(newPlatformUtilGameObject);
            CustomPlatformUtil customPlatformUtil = newPlatformUtilGameObject.AddComponent<CustomPlatformUtil>();
            customPlatformUtil.OriginalQualityLevel = instance.OriginalQualityLevel;
            customPlatformUtil.PlatformSubtype = PlatformSubtypeEnum.NotApplicable;
            customPlatformUtil.PlayerController = instance.PlayerController;
            instance = customPlatformUtil;
        }

        public override bool IsControlTypeSupported(ControlType controlType)
        {
            if (controlType == ControlType.Motion)
                return true;
            return base.IsControlTypeSupported(controlType);
        }
    }

    public class CustomVRController : MonoBehaviour
    {
        private OverlapSphereZone overlapSphereZone;
        private float speed = 0.02f;
        private GameObject currentHoldable;
        private Selectable currentSelectable;

        public static CustomVRController CreateController()
        {
            GameObject sphereGameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereGameObject.name = "CustomController";
            sphereGameObject.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            sphereGameObject.GetComponent<Collider>().enabled = false;
            DontDestroyOnLoad(sphereGameObject);
            CustomVRController controller = sphereGameObject.AddComponent<CustomVRController>();
            controller.overlapSphereZone = sphereGameObject.AddComponent<OverlapSphereZone>();
            controller.overlapSphereZone.Radius = 0.015f;
            return controller;
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                transform.position = FindObjectOfType<BombBinder>().transform.position;
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log(transform.position);
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                transform.position = new Vector3(0.2f, 1.4f, -1.2f);
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                Collider[] colliders = new Collider[100];
                int count = Physics.OverlapSphereNonAlloc(transform.position, 0.015f, colliders, 2048);
                Debug.Log(count);
                for (int i = 0; i < count; i++)
                {
                    SelectableArea selectableArea = colliders[i].gameObject.GetComponent<SelectableArea>();
                    Debug.Log(selectableArea);
                    if (selectableArea != null)
                    {
                        Debug.Log(selectableArea.name);
                        Debug.Log(selectableArea.Selectable);
                        if (selectableArea.Selectable != null)
                            Debug.Log(selectableArea.Selectable.gameObject.name);
                        Debug.Log(selectableArea.IsActive);
                        Debug.Log(selectableArea.isActiveAndEnabled);
                        Debug.Log(selectableArea.Selectable != null);
                        Debug.Log(selectableArea.Selectable.GetComponent<FloatingHoldable>() == null);
                        if (selectableArea.IsActive && selectableArea.isActiveAndEnabled && selectableArea.Selectable != null && selectableArea.Selectable.GetComponent<FloatingHoldable>() == null)
                        {
                            Debug.Log("Ok");
                        }
                    }
                }
            }

            float speed = this.speed;
            if (Input.GetKey(KeyCode.LeftControl))
                speed *= 0.5f;
            if (Input.GetKey(KeyCode.UpArrow))
                transform.position += new Vector3(0, 0, speed);
            if (Input.GetKey(KeyCode.DownArrow))
                transform.position += new Vector3(0, 0, -speed);
            if (Input.GetKey(KeyCode.RightArrow))
                transform.position += new Vector3(speed, 0, 0);
            if (Input.GetKey(KeyCode.LeftArrow))
                transform.position += new Vector3(-speed, 0, 0);
            if (Input.GetKey(KeyCode.PageUp))
                transform.position += new Vector3(0, speed, 0);
            if (Input.GetKey(KeyCode.PageDown))
                transform.position += new Vector3(0, -speed, 0);

            GameObject holdable = overlapSphereZone.GetFloatingHoldableGO();
            if (Input.GetMouseButtonDown(0) && holdable != currentHoldable)
            {
                if (holdable != null)
                {
                    holdable.GetComponent<FloatingHoldable>().DisableMotionControls();
                    Selectable holdableSelectable = holdable.GetComponent<Selectable>();
                    if (holdableSelectable != null)
                        holdableSelectable.OnInteract();
                }
                if (currentHoldable != null)
                {
                    currentHoldable.GetComponent<FloatingHoldable>().EnableMotionControls();
                    currentHoldable.GetComponent<FloatingHoldable>().Reset();
                }
                currentHoldable = holdable;
            }

            Selectable selectable = overlapSphereZone.GetSelectable();
            if (selectable != currentSelectable)
            {
                if (currentSelectable != null)
                    currentSelectable.RemoveHighlight();
                if (selectable != null)
                    selectable.AddHighlight();
                currentSelectable = selectable;
            }
            if (Input.GetMouseButtonDown(1))
            {
                if (currentSelectable != null)
                {
                    currentSelectable.HandleInteract();
                }
            }

            if (currentHoldable != null)
            {
                currentHoldable.transform.position = SceneManager.Instance.PlayerController.FloatingHoldableTargetTransform.transform.position;
                currentHoldable.transform.eulerAngles = SceneManager.Instance.PlayerController.FloatingHoldableTargetTransform.transform.eulerAngles + new Vector3(-90, 0, 0);
            }
        }
    }
}
*/