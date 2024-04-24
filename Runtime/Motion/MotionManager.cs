﻿using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public class MotionManager : ISystem
    {
        private readonly ListEx<MotionBase> MotionList_ = new ListEx<MotionBase>();

        public MotionManager()
        {
            MotionList_.Clear();
        }

        public void Dispose()
        {
            MotionList_.Clear();
        }

        public void Tick(float deltaTime)
        {
            MotionList_.Foreach((entity, time) =>
            {
                if (entity.Master == null)
                {
                    MotionList_.Remove(entity);
                }
                else if (entity.IsEnd)
                {
                    entity.Exit();
                    MotionList_.Remove(entity);
                }
                else
                {
                    entity.Tick(time);
                }
            }, deltaTime);
        }

        public MotionBase Execute(Transform master, MotionBase motion)
        {
            if (master == null || motion == null)
            {
                return null;
            }

            motion.Master = master;
            motion.Enter();
            MotionList_.Add(motion);
            return motion;
        }

        public List<MotionBase> GetMotion(Transform master)
        {
            var result = new List<MotionBase>();

            foreach (var motion in MotionList_)
            {
                if (!motion.IsEnd && motion.Master == master)
                {
                    result.Add(motion);
                }
            }

            return result;
        }

        public void Abandon(MotionBase motion)
        {
            motion?.Stop();
        }

        public void Abandon(Transform master)
        {
            if (master == null)
            {
                return;
            }

            var motionList = GetMotion(master);
            foreach (var motion in motionList)
            {
                Abandon(motion);
            }
        }

        // public static MotionBase ExecuteMotion(this Transform master, MotionBase motion)
        // {
        //     return Execute(master, motion);
        // }
        //
        // public static void AbandonMotion(this Transform master)
        // {
        //     Abandon(master);
        // }
        //
        // public static bool IsExecute(Transform master)
        // {
        //     return GetMotion(master).Count > 0;
        // }
        //
        // public static bool HasExecuteMotion(this Transform master)
        // {
        //     return IsExecute(master);
        // }
    }
}