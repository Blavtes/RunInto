using UnityEngine;
using System.Collections;

/*
 * The player collects coins to be able to purchase power ups
 */
public class CoinObject : CollidableObject {
	
	public float collectSpeed;
    public float rotationSpeed;
    public float rotationDelay;

    private int coinValue; // coin value with the double coin power up considered
    private int playerLayer;
    private int coinMagnetLayer;
    private bool collect;
    private Vector3 collectPoint;
    private Vector3 startLocalPosition;
    private bool canRotate;

	private GameManager gameManager;

    public override void init()
    {
        base.init();
        objectType = ObjectType.Coin;
    }
	
	public override void Awake()
	{
		base.Awake();
		playerLayer = LayerMask.NameToLayer("Player");
        coinMagnetLayer = LayerMask.NameToLayer("CoinMagnet");
        collectPoint = new Vector3(0, 1, 0);
        startLocalPosition = thisTransform.localPosition;
        collect = canRotate = false;
        enabled = rotationSpeed != 0;

        GameManager.instance.onPauseGame += gamePaused;

        if (rotationSpeed > 0) {
            StartCoroutine("rotate", rotationDelay);
        }
	}

	public void Update()
	{
        if (canRotate) {
            thisTransform.Rotate(0, rotationSpeed, 0);
        }

        if (!collect)
            return;

        if (thisTransform.localPosition != collectPoint) {
            thisTransform.localPosition = Vector3.MoveTowards(thisTransform.localPosition, collectPoint, collectSpeed);
		} else {
            PlayerController.instance.coinCollected();
            CoinGUICollection.instance.coinCollected(coinValue);
			collect = false;
            enabled = rotationSpeed != 0;
            collidableDeactivation();
            thisTransform.localPosition = startLocalPosition;
		}
	}

    public IEnumerator rotate(float delay)
    {
        if (delay > 0) {
            yield return new WaitForSeconds(delay);
        }

        canRotate = true;
    }
	
	void OnTriggerEnter(Collider other)
	{
        if ((other.gameObject.layer == playerLayer || other.gameObject.layer == coinMagnetLayer) && !collect) {
			collectCoin();
		}
	}
	
	public void collectCoin()
	{
        coinValue = GameManager.instance.coinCollected();
			
		// the coin may have been collected from far away with the coin magnet. Fly towards the player when collected
		thisTransform.parent = PlayerController.instance.transform;
		collect = true;
        enabled = true;
	}
    
    public void OnDisable()
    {
        GameManager.instance.onPauseGame -= gamePaused;
    }

    private void gamePaused(bool paused)
    {
        if (rotationSpeed > 0) {
            if (paused) {
                StopCoroutine("rotate");
            } else {
                StartCoroutine("rotate", 0);
            }
        }
    }
}
