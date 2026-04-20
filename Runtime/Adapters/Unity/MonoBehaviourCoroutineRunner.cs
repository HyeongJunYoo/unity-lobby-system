using System.Collections;
using UnityEngine;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Adapters.Unity
{
    /// <summary>
    /// MonoBehaviour 기반 ICoroutineRunner 구현.
    /// Start/Stop은 명시적 인터페이스 구현으로 선언해 Unity의 MonoBehaviour.Start 생명주기
    /// 콜백 이름과 충돌하지 않도록 한다.
    /// </summary>
    public sealed class MonoBehaviourCoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        object ICoroutineRunner.Start(IEnumerator routine)
            => StartCoroutine(Wrap(routine));

        void ICoroutineRunner.Stop(object handle)
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
