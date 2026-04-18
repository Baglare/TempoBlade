using UnityEngine;

public class ProjectilePlayerMovementLockEffect : MonoBehaviour
{
    public float movementLockDuration = 0.55f;
    public bool cancelDashOnHit = true;

    public void Apply(GameObject target)
    {
        if (target == null || movementLockDuration <= 0f)
            return;

        PlayerController controller = target.GetComponent<PlayerController>();
        if (controller != null)
            controller.ApplyMovementLock(movementLockDuration, cancelDashOnHit);
    }
}
