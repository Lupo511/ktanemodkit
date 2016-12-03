using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultipleBombsAssembly
{
    public static class BombInfoRedirection
    {
        private static int bombCount = 1;

        internal static void SetBombCount(int count)
        {
            bombCount = count;
        }

        internal static float GetTime(Bomb bomb)
        {
            if (bomb == null)
                return 0f;
            return bomb.GetTimer().TimeRemaining;
        }

        internal static string GetFormattedTime(Bomb bomb)
        {
            if (bomb == null)
                return "";
            return TimerComponent.GetFormattedTime(bomb.GetTimer().TimeRemaining, true);
        }

        internal static int GetStrikes(Bomb bomb)
        {
            if (bomb == null)
                return 0;
            return bomb.NumStrikes;
        }

        internal static List<string> GetModuleNames(Bomb bomb)
        {
            List<string> modules = new List<string>();
            if (bomb == null)
                return modules;
            foreach (BombComponent component in bomb.BombComponents)
            {
                if (component.ComponentType != Assets.Scripts.Missions.ComponentTypeEnum.Empty && component.ComponentType != Assets.Scripts.Missions.ComponentTypeEnum.Timer)
                {
                    modules.Add(component.GetModuleDisplayName());
                }
            }
            return modules;
        }

        internal static List<string> GetSolvableModuleNames(Bomb bomb)
        {
            List<string> modules = new List<string>();
            if (bomb == null)
                return modules;
            foreach (BombComponent component in bomb.BombComponents)
            {
                if (component.IsSolvable)
                {
                    modules.Add(component.GetModuleDisplayName());
                }
            }
            return modules;
        }

        internal static List<string> GetSolvedModuleNames(Bomb bomb)
        {
            List<string> modules = new List<string>();
            if (bomb == null)
                return modules;
            foreach (BombComponent component in bomb.BombComponents)
            {
                if (component.IsSolvable && component.IsSolved)
                {
                    modules.Add(component.GetModuleDisplayName());
                }
            }
            return modules;
        }

        internal static List<string> GetWidgetQueryResponses(Bomb bomb, string queryKey, string queryInfo)
        {
            List<string> responses = new List<string>();
            if (bomb == null)
                return responses;
            foreach (Widget widget in bomb.WidgetManager.GetWidgets())
            {
                string queryResponse = widget.GetQueryResponse(queryKey, queryInfo);
                if (queryResponse != null && queryResponse != "")
                    responses.Add(queryResponse);
            }
            if (queryKey == "MultipleBombs")
            {
                Dictionary<string, int> response = new Dictionary<string, int>();
                response.Add("bombCount", bombCount);
                responses.Add(JsonConvert.SerializeObject(response));
            }
            return responses;
        }

        internal static bool IsBombPresent(Bomb bomb)
        {
            return bomb != null;
        }
    }
}
