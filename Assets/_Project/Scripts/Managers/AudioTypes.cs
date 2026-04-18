using UnityEngine;

public enum AudioEventId
{
    None = 0,

    PlayerAttack,
    PlayerHit,
    PlayerWhiff,
    PlayerDash,
    PlayerParryStart,
    PlayerParry,
    PlayerPerfectParry,
    PlayerDeflect,
    PlayerParryFail,
    PlayerCounter,
    PlayerDamageTaken,
    PlayerDeath,
    PlayerFinisher,
    PlayerHeal,

    EnemyHurt,
    EnemyDeath,
    EnemyStun,
    EnemyAttack,
    EnemyBossPhaseTransition,
    EnemyBossBulletBurst,

    ProjectileLaunch,
    ProjectileDeflect,
    ProjectileHit,

    MechanicBlackHoleStart,
    MechanicBlackHoleLoop,
    MechanicBlackHoleEnd,
    MechanicBurst,
    MechanicFlowMark,
    MechanicSnapback,

    UIUnlock,
    UIFail,
}
