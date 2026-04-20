using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Adapters.Unity
{
    public sealed class PlayerPrefsPlayerIdentityStore : IPlayerIdentityStore
    {
        const string k_GuidKey = "lobby_player_guid";
        const string k_ProfileCommandLineArg = "-AuthProfile";

        public string GetOrCreateGuid(string profile)
        {
            var key = string.IsNullOrEmpty(profile) ? k_GuidKey : $"{k_GuidKey}_{profile}";
            var guid = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(guid))
            {
                guid = Guid.NewGuid().ToString();
                PlayerPrefs.SetString(key, guid);
                PlayerPrefs.Save();
            }
            return guid;
        }

        public string ResolveProfile()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
                if (args[i] == k_ProfileCommandLineArg && i + 1 < args.Length) return args[i + 1];

#if UNITY_EDITOR
            var hashed = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashed, 16);
            return new Guid(hashed).ToString("N").Substring(0, 30);
#else
            return "";
#endif
        }
    }
}
