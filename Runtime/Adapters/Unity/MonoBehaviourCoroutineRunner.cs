using System.Collections;
using UnityEngine;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Adapters.Unity
{
    public sealed class MonoBehaviourCoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        public object Start(IEnumerator routine)
            => StartCoroutine(Wrap(routine));

        public void Stop(object handle)
        {
            if (handle is Coroutine c) StopCoroutine(c);
        }

        // Core가 yield return double/float 하면 WaitForSeconds로 해석.
        // 기타는 Unity 기본 처리에 위임.
        static IEnumerator Wrap(IEnumerator inner)
        {
            while (inner.MoveNext())
            {
                switch (inner.Current)
                {
                    case double d: yield return new WaitForSeconds((float)d); break;
                    case float f:  yield return new WaitForSeconds(f); break;
                    default:       yield return inner.Current; break;
                }
            }
        }
    }
}
