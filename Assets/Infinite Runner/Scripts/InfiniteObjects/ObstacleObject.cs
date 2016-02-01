using UnityEngine;
using System.Collections;

/*
 * The player can only run into so many obstacle objects before it is game over.
 */
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AppearanceProbability))]
[RequireComponent(typeof(CollidableAppearanceRules))]
public class ObstacleObject : CollidableObject
{
    // Used for the player death animation. On a jump the player will flip over, while a duck the player will fall backwards
    public bool isJump;

    // True if the player is allowed to run on top of the obstacle. If the player hits the obstacle head on it will still hurt him.
    public bool canRunOnTop;

    // True if the object can be destroyed through an attack. The method obstacleAttacked will be called to play the handle the destruction
    public bool isDestructible;

    public ParticleSystem destructionParticles;
    public Animation destructionAnimation;
    public string destructionAnimationName = "Destruction";

    protected bool collideWithPlayer;
	protected int playerLayer;
    private int startLayer;
    private int platformLayer;
    private WaitForSeconds destructionDelay;

    private GameManager gameManager;
    private PlayerAnimation playerAnimation;

    public override void init()
    {
        base.init();
        objectType = ObjectType.Obstacle;
    }
	
	public override void Awake()
	{
		base.Awake();
        playerLayer = LayerMask.NameToLayer("Player");
        platformLayer = LayerMask.NameToLayer("Platform");
	}
	
	public virtual void Start()
	{
		gameManager = GameManager.instance;
        playerAnimation = PlayerController.instance.GetComponent<PlayerAnimation>();
        startLayer = gameObject.layer;

        if (destructionAnimation && isDestructible) {
            destructionAnimation[destructionAnimationName].wrapMode = WrapMode.Once;
            destructionDelay = new WaitForSeconds(0.2f);
        }

        collideWithPlayer = true;
	}

    public virtual void OnTriggerEnter(Collider other) 
	{
        if (other.gameObject.layer == playerLayer && collideWithPlayer) {
            bool collide = true;
            if (canRunOnTop) {
                if (Vector3.Dot(Vector3.up, (other.transform.position - thisTransform.position)) > 0) {
                    collide = false;
                    thisGameObject.layer = platformLayer;
                    playerAnimation.run();
                }
            }

            if (collide) {
                gameManager.obstacleCollision(this, other.ClosestPointOnBounds(thisTransform.position));
            }
		}
	}

    public void obstacleAttacked()
    {
        collideWithPlayer = false;

        if (destructionAnimation) {
            StartCoroutine(playDestructionAnimation());
        }
    }

    private IEnumerator playDestructionAnimation()
    {
        yield return destructionDelay;
        destructionAnimation.Play();
        destructionParticles.Play();
    }

    public override void collidableDeactivation()
    {
        base.collidableDeactivation();

        // reset
        collideWithPlayer = true;
        if (canRunOnTop) {
            thisGameObject.layer = startLayer;
        }
        if (destructionAnimation) {
            destructionAnimation.Rewind();
            destructionAnimation.Play();
            destructionAnimation.Sample();
            destructionAnimation.Stop();

            if (destructionParticles)
                destructionParticles.Clear();
        }
    }
}
