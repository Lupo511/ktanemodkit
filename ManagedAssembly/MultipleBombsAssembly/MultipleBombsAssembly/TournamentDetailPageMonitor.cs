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

            bool canStart = MissionDetailPageMonitor.UpdateMissionDetailInformation(MultipleBombsMissionDetails.ReadMission(currentMission), currentMission.DescriptionTerm, MultipleBombs.GetCurrentMaximumBombCount(), page.TextDescription, page.TextTime, page.TextModuleCount, page.TextStrikes);
            canStartField.SetValue(page, canStart);
        }
    }
}
