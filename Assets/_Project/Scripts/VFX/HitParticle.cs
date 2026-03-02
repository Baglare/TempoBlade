using UnityEngine;
using System.Collections;

public class HitParticle : MonoBehaviour
{
    public float lifetime = 0.2f;

    private void Start()
    {
        // Basit bir donus ve rastgele boyut ile etkiyi arttir
        float randomAngle = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0, 0, randomAngle);
        
        float randomScale = Random.Range(0.8f, 1.3f);
        transform.localScale = Vector3.one * randomScale;

        // Omru dolunca kendini yok et
        Destroy(gameObject, lifetime);
    }
}
