using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class ResultFreeplayPageMonitor : MonoBehaviour
    {
        private int bombCount;

        private void OnEnable()
        {
            if (bombCount >= 2)
            {
                Debug.Log("Starting coroutine");
                StartCoroutine(changeTextNextFrame(bombCount));
                bombCount = 1;
            }
        }

        public void SetBombCount(int count)
        {
            bombCount = count;
        }

        private IEnumerator changeTextNextFrame(int bombCount)
        {
            Debug.Log("Coroutine started");
            yield return null;
            ResultFreeplayPage page = GetComponent<ResultFreeplayPage>();
            page.MissionName.Text = "Free Play - " + bombCount + " Bombs";
        }
    }
}
