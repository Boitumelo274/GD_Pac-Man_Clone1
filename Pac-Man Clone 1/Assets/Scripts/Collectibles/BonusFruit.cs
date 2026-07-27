using UnityEngine;

public class BonusFruit : Collectible
{
    private void OnEnable()
    {
        GameEvents.RaiseFruitSpawned(transform.position);
    }

    protected override void OnCollected()
    {
        GameEvents.RaiseFruitDespawned(transform.position);
        CollectibleManager.Instance?.NotifyFruitCollected(this);
    }
}