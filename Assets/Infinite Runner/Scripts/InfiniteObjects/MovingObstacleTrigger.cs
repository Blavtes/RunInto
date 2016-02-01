using UnityEngine;
using System.Collections;

public class MovingObstacleTrigger : MonoBehaviour
{
    public MovingObstacleObject parent;

    public void OnTriggerEnter(Collider other)
    {
        parent.OnTriggerEnter(other);
    }
}
