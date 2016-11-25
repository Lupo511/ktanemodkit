using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultipleBombsAssembly
{
    public static class BombInfoRedirection
    {
        internal static float GetTime(ModBombComponent caller)
        {
            Bomb bomb = caller.Bomb;
            if (bomb == null)
                return 0f;
            return bomb.GetTimer().TimeRemaining;
        }

        internal static string GetFormattedTime(ModBombComponent caller)
        {
            Bomb bomb = caller.Bomb;
            if (bomb == null)
                return "";
            return TimerComponent.GetFormattedTime(bomb.GetTimer().TimeRemaining, true);
        }

        internal static int GetStrikes(ModBombComponent caller)
        {
            Bomb bomb = caller.Bomb;
            if (bomb == null)
                return 0;
            return bomb.NumStrikes;
        }

        internal static List<string> GetModuleNames(ModBombComponent caller)
        {
            List<string> modules = new List<string>();
            Bomb bomb = caller.Bomb;
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

        internal static List<string> GetSolvableModuleNames(ModBombComponent caller)
        {
            List<string> modules = new List<string>();
            Bomb bomb = caller.Bomb;
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

        internal static List<string> GetSolvedModuleNames(ModBombComponent caller)
        {
            List<string> modules = new List<string>();
            Bomb bomb = caller.Bomb;
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

        internal static List<string> GetWidgetQueryResponses(ModBombComponent caller, string queryKey, string queryInfo)
        {
            List<string> responses = new List<string>();
            Bomb bomb = caller.Bomb;
            if (bomb == null)
                return responses;
            foreach (Widget widget in bomb.WidgetManager.GetWidgets())
            {
                string queryResponse = widget.GetQueryResponse(queryKey, queryInfo);
                if (queryResponse != null && queryResponse != "")
                    responses.Add(queryResponse);
            }
            return responses;
        }

        internal static bool IsBombPresent(ModBombComponent caller)
        {
            return caller.Bomb != null;
        }
    }
}
