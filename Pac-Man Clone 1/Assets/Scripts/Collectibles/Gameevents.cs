using System;
using UnityEngine;

public static class GameEvents
{
    //Fired specifically when a power pellet is eaten.
    //Ghost AI should subscribe to this to enter "frightened" state.
    public static event Action<float> OnPowerPelletCollected; // float = frightened duration in seconds

    //Fired when every pellet (regular + power) spawned this round has been
    //collected. CollectibleManager listens for this to spawn the fruit.
    public static event Action OnAllPelletsCollected;

    //Fired when a bonus fruit becomes active at a guarded spawn point.
    //Enemy AI can subscribe to this to go "on alert" near that position.
    public static event Action<Vector3> OnFruitSpawned;

    //Fired when fruit is eaten and removed from the board.
    public static event Action<Vector3> OnFruitDespawned;

    public static event Action<int, int> OnFruitProgressChanged; //(eaten, required)

    public static event Action OnGameWon;

    public static event Action OnPlayerDied;

    public static event Action OnRoundStarted;

    public static void RaisePowerPelletCollected(float frightenedDuration) =>
        OnPowerPelletCollected?.Invoke(frightenedDuration);

    public static void RaiseAllPelletsCollected() => OnAllPelletsCollected?.Invoke();

    public static void RaiseFruitSpawned(Vector3 position) => OnFruitSpawned?.Invoke(position);

    public static void RaiseFruitDespawned(Vector3 position) => OnFruitDespawned?.Invoke(position);

    public static void RaiseFruitProgressChanged(int eaten, int required) =>
        OnFruitProgressChanged?.Invoke(eaten, required);

    public static void RaiseGameWon() => OnGameWon?.Invoke();

    public static void RaisePlayerDied() => OnPlayerDied?.Invoke();

    public static void RaiseRoundStarted() => OnRoundStarted?.Invoke();
}