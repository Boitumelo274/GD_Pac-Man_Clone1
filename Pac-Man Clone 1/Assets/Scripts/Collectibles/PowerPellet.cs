using UnityEngine;


public class PowerPellet : Collectible
{
    [SerializeField] private float frightenedDuration = 8f;

    protected override void OnCollected()
    {
        GameEvents.RaisePowerPelletCollected(frightenedDuration);
        PelletRoundSpawner.Instance?.NotifyPelletCollected();
    }
}