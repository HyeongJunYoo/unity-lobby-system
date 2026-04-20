using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeLogger : ILobbyLogger
    {
        public readonly List<string> Infos    = new();
        public readonly List<string> Warnings = new();
        public readonly List<string> Errors   = new();

        public void Info(string message)    => Infos.Add(message);
        public void Warning(string message) => Warnings.Add(message);
        public void Error(string message)   => Errors.Add(message);

        public void Clear()
        {
            Infos.Clear();
            Warnings.Clear();
            Errors.Clear();
        }
    }
}
