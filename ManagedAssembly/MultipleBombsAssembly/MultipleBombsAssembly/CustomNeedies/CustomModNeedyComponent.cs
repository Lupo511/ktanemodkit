using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultipleBombsAssembly
{
    public class CustomModNeedyComponent : ModNeedyComponent
    {
        protected override void Awake()
        {
            Bomb.GetTimer().TimerTick = OnBombTimerTick;
            timer.OnTimerExpire = OnTimerExpired;
            timer.OnTimerWarn = OnTimerWarn;
        }

        protected override void ResetAndStart()
        {
            if (!Bomb.IsSolved())
            {
                timer.Reset();
                timer.StartTimer();
                DarkTonic.MasterAudio.MasterAudio.PlaySound3DFollowTransformAndForget("needy_activated", transform, 1f, null, 0f, null);
                ChangeState(NeedyStateEnum.Running);
            }
        }
    }
}
