public struct CaptureResult
{
    public bool success;
    public CaptureFailReason failureReason;
    public CreatureData capturedUVGmon;
    public bool jarConsumed;
    public float impactDistance;
    public float indicatorRadiusAtImpact;

    public static CaptureResult Fail(CaptureFailReason reason, bool jarConsumed = false)
    {
        return new CaptureResult
        {
            success = false,
            failureReason = reason,
            capturedUVGmon = null,
            jarConsumed = jarConsumed,
            impactDistance = 0f,
            indicatorRadiusAtImpact = 0f
        };
    }
}
