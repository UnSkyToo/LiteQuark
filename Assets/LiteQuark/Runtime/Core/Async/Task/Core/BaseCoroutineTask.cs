using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class BaseCoroutineTask : BaseTask
    {
        private Coroutine _coroutine;
        private readonly Stack<IEnumerator> _enumerators = new();
        private bool _isAdvancing;

        protected BaseCoroutineTask()
            : base()
        {
        }

        protected override void OnTerminated()
        {
            if ((State is TaskState.Failed or TaskState.Cancelled) && !_isAdvancing)
            {
                var coroutine = _coroutine;
                _coroutine = null;
                try
                {
                    if (coroutine != null)
                    {
                        LiteRuntime.Task.StopCoroutine(coroutine);
                    }
                }
                finally
                {
                    DisposeEnumerators();
                }
            }
        }

        protected override void OnExecute()
        {
            _enumerators.Push(ExecuteInternal());
            var coroutine = LiteRuntime.Task.StartCoroutine(Run());
            if (!IsDone)
            {
                _coroutine = coroutine;
            }
        }

        private IEnumerator Run()
        {
            try
            {
                while (!IsDone && _enumerators.Count > 0)
                {
                    var routine = _enumerators.Peek();
                    var moved = MoveNext(routine, out var current);
                    if (IsDone)
                    {
                        yield break;
                    }

                    if (!moved)
                    {
                        _enumerators.Pop();
                        DisposeEnumerator(routine);
                    }
                    else if (current is IEnumerator nested && current is not ITask)
                    {
                        _enumerators.Push(nested);
                    }
                    else
                    {
                        // 已登记的 Task 只被等待，不接管其执行或释放。
                        yield return current;
                    }
                }
            }
            finally
            {
                _coroutine = null;
                DisposeEnumerators();
            }
        }

        private bool MoveNext(IEnumerator routine, out object current)
        {
            current = null;
            _isAdvancing = true;
            try
            {
                var moved = routine.MoveNext();
                if (moved)
                {
                    current = routine.Current;
                }
                return moved;
            }
            catch (Exception ex)
            {
                if (IsDone)
                {
                    LLog.Exception(ex);
                }
                else
                {
                    Fail(ex);
                }
                return false;
            }
            finally
            {
                _isAdvancing = false;
            }
        }

        private void DisposeEnumerators()
        {
            while (_enumerators.Count > 0)
            {
                DisposeEnumerator(_enumerators.Pop());
            }
        }

        private void DisposeEnumerator(IEnumerator routine)
        {
            try
            {
                (routine as IDisposable)?.Dispose();
            }
            catch (Exception ex)
            {
                if (IsDone)
                {
                    LLog.Exception(ex);
                }
                else
                {
                    Fail(ex);
                }
            }
        }

        protected abstract IEnumerator ExecuteInternal();
    }
}
