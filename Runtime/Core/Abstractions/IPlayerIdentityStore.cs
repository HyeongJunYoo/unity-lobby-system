namespace Multiplayer.Lobby.Abstractions
{
    public interface IPlayerIdentityStore
    {
        /// <summary>Profile별 영구 GUID 반환. 없으면 생성해 저장 후 반환.</summary>
        string GetOrCreateGuid(string profile);

        /// <summary>현재 환경의 프로필 결정 (커맨드라인, 에디터 해시 등). 빈 문자열도 유효.</summary>
        string ResolveProfile();
    }
}
