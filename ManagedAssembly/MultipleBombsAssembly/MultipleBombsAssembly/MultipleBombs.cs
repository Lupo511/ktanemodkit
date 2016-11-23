using Assets.Scripts.Records;
using Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class MultipleBombs : MonoBehaviour
    {
        private bool gameplayInitialized;

        public void Awake()
        {
            gameplayInitialized = false;
        }

        public void Update()
        {
            if (SceneManager.Instance != null)
            {
                if (SceneManager.Instance.CurrentState == SceneManager.State.Gameplay)
                {
                    if (!gameplayInitialized)
                    {
                        Debug.Log("[MultipleBombs]Initializing multiple bombs");
                        gameplayInitialized = true;
                        foreach (BombComponent component in FindObjectOfType<Bomb>().BombComponents)
                        {
                            component.OnPass = onComponentPass;
                        }
                        Debug.Log("[MultipleBombs]Vanilla components initialized");
                        StartCoroutine(CreateNewBomb(SceneManager.Instance.GameplayState, FindObjectOfType<BombGenerator>()));
                        Debug.Log("[MultipleBombs]All bombs generated");
                    }
                }
                else if (gameplayInitialized)
                {
                    Debug.Log("[MultipleBombs]Cleaning custom bombs");
                    StopAllCoroutines();
                    foreach (Bomb bomb in FindObjectsOfType<Bomb>())
                    {
                        bomb.gameObject.SetActive(false);
                    }
                    gameplayInitialized = false;
                }
            }
        }

        private bool onComponentPass(BombComponent source)
        {
            Debug.Log("[MultipleBombs]A component was solved");
            if (source.Bomb.HasDetonated)
                return false;
            RecordManager.Instance.RecordModulePass();
            if (source.Bomb.IsSolved())
            {
                Debug.Log("[MultipleBombs]A bomb was solved");
                source.Bomb.GetTimer().StopTimer();
                source.Bomb.GetTimer().Blink(1.5f);
                DarkTonic.MasterAudio.MasterAudio.PlaySound3DAtTransformAndForget("bomb_defused", base.transform, 1f, null, 0f, null);
                if (BombEvents.OnBombSolved != null)
                {
                    BombEvents.OnBombSolved();
                }
                foreach (Bomb bomb in FindObjectsOfType<Bomb>())
                    if (!bomb.IsSolved())
                        return true;
                Debug.Log("[MultipleBombs]All bombs solved, what a winner!");
                SceneManager.Instance.GameplayState.OnWin();
                return true;
            }
            return false;
        }

        private IEnumerator CreateNewBomb(GameplayState gameplayState, BombGenerator bombGenerator)
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
            bomb.GetTimer().text.gameObject.SetActive(false);
            bomb.GetTimer().LightGlow.enabled = false;
            if (!bomb.HasDetonated)
            {
                foreach (BombComponent component in bomb.BombComponents)
                {
                    component.OnPass = onComponentPass;
                }
            }

            Debug.Log("[MultipleBombs]Bomb generated");
            yield return new WaitForSeconds(2f);

            Debug.Log("[MultipleBombs]Activating custom bomb timer");
            bomb.GetTimer().text.gameObject.SetActive(true);
            bomb.GetTimer().LightGlow.enabled = true;
            Debug.Log("[MultipleBombs]Custom bomb timer activated");
            yield return new WaitForSeconds(4f);

            Debug.Log("[MultipleBombs]Activating custom bomb components");
            bomb.WidgetManager.ActivateAllWidgets();
            if (!bomb.HasDetonated)
            {
                foreach (BombComponent component in bomb.BombComponents)
                {
                    component.Activate();
                }
            }
            Debug.Log("[MultipleBombs]Custom bomb activated");
        }
    }
}
