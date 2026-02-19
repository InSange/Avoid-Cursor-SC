using System.Collections.Generic;

public interface IHittable
{
    void OnHit(int damage);
}

public interface IPlayerCursorState
{
    void OnEnter();
    void OnExit();
    void OnUpdate();
}

public interface IPoolable
{
    /// <summary>풀에서 꺼내질 때 호출됩니다.</summary>
    void OnSpawn();

    /// <summary>풀로 반환될 때 호출됩니다.</summary>
    void OnDespawn();
}

/// <summary>
/// 이 개체(주로 보스)가 처치되었을 때
/// 해금되는 요소(UnlockID) 목록을 제공하는 인터페이스입니다.
/// </summary>
public interface IUnlockProvider
{
    /// <summary>
    /// 처치 시 해금되는 모든 UnlockID의 목록을 반환합니다.
    /// </summary>
    IEnumerable<UnlockID> GetUnlocksOnDefeat();
}

/// <summary>
/// 인게임 PlayerCursor(물리)에 의해 클릭될 수 있는 UI 오브젝트 인터페이스
/// </summary>
public interface IUIClickable
{
    /// <summary>
    /// 플레이어 커서가 이 UI를 클릭했을 때 호출됩니다.
    /// </summary>
    void OnCursorClick(PlayerLogicBase cursor);
}