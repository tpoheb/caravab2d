using System.Collections.Generic;

/// <summary>
/// Реестр занятых путей на текущий ход.
/// Создаётся заново каждый ход в TurnQueue.
///
/// Правило: один PathCellInitializer (ребро между городами)
/// в один ход может занять только один торговец.
/// Кто первый по инициативе — тот и занял.
/// </summary>
public class PathClaims
{
    private readonly HashSet<PathCellInitializer> _claimed
        = new HashSet<PathCellInitializer>();

    /// <summary>
    /// Попытка занять путь.
    /// true — путь свободен, теперь занят этим торговцем.
    /// false — путь уже занят.
    /// </summary>
    public bool TryClaim(PathCellInitializer path)
    {
        if (path == null)       return false;
        if (_claimed.Contains(path)) return false;

        _claimed.Add(path);
        return true;
    }

    public bool IsClaimed(PathCellInitializer path) =>
        path != null && _claimed.Contains(path);

    public void Clear() => _claimed.Clear();
}
