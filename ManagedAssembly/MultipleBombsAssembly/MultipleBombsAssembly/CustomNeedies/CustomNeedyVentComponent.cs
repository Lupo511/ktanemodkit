using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class CustomNeedyVentComponent : NeedyVentComponent
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            Bomb.GetTimer().TimerTick += OnBombTimerTick;
        }

        protected override void ResetAndStart()
        {
            base.ResetAndStart();
            if (!Bomb.IsSolved() && SceneManager.Instance.GameplayState.Bomb.IsSolved())
            {
                timer.Reset();
                timer.StartTimer();
                DarkTonic.MasterAudio.MasterAudio.PlaySound3DFollowTransformAndForget("needy_activated", transform, 1f, null, 0f, null);
                ChangeState(NeedyStateEnum.Running);
            }
        }
    }
}
