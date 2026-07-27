using UnityEngine;

public class Pellet : Collectible
{
    protected override void OnCollected()
    {
        PelletRoundSpawner.Instance?.NotifyPelletCollected();
    }
}