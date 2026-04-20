using System.Collections;

namespace Multiplayer.Lobby.Abstractions
{
    public interface ICoroutineRunner
    {
        object Start(IEnumerator routine);
        void Stop(object handle);
    }
}
