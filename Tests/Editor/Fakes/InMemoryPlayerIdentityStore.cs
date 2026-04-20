using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class InMemoryPlayerIdentityStore : IPlayerIdentityStore
    {
        readonly Dictionary<string, string> m_Guids = new();
        public string Profile { get; set; } = "";

        public string GetOrCreateGuid(string profile)
        {
            var key = profile ?? "";
            if (!m_Guids.TryGetValue(key, out var g))
            {
                g = Guid.NewGuid().ToString();
                m_Guids[key] = g;
            }
            return g;
        }

        public string ResolveProfile() => Profile;
    }
}
