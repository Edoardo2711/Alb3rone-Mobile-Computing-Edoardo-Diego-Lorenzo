/// <summary>
/// Implementata da tutti i componenti d'attacco dei mob (corpo a corpo o a distanza)
/// cosi' che MobAnimatorController possa interrogarli senza sapere quale sia.
/// </summary>
public interface IMobAttack
{
    /// <summary>True mentre e' in corso la routine d'attacco.</summary>
    bool IsAttacking { get; }

    /// <summary>Interrompe l'attacco in corso e ripristina lo stato del mob.</summary>
    void CancelAttack();
}
