using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class ResultMissionPageMonitor : MonoBehaviour
    {
        private int bombCount;
        private TMPro.AlignmentTypes originalAllignment;

        private void OnEnable()
        {
            if (bombCount >= 2)
            {
                StartCoroutine(changeTextNextFrame(bombCount));
                bombCount = 1;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ResultMissionPage page = GetComponent<ResultMissionPage>();
            page.NumStrikes.alignment = originalAllignment;
        }

        public void SetBombCount(int count)
        {
            bombCount = count;
        }

        private IEnumerator changeTextNextFrame(int bombCount)
        {
            ResultMissionPage page = GetComponent<ResultMissionPage>();
            originalAllignment = page.NumStrikes.alignment;
            yield return null;
            //page.NumModules.text = "<size=0.1>" + bombCount + " x </size>" + page.NumModules.text;
            page.NumStrikes.alignment = TMPro.AlignmentTypes.Right;
            page.NumStrikes.text = bombCount + " Bombs\n" + page.NumStrikes.text + "\n ";
        }
    }
}
