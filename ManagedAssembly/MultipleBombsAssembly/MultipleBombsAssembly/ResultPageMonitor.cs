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
            if (currentMission.BombCount >= 2)
            {
                StartCoroutine(changeTextNextFrame());
            }
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
                yield return null;
                ResultFreeplayPage page = GetComponent<ResultFreeplayPage>();
                if (page.MissionName.SingleLine.IsActive())
                    page.MissionName.SingleLine.text += " - " + currentMission.BombCount + " Bombs";
                else
                    page.MissionName.DoubleLine.text += " - " + currentMission.BombCount + " Bombs";

                float time = currentMission.GeneratorSettings[0].TimeLimit;
                int modules = currentMission.GeneratorSettings[0].GetComponentCount();
                bool isHardcore = currentMission.GeneratorSettings[0].NumStrikes == 1;
                for (int i = 1; i < currentMission.BombCount; i++)
                {
                    GeneratorSetting generatorSetting;
                    if (currentMission.GeneratorSettings.TryGetValue(i, out generatorSetting))
                    {
                        if (generatorSetting.TimeLimit > time)
                            time = generatorSetting.TimeLimit;
                        modules += generatorSetting.GetComponentCount();
                        if (isHardcore && generatorSetting.NumStrikes != 1)
                            isHardcore = false;
                    }
                    else
                    {
                        modules += currentMission.GeneratorSettings[0].GetComponentCount();
                    }
                }

                page.FreeplayTime.text = string.Format("{0}:{1:00}", (int)time / 60, time % 60);
                Localization.SetTerm("BombBinder/txtModuleCount", page.FreeplayModules.gameObject);
                Localization.SetParameter("MODULE_COUNT", modules.ToString(), page.FreeplayModules.gameObject);
                Localization.SetTerm(isHardcore ? "BombBinder/results_HardcoreOn" : "BombBinder/results_HardcoreOff", page.FreeplayHardcore.gameObject);
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
