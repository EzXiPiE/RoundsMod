namespace RoundsMod.Utils
{
    public interface IKojnerAbility
    {
        string AbilityName { get; }
        float CooldownRemaining { get; }
        void TryActivateAbility();
    }
}
