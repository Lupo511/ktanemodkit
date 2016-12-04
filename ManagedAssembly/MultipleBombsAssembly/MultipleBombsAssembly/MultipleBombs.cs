using Assets.Scripts.Missions;
using Assets.Scripts.Records;
using Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class MultipleBombs : MonoBehaviour
    {
        protected enum NeedyStateEnum
        {
            InitialSetup,
            AwaitingActivation,
            Running,
            Cooldown,
            Terminated,
            BombComplete
        }

        private bool setupRoomInitialized;
        private bool gameplayInitialized;
        private const int maxBombCount = 2;
        private int bombsCount;
        private Dictionary<KMBombInfo, Bomb> redirectedInfos;
        private Dictionary<string, int> multipleBombsMissions;
        private List<NeedyComponent> activatedNeedies;
        private MethodInfo gameplaySetBombMethod = typeof(GameplayState).GetMethod("set_Bomb", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo needystateField = typeof(NeedyComponent).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo needyplayerChangedBombEventCountField = typeof(NeedyComponent).GetField("playerChangedBombEventCount", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo needychangedBombResponseInProgressField = typeof(NeedyComponent).GetField("changedBombResponseInProgress", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo needyactivationChanceAfterFirstChangeField = typeof(NeedyComponent).GetField("activationChanceAfterFirstChange", BindingFlags.Instance | BindingFlags.NonPublic);
        private MethodInfo needyStartRunningMethod = typeof(NeedyComponent).GetMethod("StartRunning", BindingFlags.Instance | BindingFlags.NonPublic);
        private MethodInfo needyResetAndStartMethod = typeof(NeedyComponent).GetMethod("ResetAndStart", BindingFlags.Instance | BindingFlags.NonPublic);
        private ResultFreeplayPageMonitor defusedPageMonitor;
        private ResultFreeplayPageMonitor explodedPageMonitor;

        public void Awake()
        {
            Debug.Log("[MultipleBombs]Initializing");
            setupRoomInitialized = false;
            gameplayInitialized = false;
            bombsCount = 1;
            multipleBombsMissions = new Dictionary<string, int>();
        }

        public void Update()
        {
            if (SceneManager.Instance != null)
            {
                if (SceneManager.Instance.CurrentState == SceneManager.State.Setup)
                {
                    if (!setupRoomInitialized)
                    {
                        FreeplayDevice device = FindObjectOfType<FreeplayDevice>();
                        if (device != null)
                        {
                            Debug.Log("[MultipleBombs]Adding FreePlay option");
                            setupRoomInitialized = true;
                            GameObject modulesObject = device.ModuleCountIncrement.transform.parent.gameObject;
                            GameObject bombsObject = (GameObject)Instantiate(modulesObject, modulesObject.transform.position, modulesObject.transform.rotation, modulesObject.transform.parent);
                            device.ObjectsToDisableOnLidClose.Add(bombsObject);
                            bombsObject.transform.localPosition = modulesObject.transform.localPosition + new Vector3(0, 0f, -0.025f);
                            bombsObject.transform.FindChild("ModuleCountLabel").GetComponent<TextMeshPro>().text = "Bombs";
                            TextMeshPro valueText = bombsObject.transform.FindChild("ModuleCountValue").GetComponent<TextMeshPro>();
                            valueText.text = bombsCount.ToString();
                            bombsObject.transform.FindChild("ModuleCountLED").gameObject.SetActive(false);

                            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            background.GetComponent<Renderer>().material.color = Color.black;
                            background.transform.localScale = new Vector3(0.048f, 0.023f, 0.005f); //Accurate Y would be 0.025
                            background.transform.parent = bombsObject.transform;
                            background.transform.localPosition = valueText.gameObject.transform.localPosition + new Vector3(0.00025f, -0.0027f, 0);
                            background.transform.localEulerAngles = valueText.gameObject.transform.localEulerAngles;

                            GameObject incrementButton = bombsObject.transform.FindChild("Modules_INCR_btn").gameObject;
                            GameObject decrementButton = bombsObject.transform.FindChild("Modules_DECR_btn").gameObject;
                            Selectable deviceSelectable = device.GetComponent<Selectable>();
                            List<Selectable> children = deviceSelectable.Children.ToList();
                            children.Insert(2, incrementButton.GetComponent<Selectable>());
                            children.Insert(2, decrementButton.GetComponent<Selectable>());
                            deviceSelectable.Children = children.ToArray();
                            deviceSelectable.Init();
                            incrementButton.GetComponent<KeypadButton>().OnPush = new PushEvent(() =>
                            {
                                if (bombsCount >= maxBombCount)
                                    return;
                                bombsCount++;
                                valueText.text = bombsCount.ToString();
                            });
                            decrementButton.GetComponent<KeypadButton>().OnPush = new PushEvent(() =>
                            {
                                if (bombsCount <= 1)
                                    return;
                                bombsCount--;
                                valueText.text = bombsCount.ToString();
                            });
                            //string textColor = "#" + valueText.color.r.ToString("x2") + valueText.color.g.ToString("x2") + valueText.color.b.ToString("x2");
                            incrementButton.GetComponent<Selectable>().OnHighlight = new Action(() =>
                            {
                                device.Screen.CurrentState = FreeplayScreen.State.Start;
                                device.Screen.ScreenText.text = "BOMBS:\n\nNumber of bombs\nto defuse\n\n<size=20><#00ff00>Multiple Bombs Mod</color></size>";
                            });
                            decrementButton.GetComponent<Selectable>().OnHighlight = new Action(() =>
                            {
                                device.Screen.CurrentState = FreeplayScreen.State.Start;
                                device.Screen.ScreenText.text = "BOMBS:\n\nNumber of bombs\nto defuse\n\n<size=20><#00ff00>Multiple Bombs Mod</color></size>";
                            });
                            if (FreeplayDevice.MAX_SECONDS_TO_SOLVE == 600f)
                            {
                                float newMaxTime = FreeplayDevice.MAX_SECONDS_TO_SOLVE * maxBombCount;
                                if (ModManager.Instance.GetMaximumModules() > FreeplayDevice.MAX_MODULE_COUNT)
                                {
                                    newMaxTime += (ModManager.Instance.GetMaximumModules() - FreeplayDevice.MAX_MODULE_COUNT) * 60 * (maxBombCount - 1);
                                }
                                FreeplayDevice.MAX_SECONDS_TO_SOLVE = newMaxTime;
                            }

                            modulesObject.transform.FindChild("Modules_INCR_btn").GetComponent<Selectable>().OnHighlight = new Action(() =>
                            {
                                device.Screen.CurrentState = FreeplayScreen.State.Start;
                                device.Screen.ScreenText.text = "MODULES:\n\nNumber of modules\nper bomb";
                            });
                            modulesObject.transform.FindChild("Modules_DECR_btn").GetComponent<Selectable>().OnHighlight = new Action(() =>
                            {
                                device.Screen.CurrentState = FreeplayScreen.State.Start;
                                device.Screen.ScreenText.text = "MODULES:\n\nNumber of modules\nper bomb";
                            });

                            incrementButton.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.015f, -0.015f, -0.015f);
                            decrementButton.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.015f, -0.015f, -0.015f);
                            device.ModuleCountIncrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.012f, -0.012f, -0.012f);
                            device.ModuleCountDecrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.012f, -0.012f, -0.012f);
                            device.TimeIncrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.01f, -0.01f, -0.01f);
                            device.TimeDecrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.01f, -0.01f, -0.01f);
                            Debug.Log("[MultipleBombs]FreePlay option added");

                            foreach (ModMission mission in ModManager.Instance.ModMissions)
                            {
                                if (!multipleBombsMissions.ContainsKey(mission.ID))
                                {
                                    int bombCount = 1;
                                    for (int i = mission.GeneratorSetting.ComponentPools.Count - 1; i >= 0; i--)
                                    {
                                        ComponentPool pool = mission.GeneratorSetting.ComponentPools[i];
                                        if (pool.ModTypes != null && pool.ModTypes.Count == 1 && pool.ModTypes[0] == "Multiple Bombs")
                                        {
                                            mission.GeneratorSetting.ComponentPools.RemoveAt(i);
                                            bombCount++;
                                        }
                                    }
                                    if (bombCount >= 2)
                                    {
                                        if (bombCount > maxBombCount)
                                            bombCount = maxBombCount;
                                        multipleBombsMissions.Add(mission.ID, bombCount);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (setupRoomInitialized)
                {
                    setupRoomInitialized = false;
                }
                if (SceneManager.Instance.CurrentState == SceneManager.State.Gameplay && GameplayState.MissionToLoad == FreeplayMissionGenerator.FREEPLAY_MISSION_ID || multipleBombsMissions.ContainsKey(GameplayState.MissionToLoad))
                {
                    if (!gameplayInitialized)
                    {
                        int bombsToSpawn = 1;
                        if (GameplayState.MissionToLoad == FreeplayMissionGenerator.FREEPLAY_MISSION_ID)
                            bombsToSpawn = bombsCount;
                        else
                            bombsToSpawn = multipleBombsMissions[GameplayState.MissionToLoad];
                        if (bombsToSpawn == 1)
                            return;

                        Debug.Log("[MultipleBombs]Initializing multiple bombs");
                        gameplayInitialized = true;
                        redirectedInfos = new Dictionary<KMBombInfo, Bomb>();
                        activatedNeedies = new List<NeedyComponent>();
                        BombInfoRedirection.SetBombCount(bombsToSpawn);

                        Bomb vanillaBomb = FindObjectOfType<Bomb>();
                        foreach (BombComponent component in vanillaBomb.BombComponents)
                        {
                            component.OnPass = onComponentPass;
                        }
                        GameObject spawn0 = GameObject.Find("MultipleBombs_Spawn_0");
                        if (spawn0 != null)
                        {
                            vanillaBomb.gameObject.transform.position = spawn0.transform.position;
                            vanillaBomb.gameObject.transform.rotation = spawn0.transform.rotation;
                        }
                        else
                        {
                            vanillaBomb.gameObject.transform.position += new Vector3(-0.4f, 0, 0);
                            vanillaBomb.gameObject.transform.eulerAngles += new Vector3(0, -30, 0);
                        }
                        vanillaBomb.GetComponent<FloatingHoldable>().Initialize();
                        RedirectPresentBombInfos(vanillaBomb);
                        foreach (BombComponent component in vanillaBomb.BombComponents)
                        {
                            if (component is NeedyComponent)
                            {
                                component.StartCoroutine(startNeedyAfter((NeedyComponent)component, ((NeedyComponent)component).SecondsBeforeForcedActivation + 8f));
                            }
                        }
                        Debug.Log("[MultipleBombs]Default bomb initialized");

                        GameObject spawn1 = GameObject.Find("MultipleBombs_Spawn_1");
                        if (spawn1 != null)
                        {
                            StartCoroutine(createNewBomb(FindObjectOfType<BombGenerator>(), spawn1.transform.position, spawn1.transform.eulerAngles));
                        }
                        else
                        {
                            StartCoroutine(createNewBomb(FindObjectOfType<BombGenerator>(), SceneManager.Instance.GameplayState.Room.BombSpawnPosition.transform.position + new Vector3(0.4f, 0, 0), new Vector3(0, 30, 0)));
                        }
                        vanillaBomb.GetComponent<Selectable>().Parent.Init();
                        Debug.Log("[MultipleBombs]All bombs generated");

                        //Remove needies events (vanillaBomb is equal to SceneManager.Instance.GameplayState.Bomb)
                        vanillaBomb.GetTimer().TimerTick = new TimerComponent.TimerTickEvent((elapsed, remaining) => SceneManager.Instance.GameplayState.GetPaceMaker().OnTimerTick(elapsed, remaining));
                        BombComponentEvents.OnComponentPass = new BombComponentEvents.ComponentPassEvent((BombComponent component, bool finalPass) =>
                        {
                            SceneManager.Instance.GameplayState.GetPaceMaker().OnComponentPass(component, finalPass);
                            foreach (BombComponent bombComponent in component.Bomb.BombComponents)
                            {
                                if (bombComponent is NeedyComponent)
                                {
                                    if (bombComponent != component && !finalPass)
                                    {
                                        bombComponent.StartCoroutine(startNeedyChange((NeedyComponent)bombComponent, 0.25f));
                                    }
                                }
                            }
                        });
                        BombComponentEvents.OnComponentStrike = new BombComponentEvents.ComponentStrikeEvent((BombComponent component, bool finalStrike) =>
                        {
                            SceneManager.Instance.GameplayState.GetPaceMaker().OnComponentStrike(component, finalStrike);
                            foreach (BombComponent bombComponent in component.Bomb.BombComponents)
                            {
                                if (bombComponent is NeedyComponent)
                                {
                                    if (bombComponent != component && !finalStrike)
                                    {
                                        bombComponent.StartCoroutine(startNeedyChange((NeedyComponent)bombComponent, 0.25f));
                                    }
                                }
                            }
                        });
                        Debug.Log("[MultipleBombs]Needy events redirected");

                        //Setup results screen
                        if (defusedPageMonitor == null)
                        {
                            defusedPageMonitor = SceneManager.Instance.PostGameState.Room.BombBinder.ResultFreeplayDefusedPage.gameObject.AddComponent<ResultFreeplayPageMonitor>();
                        }
                        if (explodedPageMonitor == null)
                        {
                            explodedPageMonitor = SceneManager.Instance.PostGameState.Room.BombBinder.ResultFreeplayExplodedPage.gameObject.AddComponent<ResultFreeplayPageMonitor>();
                        }
                        defusedPageMonitor.SetBombCount(bombsToSpawn);
                        explodedPageMonitor.SetBombCount(bombsToSpawn);
                        Debug.Log("[MultipleBombs]Result screens initialized");
                    }
                    foreach (NeedyComponent component in activatedNeedies)
                    {
                        if ((NeedyStateEnum)needystateField.GetValue(component) == NeedyStateEnum.Cooldown)
                        {
                            component.StopAllCoroutines();
                            component.StartCoroutine(resetAndStartNeedy(component));
                            activatedNeedies.Remove(component);
                        }
                    }
                }
                else if (gameplayInitialized)
                {
                    Debug.Log("[MultipleBombs]Cleaning up");
                    StopAllCoroutines();
                    foreach (Bomb bomb in FindObjectsOfType<Bomb>())
                    {
                        bomb.gameObject.SetActive(false);
                    }
                    gameplayInitialized = false;
                    activatedNeedies.Clear();
                }
            }
        }

        private void RedirectPresentBombInfos(Bomb bomb)
        {
            foreach (KMBombInfo info in FindObjectsOfType<KMBombInfo>())
            {
                if (!redirectedInfos.ContainsKey(info))
                {
                    info.TimeHandler = new KMBombInfo.GetTimeHandler(() => BombInfoRedirection.GetTime(bomb));
                    info.FormattedTimeHandler = new KMBombInfo.GetFormattedTimeHandler(() => BombInfoRedirection.GetFormattedTime(bomb));
                    info.StrikesHandler = new KMBombInfo.GetStrikesHandler(() => BombInfoRedirection.GetStrikes(bomb));
                    info.ModuleNamesHandler = new KMBombInfo.GetModuleNamesHandler(() => BombInfoRedirection.GetModuleNames(bomb));
                    info.SolvableModuleNamesHandler = new KMBombInfo.GetSolvableModuleNamesHandler(() => BombInfoRedirection.GetSolvableModuleNames(bomb));
                    info.SolvedModuleNamesHandler = new KMBombInfo.GetSolvedModuleNamesHandler(() => BombInfoRedirection.GetSolvedModuleNames(bomb));
                    info.WidgetQueryResponsesHandler = new KMBombInfo.GetWidgetQueryResponsesHandler((string queryKey, string queryInfo) => BombInfoRedirection.GetWidgetQueryResponses(bomb, queryKey, queryInfo));
                    info.IsBombPresentHandler = new KMBombInfo.KMIsBombPresent(() => BombInfoRedirection.IsBombPresent(bomb));
                    redirectedInfos.Add(info, bomb);
                }
            }
            Debug.Log("[MultipleBombs]KMBombInfos redirected");
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
                foreach (KMBombInfo info in FindObjectsOfType<KMBombInfo>())
                {
                    ModBombComponent component = info.GetComponent<ModBombComponent>();
                    if (component != null && component.Bomb == source.Bomb)
                    {
                        if (info.OnBombSolved != null)
                            info.OnBombSolved();
                    }
                }
                foreach (NeedyComponent component in FindObjectsOfType<NeedyComponent>())
                {
                    if (component.Bomb == source.Bomb)
                    {
                        component.TurnOff(true);
                    }
                }
                foreach (TimerComponent timer in FindObjectsOfType<TimerComponent>())
                {
                    if (timer.Bomb == source.Bomb)
                    {
                        timer.StrikeIndicator.StopAllCoroutines();
                    }
                }
                foreach (KMBombInfo bombInfo in FindObjectsOfType<KMBombInfo>())
                {
                    if (redirectedInfos[bombInfo] == source.Bomb)
                    {
                        if (bombInfo.OnBombSolved != null)
                        {
                            bombInfo.OnBombSolved();
                        }
                    }
                }
                foreach (Bomb bomb in FindObjectsOfType<Bomb>())
                    if (!bomb.IsSolved())
                        return true;
                Debug.Log("[MultipleBombs]All bombs solved, what a winner!");
                SceneManager.Instance.GameplayState.OnWin();
                RecordManager.Instance.SetResult(GameResultEnum.Defused, source.Bomb.GetTimer().TimeElapsed, SceneManager.Instance.GameplayState.GetElapsedRealTime());
                StopAllCoroutines();
                foreach (Bomb bomb in FindObjectsOfType<Bomb>())
                    if (bomb != SceneManager.Instance.GameplayState.Bomb)
                        StartCoroutine(timedDestroy(bomb.gameObject, 9f));
                gameplayInitialized = false;
                return true;
            }
            return false;
        }

        private IEnumerator timedDestroy(GameObject gameObject, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            Destroy(gameObject);
        }

        private void StartNeedy(NeedyComponent needy)
        {
            Bomb oldBomb = SceneManager.Instance.GameplayState.Bomb;
            gameplaySetBombMethod.Invoke(SceneManager.Instance.GameplayState, new object[] { needy.Bomb });
            needyStartRunningMethod.Invoke(needy, null);
            gameplaySetBombMethod.Invoke(SceneManager.Instance.GameplayState, new object[] { oldBomb });
            activatedNeedies.Add(needy);
        }

        private IEnumerator startNeedyAfter(NeedyComponent component, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            StartNeedy(component);
        }

        private IEnumerator startNeedyChange(NeedyComponent component, float seconds)
        {
            if ((bool)needychangedBombResponseInProgressField.GetValue(component))
                yield break;
            needychangedBombResponseInProgressField.SetValue(component, true);
            yield return new WaitForSeconds(seconds);
            int count = (int)needyplayerChangedBombEventCountField.GetValue(component);
            count++;
            needyplayerChangedBombEventCountField.SetValue(component, count);
            if (count > 1)
            {
                StartNeedy(component);
            }
            else if (UnityEngine.Random.value < (float)needyactivationChanceAfterFirstChangeField.GetValue(component))
            {
                StartNeedy(component);
            }
            needychangedBombResponseInProgressField.SetValue(component, false);
        }

        private IEnumerator resetAndStartNeedy(NeedyComponent component)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(component.ResetDelayMin, component.ResetDelayMax));
            Bomb oldBomb = SceneManager.Instance.GameplayState.Bomb;
            gameplaySetBombMethod.Invoke(SceneManager.Instance.GameplayState, new object[] { component.Bomb });
            needyResetAndStartMethod.Invoke(component, null);
            gameplaySetBombMethod.Invoke(SceneManager.Instance.GameplayState, new object[] { oldBomb });
            activatedNeedies.Add(component);
        }

        private IEnumerator createNewBomb(BombGenerator bombGenerator, Vector3 position, Vector3 eulerAngles)
        {
            Debug.Log("[MultipleBombs]Generating new bomb");

            GameplayState gameplayState = SceneManager.Instance.GameplayState;

            GameObject spawnPointGO = new GameObject("CustomBombSpawnPoint");
            spawnPointGO.transform.position = position;
            spawnPointGO.transform.eulerAngles = eulerAngles;
            HoldableSpawnPoint spawnPoint = spawnPointGO.AddComponent<HoldableSpawnPoint>();
            spawnPoint.HoldableTarget = gameplayState.Room.BombSpawnPosition.HoldableTarget;

            Bomb bomb = bombGenerator.CreateBomb(gameplayState.Mission.GeneratorSetting, spawnPoint, (new System.Random()).Next(), Assets.Scripts.Missions.BombTypeEnum.Default);

            GameObject roomGO = (GameObject)gameplayState.GetType().GetField("roomGO", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameplayState);
            Selectable mainSelectable = roomGO.GetComponent<Selectable>();
            List<Selectable> children = mainSelectable.Children.ToList();
            children.Insert(2, bomb.GetComponent<Selectable>());
            mainSelectable.Children = children.ToArray();
            mainSelectable.ChildRowLength++;
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

            RedirectPresentBombInfos(bomb);
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
            yield return new WaitForSeconds(2f);

            bomb.GetTimer().StartTimer();
            Debug.Log("[MultipleBombs]Custom bomb timer started");

            foreach (BombComponent component in bomb.BombComponents)
            {
                if (component is NeedyComponent)
                {
                    component.StartCoroutine(startNeedyAfter((NeedyComponent)component, ((NeedyComponent)component).SecondsBeforeForcedActivation));
                }
            }
        }
    }
}
