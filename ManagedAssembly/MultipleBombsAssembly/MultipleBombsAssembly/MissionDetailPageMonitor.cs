using Assets.Scripts.Missions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class MissionDetailPageMonitor : MonoBehaviour
    {
        private Dictionary<string, int> missionList;
        private TMPro.AlignmentTypes originalAlignment;

        private void OnEnable()
        {
            StartCoroutine(changeTextNextFrame());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            GetComponent<MissionDetailPage>().TextStrikes.alignment = originalAlignment;
        }

        private IEnumerator changeTextNextFrame()
        {
            MissionDetailPage page = GetComponent<MissionDetailPage>();
            originalAlignment = page.TextStrikes.alignment;
            yield return null;
            Mission currentMission = (Mission)page.GetType().BaseType.GetField("currentMission", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(page);
            if (missionList.ContainsKey(currentMission.ID))
            {
                page.TextStrikes.alignment = TMPro.AlignmentTypes.Right;
                page.TextStrikes.text = missionList[currentMission.ID] + " Bombs\n" + page.TextStrikes.text + "\n ";
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
