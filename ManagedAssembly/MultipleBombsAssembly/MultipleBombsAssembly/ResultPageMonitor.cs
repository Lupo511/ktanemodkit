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
        private TMPro.AlignmentTypes originalAllignment;

        protected virtual void Awake()
        {
            ResultPage page = GetComponent<ResultPage>();
            if (page.RetryButton != null)
            {
                page.RetryButton.OnInteract = (Selectable.OnInteractHandler)Delegate.Combine(page.RetryButton.OnInteract, new Selectable.OnInteractHandler(OnRetry));
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
                page.NumStrikes.alignment = originalAllignment;
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
            }
            else
            {
                ResultMissionPage page = GetComponent<ResultMissionPage>();
                originalAllignment = page.NumStrikes.alignment;
                yield return null;
                //page.NumModules.text = "<size=0.1>" + bombCount + " x </size>" + page.NumModules.text;
                page.NumStrikes.alignment = TMPro.AlignmentTypes.Right;
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
