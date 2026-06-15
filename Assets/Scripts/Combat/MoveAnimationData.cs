using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Move Animation")]
public class MoveAnimationData : ScriptableObject
{
    [Header("Startup")]
    public BattleMotionType startupMotion = BattleMotionType.None;
    [Min(0f)] public float startupDuration = 0.2f;

    [Header("Attack Visual")]
    public MovePresentationType presentationType = MovePresentationType.Instant;
    public GameObject attackPrefab;
    [Min(0f)] public float attackTravelDuration = 0.35f;
    [Min(0f)] public float attackHoldDuration = 0.15f;

    [Header("Impact")]
    public GameObject impactVfxPrefab;
    [Min(0f)] public float impactVfxLifetime = 0.5f;
    [Min(0f)] public float hitReactionDuration = 0.12f;

    [Header("Screen Effects")]
    public bool screenShake = false;
    [Min(0f)] public float screenShakeDuration = 0.15f;
    [Min(0f)] public float screenShakeMagnitude = 6f;

    [Header("Audio")]
    public AudioClip sound;
    [Range(0f, 1f)] public float soundVolume = 1f;
}