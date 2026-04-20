using System;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Connection
{
    public sealed class PlayerIdentity
    {
        readonly IPlayerIdentityStore m_Store;
        string m_Profile;
        string m_Guid;

        public event Action OnProfileChanged;

        public PlayerIdentity(IPlayerIdentityStore store)
        {
            m_Store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public string Profile
        {
            get
            {
                if (m_Profile == null) m_Profile = m_Store.ResolveProfile() ?? "";
                return m_Profile;
            }
            set
            {
                m_Profile = value ?? "";
                m_Guid = null;
                OnProfileChanged?.Invoke();
            }
        }

        public string GetOrCreateGuid()
        {
            if (m_Guid != null) return m_Guid;
            m_Guid = m_Store.GetOrCreateGuid(Profile);
            return m_Guid;
        }

        public string GetPlayerId() => GetOrCreateGuid() + Profile;
    }
}
