using UnityEngine;
using System.Collections;

/*
 * A small class for common functions. Prefixed with InfiniteRunnerStarterPack to avoid naming collisions, namespaces don't work well in Unity 3
 */
public class InfiniteRunnerStarterPackUtility : MonoBehaviour
{
#if !UNITY_3_5
    public static void ActiveRecursively(Transform obj, bool active)
    {
        foreach (Transform child in obj) {
            InfiniteRunnerStarterPackUtility.ActiveRecursively(child, active);
        }
        obj.gameObject.SetActive(active);
    }
#endif

    public static Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        // quadratic bezier curve formula: p0(1-t)^2+2p1t(1-t)+p2t^2
        float u = 1 - t;
        return ((u * u) * p0) + (2 * t * u * p1) + (t * t * p2);
    }
}
