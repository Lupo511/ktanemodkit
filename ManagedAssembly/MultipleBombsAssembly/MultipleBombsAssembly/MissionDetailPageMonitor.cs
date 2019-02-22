using Assets.Scripts.Missions;
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

            bool canStart = UpdateMissionDetailInformation(MultipleBombsMissionDetails.ReadMission(currentMission), currentMission.DescriptionTerm, MultipleBombs.GetCurrentMaximumBombCount(), page.TextDescription, page.TextTime, page.TextModuleCount, page.TextStrikes);
            canStartField.SetValue(page, canStart);
        }

        public static bool UpdateMissionDetailInformation(MultipleBombsMissionDetails missionDetails, string descriptionTerm, int maxBombCount, TextMeshPro textDescription, TextMeshPro textTime, TextMeshPro textModuleCount, TextMeshPro textStrikes)
        {
            bool canStart = false;

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

            float maxTime = 0;
            int totalModules = 0;
            int totalStrikes = 0;
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

            if (textDescription != null)
            {
                if (missingModTypes.Count > 0)
                {
                    canStart = false;
                    Localization.SetTerm("BombBinder/error_missingModules", textDescription.gameObject);
                    Localization.SetParameter("MISSING_MODULES_LIST", string.Join("\n", missingModTypes.ToArray()), textDescription.gameObject);
                }
                else if (requiredModuleCount > maxModuleCount)
                {
                    canStart = false;
                    Localization.SetTerm("BombBinder/error_needABiggerBomb", textDescription.gameObject);
                    Localization.SetParameter("MAX_MODULE_COUNT", maxModuleCount.ToString(), textDescription.gameObject);
                }
                else if (requiredFrontFaceModuleCount > maxFrontFaceModuleCount)
                {
                    canStart = false;
                    Localization.SetTerm("BombBinder/error_needABiggerBomb", textDescription.gameObject);
                    Localization.SetParameter("MAX_MODULE_COUNT", maxModuleCount.ToString(), textDescription.gameObject);
                }
                else if (missionDetails.BombCount > maxBombCount)
                {
                    canStart = false;
                    textDescription.text = "A room that can support more bombs is required.\n\nCurrent rooms only support up to " + maxBombCount + " bombs.";
                }
                else
                {
                    canStart = true;
                    Localization.SetTerm(descriptionTerm, textDescription.gameObject);
                }
            }

            Localization.SetTerm("BombBinder/txtModuleCount", textModuleCount.gameObject);
            Localization.SetParameter("MODULE_COUNT", totalModules.ToString(), textModuleCount.gameObject);
            if (missionDetails.BombCount > 1)
            {
                textTime.text = string.Format("{0}:{1:00}", (int)maxTime / 60, maxTime % 60);
                Localization.SetTerm("BombBinder/txtStrikeCount", textStrikes.gameObject);
                Localization.SetParameter("STRIKE_COUNT", totalStrikes.ToString(), textStrikes.gameObject);
                textStrikes.alignment = TextAlignmentOptions.Right;
                textStrikes.enableAutoSizing = false;
                textStrikes.text = missionDetails.BombCount + " Bombs\n" + textStrikes.text + "\n ";
            }

            return canStart;
        }
    }
}
