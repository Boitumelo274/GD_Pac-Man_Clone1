using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class Collectible : MonoBehaviour
{
    [SerializeField] protected string playerTag = "Player";

    private bool _collected = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_collected) return;
        if (!other.CompareTag(playerTag)) return;

        _collected = true;
        OnCollected();
        gameObject.SetActive(false);
    }

    protected abstract void OnCollected();
}