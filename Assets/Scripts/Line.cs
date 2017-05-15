using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Assertions;

public enum PointType {
    None,
    RoundedCorner,
}

[Serializable]
public class Line : MonoBehaviour {

    [SerializeField] public bool ClosedLine;
    [SerializeField] private List<AnchorPoint> InternalAnchorPoints = new List<AnchorPoint>();

    public List<AnchorPoint> AnchorPoints { get { return InternalAnchorPoints; } }

#region Points data handling

    public AnchorPoint GetNextPoint(int index) {

        //        Assert.IsTrue(index >= 0 && index < AnchorPoints.Count, string.Format("Out of range! index={0}, AnchorPoints.Count={1}", index, AnchorPoints.Count));
        if (index < 0 || index >= AnchorPoints.Count)
            return null;

        AnchorPoint nextPoint = null;

        if (index < AnchorPoints.Count - 1) {
            nextPoint = AnchorPoints[index + 1];
        } else if (ClosedLine) {
            nextPoint = AnchorPoints[0];
        }

        return nextPoint;
    }

    public AnchorPoint GetPreviousPoint(int index) {

        //        Assert.IsTrue(index >= 0 && index < AnchorPoints.Count, string.Format("Out of range! index={0}, AnchorPoints.Count={1}", index, AnchorPoints.Count));
        if (index < 0 || index >= AnchorPoints.Count)
            return null;

        AnchorPoint previousPoint = null;

        if (index > 0) {
            previousPoint = AnchorPoints[index - 1];
        } else if (ClosedLine) {
            previousPoint = AnchorPoints[AnchorPoints.Count - 1];
        }

        return previousPoint;
    }

    public void AddPointAtTheEnd(Vector2 position) {

        int pointsCount = AnchorPoints.Count;
        AnchorPoint previousPoint = GetPreviousPoint(pointsCount - 1);

        AnchorPoint point = new AnchorPoint(previousPoint, null);
        point.Position = position;
        point.Name = "Point " + pointsCount;

        if (previousPoint != null)
            previousPoint.NextPoint = point;

        AnchorPoints.Add(point);
    }

    // TODO: not used and tested yet
    public void InsertPointAtIndex(int index, Vector2 position) {
        
        AnchorPoint previousPoint = GetPreviousPoint(index);
        AnchorPoint nextPoint = GetNextPoint(index);

        AnchorPoint point = new AnchorPoint(previousPoint, nextPoint);

        point.Name = "Point " + index;
        point.Position = position;

        if (previousPoint != null) previousPoint.NextPoint = point;
        if (nextPoint != null) nextPoint.PreviousPoint = point;

        AnchorPoints.Add(point);
    }

    public void RemovePoint(int index) {

        if (AnchorPoints.Count == 0)
            return;

        if (AnchorPoints.Count == 1) {
            AnchorPoints.Clear();
            return;
        }

        AnchorPoint previousPoint = GetPreviousPoint(index);
        AnchorPoint nextPoint = GetNextPoint(index);

        if (AnchorPoints.Count == 2) {
            nextPoint.PreviousPoint = null;
            nextPoint.NextPoint = null;
            nextPoint.Name = "point 0";
            AnchorPoints.RemoveAt(index);
            nextPoint.RecalculateCornerValues();
            return;
        }

        if (previousPoint != null) {
            previousPoint.NextPoint = nextPoint;
        }

        if (nextPoint != null) {
            nextPoint.PreviousPoint = previousPoint;
            nextPoint.Name = "point " + index;
        }

        AnchorPoints.RemoveAt(index);

        for (int i = index; i < AnchorPoints.Count; i++) {
            AnchorPoints[i].Name = "point " + i;
        }

        if (nextPoint != null) nextPoint.RecalculateCornerValues();
        if (previousPoint != null) previousPoint.RecalculateCornerValues();
    }

#endregion

    public void ReasignNeighbourPoints() {
        int pointsCount = AnchorPoints.Count;
        for (int i = 0; i < pointsCount; i++) {

            AnchorPoints[i].Name = "point " + i;

            AnchorPoints[i].PreviousPoint = null;
            AnchorPoints[i].NextPoint = null;

            int indexNext = (i + 1) % pointsCount;
            int indexPrevious = (i - 1 < 0) ? pointsCount - 1 : i - 1;
            AnchorPoint point = AnchorPoints[i];
            AnchorPoint pointNext = AnchorPoints[indexNext];
            AnchorPoint pointPrevious = AnchorPoints[indexPrevious];

            if (!ClosedLine) {
                if (i == 0) {
                    point.ResetValues();
                    point.PreviousPoint = null;
                }
                if (i == pointsCount - 1) {
                    point.ResetValues();
                    point.NextPoint = null;
                }
            }
//            AnchorPoints[i] = point;
            if (i == 0 && ClosedLine || i != 0)
                point.PreviousPoint = pointPrevious;
            if (i == pointsCount - 1 && ClosedLine || i != pointsCount - 1)
                point.NextPoint = pointNext;
        }
    }

#region Get Lengths

    public float GetLineLength() {
        float length = 0;

        int closeLoopAddition = ClosedLine ? 1 : 0;

        for (int i = 0; i < AnchorPoints.Count - 1 + closeLoopAddition; i++) {
            AnchorPoint p0 = AnchorPoints[i];
            AnchorPoint p1 = AnchorPoints[(i + 1) % AnchorPoints.Count];
            length += GetSegmentLength(p0, p1);
        }
        return length;
    }

    private float GetSegmentLength(AnchorPoint p0, AnchorPoint p1) {
        if (p0.PointType == PointType.None && p1.PointType == PointType.None) {
            return (p0.Position - p1.Position).magnitude;
        } else if (p0.PointType == PointType.RoundedCorner || p1.PointType == PointType.RoundedCorner) {
            return GetRoundedCornerLength(p0, p1);
        }
        return -1;
    }

    private float GetRoundedCornerLength(AnchorPoint p0, AnchorPoint p1) {
        float section1Length = p0.ArcAngle * Mathf.Deg2Rad * p0.Radius * 0.5f;
        float section2Length = ((p1.Position + p1.LeftTangent) - (p0.Position + p0.RightTangent)).magnitude;
        float section3Length = p1.ArcAngle * Mathf.Deg2Rad * p1.Radius * 0.5f;
        return section1Length + section2Length + section3Length;
    }

#endregion

    public Vector2 GetPointOnArc(Vector2 center, Vector2 startVector, float arcAngle, float radius, float t) {
        float startAngle = Mathf.Atan2(startVector.y, startVector.x);
        //        if (startVector.x < 0)
        //            arcAngle += 180;
        arcAngle *= Mathf.Deg2Rad;
        float pointAngle = startAngle + arcAngle * t;
        float x = Mathf.Cos(pointAngle);
        float y = Mathf.Sin(pointAngle);
        return center + new Vector2(x, y) * radius;
    }

#region Normals on line

    public Ray GetPointOnLine(float t) {

        float lineLength = GetLineLength();

        Assert.IsTrue(lineLength > 0, "Line must have some length to get point on it. Probably points are on top of each other.");

        float segmentPercent = 0;
        float totalPercent = 0;

        AnchorPoint p0 = null;
        AnchorPoint p1 = null;

        int pointsCount = AnchorPoints.Count;
        if (ClosedLine) pointsCount++;

        for (int i = 0; i < pointsCount - 1; i++) {
            p0 = AnchorPoints[i];
            p1 = AnchorPoints[(i + 1) % AnchorPoints.Count];

            segmentPercent = GetSegmentLength(p0, p1) / lineLength;

            if (totalPercent + segmentPercent > t || Mathf.Approximately(totalPercent + segmentPercent, t)) {
                break;
            } else {
                totalPercent += segmentPercent;
            }
        }
        return GetSegmentNormal(p0, p1, (t - totalPercent) / segmentPercent);
    }

    private Ray GetSegmentNormal(AnchorPoint P0, AnchorPoint P1, float t) {
        if (P0.PointType == PointType.None && P1.PointType == PointType.None) {
            if (Mathf.Approximately(t, 0)) return new Ray(P0.Position, P0.Normal);
            if (Mathf.Approximately(t, 1)) return new Ray(P1.Position, P1.Normal);
            return GetPointOnLineSegment(P0.Position, P1.Position, false, t);
        } else if (P0.PointType == PointType.RoundedCorner || P1.PointType == PointType.RoundedCorner) {
            return GetPointOnRoundedCorner(P0, P1, t);
        }
        return new Ray(P0.Position, P0.Normal);
    }

    public Ray GetPointOnRoundedCorner(AnchorPoint P0, AnchorPoint P1, float t) {

        Vector2 A = P0.Position;
        Vector2 B = P1.Position;

        if (Mathf.Approximately(t, 0) && P0.PointType != PointType.RoundedCorner) {
            return new Ray(P0.Position, P0.Normal);
        }

        float section1Length = P0.ArcAngle * Mathf.Deg2Rad * P0.Radius * 0.5f;
        float section2Length = ((B + P1.LeftTangent) - (A + P0.RightTangent)).magnitude;
        float section3Length = P1.ArcAngle * Mathf.Deg2Rad * P1.Radius * 0.5f;

        float totalLength = section1Length + section2Length + section3Length;

        if (Mathf.Approximately(totalLength, 0)) {
            return new Ray(P0.Position, P0.Normal);
        }

        float section1Percent = section1Length / totalLength;
        float section2Percent = section2Length / totalLength;
        float section3Percent = section3Length / totalLength;

        float currentPercent = 0;

        if (currentPercent + section1Percent > t || Mathf.Approximately(currentPercent + section1Percent, t)) {
            t -= currentPercent;
            if (section1Percent > 0) t = t / section1Percent;
            t = P0.IsConvex ? 1 - t : t;
            Vector2 startVector = P0.IsConvex ? P0.StartVector : -P0.VhN;
            Vector2 origin = GetPointOnArc(A + P0.Vh, startVector, P0.ArcAngle * 0.5f, P0.Radius, t);
            Vector2 direction = GetPointOnArc(P0, origin);

            return new Ray(origin, direction);
        }

        currentPercent += section1Percent;

        if (currentPercent + section2Percent > t || Mathf.Approximately(currentPercent + section2Percent, t)) {
            t -= currentPercent;
            if (section2Percent > 0) t = t / section2Percent;
            //            return Vector2.Lerp(A + p0.RightTangent, B + p1.LeftTangent, t);
            return GetPointOnLineSegment(A + P0.RightTangent, B + P1.LeftTangent, P1.IsConvex, t);
        }

        currentPercent += section2Percent;

        if (currentPercent + section3Percent > t || Mathf.Approximately(currentPercent + section3Percent, t)) {
            t -= currentPercent;
            if (section3Percent > 0) t = t / section3Percent;
            t = P1.IsConvex ? 1 - t : t;
            Vector2 startVector = P1.IsConvex ? -P1.VhN : P1.StartVector;
            Vector2 origin = GetPointOnArc(B + P1.Vh, startVector, P1.ArcAngle * 0.5f, P1.Radius, t);
            Vector2 direction = GetPointOnArc(P1, origin);

            return new Ray(origin, direction);
        }

        return new Ray(P1.Position, P1.Normal);
    }

    //    public Vector2 GetNormalOnArc(AnchorPoint P, float t) {
    //
    //        Vector2 pointOnArc = GetPointOnArc(P.Center, P.StartVector, P.ArcAngle, P.Radius, t);
    //
    //        if (P.IsConvex) {
    //            return (pointOnArc - P.Center).normalized;
    //        } else {
    //            return (P.Center - pointOnArc).normalized;
    //        }
    //    }

    private Vector2 GetPointOnArc(AnchorPoint P, Vector2 pointOnArc) {
        if (P.IsConvex) {
            return (pointOnArc - P.Center).normalized;
        } else {
            return (P.Center - pointOnArc).normalized;
        }
    }

    public Ray GetPointOnLineSegment(Vector2 A, Vector2 B, bool isConvex, float t) {
        Vector2 AB = B - A;
        Vector2 origin = A + AB * t;
        //        Vector2 direction = isConvex ? LeftNormal(AB) : RightNormal(AB);
        Vector2 direction = LeftNormal(AB);
        return new Ray(origin, direction);
    }

#endregion

#region Normals

    public Vector2 LeftNormal(Vector2 p) {
        return new Vector2(-p.y, p.x).normalized;
    }

    public Vector2 RightNormal(Vector2 p) {
        return new Vector2(p.y, -p.x).normalized;
    }

#endregion
}