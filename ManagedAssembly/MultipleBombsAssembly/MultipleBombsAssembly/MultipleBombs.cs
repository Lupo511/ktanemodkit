using Assets.Scripts.Missions;
using Assets.Scripts.Props;
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

        private int bombsCount;
        private int currentBombCount;
        private float? defaultMaxTime = null;
        private int currentMaxModModules;
        private TextMeshPro currentBombsCountLabel;
        private Dictionary<string, int> multipleBombsMissions;
        private Dictionary<GameplayRoom, int> multipleBombsRooms;
        private bool usingRoomPrefabOverride;
        private Dictionary<KMBombInfo, Bomb> redirectedInfos;
        private List<NeedyComponent> activatedNeedies;
        private MethodInfo gameplaySetBombMethod = typeof(GameplayState).GetMethod("set_Bomb", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo needystateField = typeof(NeedyComponent).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo needyplayerChangedBombEventCountField = typeof(NeedyComponent).GetField("playerChangedBombEventCount", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo needychangedBombResponseInProgressField = typeof(NeedyComponent).GetField("changedBombResponseInProgress", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo needyactivationChanceAfterFirstChangeField = typeof(NeedyComponent).GetField("activationChanceAfterFirstChange", BindingFlags.Instance | BindingFlags.NonPublic);
        private MethodInfo needyStartRunningMethod = typeof(NeedyComponent).GetMethod("StartRunning", BindingFlags.Instance | BindingFlags.NonPublic);
        private MethodInfo needyResetAndStartMethod = typeof(NeedyComponent).GetMethod("ResetAndStart", BindingFlags.Instance | BindingFlags.NonPublic);
        private ResultPageMonitor freePlayDefusedPageMonitor;
        private ResultPageMonitor freePlayExplodedPageMonitor;
        private ResultPageMonitor missionDefusedPageMonitor;
        private ResultPageMonitor missionExplodedPageMonitor;
        private MissionDetailPageMonitor missionDetailPageMonitor;

        public void Awake()
        {
            Debug.Log("[MultipleBombs]Initializing");
            bombsCount = 1;
            multipleBombsMissions = new Dictionary<string, int>();
            multipleBombsRooms = new Dictionary<GameplayRoom, int>();
            usingRoomPrefabOverride = false;
            GameEvents.OnGameStateChange = (GameEvents.GameStateChangedEvent)Delegate.Combine(GameEvents.OnGameStateChange, new GameEvents.GameStateChangedEvent(onGameStateChanged));
            Debug.Log("[MultipleBombs]Initialized");
        }

        public void Update()
        {
            if (SceneManager.Instance != null)
            {
                if (SceneManager.Instance.CurrentState == SceneManager.State.Gameplay && (GameplayState.MissionToLoad == FreeplayMissionGenerator.FREEPLAY_MISSION_ID || GameplayState.MissionToLoad != null && multipleBombsMissions.ContainsKey(GameplayState.MissionToLoad)))
                {
                    if (activatedNeedies != null && currentBombCount > 1)
                    {
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
                }
                if (SceneManager.Instance.CurrentState == SceneManager.State.Setup)
                {
                    int maxBombCount = GetCurrentMaximumBombCount();
                    if (defaultMaxTime == null)
                        defaultMaxTime = FreeplayDevice.MAX_SECONDS_TO_SOLVE;
                    float newMaxTime = defaultMaxTime.Value * maxBombCount;
                    int maxModules = currentMaxModModules;
                    if (GameplayState.GameplayRoomPrefabOverride != null && GameplayState.GameplayRoomPrefabOverride.GetComponent<GameplayRoom>().BombPrefabOverride != null)
                    {
                        maxModules = Math.Max(GameplayState.GameplayRoomPrefabOverride.GetComponent<GameplayRoom>().BombPrefabOverride.GetComponent<Bomb>().GetMaxModules(), FreeplayDevice.MAX_MODULE_COUNT);
                        maxModules = Math.Max(currentMaxModModules, maxModules);
                    }
                    if (maxModules > FreeplayDevice.MAX_MODULE_COUNT)
                    {
                        newMaxTime += (maxModules - FreeplayDevice.MAX_MODULE_COUNT) * 60 *
                                      (maxBombCount - 1);
                    }
                    FreeplayDevice.MAX_SECONDS_TO_SOLVE = newMaxTime;

                    if (bombsCount > maxBombCount)
                    {
                        bombsCount = maxBombCount;
                        currentBombsCountLabel.text = bombsCount.ToString();
                    }
                }
            }
        }

        private void onGameStateChanged(SceneManager.State state)
        {
            if (state == SceneManager.State.Gameplay)
            {
                if (GameplayState.MissionToLoad == FreeplayMissionGenerator.FREEPLAY_MISSION_ID || GameplayState.MissionToLoad != null && multipleBombsMissions.ContainsKey(GameplayState.MissionToLoad))
                    StartCoroutine(setupGameplayStateNextFrame());
            }
            else
            {
                if (currentBombCount > 1)
                {
                    Debug.Log("[MultipleBombs]Cleaning up");
                    currentBombCount = 1;
                    StopAllCoroutines();
                    foreach (Bomb bomb in FindObjectsOfType<Bomb>())
                    {
                        bomb.gameObject.SetActive(false);
                    }
                    activatedNeedies.Clear();
                }
                if (state == SceneManager.State.Setup)
                {
                    StartCoroutine(setupSetupRoomNextFrame());
                }
            }
        }

        private IEnumerator setupSetupRoomNextFrame()
        {
            yield return null;
            Debug.Log("[MultipleBombs]Adding FreePlay option");
            FreeplayDevice device = FindObjectOfType<FreeplayDevice>();
            if (device == null)
            {
                Debug.Log("[MultipleBombs]Error: FreePlayDevice not found!");
            }
            else
            {
                GameObject modulesObject = device.ModuleCountIncrement.transform.parent.gameObject;
                GameObject bombsObject = (GameObject)Instantiate(modulesObject, modulesObject.transform.position, modulesObject.transform.rotation, modulesObject.transform.parent);
                device.ObjectsToDisableOnLidClose.Add(bombsObject);
                bombsObject.transform.localPosition = modulesObject.transform.localPosition + new Vector3(0, 0f, -0.025f);
                bombsObject.transform.FindChild("ModuleCountLabel").GetComponent<TextMeshPro>().text = "Bombs";
                currentBombsCountLabel = bombsObject.transform.FindChild("ModuleCountValue").GetComponent<TextMeshPro>();
                currentBombsCountLabel.text = bombsCount.ToString();
                bombsObject.transform.FindChild("ModuleCountLED").gameObject.SetActive(false);

                GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
                background.GetComponent<Renderer>().material.color = Color.black;
                background.transform.localScale = new Vector3(0.048f, 0.023f, 0.005f); //Accurate Y would be 0.025
                background.transform.parent = bombsObject.transform;
                background.transform.localPosition = currentBombsCountLabel.gameObject.transform.localPosition + new Vector3(0.00025f, -0.0027f, 0);
                background.transform.localEulerAngles = currentBombsCountLabel.gameObject.transform.localEulerAngles;

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
                    if (bombsCount >= GetCurrentMaximumBombCount())
                        return;
                    bombsCount++;
                    currentBombsCountLabel.text = bombsCount.ToString();
                });
                decrementButton.GetComponent<KeypadButton>().OnPush = new PushEvent(() =>
                {
                    if (bombsCount <= 1)
                        return;
                    bombsCount--;
                    currentBombsCountLabel.text = bombsCount.ToString();
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

                modulesObject.transform.FindChild("Modules_INCR_btn").GetComponent<Selectable>().OnHighlight = new Action(() =>
                {
                    device.Screen.CurrentState = FreeplayScreen.State.Modules;
                    device.Screen.ScreenText.text = "MODULES:\n\nNumber of modules\nper bomb";
                });
                modulesObject.transform.FindChild("Modules_DECR_btn").GetComponent<Selectable>().OnHighlight = new Action(() =>
                {
                    device.Screen.CurrentState = FreeplayScreen.State.Modules;
                    device.Screen.ScreenText.text = "MODULES:\n\nNumber of modules\nper bomb";
                });

                device.StartButton.OnPush = new PushEvent(() =>
                {
                    SetNextGameplayRoom(bombsCount);
                    device.GetType().GetMethod("StartGame", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(device, null);

                });

                incrementButton.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.015f, -0.015f, -0.015f);
                decrementButton.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.015f, -0.015f, -0.015f);
                device.ModuleCountIncrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.012f, -0.012f, -0.012f);
                device.ModuleCountDecrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.012f, -0.012f, -0.012f);
                device.TimeIncrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.01f, -0.01f, -0.01f);
                device.TimeDecrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.01f, -0.01f, -0.01f);
                Debug.Log("[MultipleBombs]FreePlay option added");

                currentMaxModModules = ModManager.Instance.GetMaximumModules();

                foreach (ModMission mission in ModManager.Instance.ModMissions)
                {
                    if (!multipleBombsMissions.ContainsKey(mission.ID))
                    {
                        int missionBombCount = 1;
                        for (int i = mission.GeneratorSetting.ComponentPools.Count - 1; i >= 0; i--)
                        {
                            ComponentPool pool = mission.GeneratorSetting.ComponentPools[i];
                            if (pool.ModTypes != null && pool.ModTypes.Count == 1 && pool.ModTypes[0] == "Multiple Bombs")
                            {
                                mission.GeneratorSetting.ComponentPools.RemoveAt(i);
                                missionBombCount += pool.Count;
                            }
                        }
                        if (missionBombCount >= 2)
                        {
                            multipleBombsMissions.Add(mission.ID, missionBombCount);
                        }
                    }
                }
                Debug.Log("[MultipleBombs]Missions processed");

                multipleBombsRooms = new Dictionary<GameplayRoom, int>();
                foreach (GameplayRoom room in ModManager.Instance.GetGameplayRooms())
                {
                    for (int i = 2; i < int.MaxValue; i++)
                    {
                        if (!room.transform.FindRecursive("MultipleBombs_Spawn_" + i))
                        {
                            multipleBombsRooms.Add(room, i);
                            break;
                        }
                    }
                }
                Debug.Log("[MultipleBombs]GamePlayRooms processed");

                if (missionDetailPageMonitor == null)
                {
                    missionDetailPageMonitor = FindObjectOfType<SetupRoom>().BombBinder.MissionDetailPage.gameObject.AddComponent<MissionDetailPageMonitor>();
                    missionDetailPageMonitor.MultipleBombs = this;
                }
                missionDetailPageMonitor.MissionList = multipleBombsMissions;
                Debug.Log("[MultipleBombs]BombBinder info added");
            }
        }

        private IEnumerator setupGameplayStateNextFrame()
        {
            yield return null;
            Debug.Log("[MultipleBombs]Initializing gameplay state");

            if (usingRoomPrefabOverride)
            {
                ModGameplayRoom room = (ModGameplayRoom)SceneManager.Instance.GameplayState.Room;
                room.CopySettingsFromProxy();
                SceneManager.Instance.GameplayState.Bomb.GetComponent<FloatingHoldable>().HoldableTarget = room.BombSpawnPosition.HoldableTarget;
                room.MainMenu.FloatingHoldable.HoldableTarget = room.DossierSpawn.HoldableTarget;
                FindObjectOfType<AlarmClock>().GetComponent<FloatingHoldable>().HoldableTarget = room.AlarmClockSpawn.HoldableTarget;
                GameplayState.GameplayRoomPrefabOverride = null;
                usingRoomPrefabOverride = false;
            }

            currentBombCount = 1;
            if (GameplayState.MissionToLoad == FreeplayMissionGenerator.FREEPLAY_MISSION_ID)
                currentBombCount = bombsCount;
            else
                currentBombCount = multipleBombsMissions[GameplayState.MissionToLoad];
            Debug.Log("[MultipleBombs]Bombs to spawn: " + currentBombCount);

            //Setup results screen
            if (freePlayDefusedPageMonitor == null)
            {
                freePlayDefusedPageMonitor = SceneManager.Instance.PostGameState.Room.BombBinder.ResultFreeplayDefusedPage.gameObject.AddComponent<ResultPageMonitor>();
                freePlayDefusedPageMonitor.MultipleBombs = this;
            }
            if (freePlayExplodedPageMonitor == null)
            {
                freePlayExplodedPageMonitor = SceneManager.Instance.PostGameState.Room.BombBinder.ResultFreeplayExplodedPage.gameObject.AddComponent<ResultPageMonitor>();
                freePlayExplodedPageMonitor.MultipleBombs = this;
            }
            freePlayDefusedPageMonitor.SetBombCount(currentBombCount);
            freePlayExplodedPageMonitor.SetBombCount(currentBombCount);
            if (missionDefusedPageMonitor == null)
            {
                missionDefusedPageMonitor = SceneManager.Instance.PostGameState.Room.BombBinder.ResultDefusedPage.gameObject.AddComponent<ResultPageMonitor>();
                missionDefusedPageMonitor.MultipleBombs = this;
            }
            if (missionExplodedPageMonitor == null)
            {
                missionExplodedPageMonitor = SceneManager.Instance.PostGameState.Room.BombBinder.ResultExplodedPage.gameObject.AddComponent<ResultPageMonitor>();
                missionExplodedPageMonitor.MultipleBombs = this;
            }
            missionDefusedPageMonitor.SetBombCount(currentBombCount);
            missionExplodedPageMonitor.SetBombCount(currentBombCount);
            Debug.Log("[MultipleBombs]Result screens initialized");
            if (currentBombCount == 1)
                yield break;

            redirectedInfos = new Dictionary<KMBombInfo, Bomb>();
            activatedNeedies = new List<NeedyComponent>();
            BombInfoRedirection.SetBombCount(currentBombCount);

            Bomb vanillaBomb = FindObjectOfType<Bomb>();
            foreach (BombComponent component in vanillaBomb.BombComponents)
            {
                component.OnPass = onComponentPass;
                component.OnStrike = onComponentStrike;
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

            for (int i = 2; i < currentBombCount; i++)
            {
                GameObject spawn = GameObject.Find("MultipleBombs_Spawn_" + i);
                if (spawn == null)
                    throw new Exception("Current gameplay room doesn't support " + (i + 1) + " bombs");
                StartCoroutine(createNewBomb(FindObjectOfType<BombGenerator>(), spawn.transform.position, spawn.transform.eulerAngles));
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
                currentBombCount = 1;
                SceneManager.Instance.GameplayState.OnWin();
                RecordManager.Instance.SetResult(GameResultEnum.Defused, source.Bomb.GetTimer().TimeElapsed, SceneManager.Instance.GameplayState.GetElapsedRealTime());
                StopAllCoroutines();
                foreach (Bomb bomb in FindObjectsOfType<Bomb>())
                    if (bomb != SceneManager.Instance.GameplayState.Bomb)
                        StartCoroutine(timedDestroy(bomb.gameObject, 9f));
                activatedNeedies.Clear();
                return true;
            }
            return false;
        }

        private bool onComponentStrike(BombComponent source)
        {
            bool lastStrike = source.Bomb.OnStrike(source);
            GameRecord currentRecord = RecordManager.Instance.GetCurrentRecord();
            if (currentRecord.GetStrikeCount() == currentRecord.Strikes.Length)
            {
                if (!lastStrike)
                {
                    List<StrikeSource> strikes = currentRecord.Strikes.ToList();
                    strikes.Add(null);
                    currentRecord.Strikes = strikes.ToArray();
                }
            }
            return lastStrike;
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
            children.Insert(children.Count - 1, bomb.GetComponent<Selectable>());
            mainSelectable.Children = children.ToArray();
            mainSelectable.ChildRowLength++;
            bomb.GetComponent<Selectable>().Parent = mainSelectable;
            KTInputManager.Instance.SelectableManager.ConfigureSelectableAreas(KTInputManager.Instance.RootSelectable);
            bomb.GetTimer().text.gameObject.SetActive(false);
            bomb.GetTimer().LightGlow.enabled = false;
            if (!bomb.HasDetonated)
            {
                foreach (BombComponent component in bomb.BombComponents)
                {
                    component.OnPass = onComponentPass;
                    component.OnStrike = onComponentStrike;
                }
            }

            bomb.GetTimer().TimerTick = new TimerComponent.TimerTickEvent((elapsed, remaining) => SceneManager.Instance.GameplayState.GetPaceMaker().OnTimerTick(elapsed, remaining));

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

        public int GetCurrentMaximumBombCount()
        {
            if (GameplayState.GameplayRoomPrefabOverride != null)
            {
                if (GameplayState.GameplayRoomPrefabOverride.GetComponent<ElevatorRoom>() != null)
                    return 1;
                GameplayRoom room = GameplayState.GameplayRoomPrefabOverride.GetComponent<GameplayRoom>();
                if (multipleBombsRooms.ContainsKey(room))
                {
                    return multipleBombsRooms[room];
                }
                return 2;
            }
            else
            {
                int max = 2;
                foreach (int count in multipleBombsRooms.Values)
                {
                    if (count > max)
                        max = count;
                }
                return max;
            }
        }

        internal void SetNextGameplayRoom(int bombs)
        {
            if (bombs > 2)
            {
                if (GameplayState.GameplayRoomPrefabOverride == null)
                {
                    List<GameplayRoom> rooms = new List<GameplayRoom>();
                    foreach (KeyValuePair<GameplayRoom, int> room in multipleBombsRooms)
                    {
                        if (room.Value >= bombs)
                            rooms.Add(room.Key);
                    }
                    GameplayRoom selectedRoom = rooms[UnityEngine.Random.Range(0, rooms.Count)];
                    GameplayState.GameplayRoomPrefabOverride = selectedRoom.gameObject;
                    usingRoomPrefabOverride = true;
                }
            }
        }
    }
}
