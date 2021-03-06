﻿using UnityEngine;
using System.Collections;

using UnityEngine.UI;
using System.Collections.Generic;

public class UIMeshLine : MaskableGraphic, IMeshModifier
{
    //todo : make UIMeshLine

    public List<LinePoint> points = new List<LinePoint>();
    public float width = 10f;

    [Range(1,100)]
    public int divideCount = 10;
    public bool useGradient = false;
    public Gradient gradient;

    public bool fillLineJoint = false;
    public float fillDivideAngle = 25f;
          
    public float lineLength
    {
        get
        {
            float sum = 0f;
            for (int n = 0; n < points.Count - 1; n++)
            {
                sum += Vector2.Distance(points[n].point, points[n + 1].point);
            }
            return sum;
        }
    }
    public bool roundEdge = false;
    public int roundEdgePolygonCount = 5;
    [Range(0, 1)][Header("0일땐 안그림 1일때 전부그림")][SerializeField]
    float m_lengthRatio = 1f;
    public float lengthRatio { get{ return m_lengthRatio; }
        set
        {
            m_lengthRatio = value;
            UpdateGeometry();
        }
    }
    
    /// UI Interface
    public void ModifyMesh(VertexHelper vh)
    {
        EditMesh(vh);
    }
    public void ModifyMesh(Mesh mesh)
    {
        using (var vh = new VertexHelper(mesh))
        {
            EditMesh(vh);
            vh.FillMesh(mesh);
        }
    }
    
    /// private function
    void EditMesh(VertexHelper vh)
    {
        vh.Clear();
        UIVertex[] prvVert = null;
        for (int n  = 0; n < points.Count-1; n++)
        {
            if (CheckLength(GetLength(n)))
            {
                break;
            }
            prvVert = DrawLine(n, vh, prvVert);
           
        }
    }
    UIVertex[] DrawLine(int index, VertexHelper vh, UIVertex[] prvLineVert=null)
    {
        UIVertex[] prvVert = null;
        float ratio = GetLength(index)/lineLength;
        float ratioEnd = GetLength(index + 1)/lineLength;

        if (IsCurve(index))
        {
            float curveLength = 0f;
            float currentRatio = ratio;
            for (int n = 0; n < divideCount; n++)
            {
                Vector3 p0 = EvaluatePoint(index, 1f / divideCount * n);
                Vector3 p1 = EvaluatePoint(index, 1f / divideCount * (n + 1));
                curveLength += Vector2.Distance(p0, p1);
            }

            for (int n = 0; n < divideCount; n++)
            {
                Vector3 p0 = EvaluatePoint(index, 1f / divideCount * n);
                Vector3 p1 = EvaluatePoint(index, 1f / divideCount * (n + 1));

                Color c0 = useGradient ? gradient.Evaluate(currentRatio) : color;
                float deltaRatio = Vector2.Distance(p0, p1) / curveLength * (ratioEnd - ratio);
                currentRatio += deltaRatio;
                Color c1 = useGradient ? gradient.Evaluate(currentRatio) : color;

                ///check final
                //float length = GetLength(index + 1);
                bool isFinal = false;
                if (currentRatio > m_lengthRatio)
                {
                    currentRatio -= deltaRatio;
                    float targetlength = lineLength * m_lengthRatio;
                    Vector3 lineVector = p1 - p0;
                    p1 = p0 + lineVector.normalized * (targetlength - lineLength * currentRatio);
                    isFinal = true;
                }

                if (roundEdge && index == 0 && n == 0)
                {
                    DrawRoundEdge(vh, p0, p1, c0);
                }
                if (roundEdge && (index == points.Count - 2 && n == divideCount - 1 || isFinal))
                {
                    DrawRoundEdge(vh, p1, p0, c1);
                }

                var quad = MakeQuad(vh, p0, p1, c0, c1, prvVert);

                if (fillLineJoint && prvLineVert != null)
                {
                    FillJoint(vh, quad[0], quad[1], prvLineVert, c0);
                    prvLineVert = null;
                }

                if (isFinal)
                    break;

                if (prvVert == null) { prvVert = new UIVertex[2]; }
                prvVert[0] = quad[3];
                prvVert[1] = quad[2];
            }
        }
        else
        {
            Vector3 p0 = points[index].point;
            Vector3 p1 = points[index + 1].point;

            Color c0 = useGradient ? gradient.Evaluate(ratio) : color;
            Color c1 = useGradient ? gradient.Evaluate(ratioEnd) : color;

            //todo length check
            float length = GetLength(index + 1);
            bool isFinal = false;
            if (CheckLength(length))
            {
                float targetlength = lineLength * m_lengthRatio;
                Vector3 lineVector = p1 - p0;
                p1 = p0 + lineVector.normalized * (targetlength - GetLength(index));
                isFinal = true;
            }

            if (roundEdge && index == 0)
            {
                DrawRoundEdge(vh, p0, p1, c0);
            }
            if (roundEdge && (index == points.Count - 2 || isFinal))
            {
                DrawRoundEdge(vh, p1, p0, c1);
            }
            
            var quad = MakeQuad(vh, p0, p1, c0, c1);

            if (fillLineJoint && prvLineVert != null)
            {
                FillJoint(vh, quad[0], quad[1], prvLineVert, c0);
                prvLineVert = null;
            }

            if (prvVert == null) { prvVert = new UIVertex[2]; }
            prvVert[0] = quad[3];
            prvVert[1] = quad[2];
        }
        return prvVert;
    } 
    void FillJoint( VertexHelper vh, UIVertex vp0, UIVertex vp1, UIVertex[] prvLineVert, Color color)
    {
        Vector3 forwardWidthVector = vp1.position - vp0.position;
        Vector3 prvWidthVector = prvLineVert[1].position - prvLineVert[0].position;

        Vector3 prvVector = Vector3.Cross(prvWidthVector, new Vector3(0, 0, 1));
        
        Vector3 p0;
        Vector3 p1;
        Vector3 center = (vp0.position + vp1.position) / 2f;
                               
        if(Vector3.Dot(prvVector, forwardWidthVector) > 0)
        {
            p0 = vp1.position;
            p1 = prvLineVert[1].position;
        }
        else
        {
            p0 = vp0.position;
            p1 = prvLineVert[0].position;
        }

        Vector3 cp0 = (p0 + p1- center*2).normalized * width *0.5f + center;

        float angle = Vector3.Angle(p0 - center, p1 - center);

        int currentVert = vh.currentVertCount;
        int divideCount = (int)(angle / fillDivideAngle);
        if(divideCount == 0) { divideCount = 1; }

        float unit = 1f / divideCount;

        vh.AddVert(center, color, Vector2.zero);
        vh.AddVert(p0, color, Vector2.zero);
        for (int n = 0; n < divideCount; n++)
        {
            vh.AddVert(Curve.CalculateBezier(p0, p1, cp0, unit *(n+1)), color, Vector2.zero);
            vh.AddTriangle(currentVert, currentVert + 1 + n, currentVert + 2 + n);
        } 
    }
    /// <summary>
    /// v0          v2  
    /// ┌─────┐  ↑
    /// p0   quad   p1  width 
    /// └─────┘  ↓
    /// v1          v3
    /// 
    ///
    /// </summary>
    /// <param name="prvVert"> v0, v1 </param>
    /// <returns> {v0,v1,v2,v3}:UIVertex </returns>
    UIVertex[] MakeQuad(VertexHelper vh, Vector3 p0, Vector3 p1, Color c0, Color c1, UIVertex[] prvVert=null)
    {
        Vector3 lineVector = p1 - p0;
        Vector3 widthVector = Vector3.Cross(lineVector, new Vector3(0, 0, 1));
        widthVector.Normalize();
        UIVertex[] verts = new UIVertex[4];
        if (prvVert != null)
        {
            verts[0] = prvVert[0];
            verts[1] = prvVert[1];
        }
        else
        {
            verts[0].position = p0 + widthVector * width * 0.5f;
            verts[1].position = p0 - widthVector * width * 0.5f;
        }
        verts[0].uv0 = new Vector2(0, 0);
        verts[1].uv0 = new Vector2(1, 0);
        verts[2].position = p1 - widthVector * width * 0.5f; verts[2].uv0 = new Vector2(1, 1);
        verts[3].position = p1 + widthVector * width * 0.5f; verts[3].uv0 = new Vector2(0, 1);

        verts[0].color = c0;
        verts[1].color = c0;
        verts[2].color = c1;
        verts[3].color = c1;

        vh.AddUIVertexQuad(verts);
        return verts;
    }
    Vector2 EvaluatePoint(LinePoint p0, LinePoint p1, float t)
    {
        //t = t * t;//보정...
        if(p0.isNextCurve && !p1.isPrevCurve)
        {
            return Curve.CalculateBezier(p0.point, p1.point, p0.nextCurvePoint, t);
        }
        if(!p0.isNextCurve && p1.isPrevCurve)
        {
            return Curve.CalculateBezier(p0.point, p1.point, p1.prvCurvePoint, t);
        }
        if(p0.isNextCurve && p1.isPrevCurve)
        {
            return Curve.CalculateBezier(p0.point, p1.point, p0.nextCurvePoint, p1.prvCurvePoint, t);
        }
        //직선의 경우.
        return Vector2.Lerp(p0.point, p1.point, t);
    }
    Vector2 EvaluatePoint(int index, float t)
    {
        return EvaluatePoint(points[index], points[index + 1], t);
    }
    Vector2 GetDerivative(LinePoint p0, LinePoint p1, float t)
    {
        if(p0.isNextCurve || p1.isPrevCurve)
        {
            float oneMinusT = 1f - t;
            return
                3f * oneMinusT * oneMinusT * (p0.nextCurvePoint - p0.point) +
                6f * oneMinusT * t * (p1.prvCurvePoint - p0.nextCurvePoint) +
                3f * t * t * (p1.point - p1.prvCurvePoint);
        }
        return (p1.point - p0.point).normalized;
    }
    bool CheckLength(float currentLength)
    {
        return currentLength / lineLength > m_lengthRatio;
    }
    float GetLength(int index)
    {
        if(index <= 0)
        {
            return 0f;
        }
        float sum = 0f;
        for (int n = 0; n < index; n++)
        {
            sum += Vector2.Distance(points[n].point, points[n + 1].point);
        }
        return sum;
    }
  
    //public function
    public Vector2 GetPoint(int index, int curveIndex)
    {
        if(curveIndex >= divideCount)
        {
            throw new System.Exception("index Error index : "+ curveIndex+" maxValue : "+divideCount);
        }

        return transform.TransformPoint( EvaluatePoint(points[index], points[index + 1], 1f / divideCount * curveIndex));
    }
    public bool IsCurve(int index)
    {
        if (points.Count -1 <= index)
        {
            throw new System.Exception("인덱스가 작음 index:" + index+ " maxValue : "+(points.Count-1));
        }
        if (points[index].isNextCurve || points[index + 1].isPrevCurve)
            return true;

        return false;
    }
    /// <summary>
    /// 해당 index가 차지하고 있는 
    /// length 비율
    /// </summary>
    public void DrawRoundEdge(VertexHelper vh, Vector2 p0, Vector2 p1, Color color)
    {
        Vector2 widthVector = Vector3.Cross(p0 - p1, new Vector3(0, 0, 1));
        widthVector.Normalize();
        widthVector = widthVector * width / 2f;
        Vector2 lineVector = (p0 - p1).normalized * width / 2f;

        int count = roundEdgePolygonCount;
        int current = vh.currentVertCount;
        float angleUnit = Mathf.PI / (count-1);

        vh.AddVert(p0, color, Vector2.zero);
        vh.AddVert(p0 + widthVector, color, Vector2.zero);
        
        for (int n = 0; n< count; n++)
        {
            vh.AddVert(p0 + Mathf.Cos(angleUnit * n) * widthVector + Mathf.Sin(angleUnit * n) * lineVector, color, Vector2.zero);
            vh.AddTriangle(current, current + 1 + n, current + 2 + n);
        }
    }
}
