using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.Timeline
{
    
    public class BaseTimelineMixerBehavior : PlayableBehaviour
    {
        #region Data
        // Data
        // private int curInputIndex = -1; // 记录上一个状态
        private List<int> curTrackIndexes = new List<int>();
        #endregion
        #region Public Interface

        #endregion
        //存储之前的状态，以及状态跳过
        //存储之前的状态，以及状态跳过
        private double lastTime = -1;
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            int inputCount = playable.GetInputCount();
            bool inClip = false;
            bool mixClip = false;
            var curTime  = playable.GetTime();
            int mixIndex = 0;
            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                var clip = playable.GetInput(i);
                if (clip.IsNull() || !clip.IsValid()) continue;
    #if CLIENT_CG
                var clipType = clip.GetPlayableType();
                if(!(typeof(BaseTimelineBehavior).IsAssignableFrom(clipType))) continue;
    #endif
                ScriptPlayable<BaseTimelineBehaviour> inputPlayable = (ScriptPlayable<BaseTimelineBehaviour>)clip;
                BaseTimelineBehaviour input = inputPlayable.GetBehaviour();
                if (null == input)
                    continue;
                if (inputWeight == 1) //中间态不考虑，不处理过渡态
                {
                    inClip = true;
                    if (lastTime > curTime)
                    {
                        input.OnPause(); //用于编辑用，时间回退则调用OnPause
                        if (curTrackIndexes.Contains(i)) input.OnPlay();
                    }
                }
                if (!curTrackIndexes.Contains(i))
                {
                    curTrackIndexes.Clear();
                    curTrackIndexes.Add(i);
                    input.OnPlay();
                    input.OnProcessFrame(playable, info, playerData, inputWeight, mixIndex++);
                }
                else if (inputWeight != 0)
                {
                    mixClip = true;
                    if (!curTrackIndexes.Contains(i)) curTrackIndexes.Add(i);
                    input.OnProcessFrame(playable, info, playerData, inputWeight, mixIndex++); //观合需要用 inputweight
                }
                lastTime = curTime;
                if (!inClip && !mixClip) curTrackIndexes.Clear();
            }
        }

        // 先执行 prepareFrame
        public override void PrepareFrame(Playable playable, FrameData info)
        {
            Pause(playable, false);
        }

        private void Pause(Playable playable, bool pauseAll)
        {
            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                var clip = playable.GetInput(i);
                if (clip.IsNull() || !clip.IsValid()) continue;
#if CLIENT_CG
                var clipType = clip.GetPLayableType();
                    if(!(typeof(BaseTimelineBehavior).IsAssignableFrom(clipType))) continue;
#endif
                ScriptPlayable<BaseTimelineBehaviour> inputPlayable = (ScriptPlayable<BaseTimelineBehaviour>)clip;
                BaseTimelineBehaviour input = inputPlayable.GetBehaviour();
                if (null == input)
                    continue;
                if (pauseAll || inputWeight == 0)
                {
                    if (curTrackIndexes.Contains(i)) // 商开则停止
                    {
                        curTrackIndexes.Remove(i);
                        input.OnPause();
                    }
                }
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            Pause(playable, true);
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            base.OnPlayableDestroy(playable);
        }
    }
}
