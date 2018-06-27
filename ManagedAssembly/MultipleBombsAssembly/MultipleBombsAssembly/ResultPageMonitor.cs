using Assets.Scripts.Missions;
using Assets.Scripts.Records;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class ResultPageMonitor : MonoBehaviour
    {
        private MultipleBombs multipleBombs;
        private MultipleBombsMissionDetails currentMission;
        private TMPro.TextAlignmentOptions originalAllignment;
        private bool originalEnableAutosizing;

        protected virtual void OnEnable()
        {
            if (currentMission.BombCount >= 2)
            {
                StartCoroutine(changeTextNextFrame());
            }
        }

        protected virtual void OnDisable()
        {
            StopAllCoroutines();
            ResultMissionPage page = GetComponent<ResultMissionPage>();
            if (page != null)
            {
                page.NumStrikes.alignment = originalAllignment;
                page.NumStrikes.enableAutoSizing = originalEnableAutosizing;
            }
        }

        private IEnumerator changeTextNextFrame()
        {
            if (GetComponent<ResultFreeplayPage>() != null)
            {
                yield return null;
                ResultFreeplayPage page = GetComponent<ResultFreeplayPage>();
                page.MissionName.Text = "Free Play - " + currentMission.BombCount + " Bombs";
                float time = currentMission.GeneratorSettings[0].TimeLimit;
                int modules = currentMission.GeneratorSettings[0].GetComponentCount();
                bool isHardcore = currentMission.GeneratorSettings[0].NumStrikes == 1;
                for (int i = 1; i < currentMission.BombCount; i++)
                {
                    GeneratorSetting generatorSetting;
                    if (currentMission.GeneratorSettings.TryGetValue(i, out generatorSetting))
                    {
                        if (generatorSetting.TimeLimit > time)
                            time = generatorSetting.TimeLimit;
                        modules += generatorSetting.GetComponentCount();
                        if (isHardcore && generatorSetting.NumStrikes != 1)
                            isHardcore = false;
                    }
                    else
                    {
                        modules += currentMission.GeneratorSettings[0].GetComponentCount();
                    }
                }
                page.FreeplayTime.text = string.Format("{0}:{1:00}", (int)time / 60, time % 60);
                page.FreeplayModules.text = modules + (modules == 1 ? " Module" : " Modules");
                page.FreeplayHardcore.text = isHardcore ? "Hardcore: ON" : "Hardcore: OFF";
            }
            else
            {
                ResultMissionPage page = GetComponent<ResultMissionPage>();
                originalAllignment = page.NumStrikes.alignment;
                originalEnableAutosizing = page.NumStrikes.enableAutoSizing;
                yield return null;
                float time = currentMission.GeneratorSettings[0].TimeLimit;
                int modules = currentMission.GeneratorSettings[0].GetComponentCount();
                int strikes = currentMission.GeneratorSettings[0].NumStrikes;
                for (int i = 1; i < currentMission.BombCount; i++)
                {
                    GeneratorSetting generatorSetting;
                    if (currentMission.GeneratorSettings.TryGetValue(i, out generatorSetting))
                    {
                        if (generatorSetting.TimeLimit > time)
                            time = generatorSetting.TimeLimit;
                        modules += generatorSetting.GetComponentCount();
                        strikes += generatorSetting.NumStrikes;
                    }
                    else
                    {
                        modules += currentMission.GeneratorSettings[0].GetComponentCount();
                        strikes += currentMission.GeneratorSettings[0].NumStrikes;
                    }
                }
                page.NumStrikes.alignment = TMPro.TextAlignmentOptions.Right;
                page.NumStrikes.enableAutoSizing = false;
                page.InitialTime.text = string.Format("{0}:{1:00}", (int)time / 60, time % 60);
                page.NumModules.text = modules + (modules == 1 ? " Module" : " Modules");
                //page.NumModules.text = "<size=0.1>" + currentMission.BombCount + " x </size>" + page.NumModules.text;
                page.NumStrikes.text = currentMission.BombCount + " Bombs\n" + strikes + (strikes == 1 ? " Strike" : " Strikes") + "\n ";
            }
        }

        public void SetCurrentMission(MultipleBombsMissionDetails mission)
        {
            currentMission = mission;
        }

        public MultipleBombs MultipleBombs
        {
            get
            {
                return multipleBombs;
            }
            set
            {
                multipleBombs = value;
            }
        }
    }
}
