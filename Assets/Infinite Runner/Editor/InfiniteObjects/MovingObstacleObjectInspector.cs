using UnityEngine;
using UnityEditor;

/*
 * Custom editor insepectors don't support inheritance.. get around that by subclassing
 */
[CustomEditor(typeof(MovingObstacleObject))]
public class MovingObstacleObjectInspector : CollidableObjectInspector
{
    // Intentionally left blank, use the parent class
}