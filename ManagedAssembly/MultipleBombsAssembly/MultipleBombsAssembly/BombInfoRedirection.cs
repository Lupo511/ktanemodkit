using Assets.Scripts.Missions;
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

        internal static void RedirectBombInfo(KMBombInfo bombInfo, Bomb bomb)
        {
            ModBombInfo modBombInfo = bombInfo.GetComponent<ModBombInfo>();
            bombInfo.TimeHandler = (KMBombInfo.GetTimeHandler)patchDelegate(bombInfo.TimeHandler, (KMBombInfo.GetTimeHandler)modBombInfo.GetTime, new KMBombInfo.GetTimeHandler(() => GetTime(bomb)));
            bombInfo.FormattedTimeHandler = (KMBombInfo.GetFormattedTimeHandler)patchDelegate(bombInfo.FormattedTimeHandler, (KMBombInfo.GetFormattedTimeHandler)modBombInfo.GetFormattedTime, new KMBombInfo.GetFormattedTimeHandler(() => GetFormattedTime(bomb)));
            bombInfo.StrikesHandler = (KMBombInfo.GetStrikesHandler)patchDelegate(bombInfo.StrikesHandler, (KMBombInfo.GetStrikesHandler)modBombInfo.GetStrikes, new KMBombInfo.GetStrikesHandler(() => GetStrikes(bomb)));
            bombInfo.ModuleNamesHandler = (KMBombInfo.GetModuleNamesHandler)patchDelegate(bombInfo.ModuleNamesHandler, (KMBombInfo.GetModuleNamesHandler)modBombInfo.GetModuleNames, new KMBombInfo.GetModuleNamesHandler(() => GetModuleNames(bomb)));
            bombInfo.SolvableModuleNamesHandler = (KMBombInfo.GetSolvableModuleNamesHandler)patchDelegate(bombInfo.SolvableModuleNamesHandler, (KMBombInfo.GetSolvableModuleNamesHandler)modBombInfo.GetSolvableModuleNames, new KMBombInfo.GetSolvableModuleNamesHandler(() => GetSolvableModuleNames(bomb)));
            bombInfo.SolvedModuleNamesHandler = (KMBombInfo.GetSolvedModuleNamesHandler)patchDelegate(bombInfo.SolvedModuleNamesHandler, (KMBombInfo.GetSolvedModuleNamesHandler)modBombInfo.GetSolvedModuleNames, new KMBombInfo.GetSolvedModuleNamesHandler(() => GetSolvedModuleNames(bomb)));
            bombInfo.ModuleIDsHandler = (KMBombInfo.GetModuleIDsHandler)patchDelegate(bombInfo.ModuleIDsHandler, (KMBombInfo.GetModuleIDsHandler)modBombInfo.GetModuleTypes, new KMBombInfo.GetModuleIDsHandler(() => GetModuleTypes(bomb)));
            bombInfo.SolvableModuleIDsHandler = (KMBombInfo.GetSolvableModuleIDsHandler)patchDelegate(bombInfo.SolvableModuleIDsHandler, (KMBombInfo.GetSolvableModuleIDsHandler)modBombInfo.GetSolvableModuleTypes, new KMBombInfo.GetSolvableModuleIDsHandler(() => GetSolvableModuleTypes(bomb)));
            bombInfo.SolvedModuleIDsHandler = (KMBombInfo.GetSolvedModuleIDsHandler)patchDelegate(bombInfo.SolvedModuleIDsHandler, (KMBombInfo.GetSolvedModuleIDsHandler)modBombInfo.GetSolvedModuleTypes, new KMBombInfo.GetSolvedModuleIDsHandler(() => GetSolvedModuleTypes(bomb)));
            bombInfo.WidgetQueryResponsesHandler = (KMBombInfo.GetWidgetQueryResponsesHandler)patchDelegate(bombInfo.WidgetQueryResponsesHandler, (KMBombInfo.GetWidgetQueryResponsesHandler)modBombInfo.GetWidgetQueryResponses, new KMBombInfo.GetWidgetQueryResponsesHandler((string queryKey, string queryInfo) => GetWidgetQueryResponses(bomb, queryKey, queryInfo)));
            bombInfo.IsBombPresentHandler = (KMBombInfo.KMIsBombPresent)patchDelegateHard(bombInfo.IsBombPresentHandler, modBombInfo, new KMBombInfo.KMIsBombPresent(() => IsBombPresent(bomb)));
        }

        private static Delegate patchDelegate(Delegate source, Delegate remove, Delegate add)
        {
            source = Delegate.Remove(source, remove);
            source = Delegate.Combine(source, add);
            return source;
        }

        private static Delegate patchDelegateHard(Delegate source, object removeTarget, Delegate add)
        {
            foreach (Delegate target in source.GetInvocationList())
            {
                if (target.Target != null && ReferenceEquals(target.Target, removeTarget))
                {
                    source = Delegate.Remove(source, target);
                    break;
                }
            }
            source = Delegate.Combine(source, add);
            return source;
        }

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
            return bomb.GetTimer().GetFormattedTime(bomb.GetTimer().TimeRemaining, true);
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
                if (component.ComponentType != ComponentTypeEnum.Empty && component.ComponentType != ComponentTypeEnum.Timer)
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

        internal static List<string> GetModuleTypes(Bomb bomb)
        {
            List<string> modules = new List<string>();
            if (bomb == null)
                return modules;
            foreach (BombComponent component in bomb.BombComponents)
            {
                if (component.ComponentType != ComponentTypeEnum.Empty && component.ComponentType != ComponentTypeEnum.Timer)
                {
                    modules.Add(GetBombComponentModuleType(component));
                }
            }
            return modules;
        }

        internal static List<string> GetSolvableModuleTypes(Bomb bomb)
        {
            List<string> modules = new List<string>();
            if (bomb == null)
                return modules;
            foreach (BombComponent component in bomb.BombComponents)
            {
                if (component.IsSolvable)
                {
                    modules.Add(GetBombComponentModuleType(component));
                }
            }
            return modules;
        }

        internal static List<string> GetSolvedModuleTypes(Bomb bomb)
        {
            List<string> modules = new List<string>();
            if (bomb == null)
                return modules;
            foreach (BombComponent component in bomb.BombComponents)
            {
                if (component.IsSolvable && component.IsSolved)
                {
                    modules.Add(GetBombComponentModuleType(component));
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

        private static string GetBombComponentModuleType(BombComponent component)
        {
            if (component.ComponentType == ComponentTypeEnum.Mod)
            {
                ModBombComponent modComponent = component.GetComponent<ModBombComponent>();
                if (modComponent != null)
                {
                    return modComponent.GetModComponentType();
                }
            }
            else if (component.ComponentType == ComponentTypeEnum.NeedyMod)
            {
                ModNeedyComponent modNeedyComponent = component.GetComponent<ModNeedyComponent>();
                if (modNeedyComponent != null)
                {
                    return modNeedyComponent.GetModComponentType();
                }
            }
            else
            {
                return component.ComponentType.ToString();
            }
            return "Unknown";
        }
    }
}
