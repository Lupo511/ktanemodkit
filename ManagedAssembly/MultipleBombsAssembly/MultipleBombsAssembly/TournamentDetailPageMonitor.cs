using Assets.Scripts.Missions;
using I2.Loc;
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
        private TextMeshPro textBombs;
        private FieldInfo canStartField = typeof(TournamentDetailPage).GetField("canStartMission", BindingFlags.Instance | BindingFlags.NonPublic);

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
            if (textBombs != null)
                textBombs.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (textBombs != null)
                Destroy(textBombs);
        }

        private IEnumerator setupPage()
        {
            page.TextDescription.gameObject.SetActive(false);
            page.TextTime.gameObject.SetActive(false);
            page.TextModuleCount.gameObject.SetActive(false);
            page.TextStrikes.gameObject.SetActive(false);
            yield return null;
            page.TextDescription.gameObject.SetActive(true);
            page.TextTime.gameObject.SetActive(true);
            page.TextModuleCount.gameObject.SetActive(true);
            page.TextStrikes.gameObject.SetActive(true);
            if (textBombs == null)
            {
                textBombs = Instantiate(page.TextStrikes, page.TextStrikes.transform.position, page.TextStrikes.transform.rotation, page.TextStrikes.transform.parent);
                textBombs.gameObject.SetActive(false);
                Destroy(textBombs.GetComponent<Localize>());
                textBombs.transform.localPosition += new Vector3(0, 0.012f, 0);
                textBombs.text = "X Bombs";
            }

            Mission currentMission = (Mission)page.GetType().GetField("currentMission", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(page);

            bool canStart = MissionDetailPageMonitor.UpdateMissionDetailInformation(MultipleBombsMissionDetails.ReadMission(currentMission), currentMission.DescriptionTerm, MultipleBombs.GetCurrentMaximumBombCount(), page.TextDescription, page.TextTime, page.TextModuleCount, page.TextStrikes, textBombs);
            canStartField.SetValue(page, canStart);
        }
    }
}
