using UnityEngine;
using System.Collections;

public class HitParticle : MonoBehaviour, IRuntimePoolable
{
    public float lifetime = 0.2f;
    private DamagePopupManager owner;
    private Coroutine lifetimeRoutine;

    private void Start()
    {
        if (owner == null)
            Play(null, transform.position);
    }

    public void Play(DamagePopupManager particleOwner, Vector3 position)
    {
        owner = particleOwner;
        transform.position = position;

        if (lifetimeRoutine != null)
            StopCoroutine(lifetimeRoutine);

        float randomAngle = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0, 0, randomAngle);
        
        float randomScale = Random.Range(0.8f, 1.3f);
        transform.localScale = Vector3.one * randomScale;

        lifetimeRoutine = StartCoroutine(LifetimeRoutine());
    }

    public void OnSpawnedFromPool()
    {
    }

    public void OnReturnedToPool()
    {
        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }

        owner = null;
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, lifetime));
        lifetimeRoutine = null;

        if (owner != null)
            owner.Recycle(this);
        else
            Destroy(gameObject);
    }
}
