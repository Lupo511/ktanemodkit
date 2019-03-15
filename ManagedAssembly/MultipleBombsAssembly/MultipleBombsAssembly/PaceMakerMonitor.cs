using Assets.Scripts.Pacing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class PaceMakerMonitor : MonoBehaviour
    {
        private PaceMaker paceMaker;
        private FieldInfo isActiveField;
        private float secondsUntilNextIdleAction;
        private float MIN_SECONDS_BETWEEN_IDLE_ACTIONS;
        private float MAX_SECONDS_BETWEEN_IDLE_ACTIONS;
        private List<Bomb> oneMinuteLeftBombs;

        public void Awake()
        {
            paceMaker = GetComponent<PaceMaker>();
            isActiveField = paceMaker.GetType().GetField("isActive", BindingFlags.Instance | BindingFlags.NonPublic);
            MIN_SECONDS_BETWEEN_IDLE_ACTIONS = (float)paceMaker.GetType().GetField("MIN_SECONDS_BETWEEN_IDLE_ACTIONS", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(paceMaker);
            MAX_SECONDS_BETWEEN_IDLE_ACTIONS = (float)paceMaker.GetType().GetField("MAX_SECONDS_BETWEEN_IDLE_ACTIONS", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(paceMaker);
            oneMinuteLeftBombs = new List<Bomb>();
        }

        public void Start()
        {
            paceMaker.enabled = false;
            secondsUntilNextIdleAction = UnityEngine.Random.Range(MIN_SECONDS_BETWEEN_IDLE_ACTIONS, MAX_SECONDS_BETWEEN_IDLE_ACTIONS);
        }

        public void OnBombTimerTick(Bomb bomb, int elapsed, int remaining)
        {
            if (remaining < 60 && !oneMinuteLeftBombs.Contains(bomb))
            {
                oneMinuteLeftBombs.Add(bomb);
                paceMaker.ExecuteRandomAction(PaceEvent.OneMinuteLeft);
            }
        }

        public void Update()
        {
            if ((bool)isActiveField.GetValue(paceMaker))
            {
                secondsUntilNextIdleAction -= Time.deltaTime;
                if (secondsUntilNextIdleAction <= 0)
                {
                    float successRating = CalculateCustomSuccessRating();
                    if (UnityEngine.Random.value < 0.1)
                    {
                        Debug.Log("[MultipleBombs-PaceMaker]Skipping idle action for variety's sake");
                    }
                    else if (successRating > 0.5f)
                    {
                        paceMaker.ExecuteRandomAction(PaceEvent.Idle_DoingWell);
                    }
                    secondsUntilNextIdleAction = UnityEngine.Random.Range(MIN_SECONDS_BETWEEN_IDLE_ACTIONS, MAX_SECONDS_BETWEEN_IDLE_ACTIONS);
                    Debug.Log("[MultipleBombs-PaceMaker]Next idle action in " + secondsUntilNextIdleAction + " seconds");
                }
            }
        }

        public float CalculateCustomSuccessRating()
        {
            if (SceneManager.Instance.GameplayState.Bombs.Count == 0)
                return 1f;

            int solvableComponentCount = 0;
            int solvedComponentCount = 0;
            int numStrikes = 0;
            int numStrikesToLose = 0;
            float totalTime = float.MaxValue;
            float timeRemaining = float.MaxValue;
            foreach (Bomb bomb in SceneManager.Instance.GameplayState.Bombs)
            {
                solvableComponentCount += bomb.GetSolvableComponentCount();
                solvedComponentCount += bomb.GetSolvedComponentCount();

                if (bomb.NumStrikesToLose > 1)
                {
                    numStrikes += bomb.NumStrikes;
                    numStrikesToLose += bomb.NumStrikesToLose;
                }

                if (bomb.TotalTime < totalTime)
                    totalTime = bomb.TotalTime;
                TimerComponent timer = bomb.GetTimer();
                if (!bomb.IsSolved() && timer.TimeRemaining < timeRemaining)
                    timeRemaining = timer.TimeRemaining;
            }

            float solvedFactorMultiplier = 0.5f;
            float solvedFactor = 1f;
            float strikesFactorMultiplier = 0.3f;
            float strikesFactor = 0f;
            float timeFactorMultiplier = 0.2f;
            float timeFactor = 0f;

            if ((solvedComponentCount < solvableComponentCount) && (solvableComponentCount > 1))
            {
                solvedFactor = solvedComponentCount / (float)(solvableComponentCount - 1);
                solvedFactor = Mathf.Clamp(solvedFactor, 0f, 1f) * solvedFactorMultiplier;
            }
            if ((numStrikes < numStrikesToLose) && (numStrikesToLose > 1))
            {
                strikesFactor = 1f - numStrikes / (float)(numStrikesToLose - 1);
                strikesFactor = Mathf.Clamp(strikesFactor, 0f, 1f) * strikesFactorMultiplier;
            }
            if (totalTime > 60f)
            {
                timeFactor = Mathf.Clamp(Mathf.Lerp(0f, 1f, timeRemaining / totalTime), 0f, 1f) * timeFactorMultiplier;
            }
            float successRating = Mathf.Clamp(solvedFactor + strikesFactor + timeFactor, 0f, 1f);
            Debug.LogFormat("[MultipleBombs-PaceMaker]PlayerSuccessRating: {0} (Factors: solved: {1}, strikes: {2}, time: {3})", successRating, solvedFactor, strikesFactor, timeFactor);
            return successRating;
        }
    }
}
