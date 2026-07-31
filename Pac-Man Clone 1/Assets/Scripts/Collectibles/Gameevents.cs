using System;
using UnityEngine;

public static class GameEvents
{

    public static event Action<float> OnPowerPelletCollected; // float = frightened duration in seconds


    public static event Action OnAllPelletsCollected;

    public static event Action<int, int> OnPelletCountChanged; // (remaining, total)

    public static event Action<Vector3> OnFruitSpawned;


    public static event Action<Vector3> OnFruitDespawned;

    public static event Action<int, int> OnFruitProgressChanged; // (eaten, required)

    public static event Action OnGameWon;

    public static event Action OnPlayerDied;

    public static event Action OnRoundStarted;

    public static void RaisePowerPelletCollected(float frightenedDuration) =>
        OnPowerPelletCollected?.Invoke(frightenedDuration);

    public static void RaiseAllPelletsCollected() => OnAllPelletsCollected?.Invoke();

    public static void RaisePelletCountChanged(int remaining, int total) =>
        OnPelletCountChanged?.Invoke(remaining, total);

    public static void RaiseFruitSpawned(Vector3 position) => OnFruitSpawned?.Invoke(position);

    public static void RaiseFruitDespawned(Vector3 position) => OnFruitDespawned?.Invoke(position);

    public static void RaiseFruitProgressChanged(int eaten, int required) =>
        OnFruitProgressChanged?.Invoke(eaten, required);

    public static void RaiseGameWon() => OnGameWon?.Invoke();

    public static void RaisePlayerDied() => OnPlayerDied?.Invoke();

    public static void RaiseRoundStarted() => OnRoundStarted?.Invoke();
}