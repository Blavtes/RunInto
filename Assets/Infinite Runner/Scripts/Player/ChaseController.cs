using UnityEngine;

/*
 * The chase controller manages the enemy chase object. The chase object will approach the character will the player is about to
 * collide with too many obstacles, and will then attack when the player hits the final obstacle
 */
public class ChaseController : MonoBehaviour
{
    public static ChaseController instance;

    // the position/rotation of the chase object at the start of the game
    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    // the local distance offset from the player when the chase object is outside of view
    public Vector3 hiddenDistance;
    // the local distance offset from the player when the chase object is in view (creeping on the player)
    public Vector3 visibleDistance;

    // the move/rotation speed of the chase object when it is changing positions/rotations
    public float moveSpeed;
    public float rotateSpeed;
    public float smoothMoveTime;
    // length of time that the chase object should appear in view at the start of the game
    public float previewDuration;
    // length of time that the chase object should appear in view after the player hit an obstacle
    public float visibleDuration;

    public ParticleSystem attackParticles;

    public string idleAnimationName = "Idle";
    public string runAnimationName = "Run";
    public string attackAnimationName = "Attack";

    private float startTime;
    private float approachTime;
    private float pauseTime;
    private int platformLayer;
    private bool gameActive;

    private PlayerController playerController;
    private Transform playerTransform;
    private Transform thisTransform;
    private Animation thisAnimation;

    public void Awake()
    {
        instance = this;
    }

	public void Start()
    {
        thisTransform = transform;
        thisAnimation = animation;
        
        thisTransform.position = spawnPosition;
        thisTransform.eulerAngles = spawnRotation;
        platformLayer = (1 << LayerMask.NameToLayer("Platform")) | (1 << LayerMask.NameToLayer("PlatformJump")) | (1 << LayerMask.NameToLayer("Floor"));

        // setup the animation wrap modes
        thisAnimation[idleAnimationName].wrapMode = WrapMode.Loop;
        thisAnimation[runAnimationName].wrapMode = WrapMode.Loop;
        thisAnimation[attackAnimationName].wrapMode = WrapMode.Once;
        thisAnimation.Play(idleAnimationName);
	}

    public void startGame()
    {
        playerController = PlayerController.instance;
        playerTransform = playerController.transform;
        startTime = Time.time;
        approachTime = -visibleDuration;
        thisAnimation.Play(runAnimationName);
        gameActive = true;
        GameManager.instance.onPauseGame += gamePaused;	
    }

    public void reset()
    {
        thisTransform.position = spawnPosition;
        thisTransform.eulerAngles = spawnRotation;
        attackParticles.Stop();
        attackParticles.Clear();
        thisAnimation.Play(idleAnimationName);
        gameActive = false;

        GameManager.instance.onPauseGame -= gamePaused;	
    }

    public void Update()
    {
        if (!gameActive)
            return;

        // at the start of the game move within the camera view so the player knows that they are being chased. Move to this same spot
        // if the player has hit too many obstacles. Also, don't move within the camera view if the player is on a sloped platform
        // since the chase object can obstruct the camera view
        if (!playerController.abovePlatform(false) && ((Time.time < approachTime + visibleDuration) || (Time.time < startTime + previewDuration) || (playerController.maxCollisions != 0 && approachTime > 0) || attackParticles.isPlaying)) {
            Vector3 relativePosition = playerTransform.TransformPoint(visibleDistance);
            if (thisTransform.position != relativePosition) {
                // use smooth damping if the chase object is close to the target position
                if (Vector3.SqrMagnitude(thisTransform.position - relativePosition) < 2) {
                    Vector3 currentVelocity = Vector3.zero;
                    thisTransform.position = Vector3.SmoothDamp(thisTransform.position, relativePosition, ref currentVelocity, smoothMoveTime);
                } else {
                    thisTransform.position = Vector3.MoveTowards(thisTransform.position, relativePosition, moveSpeed);
                }
            }
            // keep the chase character on the ground if the player is in the air
            if (playerController.inAir()) {
                // adjust the vertical position for any height changes
                RaycastHit hit;
                if (Physics.Raycast(thisTransform.position + Vector3.up, -thisTransform.up, out hit, Mathf.Infinity, platformLayer)) {
                    Vector3 targetPosition = thisTransform.position;
                    targetPosition.y = hit.point.y + visibleDistance.y;
                    thisTransform.position = targetPosition;
                }
            }
        } else {
            // stay hidden for now
            if (thisTransform.position != playerTransform.TransformPoint(hiddenDistance)) {
                thisTransform.position = Vector3.MoveTowards(thisTransform.position, playerTransform.TransformPoint(hiddenDistance), moveSpeed);
            }
        }

        Vector3 rotation = Vector3.zero;
        rotation.y = playerTransform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.identity;
        targetRotation.eulerAngles = rotation;
        if (thisTransform.rotation != targetRotation) {
            thisTransform.rotation = Quaternion.RotateTowards(thisTransform.rotation, targetRotation, rotateSpeed);
        }
    }

    public void approach()
    {
        approachTime = Time.time;
    }

    public bool isVisible()
    {
        return Time.time < approachTime + visibleDuration;
    }

    public void transitionHeight(float amount)
    {
        Vector3 position = thisTransform.position;
        position.y -= amount;
        thisTransform.position = position;
    }

    public void gameOver(GameOverType gameOverType)
    {
        // attack
        if (gameOverType == GameOverType.DuckObstacle || gameOverType == GameOverType.JumpObstacle) {
            thisAnimation.Play(attackAnimationName);
            attackParticles.Play();
        } else {
            thisAnimation.Stop();
        }
    }

    public void gamePaused(bool paused)
    {
        thisAnimation.enabled = !paused;
        if (paused) {
            pauseTime = Time.time;
            enabled = false;
        } else {
            startTime += (Time.time - pauseTime);
            approachTime += (Time.time - pauseTime);
            enabled = true;
        }
    }
}
