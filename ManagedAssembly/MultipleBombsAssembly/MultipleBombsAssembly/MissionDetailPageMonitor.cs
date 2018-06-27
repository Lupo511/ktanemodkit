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
            page.TextModuleCount.gameObject.SetActive(false);
            page.TextStrikes.gameObject.SetActive(false);
            yield return null;
            page.TextDescription.gameObject.SetActive(true);
            page.TextModuleCount.gameObject.SetActive(true);
            page.TextStrikes.gameObject.SetActive(true);

            Mission currentMission = (Mission)page.GetType().BaseType.GetField("currentMission", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(page);
            MultipleBombsMissionDetails missionDetails = MultipleBombsMissionDetails.ReadMission(currentMission);
            if (missionDetails.BombCount > 1)
            {
                GeneratorSetting originalGeneratorSetting = currentMission.GeneratorSetting;
                currentMission.GeneratorSetting = missionDetails.GeneratorSettings[0];
                page.SetMission(currentMission, page.BombBinder.MissionTableOfContentsPageManager.GetMissionEntry(currentMission.ID).Selectable, page.BombBinder.MissionTableOfContentsPageManager.GetCurrentToCIndex(), page.BombBinder.MissionTableOfContentsPageManager.GetCurrentPage());
                currentMission.GeneratorSetting = originalGeneratorSetting;

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

                if ((bool)canStartField.GetValue(page) && missionDetails.BombCount > MultipleBombs.GetCurrentMaximumBombCount())
                {
                    canStartField.SetValue(page, false);
                    page.TextDescription.text = "A room that can support more bombs is required.\n\nCurrent rooms only support up to " + MultipleBombs.GetCurrentMaximumBombCount() + " bombs.";
                }
            }
        }
    }
}
