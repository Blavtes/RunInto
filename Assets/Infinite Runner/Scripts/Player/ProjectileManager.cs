using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// The projectile manager acts as an object pool for the player projectiles, as well as determining if the player can actually fire a projectile.
public class ProjectileManager : MonoBehaviour
{
    public ProjectileObject projectilePrefab; // the prefab of the projectile
    public Vector3 spawnPositionOffset; // the offset from the spawnTransform that the projectile should spawn
    public float reloadTime; // the amount of time that it takes to reload
    public float fireDelay; // the delay before the projectile is actually spawned

    private float fireTime;
    private Transform playerTransform;
    private List<ProjectileObject> pool;
    private int poolIndex;
    private CoroutineData fireData;

	public void Start()
    {
        playerTransform = PlayerController.instance.transform;
        pool = new List<ProjectileObject>();
        poolIndex = 0;
        fireTime = -reloadTime;
        fireData = new CoroutineData();
	}

    public bool canFire()
    {
        return fireTime + reloadTime < Time.time;
    }

    public void fire()
    {
        if (canFire()) {
            fireTime = Time.time;

            GameManager.instance.onPauseGame += gamePaused;
            fireData.duration = fireDelay;
            StartCoroutine("doFire");
        }
    }

    public IEnumerator doFire()
    {
        fireData.startTime = Time.time;
        yield return new WaitForSeconds(fireData.duration);

        ProjectileObject projectile = projectileFromPool();
        
        // rotate to the correct direction
        Vector3 eulerAngles = projectilePrefab.transform.eulerAngles;
        eulerAngles.y += playerTransform.eulerAngles.y;
        Quaternion rotation = Quaternion.identity;
        rotation.eulerAngles = eulerAngles;
        projectile.fire(playerTransform.position + playerTransform.rotation * spawnPositionOffset, rotation, playerTransform.forward);
        GameManager.instance.onPauseGame -= gamePaused;
    }

    private ProjectileObject projectileFromPool()
    {
        ProjectileObject obj = null;

        // keep a start index to prevent the constant pushing and popping from the list
        if (pool.Count > 0 && pool[poolIndex].isActive() == false) {
            obj = pool[poolIndex];
            poolIndex = (poolIndex + 1) % pool.Count;
            return obj;
        }

        // No inactive objects, need to instantiate a new one
        obj = (GameObject.Instantiate(projectilePrefab.gameObject) as GameObject).GetComponent<ProjectileObject>();

        obj.init();
        obj.setStartParent(playerTransform);

        pool.Insert(poolIndex, obj);
        poolIndex = (poolIndex + 1) % pool.Count;

        return obj;
    }

    public void transitionHeight(float amount)
    {
        for (int i = 0; i < pool.Count; ++i) {
            if (pool[i].isActive()) {
                pool[i].transitionHeight(amount);
            }
        }
    }

    public void reset()
    {
        if (fireData != null)
            fireData.duration = 0;
    }

    private void gamePaused(bool paused)
    {
        if (paused) {
            StopCoroutine("doFire");
            fireData.calcuateNewDuration();
        } else {
            StartCoroutine("doFire");
        }
    }
}
