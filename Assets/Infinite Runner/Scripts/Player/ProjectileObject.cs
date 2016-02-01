using UnityEngine;
using System.Collections;

// The projectile object is a relatively simply class which moves a projectile on each Update call after it has been fired
public class ProjectileObject : MonoBehaviour {

    public float speed;
    public float destroyDistance; // the distance at which the projectile gets destroyed without it hitting anything

    private Transform startParent;
    private float distanceTravelled;
    private Vector3 forwardDirection;
    private int playerLayer;

    private GameObject thisGameObject;
    private Transform thisTransform;

	public void init()
    {
        thisGameObject = gameObject;
        thisTransform = transform;

        playerLayer = LayerMask.NameToLayer("Player");
	}

    public bool isActive()
    {
#if UNITY_3_5
        return thisGameObject.active;
#else
		return thisGameObject.activeSelf;
#endif
    }

    public void setStartParent(Transform parent)
    {
        startParent = parent;
    }

    public void fire(Vector3 position, Quaternion rotation, Vector3 forward)
    {
        distanceTravelled = 0;
        forwardDirection = forward;
        thisTransform.position = position;
        thisTransform.rotation = rotation;
        thisTransform.parent = null;
        
#if UNITY_3_5
        thisGameObject.SetActiveRecursively(true);
#else
		thisGameObject.SetActive(true);
#endif

        GameManager.instance.onPauseGame += gamePaused;	
    }

    public void Update()
    {
        float deltaDistance = speed * Time.deltaTime;
        thisTransform.position = Vector3.MoveTowards(thisTransform.position, thisTransform.position + forwardDirection * deltaDistance, deltaDistance);

        distanceTravelled += deltaDistance;
        if (distanceTravelled > destroyDistance) {
            deactivate();
        }
    }

    public void transitionHeight(float amount)
    {
        Vector3 position = thisTransform.position;
        position.y -= amount;
        thisTransform.position = position;
    }

    public void OnTriggerEnter(Collider other)
    {
        // ignore player collisions
        if (other.gameObject.layer == playerLayer) {
            return;
        }

        // ignore tutorial triggers
        if (other.gameObject.GetComponent<TutorialTrigger>() != null) {
            return;
        }

        ObstacleObject obstacle;
        if ((obstacle = other.GetComponent<ObstacleObject>()) != null) {
            if (obstacle.isDestructible) {
                obstacle.obstacleAttacked();
            }
        }
        deactivate();
    }

    private void deactivate()
    {
        thisTransform.parent = startParent;
#if UNITY_3_5
        thisGameObject.SetActiveRecursively(false);
#else
		thisGameObject.SetActive(false);
#endif

        GameManager.instance.onPauseGame -= gamePaused;	
    }

    private void gamePaused(bool paused)
    {
        enabled = !paused;
    }
}
