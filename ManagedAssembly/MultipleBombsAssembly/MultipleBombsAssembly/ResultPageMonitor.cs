using Assets.Scripts.Missions;
using Assets.Scripts.Records;
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class ResultPageMonitor : MonoBehaviour
    {
        private MultipleBombs multipleBombs;
        private MultipleBombsMissionDetails currentMission;
        private TextMeshPro numBombs;

        protected virtual void OnEnable()
        {
            StartCoroutine(changeTextNextFrame());
        }

        protected virtual void OnDisable()
        {
            StopAllCoroutines();
            if (numBombs != null)
                numBombs.gameObject.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            if (numBombs != null)
                Destroy(numBombs);
        }

        private IEnumerator changeTextNextFrame()
        {
            if (GetComponent<ResultFreeplayPage>() != null)
            {
                if (currentMission.BombCount > 1)
                {
                    yield return null;
                    ResultFreeplayPage page = GetComponent<ResultFreeplayPage>();
                    if (numBombs == null)
                    {
                        numBombs = Instantiate(page.FreeplayModules, page.FreeplayModules.transform.position, page.FreeplayModules.transform.rotation, page.FreeplayModules.transform.parent);
                        Destroy(numBombs.GetComponent<Localize>());
                        numBombs.transform.localPosition += new Vector3(0, 0.012f, 0);
                        numBombs.text = "X Bombs";
                    }

                    float time;
                    int modules;
                    int strikes;
                    currentMission.GetMissionInfo(out time, out modules, out strikes);

                    numBombs.text = currentMission.BombCount + " Bombs";
                    numBombs.gameObject.SetActive(true);
                    page.FreeplayTime.text = string.Format("{0}:{1:00}", (int)time / 60, time % 60);
                    Localization.SetTerm("BombBinder/txtModuleCount", page.FreeplayModules.gameObject);
                    Localization.SetParameter("MODULE_COUNT", modules.ToString(), page.FreeplayModules.gameObject);
                    Localization.SetTerm(strikes == currentMission.BombCount ? "BombBinder/results_HardcoreOn" : "BombBinder/results_HardcoreOff", page.FreeplayHardcore.gameObject); //Assumes always positive strikes
                }
            }
            else
            {
                ResultMissionPage page = GetComponent<ResultMissionPage>();
                yield return null;
                if (numBombs == null)
                {
                    numBombs = Instantiate(page.NumStrikes, page.NumStrikes.transform.position, page.NumStrikes.transform.rotation, page.NumStrikes.transform.parent);
                    numBombs.gameObject.SetActive(false);
                    Destroy(numBombs.GetComponent<Localize>());
                    numBombs.transform.localPosition += new Vector3(0, 0.012f, 0);
                    numBombs.text = "X Bombs";
                }

                MissionDetailPageMonitor.UpdateMissionDetailInformation(currentMission, null, MultipleBombs.GetCurrentMaximumBombCount(), null, page.InitialTime, page.NumModules, page.NumStrikes, numBombs);
            }
        }

        public void SetCurrentMission(MultipleBombsMissionDetails mission)
        {
            currentMission = mission;
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
    }
}
