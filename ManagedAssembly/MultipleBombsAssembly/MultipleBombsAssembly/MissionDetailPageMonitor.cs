using Assets.Scripts.Missions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class MissionDetailPageMonitor : MonoBehaviour
    {
        public MultipleBombs MultipleBombs { get; set; }
        private MissionDetailPage page;
        private FieldInfo canStartField = typeof(MissionDetailPage).GetField("canStartMission", BindingFlags.Instance | BindingFlags.NonPublic);
        private TMPro.TextAlignmentOptions originalAlignment;
        private bool originalEnableAutoSizing;

        private void Awake()
        {
            page = GetComponent<MissionDetailPage>();
        }

        private void OnEnable()
        {
            StartCoroutine(setupPage());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            page.TextStrikes.alignment = originalAlignment;
            page.TextStrikes.enableAutoSizing = originalEnableAutoSizing;
        }

        private IEnumerator setupPage()
        {
            originalAlignment = page.TextStrikes.alignment;
            originalEnableAutoSizing = page.TextStrikes.enableAutoSizing;
            page.TextDescription.gameObject.SetActive(false);
            page.TextTime.gameObject.SetActive(false);
            page.TextModuleCount.gameObject.SetActive(false);
            page.TextStrikes.gameObject.SetActive(false);
            yield return null;
            page.TextDescription.gameObject.SetActive(true);
            page.TextTime.gameObject.SetActive(true);
            page.TextModuleCount.gameObject.SetActive(true);
            page.TextStrikes.gameObject.SetActive(true);

            Mission currentMission = (Mission)page.GetType().BaseType.GetField("currentMission", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(page);
            MultipleBombsMissionDetails missionDetails = MultipleBombsMissionDetails.ReadMission(currentMission);

            float maxTime = 0;
            int totalModules = 0;
            int totalStrikes = 0;
            string error = GetMissionDetailInformation(missionDetails, MultipleBombs.GetCurrentMaximumBombCount(), out maxTime, out totalModules, out totalStrikes);
            canStartField.SetValue(page, error == null);
            page.TextDescription.text = error ?? currentMission.Description;

            page.TextModuleCount.text = totalModules + (totalModules == 1 ? " Module" : " Modules");
            if (missionDetails.BombCount > 1)
            {
                page.TextTime.text = string.Format("{0}:{1:00}", (int)maxTime / 60, maxTime % 60);
                page.TextStrikes.alignment = TMPro.TextAlignmentOptions.Right;
                page.TextStrikes.enableAutoSizing = false;
                page.TextStrikes.text = missionDetails.BombCount + " Bombs\n" + totalStrikes + (totalStrikes == 1 ? " Strike" : " Strikes") + "\n ";
            }
        }

        public static string GetMissionDetailInformation(MultipleBombsMissionDetails missionDetails, int maxBombCount, out float maxTime, out int totalModules, out int totalStrikes)
        {
            List<string> missingModTypes = new List<string>();
            int maxModuleCount = Math.Max(11, ModManager.Instance.GetMaximumModules());
            int maxFrontFaceModuleCount = Math.Max(5, ModManager.Instance.GetMaximumModulesFrontFace());
            int requiredModuleCount = 0;
            int requiredFrontFaceModuleCount = 0;
            foreach (GeneratorSetting generatorSetting in missionDetails.GeneratorSettings.Values)
            {
                if (generatorSetting.ComponentPools != null)
                {
                    foreach (ComponentPool pool in generatorSetting.ComponentPools)
                    {
                        foreach (string modType in pool.ModTypes)
                        {
                            if (!ModManager.Instance.HasBombComponent(modType))
                            {
                                missingModTypes.Add(modType);
                            }
                        }
                    }
                }
                int moduleCount = generatorSetting.GetComponentCount();
                if (generatorSetting.FrontFaceOnly)
                    requiredFrontFaceModuleCount = Math.Max(requiredFrontFaceModuleCount, moduleCount);
                else
                    requiredModuleCount = Math.Max(requiredModuleCount, moduleCount);
            }

            maxTime = 0;
            totalModules = 0;
            totalStrikes = 0;
            for (int i = 0; i < missionDetails.BombCount; i++)
            {
                GeneratorSetting generatorSetting;
                if (missionDetails.GeneratorSettings.TryGetValue(i, out generatorSetting))
                {
                    if (generatorSetting.TimeLimit > maxTime)
                        maxTime = generatorSetting.TimeLimit;
                    totalModules += generatorSetting.GetComponentCount();
                    totalStrikes += generatorSetting.NumStrikes;
                }
                else
                {
                    totalModules += missionDetails.GeneratorSettings[0].GetComponentCount();
                    totalStrikes += missionDetails.GeneratorSettings[0].NumStrikes;
                }
            }

            if (missingModTypes.Count > 0)
            {
                return "Missing mods:\n" + string.Join("\n", missingModTypes.ToArray());
            }
            else if (requiredModuleCount > maxModuleCount)
            {
                return "A bomb that can support more modules is required.\n\nCurrent bombs only support up to " + maxModuleCount + " modules.";
            }
            else if (requiredFrontFaceModuleCount > maxFrontFaceModuleCount)
            {
                return "A bomb that can support more modules is required.\n\nCurrent bombs only support up to " + maxFrontFaceModuleCount + " modules.";
            }
            else if (missionDetails.BombCount > maxBombCount)
            {
                return "A room that can support more bombs is required.\n\nCurrent rooms only support up to " + maxBombCount + " bombs.";
            }
            else
            {
                return null;
            }
        }
    }
}
