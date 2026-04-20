using System.Collections;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeCoroutineRunner : ICoroutineRunner
    {
        readonly List<IEnumerator> m_Running = new();

        public object Start(IEnumerator routine)
        {
            m_Running.Add(routine);
            return routine;
        }

        public void Stop(object handle)
        {
            if (handle is IEnumerator e)
                m_Running.Remove(e);
        }

        /// <summary>
        /// 모든 실행 중인 루틴에 한 스텝(MoveNext) 진행. 완료된 루틴은 목록에서 제거.
        /// </summary>
        public void AdvanceAll()
        {
            for (var i = m_Running.Count - 1; i >= 0; i--)
            {
                if (!m_Running[i].MoveNext())
                    m_Running.RemoveAt(i);
            }
        }

        public int RunningCount => m_Running.Count;
    }
}
