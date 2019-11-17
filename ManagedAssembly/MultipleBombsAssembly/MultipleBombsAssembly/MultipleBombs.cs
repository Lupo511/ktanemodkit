using Assets.Scripts.Missions;
using Assets.Scripts.Pacing;
using Assets.Scripts.Props;
using Assets.Scripts.Records;
using Assets.Scripts.Settings;
using Assets.Scripts.Tournaments;
using Events;
using I2.Loc;
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
        private int currentFreePlayBombCount;
        private float? defaultMaxTime = null;
        private int currentMaxModModules;
        private TextMeshPro currentFreePlayBombCountLabel;
        private Dictionary<GameplayRoom, int> multipleBombsRooms;
        private FieldInfo gameplayStateRoomGOField;
        private FieldInfo gameplayStateLightBulbField;
        private Dictionary<Bomb, BombEvents.BombSolvedEvent> bombSolvedEvents;
        private Dictionary<Bomb, BombComponentEvents.ComponentPassEvent> bombComponentPassEvents;
        private Dictionary<Bomb, BombComponentEvents.ComponentStrikeEvent> bombComponentStrikeEvents;
        private GameplayMusicControllerMonitor gameplayMusicControllerMonitor;
        private KMGameInfo gameInfo;
        private KMGameCommands gameCommands;
        private Shader unlitColorShader;
        private MultipleBombsProperties publicProperties;

        public void Awake()
        {
            Debug.Log("[MultipleBombs]Initializing");
            DestroyImmediate(GetComponent<KMService>()); //Hide from Mod Selector
            currentFreePlayBombCount = 1;
            multipleBombsRooms = new Dictionary<GameplayRoom, int>();
            gameplayStateRoomGOField = typeof(GameplayState).GetField("roomGO", BindingFlags.Instance | BindingFlags.NonPublic);
            gameplayStateLightBulbField = typeof(GameplayState).GetField("lightBulb", BindingFlags.Instance | BindingFlags.NonPublic);
            gameInfo = GetComponent<KMGameInfo>();
            gameCommands = GetComponent<KMGameCommands>();
            gameInfo.OnStateChange += onGameStateChanged;
            unlitColorShader = Shader.Find("Unlit/Color");

            GameObject infoObject = new GameObject("MultipleBombs_Info");
            infoObject.transform.parent = gameObject.transform;
            publicProperties = infoObject.AddComponent<MultipleBombsProperties>();
            publicProperties.MultipleBombs = this;

            Debug.Log("[MultipleBombs]Basic initialization finished");

            gameplayMusicControllerMonitor = MusicManager.Instance.GameplayMusicController.gameObject.AddComponent<GameplayMusicControllerMonitor>();

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

                    if (currentFreePlayBombCount > maxBombCount)
                    {
                        currentFreePlayBombCount = maxBombCount;
                        currentFreePlayBombCountLabel.text = currentFreePlayBombCount.ToString();
                    }
                }
            }
        }

        public void OnDestroy()
        {
            if (SceneManager.Instance != null && SceneManager.Instance.CurrentState != SceneManager.State.ModManager)
                throw new NotImplementedException();
            Debug.Log("[MultipleBombs]Destroying");
            gameInfo.OnStateChange -= onGameStateChanged;
            if (gameplayMusicControllerMonitor != null)
                Destroy(gameplayMusicControllerMonitor);
            Debug.Log("[MultipleBombs]Destroyed");
        }

        private void onGameStateChanged(KMGameInfo.State state)
        {
            if (state == KMGameInfo.State.Gameplay)
            {
                StartCoroutine(setupGameplayState());
            }
            else
            {
                Debug.Log("[MultipleBombs]Cleaning up");
                StopAllCoroutines();
                bombSolvedEvents = null;
                bombComponentPassEvents = null;
                bombComponentStrikeEvents = null;
                BombEvents.OnBombDetonated -= onBombDetonated;
                BombComponentEvents.OnComponentPass -= onComponentPassEvent;
                BombComponentEvents.OnComponentStrike -= onComponentStrikeEvent;
                if (state == KMGameInfo.State.Setup)
                {
                    StartCoroutine(setupSetupRoom());
                }
            }
        }

        private IEnumerator setupSetupRoom()
        {
            yield return null;
            if (!TournamentManager.Instance.IsActive)
            {
                Debug.Log("[MultipleBombs]Adding FreePlay option");
                FreeplayDevice device = FindObjectOfType<SetupRoom>().FreeplayDevice;
                GameObject modulesObject = device.ModuleCountIncrement.transform.parent.gameObject;
                GameObject bombsObject = Instantiate(modulesObject, modulesObject.transform.position, modulesObject.transform.rotation, modulesObject.transform.parent);
                device.ObjectsToDisableOnLidClose.Add(bombsObject);
                bombsObject.name = "BombCountSettings";
                bombsObject.transform.localPosition += new Vector3(0, 0f, -0.025f);
                TextMeshPro currentFreePlayBombsLabel = bombsObject.transform.Find("ModuleCountLabel").GetComponent<TextMeshPro>();
                Destroy(currentFreePlayBombsLabel.GetComponent<Localize>());
                currentFreePlayBombsLabel.text = "Bombs";
                currentFreePlayBombCountLabel = bombsObject.transform.Find("ModuleCountValue").GetComponent<TextMeshPro>();
                currentFreePlayBombCountLabel.gameObject.name = "BombCountLabel";
                currentFreePlayBombCountLabel.text = currentFreePlayBombCount.ToString();
                GameObject modulesLedShell = modulesObject.transform.parent.Find("LEDs/Modules_LED_shell").gameObject;
                GameObject bombsLedShell = Instantiate(modulesLedShell, modulesLedShell.transform.position, modulesLedShell.transform.rotation, modulesLedShell.transform.parent);
                bombsLedShell.name = "Bombs_LED_shell";
                bombsLedShell.transform.localPosition += new Vector3(0, 0, -0.028f);
                LED bombsLed = bombsObject.transform.Find("ModuleCountLED").GetComponent<LED>();
                bombsLed.gameObject.name = "BombCountLed";
                bombsLed.transform.localPosition += new Vector3(0, 0, 0.003f);
                bombsLed.transform.Find("Modules_LED_On").gameObject.name = "Bombs_LED_On";
                bombsLed.transform.Find("Modules_LED_Off").gameObject.name = "Bombs_LED_Off";
                bombsObject.transform.Find("Modules_INCR_btn_highlight").name = "Bombs_INCR_btn_highlight";
                bombsObject.transform.Find("Modules_DECR_btn_highlight").name = "Bombs_DECR_btn_highlight";

                GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
                background.name = "BombCountBackground";
                Renderer backgroundRenderer = background.GetComponent<Renderer>();
                backgroundRenderer.material.shader = unlitColorShader;
                backgroundRenderer.material.color = Color.black;
                background.transform.localScale = new Vector3(0.048f, 0.023f, 0.005f); //Accurate Y would be 0.025
                background.transform.parent = bombsObject.transform;
                background.transform.localPosition = currentFreePlayBombCountLabel.gameObject.transform.localPosition + new Vector3(0.00025f, -0.0027f, 0);
                background.transform.localEulerAngles = currentFreePlayBombCountLabel.gameObject.transform.localEulerAngles;

                GameObject incrementButton = bombsObject.transform.Find("Modules_INCR_btn").gameObject;
                incrementButton.name = "Bombs_INCR_btn";
                GameObject decrementButton = bombsObject.transform.Find("Modules_DECR_btn").gameObject;
                decrementButton.name = "Bombs_DECR_btn";
                Selectable deviceSelectable = device.GetComponent<Selectable>();
                Selectable incrementButtonSelectable = incrementButton.GetComponent<Selectable>();
                Selectable decrementButtonSelectable = decrementButton.GetComponent<Selectable>();
                List<Selectable> children = deviceSelectable.Children.ToList();
                children.Insert(2, incrementButtonSelectable);
                children.Insert(2, decrementButtonSelectable);
                deviceSelectable.Children = children.ToArray();
                deviceSelectable.Init();

                //Call the Awake callback
                bombsObject.SetActive(true);
                bombsObject.SetActive(false);

                if (KTInputManager.Instance.IsMotionControlMode())
                {
                    incrementButtonSelectable.ActivateMotionControls();
                    decrementButtonSelectable.ActivateMotionControls();
                }

                incrementButton.GetComponent<KeypadButton>().OnPush = new PushEvent(() =>
                {
                    if (currentFreePlayBombCount >= GetCurrentMaximumBombCount())
                        return;
                    currentFreePlayBombCount++;
                    currentFreePlayBombCountLabel.text = currentFreePlayBombCount.ToString();
                    updateDifficulty(device);
                });
                decrementButton.GetComponent<KeypadButton>().OnPush = new PushEvent(() =>
                {
                    if (currentFreePlayBombCount <= 1)
                        return;
                    currentFreePlayBombCount--;
                    currentFreePlayBombCountLabel.text = currentFreePlayBombCount.ToString();
                    updateDifficulty(device);
                });
                //string textColor = "#" + valueText.color.r.ToString("x2") + valueText.color.g.ToString("x2") + valueText.color.b.ToString("x2");
                incrementButtonSelectable.OnHighlight = new Action(() =>
                {
                    device.Screen.CurrentState = FreeplayScreen.State.Start;
                    bombsLed.SetState(true);
                    device.Screen.ScreenText.text = "BOMBS:\n\nNumber of bombs\nto defuse\n\n<size=20><#00ff00>Multiple Bombs Mod</color></size>";
                });
                decrementButtonSelectable.OnHighlight = new Action(() =>
                {
                    device.Screen.CurrentState = FreeplayScreen.State.Start;
                    bombsLed.SetState(true);
                    device.Screen.ScreenText.text = "BOMBS:\n\nNumber of bombs\nto defuse\n\n<size=20><#00ff00>Multiple Bombs Mod</color></size>";
                });

                patchButtonPush(device.ModuleCountDecrement.GetComponent<KeypadButton>(), device);
                patchButtonPush(device.ModuleCountIncrement.GetComponent<KeypadButton>(), device);
                patchButtonPush(device.TimeDecrement.GetComponent<KeypadButton>(), device);
                patchButtonPush(device.TimeIncrement.GetComponent<KeypadButton>(), device);
                patchToggle(device.NeedyToggle.GetComponent<ToggleSwitch>(), device);
                patchToggle(device.HardcoreToggle.GetComponent<ToggleSwitch>(), device);

                Action setCustomModulesText = new Action(() =>
                {
                    device.Screen.CurrentState = FreeplayScreen.State.Modules;
                    device.Screen.ScreenText.text = "MODULES:\n\nNumber of modules\nper bomb";
                });
                Action disableBomsLed = new Action(() => bombsLed.SetState(false));
                Selectable moduleCountDecrementSelectable = device.ModuleCountDecrement.GetComponent<Selectable>();
                Action moduleCountDecrementAction = (Action)findFreePlayDeviceEvent(moduleCountDecrementSelectable.OnHighlight, device);
                if (moduleCountDecrementAction != null)
                {
                    moduleCountDecrementSelectable.OnHighlight -= moduleCountDecrementAction;
                    moduleCountDecrementSelectable.OnHighlight += setCustomModulesText;
                }
                moduleCountDecrementSelectable.OnHighlight += disableBomsLed;
                Selectable moduleCountIncrementSelectable = device.ModuleCountIncrement.GetComponent<Selectable>();
                Action moduleCountIncrementAction = (Action)findFreePlayDeviceEvent(moduleCountIncrementSelectable.OnHighlight, device);
                if (moduleCountIncrementAction != null)
                {
                    moduleCountIncrementSelectable.OnHighlight -= moduleCountIncrementAction;
                    moduleCountIncrementSelectable.OnHighlight += setCustomModulesText;
                }
                moduleCountIncrementSelectable.OnHighlight += disableBomsLed;
                device.TimeDecrement.GetComponent<Selectable>().OnHighlight += disableBomsLed;
                device.TimeIncrement.GetComponent<Selectable>().OnHighlight += disableBomsLed;
                device.NeedyToggle.GetComponent<Selectable>().OnHighlight += disableBomsLed;
                device.HardcoreToggle.GetComponent<Selectable>().OnHighlight += disableBomsLed;
                device.ModsOnly.GetComponent<Selectable>().OnHighlight += disableBomsLed;
                device.StartButton.GetComponent<Selectable>().OnHighlight += disableBomsLed;

                incrementButtonSelectable.SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.015f, -0.015f, -0.015f);
                decrementButtonSelectable.SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.015f, -0.015f, -0.015f);
                device.ModuleCountIncrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.012f, -0.012f, -0.012f);
                device.ModuleCountDecrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.012f, -0.012f, -0.012f);
                device.TimeIncrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.01f, -0.01f, -0.01f);
                device.TimeDecrement.GetComponent<Selectable>().SelectableArea.GetComponent<BoxCollider>().size += new Vector3(-0.01f, -0.01f, -0.01f);
                Debug.Log("[MultipleBombs]FreePlay option added");
            }

            currentMaxModModules = ModManager.Instance.GetMaximumModules();

            multipleBombsRooms = new Dictionary<GameplayRoom, int>();
            foreach (GameplayRoom room in ModManager.Instance.GetGameplayRooms())
            {
                multipleBombsRooms.Add(room, getRoomSupportedBombCount(room));
            }
            Debug.Log("[MultipleBombs]GamePlayRooms processed");

            SetupRoom setupRoom = FindObjectOfType<SetupRoom>();
            MissionDetailPageMonitor missionDetailPageMonitor = setupRoom.BombBinder.MissionDetailPage.gameObject.AddComponent<MissionDetailPageMonitor>();
            missionDetailPageMonitor.MultipleBombs = this;
            if (setupRoom.TournamentWhiteboard != null)
            {
                TournamentDetailPageMonitor tournamentDetailPageMonitor = setupRoom.TournamentWhiteboard.TournamentDetailPage.gameObject.AddComponent<TournamentDetailPageMonitor>();
                tournamentDetailPageMonitor.MultipleBombs = this;
            }
            Debug.Log("[MultipleBombs]BombBinder info added");
        }

        private Delegate findFreePlayDeviceEvent(Delegate source, FreeplayDevice device)
        {
            foreach (Delegate del in source.GetInvocationList())
            {
                if (ReferenceEquals(del.Target, device))
                {
                    return del;
                }
            }
            return null;
        }

        private void patchButtonPush(KeypadButton button, FreeplayDevice device)
        {
            PushEvent original = (PushEvent)findFreePlayDeviceEvent(button.OnPush, device);
            button.OnPush -= original;
            button.OnPush += new PushEvent(() =>
            {
                if (original != null)
                    original();
                updateDifficulty(device);
            });
        }

        private void patchToggle(ToggleSwitch toggle, FreeplayDevice device)
        {
            ToggleEvent original = (ToggleEvent)findFreePlayDeviceEvent(toggle.OnToggle, device);
            toggle.OnToggle -= original;
            toggle.OnToggle += new ToggleEvent((bool toggleState) =>
            {
                if (original != null)
                    original(toggleState);
                updateDifficulty(device);
            });
        }

        private void updateDifficulty(FreeplayDevice device)
        {
            if (device.CurrentSettings.IsHardCore)
            {
                device.DifficultyIndicator.Configure(device.CurrentSettings.Time, device.CurrentSettings.ModuleCount * currentFreePlayBombCount, device.CurrentSettings.HasNeedy, true);
                if (device.CurrentSettings.HasNeedy)
                    device.DifficultyIndicator.Difficulty += currentFreePlayBombCount - 1;
            }
            else
            {
                device.DifficultyIndicator.Configure(device.CurrentSettings.Time, device.CurrentSettings.ModuleCount, device.CurrentSettings.HasNeedy, false);
                device.DifficultyIndicator.Difficulty *= currentFreePlayBombCount;
            }
        }

        private IEnumerator setupGameplayState()
        {
            List<ComponentPool> multipleBombsComponentPools = null;
            Mission mission = null;
            MultipleBombsMissionDetails missionDetails = null;
            if (GameplayState.MissionToLoad == FreeplayMissionGenerator.FREEPLAY_MISSION_ID)
            {
                mission = FreeplayMissionGenerator.Generate(GameplayState.FreeplaySettings);
                missionDetails = new MultipleBombsMissionDetails(currentFreePlayBombCount, mission.GeneratorSetting);
            }
            else if (GameplayState.MissionToLoad == ModMission.CUSTOM_MISSION_ID)
            {
                mission = GameplayState.CustomMission;
                missionDetails = MultipleBombsMissionDetails.ReadMission(GameplayState.CustomMission, true, out multipleBombsComponentPools);
            }
            else
            {
                mission = MissionManager.Instance.GetMission(GameplayState.MissionToLoad);
                missionDetails = MultipleBombsMissionDetails.ReadMission(mission, true, out multipleBombsComponentPools);
            }

            int maximumBombCount = GetCurrentMaximumBombCount();
            if (missionDetails.BombCount > maximumBombCount)
            {
                Debug.Log("[MultipleBombs]Bomb count greater than the maximum bomb count (" + missionDetails.BombCount + " bombs, " + maximumBombCount + " maximum)");
                SceneManager.Instance.ReturnToSetupState();
                yield break;
            }
            else if (missionDetails.BombCount > 1 && GameplayState.GameplayRoomPrefabOverride == null)
            {
                Debug.Log("[MultipleBombs]Initializing room");
                List<GameplayRoom> rooms = new List<GameplayRoom>();
                if (missionDetails.BombCount <= 2)
                    rooms.Add(SceneManager.Instance.GameplayState.GameplayRoomPool.Default.GetComponent<GameplayRoom>());
                foreach (KeyValuePair<GameplayRoom, int> room in multipleBombsRooms)
                {
                    if (room.Value >= missionDetails.BombCount)
                        rooms.Add(room.Key);
                }
                if (rooms.Count == 0)
                {
                    Debug.Log("[MultipleBombs]No room found that supports " + missionDetails.BombCount + " bombs");
                    SceneManager.Instance.ReturnToSetupState();
                    yield break;
                }
                else
                {
                    GameplayRoom roomPrefab = rooms[UnityEngine.Random.Range(0, rooms.Count)];
                    Destroy((GameObject)gameplayStateRoomGOField.GetValue(SceneManager.Instance.GameplayState));
                    GameObject room = Instantiate(roomPrefab.gameObject, Vector3.zero, Quaternion.identity);
                    room.transform.parent = SceneManager.Instance.GameplayState.transform;
                    room.transform.localScale = Vector3.one;
                    gameplayStateRoomGOField.SetValue(SceneManager.Instance.GameplayState, room);
                    gameplayStateLightBulbField.SetValue(SceneManager.Instance.GameplayState, GameObject.Find("LightBulb"));
                    room.SetActive(false);
                    FindObjectOfType<BombGenerator>().BombPrefabOverride = room.GetComponent<GameplayRoom>().BombPrefabOverride;
                    Debug.Log("[MultipleBombs]Room initialized");
                }

                gameplayMusicControllerMonitor.NewRoundStarted();
            }

            BombInfoRedirection.SetBombCount(missionDetails.BombCount);
            if (bombSolvedEvents == null)
                bombSolvedEvents = new Dictionary<Bomb, BombEvents.BombSolvedEvent>();
            if (bombComponentPassEvents == null)
                bombComponentPassEvents = new Dictionary<Bomb, BombComponentEvents.ComponentPassEvent>();
            if (bombComponentStrikeEvents == null)
                bombComponentStrikeEvents = new Dictionary<Bomb, BombComponentEvents.ComponentStrikeEvent>();

            BombEvents.OnBombDetonated += onBombDetonated;
            BombComponentEvents.OnComponentPass += onComponentPassEvent;
            BombComponentEvents.OnComponentStrike += onComponentStrikeEvent;
            Debug.Log("[MultipleBombs]Events initialized");

            //Setup results screen
            ResultPageMonitor freePlayDefusedPageMonitor = SceneManager.Instance.PostGameState.Room.BombBinder.ResultFreeplayDefusedPage.gameObject.AddComponent<ResultPageMonitor>();
            freePlayDefusedPageMonitor.MultipleBombs = this;
            freePlayDefusedPageMonitor.SetCurrentMission(missionDetails);
            ResultPageMonitor freePlayExplodedPageMonitor = SceneManager.Instance.PostGameState.Room.BombBinder.ResultFreeplayExplodedPage.gameObject.AddComponent<ResultPageMonitor>();
            freePlayExplodedPageMonitor.MultipleBombs = this;
            freePlayExplodedPageMonitor.SetCurrentMission(missionDetails);
            ResultPageMonitor missionDefusedPageMonitor = SceneManager.Instance.PostGameState.Room.BombBinder.ResultDefusedPage.gameObject.AddComponent<ResultPageMonitor>();
            missionDefusedPageMonitor.MultipleBombs = this;
            missionDefusedPageMonitor.SetCurrentMission(missionDetails);
            ResultPageMonitor missionExplodedPageMonitor = SceneManager.Instance.PostGameState.Room.BombBinder.ResultExplodedPage.gameObject.AddComponent<ResultPageMonitor>();
            missionExplodedPageMonitor.MultipleBombs = this;
            missionExplodedPageMonitor.SetCurrentMission(missionDetails);
            ResultPageMonitor tournamentPageMonitor = SceneManager.Instance.PostGameState.Room.BombBinder.ResultTournamentPage.gameObject.AddComponent<ResultPageMonitor>();
            tournamentPageMonitor.MultipleBombs = this;
            tournamentPageMonitor.SetCurrentMission(missionDetails);
            Debug.Log("[MultipleBombs]Result screens initialized");
            yield return null;

            if (GameplayState.MissionToLoad == ModMission.CUSTOM_MISSION_ID || GameplayState.MissionToLoad != FreeplayMissionGenerator.FREEPLAY_MISSION_ID)
            {
                mission.GeneratorSetting.ComponentPools.AddRange(multipleBombsComponentPools);
            }

            Debug.Log("[MultipleBombs]Initializing gameplay state");

            Debug.Log("[MultipleBombs]Bombs to spawn: " + missionDetails.BombCount);

            if (missionDetails.BombCount == 1)
            {
                yield break;
            }

            List<KMBombInfo> redirectedBombInfos = new List<KMBombInfo>();

            Bomb vanillaBomb = SceneManager.Instance.GameplayState.Bomb;
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
            redirectPresentBombInfos(vanillaBomb, redirectedBombInfos);
            processBombEvents(vanillaBomb);
            Debug.Log("[MultipleBombs]Default bomb initialized");

            System.Random random = null;
            if (GameplayState.BombSeedToUse == -1)
                random = new System.Random();
            else
                random = new System.Random(GameplayState.BombSeedToUse);

            for (int i = 1; i < missionDetails.BombCount; i++)
            {
                GeneratorSetting generatorSetting;
                if (!missionDetails.GeneratorSettings.TryGetValue(i, out generatorSetting))
                {
                    generatorSetting = missionDetails.GeneratorSettings[0];
                }
                GameObject spawn = GameObject.Find("MultipleBombs_Spawn_" + i);
                if (spawn == null)
                {
                    if (i == 1)
                    {
                        StartCoroutine(createNewBomb(generatorSetting, SceneManager.Instance.GameplayState.Room.BombSpawnPosition.transform.position + new Vector3(0.4f, 0, 0), new Vector3(0, 30, 0), random.Next(), redirectedBombInfos, i));
                    }
                    else
                    {
                        Debug.LogError("[MultipleBombs]The current gameplay room doesn't support " + (i + 1) + " bombs");
                        break;
                    }
                }
                else
                {
                    StartCoroutine(createNewBomb(generatorSetting, spawn.transform.position, spawn.transform.eulerAngles, random.Next(), redirectedBombInfos, i));
                }
            }

            vanillaBomb.GetComponent<Selectable>().Parent.Init();
            Debug.Log("[MultipleBombs]All bombs generated");

            //PaceMaker
            PaceMakerMonitor monitor = FindObjectOfType<PaceMaker>().gameObject.AddComponent<PaceMakerMonitor>();
            foreach (Bomb bomb in SceneManager.Instance.GameplayState.Bombs)
            {
                if (bomb != vanillaBomb) //The vanilla bomb is still handled by PaceMaker
                    bomb.GetTimer().TimerTick = (TimerComponent.TimerTickEvent)Delegate.Combine(bomb.GetTimer().TimerTick, new TimerComponent.TimerTickEvent((elapsed, remaining) => monitor.OnBombTimerTick(bomb, elapsed, remaining)));
            }
            Debug.Log("[MultipleBombs]Pacing events initalized");
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
                if (bombSolvedEvents.ContainsKey(source.Bomb) && bombSolvedEvents[source.Bomb] != null)
                    bombSolvedEvents[source.Bomb].Invoke();
                SceneManager.Instance.GameplayState.OnBombSolved();
                foreach (Bomb bomb in SceneManager.Instance.GameplayState.Bombs)
                    if (!bomb.IsSolved())
                        return true;
                Debug.Log("[MultipleBombs]All bombs solved, what a winner!");
                GameRecord currentRecord = RecordManager.Instance.GetCurrentRecord();
                float maxTime = currentRecord.FreeplaySettings != null ? currentRecord.FreeplaySettings.Time : MissionManager.Instance.GetMission(currentRecord.MissionID).GeneratorSetting.TimeLimit;
                RecordManager.Instance.SetResult(GameResultEnum.Defused, maxTime - source.Bomb.GetTimer().TimeRemaining, SceneManager.Instance.GameplayState.GetElapsedRealTime());
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
                float timeElapsed = -1;
                foreach (Bomb bomb in SceneManager.Instance.GameplayState.Bombs)
                {
                    if (!bomb.IsSolved())
                    {
                        float bombTime = bomb.GetTimer().TimeElapsed;
                        if (bombTime > timeElapsed)
                            timeElapsed = bombTime;
                    }
                }
                if (timeElapsed != -1)
                    RecordManager.Instance.SetResult(GameResultEnum.ExplodedDueToStrikes, timeElapsed, SceneManager.Instance.GameplayState.GetElapsedRealTime());
            }
        }

        private IEnumerator createNewBomb(GeneratorSetting generatorSetting, Vector3 position, Vector3 eulerAngles, int seed, List<KMBombInfo> redirectedBombInfos, int selectableShift)
        {
            Debug.Log("[MultipleBombs]Generating new bomb");

            GameplayState gameplayState = SceneManager.Instance.GameplayState;
            Bomb bomb = createBomb(generatorSetting, position, eulerAngles, seed, redirectedBombInfos);

            bomb.GetTimer().text.gameObject.SetActive(false);
            bomb.GetTimer().LightGlow.enabled = false;
            Debug.Log("[MultipleBombs]Custom bomb timer deactivated");

            GameObject roomGO = (GameObject)gameplayStateRoomGOField.GetValue(gameplayState);
            Selectable mainSelectable = gameplayState.Room.GetComponent<Selectable>();
            List<Selectable> children = mainSelectable.Children.ToList();
            int row = 0;
            for (int i = mainSelectable.ChildRowLength; i <= children.Count; i += mainSelectable.ChildRowLength)
            {
                if (row != gameplayState.Room.BombSpawnPosition.SelectableIndexY)
                {
                    children.Insert(i, null);
                    i++;
                }
                row++;
            }
            mainSelectable.ChildRowLength++;
            mainSelectable.DefaultSelectableIndex = gameplayState.Room.BombSpawnPosition.SelectableIndexY * mainSelectable.ChildRowLength + gameplayState.Room.BombSpawnPosition.SelectableIndexX;
            children.Insert(mainSelectable.DefaultSelectableIndex + selectableShift, bomb.GetComponent<Selectable>());
            mainSelectable.Children = children.ToArray();
            bomb.GetComponent<Selectable>().Parent = mainSelectable;
            KTInputManager.Instance.SelectableManager.ConfigureSelectableAreas(KTInputManager.Instance.RootSelectable);

            Debug.Log("[MultipleBombs]Bomb generated");
            yield return new WaitForSeconds(2f);

            Debug.Log("[MultipleBombs]Activating custom bomb timer");
            bomb.GetTimer().text.gameObject.SetActive(true);
            bomb.GetTimer().LightGlow.enabled = true;
            Debug.Log("[MultipleBombs]Custom bomb timer activated");
        }

        private Bomb createBomb(Vector3 position, Vector3 eulerAngles, int seed, List<KMBombInfo> knownBombInfos)
        {
            return createBomb(0, position, eulerAngles, seed, knownBombInfos);
        }

        private Bomb createBomb(int generatorSettingsIndex, Vector3 position, Vector3 eulerAngles, int seed, List<KMBombInfo> knownBombInfos)
        {
            MultipleBombsMissionDetails missionDetails = null;
            if (GameplayState.MissionToLoad == FreeplayMissionGenerator.FREEPLAY_MISSION_ID)
            {
                missionDetails = new MultipleBombsMissionDetails(currentFreePlayBombCount, FreeplayMissionGenerator.Generate(GameplayState.FreeplaySettings).GeneratorSetting);
            }
            else if (GameplayState.MissionToLoad == ModMission.CUSTOM_MISSION_ID)
            {
                missionDetails = MultipleBombsMissionDetails.ReadMission(GameplayState.CustomMission);
            }
            else
            {
                missionDetails = MultipleBombsMissionDetails.ReadMission(MissionManager.Instance.GetMission(GameplayState.MissionToLoad));
            }
            GeneratorSetting generatorSetting;
            if (!missionDetails.GeneratorSettings.TryGetValue(generatorSettingsIndex, out generatorSetting))
                generatorSetting = missionDetails.GeneratorSettings[0];
            return createBomb(generatorSetting, position, eulerAngles, seed, knownBombInfos);
        }

        private Bomb createBomb(GeneratorSetting generatorSetting, Vector3 position, Vector3 eulerAngles, int seed, List<KMBombInfo> knownBombInfos)
        {
            return createBomb(createModFromGeneratorSetting(generatorSetting), position, eulerAngles, seed, knownBombInfos);
        }

        private Bomb createBomb(KMGeneratorSetting generatorSetting, Vector3 position, Vector3 eulerAngles, int seed, List<KMBombInfo> knownBombInfos)
        {
            if (bombSolvedEvents == null)
                bombSolvedEvents = new Dictionary<Bomb, BombEvents.BombSolvedEvent>();
            if (bombComponentPassEvents == null)
                bombComponentPassEvents = new Dictionary<Bomb, BombComponentEvents.ComponentPassEvent>();
            if (bombComponentStrikeEvents == null)
                bombComponentStrikeEvents = new Dictionary<Bomb, BombComponentEvents.ComponentStrikeEvent>();

            Debug.Log("[MultipleBombs]Creating new bomb");

            if (knownBombInfos == null)
            {
                knownBombInfos = new List<KMBombInfo>();
                foreach (KMBombInfo info in FindObjectsOfType<KMBombInfo>())
                {
                    knownBombInfos.Add(info);
                }
            }

            GameObject spawnPointGO = new GameObject("CustomBombSpawnPoint");
            spawnPointGO.transform.position = position;
            spawnPointGO.transform.eulerAngles = eulerAngles;
            Bomb bomb = gameCommands.CreateBomb(null, generatorSetting, spawnPointGO, seed.ToString()).GetComponent<Bomb>();
            Debug.Log("[MultipleBombs]Bomb spawned");

            redirectPresentBombInfos(bomb, knownBombInfos);
            Debug.Log("[MultipleBombs]KMBombInfos redirected");

            processBombEvents(bomb);
            Debug.Log("[MultipleBombs]Bomb created");
            return bomb;
        }

        private KMGeneratorSetting createModFromGeneratorSetting(GeneratorSetting generatorSetting)
        {
            KMGeneratorSetting modSetting = new KMGeneratorSetting();
            modSetting.FrontFaceOnly = generatorSetting.FrontFaceOnly;
            modSetting.NumStrikes = generatorSetting.NumStrikes;
            modSetting.TimeBeforeNeedyActivation = generatorSetting.TimeBeforeNeedyActivation;
            modSetting.TimeLimit = generatorSetting.TimeLimit;
            modSetting.OptionalWidgetCount = generatorSetting.OptionalWidgetCount;
            modSetting.ComponentPools = new List<KMComponentPool>();
            foreach (ComponentPool pool in generatorSetting.ComponentPools)
            {
                KMComponentPool modPool = new KMComponentPool();
                modPool.Count = pool.Count;
                modPool.SpecialComponentType = (KMComponentPool.SpecialComponentTypeEnum)pool.SpecialComponentType;
                modPool.AllowedSources = (KMComponentPool.ComponentSource)pool.AllowedSources;
                modPool.ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>();
                if (pool.ComponentTypes != null)
                {
                    foreach (ComponentTypeEnum componentType in pool.ComponentTypes)
                    {
                        modPool.ComponentTypes.Add((KMComponentPool.ComponentTypeEnum)componentType);
                    }
                }
                modPool.ModTypes = new List<string>();
                if (pool.ModTypes != null)
                    modPool.ModTypes.AddRange(pool.ModTypes);
                modSetting.ComponentPools.Add(modPool);
            }
            return modSetting;
        }

        private void redirectPresentBombInfos(Bomb bomb, List<KMBombInfo> knownBombInfos)
        {
            if (knownBombInfos == null)
                knownBombInfos = new List<KMBombInfo>();
            foreach (KMBombInfo info in FindObjectsOfType<KMBombInfo>())
            {
                if (!knownBombInfos.Contains(info))
                {
                    BombInfoRedirection.RedirectBombInfo(info, bomb);
                    ModBombInfo modInfo = info.GetComponent<ModBombInfo>();
                    foreach (BombEvents.BombSolvedEvent bombSolvedDelegate in BombEvents.OnBombSolved.GetInvocationList())
                    {
                        if (bombSolvedDelegate.Target != null && ReferenceEquals(bombSolvedDelegate.Target, modInfo))
                        {
                            if (bombSolvedEvents.ContainsKey(bomb))
                                bombSolvedEvents[bomb] += bombSolvedDelegate;
                            else
                                bombSolvedEvents.Add(bomb, bombSolvedDelegate);
                            BombEvents.OnBombSolved -= bombSolvedDelegate;
                            break;
                        }
                    }
                    knownBombInfos.Add(info);
                }
            }
        }

        private void processBombEvents(Bomb bomb)
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

        private int getRoomSupportedBombCount(GameplayRoom gameplayRoom)
        {
            if (gameplayRoom.GetComponent<ElevatorRoom>() != null)
                return 1;
            for (int i = 2; i < int.MaxValue; i++)
            {
                if (!gameplayRoom.transform.FindRecursive("MultipleBombs_Spawn_" + i))
                {
                    return i;
                }
            }
            return int.MaxValue;
        }

        public int GetCurrentMaximumBombCount()
        {
            if (GameplayState.GameplayRoomPrefabOverride != null)
            {
                return getRoomSupportedBombCount(GameplayState.GameplayRoomPrefabOverride.GetComponent<GameplayRoom>());
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

        public int CurrentFreePlayBombCount
        {
            get
            {
                return currentFreePlayBombCount;
            }
            set
            {
                if (SceneManager.Instance.CurrentState != SceneManager.State.Setup)
                    throw new InvalidOperationException("You can only set the current FreePlay bomb count in the Setup room.");
                if (value < 1)
                    throw new Exception("The bomb count must be greater than 0.");
                if (value > GetCurrentMaximumBombCount())
                    throw new Exception("The specified bomb count is greater than the current maximum bomb count.");
                currentFreePlayBombCount = value;
                currentFreePlayBombCountLabel.text = currentFreePlayBombCount.ToString();
            }
        }
    }
}
