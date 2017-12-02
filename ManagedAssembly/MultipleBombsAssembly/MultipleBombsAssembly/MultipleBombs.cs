using Assets.Scripts.Missions;
using Assets.Scripts.Pacing;
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
        private int bombsCount;
        private int currentBombCount;
        private float? defaultMaxTime = null;
        private int currentMaxModModules;
        private TextMeshPro currentBombsCountLabel;
        private Dictionary<string, int> multipleBombsMissions;
        private Dictionary<GameplayRoom, int> multipleBombsRooms;
        private bool usingRoomPrefabOverride;
        private Dictionary<KMBombInfo, Bomb> redirectedInfos;
        private Dictionary<Bomb, BombEvents.BombSolvedEvent> bombSolvedEvents;
        private Dictionary<Bomb, BombComponentEvents.ComponentPassEvent> bombComponentPassEvents;
        private Dictionary<Bomb, BombComponentEvents.ComponentStrikeEvent> bombComponentStrikeEvents;
        private ResultPageMonitor freePlayDefusedPageMonitor;
        private ResultPageMonitor freePlayExplodedPageMonitor;
        private ResultPageMonitor missionDefusedPageMonitor;
        private ResultPageMonitor missionExplodedPageMonitor;
        private MissionDetailPageMonitor missionDetailPageMonitor;
        private KMGameInfo gameInfo;
        private KMGameCommands gameCommands;

        public void Awake()
        {
            Debug.Log("[MultipleBombs]Initializing");
            DestroyImmediate(GetComponent<KMService>()); //Hide from Mod Selector
            bombsCount = 1;
            multipleBombsMissions = new Dictionary<string, int>();
            multipleBombsRooms = new Dictionary<GameplayRoom, int>();
            usingRoomPrefabOverride = false;
            gameInfo = GetComponent<KMGameInfo>();
            gameCommands = GetComponent<KMGameCommands>();
            gameInfo.OnStateChange += onGameStateChanged;
            Debug.Log("[MultipleBombs]Initialized");
        }

        public void Update()
        {
            if (SceneManager.Instance != null)
            {
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

        public void OnDestroy()
        {
            if (SceneManager.Instance.CurrentState != SceneManager.State.ModManager)
                throw new NotImplementedException();
            Debug.Log("[MultipleBombs]Destroying");
            foreach (Mission mission in ModManager.Instance.ModMissions)
            {
                if (multipleBombsMissions.ContainsKey(mission.ID))
                {
                    ComponentPool pool = new ComponentPool();
                    pool.ModTypes = new List<string>() { "Multiple Bombs" };
                    pool.Count = multipleBombsMissions[mission.ID] - 1;
                    mission.GeneratorSetting.ComponentPools.Add(pool);
                }
            }
            if (missionDetailPageMonitor != null)
            {
                Destroy(missionDetailPageMonitor);
            }
            if (freePlayDefusedPageMonitor != null)
            {
                Destroy(freePlayDefusedPageMonitor);
            }
            if (freePlayExplodedPageMonitor != null)
            {
                Destroy(freePlayDefusedPageMonitor);
            }
            if (missionDefusedPageMonitor != null)
            {
                Destroy(missionDefusedPageMonitor);
            }
            if (missionExplodedPageMonitor != null)
            {
                Destroy(missionDefusedPageMonitor);
            }
            gameInfo.OnStateChange -= onGameStateChanged;
            Debug.Log("[MultipleBombs]Destroyed");
        }

        private void onGameStateChanged(KMGameInfo.State state)
        {
            if (state == KMGameInfo.State.Gameplay)
            {
                if (GameplayState.MissionToLoad == FreeplayMissionGenerator.FREEPLAY_MISSION_ID || GameplayState.MissionToLoad != null && multipleBombsMissions.ContainsKey(GameplayState.MissionToLoad) || GameplayState.MissionToLoad == ModMission.CUSTOM_MISSION_ID)
                    StartCoroutine(setupGameplayStateNextFrame());
            }
            else
            {
                if (currentBombCount > 1)
                {
                    Debug.Log("[MultipleBombs]Cleaning up");
                    StopAllCoroutines();
                    currentBombCount = 1;
                    redirectedInfos = null;
                    bombSolvedEvents = null;
                    bombComponentPassEvents = null;
                    bombComponentStrikeEvents = null;
                    BombEvents.OnBombDetonated -= onBombDetonated;
                    BombComponentEvents.OnComponentPass -= onComponentPassEvent;
                    BombComponentEvents.OnComponentStrike -= onComponentStrikeEvent;
                }
                if (state == KMGameInfo.State.Setup)
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
                bombsObject.transform.Find("ModuleCountLabel").GetComponent<TextMeshPro>().text = "Bombs";
                currentBombsCountLabel = bombsObject.transform.Find("ModuleCountValue").GetComponent<TextMeshPro>();
                currentBombsCountLabel.text = bombsCount.ToString();
                bombsObject.transform.Find("ModuleCountLED").gameObject.SetActive(false);

                GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
                background.GetComponent<Renderer>().material.color = Color.black;
                background.transform.localScale = new Vector3(0.048f, 0.023f, 0.005f); //Accurate Y would be 0.025
                background.transform.parent = bombsObject.transform;
                background.transform.localPosition = currentBombsCountLabel.gameObject.transform.localPosition + new Vector3(0.00025f, -0.0027f, 0);
                background.transform.localEulerAngles = currentBombsCountLabel.gameObject.transform.localEulerAngles;

                GameObject incrementButton = bombsObject.transform.Find("Modules_INCR_btn").gameObject;
                GameObject decrementButton = bombsObject.transform.Find("Modules_DECR_btn").gameObject;
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

                modulesObject.transform.Find("Modules_INCR_btn").GetComponent<Selectable>().OnHighlight = new Action(() =>
                {
                    device.Screen.CurrentState = FreeplayScreen.State.Modules;
                    device.Screen.ScreenText.text = "MODULES:\n\nNumber of modules\nper bomb";
                });
                modulesObject.transform.Find("Modules_DECR_btn").GetComponent<Selectable>().OnHighlight = new Action(() =>
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
                        int missionBombCount = ProcessMultipleBombsMission(mission);
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

        private int ProcessMultipleBombsMission(Mission mission)
        {
            List<ComponentPool> pools;
            return ProcessMultipleBombsMission(mission, out pools);
        }

        private int ProcessMultipleBombsMission(Mission mission, out List<ComponentPool> bombsPools)
        {
            int count = 1;
            bombsPools = new List<ComponentPool>();
            for (int i = mission.GeneratorSetting.ComponentPools.Count - 1; i >= 0; i--)
            {
                ComponentPool pool = mission.GeneratorSetting.ComponentPools[i];
                if (pool.ModTypes != null && pool.ModTypes.Count == 1 && pool.ModTypes[0] == "Multiple Bombs")
                {
                    mission.GeneratorSetting.ComponentPools.RemoveAt(i);
                    count += pool.Count;
                    bombsPools.Add(pool);
                }
            }
            return count;
        }

        private IEnumerator setupGameplayStateNextFrame()
        {
            currentBombCount = 1;
            List<ComponentPool> customMissionBombsPools = null;
            if (GameplayState.MissionToLoad == FreeplayMissionGenerator.FREEPLAY_MISSION_ID)
                currentBombCount = bombsCount;
            else if (multipleBombsMissions.ContainsKey(GameplayState.MissionToLoad))
                currentBombCount = multipleBombsMissions[GameplayState.MissionToLoad];
            else if (GameplayState.MissionToLoad == ModMission.CUSTOM_MISSION_ID)
            {
                currentBombCount = ProcessMultipleBombsMission(GameplayState.CustomMission, out customMissionBombsPools);
                if (currentBombCount > 2)
                    SetNextGameplayRoom(currentBombCount);
            }
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
            {
                if (GameplayState.MissionToLoad == ModMission.CUSTOM_MISSION_ID)
                    GameplayState.CustomMission.GeneratorSetting.ComponentPools.AddRange(customMissionBombsPools);
                yield break;
            }

            redirectedInfos = new Dictionary<KMBombInfo, Bomb>();
            BombInfoRedirection.SetBombCount(currentBombCount);
            bombSolvedEvents = new Dictionary<Bomb, BombEvents.BombSolvedEvent>();
            bombComponentPassEvents = new Dictionary<Bomb, BombComponentEvents.ComponentPassEvent>();
            bombComponentStrikeEvents = new Dictionary<Bomb, BombComponentEvents.ComponentStrikeEvent>();

            Bomb vanillaBomb = FindObjectOfType<Bomb>();
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
            ProcessBombEvents(vanillaBomb);
            Debug.Log("[MultipleBombs]Default bomb initialized");

            GameObject spawn1 = GameObject.Find("MultipleBombs_Spawn_1");
            if (spawn1 != null)
            {
                StartCoroutine(createNewBomb(GameplayState.MissionToLoad, spawn1.transform.position, spawn1.transform.eulerAngles));
            }
            else
            {
                StartCoroutine(createNewBomb(GameplayState.MissionToLoad, SceneManager.Instance.GameplayState.Room.BombSpawnPosition.transform.position + new Vector3(0.4f, 0, 0), new Vector3(0, 30, 0)));
            }

            for (int i = 2; i < currentBombCount; i++)
            {
                GameObject spawn = GameObject.Find("MultipleBombs_Spawn_" + i);
                if (spawn == null)
                {
                    Debug.LogError("[MultipleBombs]The current gameplay room doesn't support " + (i + 1) + " bombs");
                    break;
                }
                StartCoroutine(createNewBomb(GameplayState.MissionToLoad, spawn.transform.position, spawn.transform.eulerAngles));
            }

            if (GameplayState.MissionToLoad == ModMission.CUSTOM_MISSION_ID)
                GameplayState.CustomMission.GeneratorSetting.ComponentPools.AddRange(customMissionBombsPools);

            vanillaBomb.GetComponent<Selectable>().Parent.Init();
            Debug.Log("[MultipleBombs]All bombs generated");

            //PaceMaker
            PaceMakerMonitor monitor = FindObjectOfType<PaceMaker>().gameObject.AddComponent<PaceMakerMonitor>();
            foreach (Bomb bomb in FindObjectsOfType<Bomb>())
            {
                if (bomb != vanillaBomb) //The vanilla bomb is still handled by PaceMaker
                    bomb.GetTimer().TimerTick = (TimerComponent.TimerTickEvent)Delegate.Combine(bomb.GetTimer().TimerTick, new TimerComponent.TimerTickEvent((elapsed, remaining) => monitor.OnBombTimerTick(bomb, elapsed, remaining)));
            }
            Debug.Log("[MultipleBombs]Pacing events initalized");

            BombEvents.OnBombDetonated += onBombDetonated;
            BombComponentEvents.OnComponentPass += onComponentPassEvent;
            BombComponentEvents.OnComponentStrike += onComponentStrikeEvent;
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

        private void ProcessBombEvents(Bomb bomb)
        {
            List<Delegate> bombSolvedDelegates = BombEvents.OnBombSolved.GetInvocationList().ToList();
            for (int i = bombSolvedDelegates.Count - 1; i >= 0; i--)
            {
                BombEvents.BombSolvedEvent bombSolvedDelegate = (BombEvents.BombSolvedEvent)bombSolvedDelegates[i];
                if (bombSolvedDelegate.Target != null && (ReferenceEquals(bombSolvedDelegate.Target, bomb.GetTimer()) || ReferenceEquals(bombSolvedDelegate.Target, bomb.StrikeIndicator)))
                {
                    BombEvents.OnBombSolved -= bombSolvedDelegate;
                    if (bombSolvedEvents.ContainsKey(bomb))
                        bombSolvedEvents[bomb] += bombSolvedDelegate;
                    else
                        bombSolvedEvents.Add(bomb, bombSolvedDelegate);
                    bombSolvedDelegates.RemoveAt(i);
                }
            }
            List<Delegate> componentPassDelegates = BombComponentEvents.OnComponentPass.GetInvocationList().ToList();
            List<Delegate> componentStrikeDelegates = BombComponentEvents.OnComponentStrike.GetInvocationList().ToList();
            foreach (BombComponent component in bomb.BombComponents)
            {
                component.OnPass = onComponentPass;
                if (component is NeedyComponent)
                {
                    for (int i = bombSolvedDelegates.Count - 1; i >= 0; i--)
                    {
                        BombEvents.BombSolvedEvent bombSolvedDelegate = (BombEvents.BombSolvedEvent)bombSolvedDelegates[i];
                        if (bombSolvedDelegate != null && ReferenceEquals(bombSolvedDelegate.Target, component))
                        {
                            BombEvents.OnBombSolved -= bombSolvedDelegate;
                            if (bombSolvedEvents.ContainsKey(bomb))
                                bombSolvedEvents[bomb] += bombSolvedDelegate;
                            else
                                bombSolvedEvents.Add(bomb, bombSolvedDelegate);
                            bombSolvedDelegates.RemoveAt(i);
                            break;
                        }
                    }
                    for (int i = componentPassDelegates.Count - 1; i >= 0; i--)
                    {
                        BombComponentEvents.ComponentPassEvent componentPassEvent = (BombComponentEvents.ComponentPassEvent)componentPassDelegates[i];
                        if (componentPassEvent.Target != null && ReferenceEquals(componentPassEvent.Target, component))
                        {
                            BombComponentEvents.OnComponentPass -= componentPassEvent;
                            if (bombComponentPassEvents.ContainsKey(bomb))
                                bombComponentPassEvents[bomb] += componentPassEvent;
                            else
                                bombComponentPassEvents.Add(bomb, componentPassEvent);
                            componentPassDelegates.RemoveAt(i);
                            break;
                        }
                    }
                    for (int i = componentStrikeDelegates.Count - 1; i >= 0; i--)
                    {
                        BombComponentEvents.ComponentStrikeEvent componentStrikeEvent = (BombComponentEvents.ComponentStrikeEvent)componentStrikeDelegates[i];
                        if (componentStrikeEvent.Target != null && ReferenceEquals(componentStrikeEvent.Target, component))
                        {
                            BombComponentEvents.OnComponentStrike -= componentStrikeEvent;
                            if (bombComponentStrikeEvents.ContainsKey(bomb))
                                bombComponentStrikeEvents[bomb] += componentStrikeEvent;
                            else
                                bombComponentStrikeEvents.Add(bomb, componentStrikeEvent);
                            componentStrikeDelegates.RemoveAt(i);
                            break;
                        }
                    }
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
                Debug.Log("[MultipleBombs]A bomb was solved (A winner is you!!)");
                source.Bomb.GetTimer().StopTimer();
                source.Bomb.GetTimer().Blink(1.5f);
                DarkTonic.MasterAudio.MasterAudio.PlaySound3DAtTransformAndForget("bomb_defused", source.Bomb.transform, 1f, null, 0f, null);
                if (BombEvents.OnBombSolved != null)
                    BombEvents.OnBombSolved();
                foreach (KeyValuePair<KMBombInfo, Bomb> infos in redirectedInfos)
                {
                    if (infos.Value == source.Bomb)
                    {
                        infos.Key.OnBombSolved();
                    }
                }
                if (bombSolvedEvents.ContainsKey(source.Bomb) && bombSolvedEvents[source.Bomb] != null)
                    bombSolvedEvents[source.Bomb].Invoke();
                SceneManager.Instance.GameplayState.OnBombSolved();
                foreach (Bomb bomb in FindObjectsOfType<Bomb>())
                    if (!bomb.IsSolved())
                        return true;
                Debug.Log("[MultipleBombs]All bombs solved, what a winner!");
                RecordManager.Instance.SetResult(GameResultEnum.Defused, source.Bomb.GetTimer().TimeElapsed, SceneManager.Instance.GameplayState.GetElapsedRealTime());
                return true;
            }
            return false;
        }

        private void onComponentPassEvent(BombComponent component, bool finalPass)
        {
            if (bombComponentPassEvents.ContainsKey(component.Bomb) && bombComponentPassEvents[component.Bomb] != null)
                bombComponentPassEvents[component.Bomb].Invoke(component, finalPass);
        }

        private void onComponentStrikeEvent(BombComponent component, bool finalStrike)
        {
            GameRecord currentRecord = RecordManager.Instance.GetCurrentRecord();
            if (!finalStrike && currentRecord.GetStrikeCount() == currentRecord.Strikes.Length)
            {
                List<StrikeSource> strikes = currentRecord.Strikes.ToList();
                strikes.Add(null);
                currentRecord.Strikes = strikes.ToArray();
            }
            if (bombComponentStrikeEvents.ContainsKey(component.Bomb) && bombComponentStrikeEvents[component.Bomb] != null)
                bombComponentStrikeEvents[component.Bomb].Invoke(component, finalStrike);
        }

        private void onBombDetonated()
        {
            foreach (Bomb bomb in SceneManager.Instance.GameplayState.Bombs)
            {
                bomb.GetTimer().StopTimer();
            }
            if (RecordManager.Instance.GetCurrentRecord().Result == GameResultEnum.ExplodedDueToStrikes)
            {
                float timeElapsed = 0;
                foreach (Bomb bomb in SceneManager.Instance.GameplayState.Bombs)
                {
                    float bombTime = bomb.GetTimer().TimeElapsed;
                    if (bombTime > timeElapsed)
                        timeElapsed = bombTime;
                }
                RecordManager.Instance.SetResult(GameResultEnum.ExplodedDueToStrikes, timeElapsed, SceneManager.Instance.GameplayState.GetElapsedRealTime());
            }
        }

        private IEnumerator createNewBomb(string missionId, Vector3 position, Vector3 eulerAngles)
        {
            Debug.Log("[MultipleBombs]Generating new bomb");

            GameplayState gameplayState = SceneManager.Instance.GameplayState;

            GameObject spawnPointGO = new GameObject("CustomBombSpawnPoint");
            spawnPointGO.transform.position = position;
            spawnPointGO.transform.eulerAngles = eulerAngles;
            HoldableSpawnPoint spawnPoint = spawnPointGO.AddComponent<HoldableSpawnPoint>();
            spawnPoint.HoldableTarget = gameplayState.Room.BombSpawnPosition.HoldableTarget;

            Bomb bomb = null;
            if (missionId == FreeplayMissionGenerator.FREEPLAY_MISSION_ID)
            {
                Mission freeplayMission = FreeplayMissionGenerator.Generate(GameplayState.FreeplaySettings);
                MissionManager.Instance.MissionDB.AddMission(freeplayMission);
                bomb = gameCommands.CreateBomb(missionId, null, spawnPointGO, new System.Random().Next().ToString()).GetComponent<Bomb>();
                MissionManager.Instance.MissionDB.Missions.Remove(freeplayMission);
            }
            else if (missionId == ModMission.CUSTOM_MISSION_ID)
            {
                Mission customMission = gameplayState.Mission;
                string oldName = customMission.name;
                customMission.name = ModMission.CUSTOM_MISSION_ID;
                MissionManager.Instance.MissionDB.AddMission(customMission);
                bomb = gameCommands.CreateBomb(missionId, null, spawnPointGO, new System.Random().Next().ToString()).GetComponent<Bomb>();
                MissionManager.Instance.MissionDB.Missions.Remove(customMission);
                customMission.name = oldName;
            }
            else
            {
                bomb = gameCommands.CreateBomb(missionId, null, spawnPointGO, new System.Random().Next().ToString()).GetComponent<Bomb>();
            }

            GameObject roomGO = (GameObject)gameplayState.GetType().GetField("roomGO", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameplayState);
            Selectable mainSelectable = gameplayState.Room.GetComponent<Selectable>();
            List<Selectable> children = mainSelectable.Children.ToList();
            children.Insert(children.Count - 1, bomb.GetComponent<Selectable>());
            mainSelectable.Children = children.ToArray();
            mainSelectable.ChildRowLength++;
            bomb.GetComponent<Selectable>().Parent = mainSelectable;
            KTInputManager.Instance.SelectableManager.ConfigureSelectableAreas(KTInputManager.Instance.RootSelectable);

            bomb.GetTimer().text.gameObject.SetActive(false);
            bomb.GetTimer().LightGlow.enabled = false;

            RedirectPresentBombInfos(bomb);
            ProcessBombEvents(bomb);

            Debug.Log("[MultipleBombs]Bomb generated");
            yield return new WaitForSeconds(2f);

            Debug.Log("[MultipleBombs]Activating custom bomb timer");
            bomb.GetTimer().text.gameObject.SetActive(true);
            bomb.GetTimer().LightGlow.enabled = true;
            Debug.Log("[MultipleBombs]Custom bomb timer activated");
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
                    if (rooms.Count == 0)
                    {
                        Debug.LogError("[MultipleBombs]No room found that supports " + bombs + " bombs");
                        return;
                    }
                    GameplayRoom selectedRoom = rooms[UnityEngine.Random.Range(0, rooms.Count)];
                    GameplayState.GameplayRoomPrefabOverride = selectedRoom.gameObject;
                    usingRoomPrefabOverride = true;
                }
            }
        }
    }
}
