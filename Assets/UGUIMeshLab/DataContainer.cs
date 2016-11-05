﻿using UnityEngine;

/// <summary>
/// KR: 주의 : inGame 모듈에선 isFold를 쓰면 안됨 쓸일도 없겠지만...
/// EN: Note: you should not use isFold in the inGame module.
/// </summary>
[System.Serializable]
public class LinePoint
{

    public Vector2 point;
    public bool isNextCurve = false;
    public Vector2 nextCurveOffset;
    public bool isPrevCurve = false;
    public Vector2 prevCurveOffset;

    // KR: todo(solved) : CurvePoint가 Offset값으로 넣엇는데 지금은 좌표로 적용되어 있음.
    // EN: todo(solved) : CurvePoint has an offset value and is now applied as a coordinate.
    public Vector2 nextCurvePoint { get { return nextCurveOffset + point; } set{ nextCurveOffset = value - point; } }
    public Vector2 prvCurvePoint {  get { return prevCurveOffset + point; } set { prevCurveOffset = value - point; } }

#if UNITY_EDITOR
    /// <summary>
    /// KR: 이 값은 에디팅에만 필요하고 게임에는 필요 없지만 방법이 없어서 그냥 넣어둠...
    /// EN: This value is only needed for editing and not needed for games, but there is no way to put it
    /// </summary>
    public bool isFold = false;
#endif

    /// <summary>
    /// Constructor for LinePoint
    /// </summary>
    public LinePoint(Vector2 point)
    {
        this.point = point;
    }

    /// <summary>
    /// Constructor for LinePoint
    /// </summary>
    public LinePoint(Vector2 point, Vector2 nextCurveOffset, Vector2 prevCurveOffset)
    {
        this.point = point;
        if (nextCurveOffset != Vector2.zero)
        {
            isNextCurve = true;
            this.nextCurveOffset = nextCurveOffset;
        }
        if (prevCurveOffset != Vector2.zero)
        {
            isPrevCurve = true;
            this.prevCurveOffset = prevCurveOffset;
        }
    }
}

