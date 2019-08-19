using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct Rectangle
{
    public float x, y, w, h;
    public Rectangle(float4 rect)
    {
        x = rect.x;
        y = rect.y;
        w = rect.z;
        h = rect.w;
    }

    public Rectangle(float x, float y, float w, float h)
    {
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }

    public bool intersects(Rectangle other)
    {
        return (x + w > other.x && other.x + other.w > x)
        && (y + h > other.y && other.y + other.h > y);
    }

    public bool contains(float2 point)
    {
        return ((point.x < x + w && point.x > x - w) && (point.y < y + h && point.y > y - h));
    }
}

public struct QuadTree
{
    Rectangle boundary;

    const int nChildren = 4;
    const int northWest = 0;
    const int northEast = 1;
    const int southWest = 2;
    const int southEast = 3;
    const int CAPACITY = 4;

    bool divided;

    float2 p0;
    float2 p1;
    float2 p2;
    float2 p3;
    int index;


    [NativeFixedLength(4)] QuadTree[] children;

    public QuadTree(Rectangle bounds)
    {
        boundary = bounds;
        children = new QuadTree[nChildren];
        divided = false;
        index = 0;
        p0 = float2.zero;
        p1 = float2.zero;
        p2 = float2.zero;
        p3 = float2.zero;

    }


    public bool TryInsert(float2 p)
    {
        if (this.boundary.contains(p))
        {
            if (index < CAPACITY)
            {
                if (index == 0)
                {
                    p0 = p;
                }
                if (index == 1)
                {
                    p1 = p;
                }
                if (index == 2)
                {
                    p2 = p;
                }
                if (index == 3)
                {
                    p3 = p;
                }
                index++;
                return true;
            }
            else
            {
                if (!divided) Subdivide();
            }

            for (int i = 0; i < nChildren; i++)
            {
                if (children[i].TryInsert(p))
                    return true;
            }
        }
        return false;
    }

    void Subdivide()
    {
        divided = true;
        var b = this.boundary;
        var h = b.h * 0.5f;
        var w = b.w * 0.5f;
        Rectangle[] rectangles = new Rectangle[4];
        rectangles[northWest] = new Rectangle(b.x - w, b.y + h, w, h);
        rectangles[northEast] = new Rectangle(b.x + w, b.y + h, w, h);
        rectangles[southWest] = new Rectangle(b.x - w, b.y - h, w, h);
        rectangles[southEast] = new Rectangle(b.x + w, b.y - h, w, h);

        for (int i = 0; i < nChildren; i++)
        {
            children[i] = new QuadTree(rectangles[i]);
        }
    }

    public float2[] Query(Rectangle range)
    {
        if (!this.boundary.intersects(range))
        {
            return new float2[0];
        }
        if (index == 0)
        {
            return new float2[0];
        }

        List<float2> result = new List<float2>();

        result.Add(p0);
        result.Add(p1);
        result.Add(p2);
        result.Add(p3);
        if (divided)
        {
            for (int i = 0; i < nChildren; i++)
            {
                result.AddRange(children[i].Query(range));
            }
        }
        return result.ToArray();
    }

}