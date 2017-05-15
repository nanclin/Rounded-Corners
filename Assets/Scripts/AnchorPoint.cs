using System;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public class AnchorPoint {

    [SerializeField] public Vector2 Position;
    [SerializeField] public PointType PointType;
    [SerializeField] public float SmoothStrength = 0.5f;
    [SerializeField] public string Name;
    [SerializeField] public AnchorPoint PreviousPoint;
    [SerializeField] public AnchorPoint NextPoint;
    [SerializeField] public bool MaxStrength = false;

    [SerializeField] public float Radius { get; private set; }

    [SerializeField] public Vector2 VhN { get; private set; }

    [SerializeField] public Vector2 Vh { get; private set; }

    [SerializeField] public Vector2 Normal { get; private set; }

    [SerializeField] public Vector2 Center { get { return Position + Vh; } }

    [SerializeField] public float CornerAngle { get; private set; }

    [SerializeField] public float ArcAngle { get; private set; }

    [SerializeField] public Vector2 LeftTangent { get; private set; }

    [SerializeField] public Vector2 RightTangent { get; private set; }

    [SerializeField] public Vector2 StartVector { get; private set; }

    [SerializeField] public bool IsConvex { get; private set; }

    public AnchorPoint(AnchorPoint previousPoint = null, AnchorPoint nextPoint = null) {
        PreviousPoint = previousPoint;
        NextPoint = nextPoint;
        PointType = PointType.None;
    }

    public void ResetValues() {
        PointType = PointType.None;
        SmoothStrength = 0.5f;
        Radius = 0.5f;
        VhN = Vector2.zero;
        Vh = Vector2.zero;
        CornerAngle = 0;
        ArcAngle = 0;
        LeftTangent = Vector2.zero;
        RightTangent = Vector2.zero;
        StartVector = Vector2.zero;
        Normal = Vector2.zero;
    }

    public void RecalculateCornerValues() {

        if (NextPoint == null || PreviousPoint == null) {
            ResetValues();
            return;
        }

        Vector2 A = PreviousPoint.Position;
        Vector2 B = Position;
        Vector2 C = NextPoint.Position;
        Vector2 BA = A - B;
        Vector2 BC = C - B;
        Vector2 AC = C - A;

        if (PreviousPoint != null && NextPoint != null) {
            Normal = -((BA.normalized + BC.normalized) * 0.5f).normalized;
        } else if (PreviousPoint == null && NextPoint != null) {
            Normal = Utils.NormalL(BC);
        } else if (PreviousPoint != null && NextPoint == null) {
            Normal = Utils.NormalL(BA);
        } else {
            Normal = Vector2.zero;
        }


        IsConvex = Vector2.Dot(Normal, Utils.NormalR(AC)) < 0;
        StartVector = IsConvex ? Utils.NormalL(BC) : Utils.NormalL(BA);
//        StartVector = IsConvex ? Utils.NormalL(BC) : -VhN;
//        StartVector = Utils.NormalL(BC);

        CornerAngle = Vector2.Angle(BA, BC);
        ArcAngle = Vector3.Angle(Utils.NormalR(BA), Utils.NormalL(BC));
        float halfArcAngleRad = CornerAngle * 0.5f * Mathf.Deg2Rad;
        //        float side2Radius = (float) Math.Tan(halfArcAngleRad);
        float side2Strength = 1 / (float) Math.Cos(halfArcAngleRad);

        float maxStrengthInner = float.MaxValue;
        {
            float shorterSide = Mathf.Min(BA.magnitude, BC.magnitude);
            maxStrengthInner = shorterSide * side2Strength;
        }

        float maxStrengthOuter = float.MaxValue;
        {
            float maxTLengthLeft = float.MaxValue;
            float maxTLengthRight = float.MaxValue;

            if (PreviousPoint != null) {
                maxTLengthLeft = BA.magnitude - PreviousPoint.RightTangent.magnitude;
            }

            if (NextPoint != null) {
                maxTLengthRight = BC.magnitude - NextPoint.LeftTangent.magnitude;
            }

            float shorterTanget = Mathf.Min(maxTLengthLeft, maxTLengthRight);
            maxStrengthOuter = shorterTanget * side2Strength;
        }

        float maxStrength = Mathf.Min(maxStrengthInner, maxStrengthOuter);
        SmoothStrength = MaxStrength ? float.MaxValue : SmoothStrength;
        SmoothStrength = Mathf.Clamp(SmoothStrength, 0, maxStrength);

        // sin(fi) = r/h => h = r/sin(fi)
        //        float hypotenuse = Radius / Mathf.Sin(halfArcAngleRad);
        Radius = Mathf.Sin(halfArcAngleRad) * SmoothStrength;

        VhN = (BA.normalized + BC.normalized).normalized;
        Vh = VhN * SmoothStrength;

        LeftTangent = (Vector2) Vector3.Project(Vh, BA);
        RightTangent = (Vector2) Vector3.Project(Vh, BC);
    }
}
