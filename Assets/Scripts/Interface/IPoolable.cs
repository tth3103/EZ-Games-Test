public interface IPoolable
{
    void ResetToInitialState();
    void OnSpawnedFromPool();
    void OnReturnedToPool();
}
