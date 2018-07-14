using Assets.Scripts.Missions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class MultipleBombsMissionDetails
    {
        public int BombCount { get; set; } = 1;
        public Dictionary<int, GeneratorSetting> GeneratorSettings { get; set; } = new Dictionary<int, GeneratorSetting>();

        public MultipleBombsMissionDetails()
        {

        }

        public MultipleBombsMissionDetails(int bombCount, GeneratorSetting firstBombGeneratorSetting)
        {
            BombCount = bombCount;
            GeneratorSettings.Add(0, firstBombGeneratorSetting);
        }

        public static MultipleBombsMissionDetails ReadMission(Mission mission)
        {
            List<ComponentPool> multipleBombsComponentPools;
            return ReadMission(mission, false, out multipleBombsComponentPools);
        }

        public static MultipleBombsMissionDetails ReadMission(Mission mission, bool removeComponentPools, out List<ComponentPool> multipleBombsComponentPools)
        {
            MultipleBombsMissionDetails missionDetails = new MultipleBombsMissionDetails();
            multipleBombsComponentPools = new List<ComponentPool>();
            if (mission.GeneratorSetting != null)
            {
                GeneratorSetting generatorSetting = UnityEngine.Object.Instantiate(mission).GeneratorSetting;
                missionDetails.GeneratorSettings.Add(0, generatorSetting);
                if (generatorSetting.ComponentPools != null)
                {
                    for (int i = generatorSetting.ComponentPools.Count - 1; i >= 0; i--)
                    {
                        ComponentPool pool = generatorSetting.ComponentPools[i];
                        if (pool.ModTypes != null && pool.ModTypes.Count == 1)
                        {
                            if (pool.ModTypes[0] == "Multiple Bombs")
                            {
                                missionDetails.BombCount += pool.Count;
                                generatorSetting.ComponentPools.RemoveAt(i);
                                multipleBombsComponentPools.Add(mission.GeneratorSetting.ComponentPools[i]);
                                if (removeComponentPools)
                                    mission.GeneratorSetting.ComponentPools.RemoveAt(i);
                            }
                            else if (pool.ModTypes[0].StartsWith("Multiple Bombs:"))
                            {
                                string[] strings = pool.ModTypes[0].Split(new char[] { ':' }, 3);
                                if (strings.Length != 3)
                                    continue;
                                int bombIndex;
                                if (!int.TryParse(strings[1], out bombIndex))
                                    continue;
                                if (missionDetails.GeneratorSettings.ContainsKey(bombIndex))
                                    continue;
                                GeneratorSetting bombGeneratorSetting = null;
                                try
                                {
                                    bombGeneratorSetting = ModMission.CreateGeneratorSettingsFromMod(JsonConvert.DeserializeObject<KMGeneratorSetting>(strings[2]));
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                                missionDetails.GeneratorSettings.Add(bombIndex, bombGeneratorSetting);
                                generatorSetting.ComponentPools.RemoveAt(i);
                                multipleBombsComponentPools.Add(mission.GeneratorSetting.ComponentPools[i]);
                                if (removeComponentPools)
                                    mission.GeneratorSetting.ComponentPools.RemoveAt(i);
                            }
                        }
                    }
                }
            }
            else
            {
                missionDetails.GeneratorSettings.Add(0, null);
            }
            return missionDetails;
        }
    }
}
