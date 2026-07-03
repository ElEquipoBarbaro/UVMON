using System.Collections;
using UnityEngine;

public class BattleAnimationPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private RectTransform projectileLayer;
    [SerializeField] private Camera battleCamera;

    [Header("Fallbacks")]
    [SerializeField] private float fallbackTravelDuration = 0.35f;

    private Vector3 cameraRestingLocalPosition;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (battleCamera != null)
            cameraRestingLocalPosition = battleCamera.transform.localPosition;
    }

    public IEnumerator PlayMoveAnimation(
        MoveAnimationData animationData,
        CreatureBattleView userView,
        CreatureBattleView targetView)
    {
        if (animationData == null || userView == null || targetView == null)
            yield break;

        Vector2 attackDirection = targetView.CurrentAnchoredPosition - userView.CurrentAnchoredPosition;
        if (attackDirection.sqrMagnitude < 0.0001f)
            attackDirection = Vector2.right;

        if (animationData.startupMotion != BattleMotionType.None)
        {
            yield return userView.PlayStartup(
                animationData.startupMotion,
                attackDirection,
                animationData.startupDuration
            );
        }

        if (animationData.sound != null && audioSource != null)
        {
            audioSource.PlayOneShot(animationData.sound, animationData.soundVolume);
        }

        yield return PlayAttackVisual(animationData, userView, targetView);

        if (animationData.screenShake)
        {
            yield return ShakeCamera(animationData.screenShakeDuration, animationData.screenShakeMagnitude);
        }

        if (animationData.impactVfxPrefab != null)
        {
            GameObject impact = InstantiateImpact(animationData.impactVfxPrefab, targetView.CurrentAnchoredPosition);
            if (impact != null)
                Destroy(impact, Mathf.Max(0.01f, animationData.impactVfxLifetime));
        }

        yield return targetView.PlayHitReaction(animationData.hitReactionDuration);
    }

    private IEnumerator PlayAttackVisual(
        MoveAnimationData animationData,
        CreatureBattleView userView,
        CreatureBattleView targetView)
    {
        if (animationData.attackPrefab == null)
        {
            yield return new WaitForSeconds(
                Mathf.Max(0f, animationData.attackTravelDuration)
            );
            yield break;
        }

        Transform parent = projectileLayer != null
            ? projectileLayer
            : userView.transform.parent;

        GameObject attackObject = Instantiate(animationData.attackPrefab, parent);

        RectTransform attackRect = attackObject.GetComponent<RectTransform>();
        Vector2 start = userView.CurrentAnchoredPosition;
        Vector2 end = targetView.CurrentAnchoredPosition;

        SetAnchoredPosition(attackRect, attackObject.transform, start);

        if (animationData.presentationType == MovePresentationType.Instant)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, animationData.attackHoldDuration));
            Destroy(attackObject);
            yield break;
        }

        float travelDuration = animationData.attackTravelDuration > 0f
            ? animationData.attackTravelDuration
            : fallbackTravelDuration;

        float elapsed = 0f;

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);

            Vector2 pos = Vector2.Lerp(start, end, t);
            SetAnchoredPosition(attackRect, attackObject.transform, pos);

            yield return null;
        }

        SetAnchoredPosition(attackRect, attackObject.transform, end);

        if (animationData.presentationType == MovePresentationType.Beam)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, animationData.attackHoldDuration));
        }

        Destroy(attackObject);
    }

    private GameObject InstantiateImpact(GameObject prefab, Vector2 anchoredPosition)
    {
        Transform parent = projectileLayer != null ? projectileLayer : transform;
        GameObject vfx = Instantiate(prefab, parent);

        RectTransform rect = vfx.GetComponent<RectTransform>();
        SetAnchoredPosition(rect, vfx.transform, anchoredPosition);

        return vfx;
    }

    private void SetAnchoredPosition(RectTransform rect, Transform fallbackTransform, Vector2 position)
    {
        if (rect != null)
        {
            rect.anchoredPosition = position;
            return;
        }

        fallbackTransform.localPosition = new Vector3(position.x, position.y, fallbackTransform.localPosition.z);
    }

    private IEnumerator ShakeCamera(float duration, float magnitude)
    {
        if (battleCamera == null || duration <= 0f)
            yield break;

        Transform camTransform = battleCamera.transform;
        Vector3 origin = camTransform.localPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            Vector2 offset = Random.insideUnitCircle * magnitude;
            camTransform.localPosition = origin + new Vector3(offset.x, offset.y, 0f);

            yield return null;
        }

        camTransform.localPosition = origin;
    }
}