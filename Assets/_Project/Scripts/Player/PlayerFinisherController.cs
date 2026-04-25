using UnityEngine;

public sealed class PlayerFinisherController
{
    private readonly PlayerCombat owner;
    private readonly PlayerWeaponRuntime weaponRuntime;
    private readonly FinisherTargetResolver targetResolver;
    private readonly FinisherExecutor executor;

    public bool IsExecuting { get; private set; }
    public string LastFailureReason { get; private set; } = string.Empty;

    public PlayerFinisherController(PlayerCombat owner, PlayerWeaponRuntime weaponRuntime)
    {
        this.owner = owner;
        this.weaponRuntime = weaponRuntime;
        targetResolver = new FinisherTargetResolver(owner);
        executor = new FinisherExecutor(owner, weaponRuntime);
    }

    public bool IsFinisherAvailable(out string reason)
    {
        reason = string.Empty;

        if (IsExecuting)
        {
            reason = "Finisher zaten calisiyor";
            return false;
        }

        if (owner.currentWeapon == null)
        {
            reason = "Silah yok";
            return false;
        }

        if (TempoManager.Instance == null)
        {
            reason = "TempoManager yok";
            return false;
        }

        FinisherSO finisher = weaponRuntime.GetActiveFinisher();
        if (finisher == null)
        {
            reason = "Finisher data eksik";
            return false;
        }

        if (TempoManager.Instance.CurrentTier < finisher.requiredTempoTier)
        {
            reason = $"Tempo {finisher.requiredTempoTier} gerekli";
            return false;
        }

        return true;
    }

    public bool TryExecute()
    {
        if (!IsFinisherAvailable(out string reason))
        {
            LastFailureReason = reason;
            Debug.LogWarning($"[Finisher] Calismadi: {reason}");
            return false;
        }

        FinisherSO finisher = weaponRuntime.GetActiveFinisher();
        if (finisher == null)
        {
            LastFailureReason = "Finisher data eksik";
            Debug.LogError("[Finisher] Aktif silah icin finisher data bulunamadi.");
            return false;
        }

        LastFailureReason = string.Empty;
        IsExecuting = true;

        AudioManager.Play(AudioEventId.PlayerFinisher, owner.gameObject);
        owner.NotifyFinisherSkillTriggered();

        FinisherResolutionResult resolution = targetResolver.Resolve(finisher);
        owner.StartCoroutine(executor.Execute(finisher, resolution, HandleExecutionFinished));
        return true;
    }

    private void HandleExecutionFinished()
    {
        IsExecuting = false;
    }
}
