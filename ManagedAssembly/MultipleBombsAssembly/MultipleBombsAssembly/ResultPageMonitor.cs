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
        private int bombCount;
        private TMPro.TextAlignmentOptions originalAllignment;
        private bool originalEnableAutosizing;

        protected virtual void Awake()
        {
            ResultPage page = GetComponent<ResultPage>();
            if (page.RetryButton != null)
            {
                page.RetryButton.OnInteract += OnRetry;
            }
        }

        protected virtual void OnEnable()
        {
            if (bombCount >= 2)
            {
                StartCoroutine(changeTextNextFrame(bombCount));
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

        private void OnDestroy()
        {
            ResultPage page = GetComponent<ResultPage>();
            if (page.RetryButton != null)
            {
                page.RetryButton.OnInteract -= OnRetry;
            }
        }

        private bool OnRetry()
        {
            MultipleBombs.SetNextGameplayRoom(bombCount);
            return false;
        }

        private IEnumerator changeTextNextFrame(int bombCount)
        {
            if (GetComponent<ResultFreeplayPage>() != null)
            {
                yield return null;
                ResultFreeplayPage page = GetComponent<ResultFreeplayPage>();
                page.MissionName.Text = "Free Play - " + bombCount + " Bombs";
                if (RecordManager.Instance.GetCurrentRecord().MissionID != FreeplayMissionGenerator.FREEPLAY_MISSION_ID)
                {
                    int moduleCount = RecordManager.Instance.GetCurrentRecord().FreeplaySettings.ModuleCount - bombCount + 1;
                    page.FreeplayModules.text = moduleCount + (moduleCount == 1 ? " Module" : " Modules");
                }
            }
            else
            {
                ResultMissionPage page = GetComponent<ResultMissionPage>();
                originalAllignment = page.NumStrikes.alignment;
                originalEnableAutosizing = page.NumStrikes.enableAutoSizing;
                yield return null;
                //page.NumModules.text = "<size=0.1>" + bombCount + " x </size>" + page.NumModules.text;
                page.NumStrikes.alignment = TMPro.TextAlignmentOptions.Right;
                page.NumStrikes.enableAutoSizing = false;
                page.NumStrikes.text = bombCount + " Bombs\n" + page.NumStrikes.text + "\n ";
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

        public void SetBombCount(int count)
        {
            bombCount = count;
        }
    }
}
