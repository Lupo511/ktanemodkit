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
            
            float maxTime = 0;
            int totalModules = 0;
            int totalStrikes = 0;
            string error = MissionDetailPageMonitor.GetMissionDetailInformation(missionDetails, MultipleBombs.GetCurrentMaximumBombCount(), out maxTime, out totalModules, out totalStrikes);
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
    }
}
