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

        /// <summary>현재 활성 루틴 중 가장 최근 것을 다음 yield까지 진행하고, yield된 값을 double로 반환.</summary>
        public double PumpToNextYield()
        {
            if (m_Running.Count == 0) return double.NaN;
            var routine = m_Running[m_Running.Count - 1];
            if (!routine.MoveNext())
            {
                m_Running.Remove(routine);
                return double.NaN;
            }
            return routine.Current switch
            {
                double d => d,
                float f  => f,
                int i    => i,
                _        => 0.0
            };
        }

        /// <summary>현재 활성 루틴 중 가장 최근 것을 끝까지 진행한다.</summary>
        public void RunRoutineToCompletion()
        {
            if (m_Running.Count == 0) return;
            var routine = m_Running[m_Running.Count - 1];
            while (routine.MoveNext()) { }
            m_Running.Remove(routine);
        }
    }
}
