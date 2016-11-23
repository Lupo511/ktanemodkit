using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class MultipleBombs : MonoBehaviour
    {
        private Rect windowRect;

        public void Awake()
        {
            windowRect = new Rect(0, 0, 300, 200);
        }

        public void Update()
        {

        }

        public void OnGUI()
        {
            windowRect = GUI.Window(1337, windowRect, windowFunction, "Multiple Bombs Debug");
        }

        private void windowFunction(int id)
        {
            if (id == 1337)
            {
                GUILayout.BeginVertical();
                if (GUILayout.Button("Generate new bomb"))
                {
                    Debug.Log("[MultipleBombs]New bomb requested");
                    GameplayState gameplayState = FindObjectOfType<GameplayState>();
                    BombGenerator bombGenerator = FindObjectOfType<BombGenerator>();
                    if (gameplayState != null)
                    {
                        if (bombGenerator != null)
                        {
                            Debug.Log("[MultipleBombs]Generating new bomb");

                            gameplayState.Room.BombSpawnPosition.transform.position += new Vector3(0.5f, 0, 0);

                            Bomb bomb = bombGenerator.CreateBomb(gameplayState.Mission.GeneratorSetting, gameplayState.Room.BombSpawnPosition, (new System.Random()).Next(), Assets.Scripts.Missions.BombTypeEnum.Default);

                            gameplayState.Room.BombSpawnPosition.transform.position -= new Vector3(0.5f, 0, 0);

                            GameObject roomGO = (GameObject)gameplayState.GetType().GetField("roomGO", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameplayState);

                            Selectable mainSelectable = roomGO.GetComponent<Selectable>();
                            List<Selectable> childern = mainSelectable.Children.ToList();
                            childern.Insert(2, bomb.GetComponent<Selectable>());
                            mainSelectable.Children = childern.ToArray();
                            bomb.GetComponent<Selectable>().Parent = mainSelectable;
                            bomb.GetTimer().gameObject.SetActive(false);
                            bomb.GetTimer().LightGlow.enabled = false;

                            Debug.Log("[MultipleBombs]Bomb generated");

                            bomb.WidgetManager.ActivateAllWidgets();
                            if (!bomb.HasDetonated)
                            {
                                foreach (BombComponent component in bomb.BombComponents)
                                {
                                    component.Activate();
                                }
                            }
                        }
                    }
                }
                GUILayout.EndVertical();
            }
        }
    }
}
