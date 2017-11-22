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
        private MultipleBombs multipleBombs;
        private Dictionary<string, int> missionList;
        private MissionDetailPage page;
        private TMPro.TextAlignmentOptions originalAlignment;
        private bool originalEnableAutoSizing;

        private void Awake()
        {
            page = GetComponent<MissionDetailPage>();
            page.StartButton.OnInteract += OnStart;
        }

        private void OnEnable()
        {
            StartCoroutine(changeTextNextFrame());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            page.TextStrikes.alignment = originalAlignment;
            page.TextStrikes.enableAutoSizing = originalEnableAutoSizing;
        }

        private void OnDestroy()
        {
            page.StartButton.OnInteract -= OnStart;
        }

        private bool OnStart()
        {
            Mission currentMission = (Mission)page.GetType().BaseType.GetField("currentMission", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(page);
            if (missionList.ContainsKey(currentMission.ID) && missionList[currentMission.ID] <= MultipleBombs.GetCurrentMaximumBombCount())
            {
                MultipleBombs.SetNextGameplayRoom(missionList[currentMission.ID]);
            }
            return false;
        }

        private IEnumerator changeTextNextFrame()
        {
            originalAlignment = page.TextStrikes.alignment;
            originalEnableAutoSizing = page.TextStrikes.enableAutoSizing;
            yield return null;
            Mission currentMission = (Mission)page.GetType().BaseType.GetField("currentMission", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(page);
            if (missionList.ContainsKey(currentMission.ID))
            {
                page.TextStrikes.alignment = TMPro.TextAlignmentOptions.Right;
                page.TextStrikes.enableAutoSizing = false;
                page.TextStrikes.text = missionList[currentMission.ID] + " Bombs\n" + page.TextStrikes.text + "\n ";
                if (missionList[currentMission.ID] > MultipleBombs.GetCurrentMaximumBombCount())
                {
                    page.GetType().GetField("canStartMission", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(page, false);
                    page.TextDescription.text = "A room that can support more bombs is required.\n\nCurrent rooms only support up to " + MultipleBombs.GetCurrentMaximumBombCount() + " bombs.";
                }
            }
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

        public Dictionary<string, int> MissionList
        {
            get
            {
                return missionList;
            }

            set
            {
                missionList = value;
            }
        }
    }
}
