using System;
using UnityEngine;

namespace Multiplayer.Lobby
{
    /// <summary>
    /// GUID-based player identification without UGS Authentication.
    /// Combines the responsibilities of Boss Room's ProfileManager and ClientPrefs.
    /// </summary>
    public class PlayerIdentity
    {
        const string k_GuidKey = "lobby_player_guid";
        const string k_ProfilesKey = "lobby_available_profiles";
        const string k_ProfileCommandLineArg = "-AuthProfile";

        string m_Profile;
        string m_Guid;

        public event Action OnProfileChanged;

        public string Profile
        {
            get
            {
                if (m_Profile == null)
                {
                    m_Profile = ResolveProfile();
                }
                return m_Profile;
            }
            set
            {
                m_Profile = value;
                m_Guid = null; // Reset GUID when profile changes
                OnProfileChanged?.Invoke();
            }
        }

        /// <summary>
        /// Returns a persistent GUID for this player + profile combination.
        /// Creates one if it doesn't exist yet.
        /// </summary>
        public string GetOrCreateGuid()
        {
            if (m_Guid != null) return m_Guid;

            var key = string.IsNullOrEmpty(Profile) ? k_GuidKey : $"{k_GuidKey}_{Profile}";
            m_Guid = PlayerPrefs.GetString(key, string.Empty);

            if (string.IsNullOrEmpty(m_Guid))
            {
                m_Guid = Guid.NewGuid().ToString();
                PlayerPrefs.SetString(key, m_Guid);
                PlayerPrefs.Save();
            }

            return m_Guid;
        }

        /// <summary>
        /// Gets the player ID, combining GUID with profile for uniqueness.
        /// </summary>
        public string GetPlayerId()
        {
            return GetOrCreateGuid() + Profile;
        }

        static string ResolveProfile()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] == k_ProfileCommandLineArg && i + 1 < arguments.Length)
                {
                    return arguments[i + 1];
                }
            }

#if UNITY_EDITOR
            // In Editor, generate a unique profile from the project path for MPPM support
            var hashedBytes = System.Security.Cryptography.MD5.Create()
                .ComputeHash(System.Text.Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes).ToString("N").Substring(0, 30);
#else
            return "";
#endif
        }
    }
}
