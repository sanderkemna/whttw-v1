
/// <summary>
/// The state of this unit, this is only as easy reference. Do not set this state and expect anything will change.
/// The leading properties to make the state happen are the enableable component tags.
/// </summary>
public enum ZombieStateType {
    Idle,
    Wander,
    Run,
    Alert,
}
