using UnityEngine;

public static class CombatPlane
{
    public const float FixedY = 0.1f;

    public static Vector3 WithFixedY(Vector3 position)
    {
        position.y = FixedY;
        return position;
    }

    public static void ClampTransform(Transform target)
    {
        target.position = WithFixedY(target.position);
    }

    public static Vector3 ProjectDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.zero;
    }

    public static Vector3 Direction(Vector3 from, Vector3 to)
    {
        return ProjectDirection(WithFixedY(to) - WithFixedY(from));
    }

    public static float DistanceSqr(Vector3 from, Vector3 to)
    {
        Vector3 offset = WithFixedY(to) - WithFixedY(from);
        offset.y = 0f;
        return offset.sqrMagnitude;
    }

    public static void ClampVelocity(Rigidbody body)
    {
        if (body == null)
        {
            return;
        }

        // Rigidbody도 Y 속도를 제거해 X/Z 평면 이동만 허용한다.
        Vector3 velocity = body.linearVelocity;
        velocity.y = 0f;
        body.linearVelocity = velocity;
    }
}
