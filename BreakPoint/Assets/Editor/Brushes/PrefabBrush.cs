using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;
[CreateAssetMenu(fileName ="PrefabBrush", menuName = "Brushes/Prefab Brush")]
[CustomGridBrush(false, true, false, "Prefab brush")]
public class PrefabBrush : GridBrushBase
{
    public GameObject prefab;

    public int zPos = 0;

    private GameObject previousBrushTarget;
    Vector3Int previousPosition;


    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        if(brushTarget)
        {
            previousBrushTarget = brushTarget;
        }
        if(brushTarget.layer == 31)
        {
            return;
        }

        Transform erased = GetTransformInCell(gridLayout, brushTarget.transform, new Vector3Int(position.x, position.y, 0));

        if(erased)
        {
            Undo.DestroyObjectImmediate(erased.gameObject);
        }

    }


    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        var obj = GetTransformInCell(gridLayout, brushTarget.transform, new Vector3Int(position.x, position.y, zPos));
        if(obj) return;
        else if(position == previousPosition) return;


        previousPosition = position;
        if(brushTarget)
        {
            previousBrushTarget = brushTarget;
        }
        brushTarget = previousBrushTarget;

        if(brushTarget.layer == 31)
        {
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if(instance)
        {
            Undo.MoveGameObjectToScene(instance, brushTarget.scene, "Paint Prefab");
            Undo.RegisterCreatedObjectUndo(instance, "Paint prefab");
            instance.transform.SetParent(brushTarget.transform);
            instance.transform.position = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(new Vector3(position.x, position.y, zPos) + Vector3.one * 0.5f));
        }
    }

    private static Transform GetTransformInCell(GridLayout gridLayout, Transform parent, Vector3Int pos)
    {
        int childCount = parent.childCount;
        float3 min = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(pos));
        float3 max = gridLayout.LocalToWorld(gridLayout.CellToLocalInterpolated(pos + Vector3Int.one));
        Bounds bounds = new Bounds((max + min) * 0.5f, max - min);

        for(int i = 0; i < childCount; i++)
        {
            var child = parent.GetChild(i);
            if(bounds.Contains(child.position))
            {
                return child;
            }
        }

        return null;
    }



}
