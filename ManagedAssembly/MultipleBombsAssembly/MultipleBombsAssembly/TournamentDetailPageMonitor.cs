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
    public class TournamentDetailPageMonitor : MonoBehaviour
    {
        public MultipleBombs MultipleBombs { get; set; }
        private TournamentDetailPage page;
        private FieldInfo canStartField = typeof(TournamentDetailPage).GetField("canStartMission", BindingFlags.Instance | BindingFlags.NonPublic);
        private TMPro.TextAlignmentOptions originalAlignment;
        private bool originalEnableAutoSizing;

        private void Awake()
        {
            page = GetComponent<TournamentDetailPage>();
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

            Mission currentMission = (Mission)page.GetType().GetField("currentMission", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(page);
            MultipleBombsMissionDetails missionDetails = MultipleBombsMissionDetails.ReadMission(currentMission);
            if (missionDetails.BombCount > 1)
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
                if (missingModTypes.Count > 0)
                {
                    canStartField.SetValue(page, false);
                    page.TextDescription.text = "Missing mods:\n" + string.Join("\n", missingModTypes.ToArray());
                }
                else if (requiredModuleCount > maxModuleCount)
                {
                    canStartField.SetValue(page, false);
                    page.TextDescription.text = "A bomb that can support more modules is required.\n\nCurrent bombs only support up to " + maxModuleCount + " modules.";
                }
                else if (requiredFrontFaceModuleCount > maxFrontFaceModuleCount)
                {
                    canStartField.SetValue(page, false);
                    page.TextDescription.text = "A bomb that can support more modules is required.\n\nCurrent bombs only support up to " + maxFrontFaceModuleCount + " modules.";
                }
                else if (missionDetails.BombCount > MultipleBombs.GetCurrentMaximumBombCount())
                {
                    canStartField.SetValue(page, false);
                    page.TextDescription.text = "A room that can support more bombs is required.\n\nCurrent rooms only support up to " + MultipleBombs.GetCurrentMaximumBombCount() + " bombs.";
                }
                else
                {
                    canStartField.SetValue(page, true);
                    page.TextDescription.text = currentMission.Description;
                }

                float time = missionDetails.GeneratorSettings[0].TimeLimit;
                int modules = missionDetails.GeneratorSettings[0].GetComponentCount();
                int strikes = missionDetails.GeneratorSettings[0].NumStrikes;
                for (int i = 1; i < missionDetails.BombCount; i++)
                {
                    GeneratorSetting generatorSetting;
                    if (missionDetails.GeneratorSettings.TryGetValue(i, out generatorSetting))
                    {
                        if (generatorSetting.TimeLimit > time)
                            time = generatorSetting.TimeLimit;
                        modules += generatorSetting.GetComponentCount();
                        strikes += generatorSetting.NumStrikes;
                    }
                    else
                    {
                        modules += missionDetails.GeneratorSettings[0].GetComponentCount();
                        strikes += missionDetails.GeneratorSettings[0].NumStrikes;
                    }
                }
                page.TextTime.text = string.Format("{0}:{1:00}", (int)time / 60, time % 60);
                page.TextModuleCount.text = modules + (modules == 1 ? " Module" : " Modules");
                page.TextStrikes.alignment = TMPro.TextAlignmentOptions.Right;
                page.TextStrikes.enableAutoSizing = false;
                page.TextStrikes.text = missionDetails.BombCount + " Bombs\n" + strikes + (strikes == 1 ? " Strike" : " Strikes") + "\n ";
            }
        }
    }
}
