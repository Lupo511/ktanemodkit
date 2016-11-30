using DarkTonic.MasterAudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class CustomNeedyKnobComponent : NeedyKnobComponent
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
            if (!Bomb.IsSolved())
            {
                timer.Reset();
                timer.StartTimer();
                MasterAudio.PlaySound3DFollowTransformAndForget("needy_activated", transform, 1f, null, 0f, null);
                ChangeState(NeedyStateEnum.Running);
                Randomize();
            }
        }
    }
}
