using System.Threading.Tasks;

/// <summary>
/// 구글 로그인으로 Firebase에 넘길 ID 토큰을 받아오는 공급자.
/// 구현을 교체(C 웹 플로우 ↔ B 네이티브)해도 <see cref="FirebaseAuthManager"/>와 게임 코드는 그대로 둔다.
/// </summary>
public interface IGoogleSignIn
{
    /// <summary>구글 로그인 수행 후 Google ID 토큰을 반환. 취소/실패 시 null.</summary>
    Task<string> SignInAsync();

    void SignOut();
}
