using UnityEngine;
using System.Collections;
using System;

/*
 * Calculate the target position/rotation of the player every frame, as well as move the objects around the player.
 * This class also manages when the player is sliding/jumping, and calls any animations.
 * The player has a collider which only collides with the platforms/walls. All obstacles/coins/power ups have their
 * own trigger system and will call the player controller if they need to.
 */
public enum SlotPosition { Left = -1, Center, Right }
public enum AttackType { None, Fixed, Projectile }
[RequireComponent(typeof(PlayerAnimation))]
public class PlayerController : MonoBehaviour
{

    public static PlayerController instance;

    public int maxCollisions; // set to 0 to allow infinite collisions. In this case the game will end with the chase character attacks the player
    [HideInInspector]
    public DistanceValueList forwardSpeeds;
    public float horizontalSpeed;
    public float slowRotationSpeed;
    public float fastRotationSpeed;
    public float stumbleSpeedDecrement; // amount to decrease the speed when the player hits an obstacle
    public float stumbleDuration;
    public float jumpHeight;
    public float gravity;
    public float slideDuration;
    public AttackType attackType;
    public float closeAttackDistance; // the minimum distance that allows the attack to hit the target
    public float farAttackDistance; // the maximum distance that allows the attack to hit the target
    public bool restrictTurns; // if true, can only turn on turn platforms
    public bool restrictTurnsToTurnTrigger; // if true, the player will only turn when the player hits a turn trigger. restrictTurns must also be enabled
    public bool autoTurn; // automatically turn on platforms
    public bool autoJump; // automatically jump on jump platforms
    public float turnGracePeriod; // if restrictTurns is on, if the player swipes within the grace period before a turn then the character will turn
    public float simultaneousTurnPreventionTime; // the amount of time that must elapse in between two different turns
    public float powerUpSpeedIncrease;

    // Deprecated variables:
    // jumpForce is deprecated. Use jumpHeight instead
    public float jumpForce;
    // jumpDownwardForce is deprecated. Use gravity instead
    public float jumpDownwardForce;

    public ParticleSystem coinCollectionParticleSystem;
    public ParticleSystem collisionParticleSystem;
    // particles must be in the same order as the PowerUpTypes
    public ParticleSystem[] powerUpParticleSystem;
    public ParticleSystem groundCollisionParticleSystem;
    public GameObject coinMagnetTrigger;

    private float totalMoveDistance;
    private SlotPosition currentSlotPosition;
    private Quaternion targetRotation;
    private Vector3 targetPosition;
    private float targetSlotValue;

    private float minForwardSpeed;
    private float maxForwardSpeed;
    private float forwardSpeedDelta;
    private bool moveForward;

    private float jumpSpeed;
    private bool isJumping;
    // isJumping gets set to false when the player lands on a platform within OnControllerColliderHit. OnControllerColliderHit may get called even before the player does
    // the jump though (such as switching from one platform to another) so isJumpPending will be set to true when the jump is initiated and to false
    // when the player actually leaves the platform for a jump
    private bool isJumpPending;
    private bool isSliding;
    private bool isStumbling;
    private float turnRequestTime;
    private bool turnRightRequest;
    private float turnTime;
    private float jumpLandTime;
    private bool onGround;
    private bool skipFrame;
    private int prevHitHashCode;

    private int platformLayer;
    private int floorLayer;
    private int wallLayer;
    private int obstacleLayer;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 turnOffset;
    private Vector3 curveOffset;

    private PlatformObject platformObject;
    private float curveMoveDistance;
    private float curveTime;
    private int curveDistanceMapIndex;
    private bool followCurve; // may not follow the curve on a turn

    // for pausing:
    private CoroutineData slideData;
    private CoroutineData stumbleData;
    private bool pauseCollisionParticlePlaying;
    private float coinMagnetScrollSpeed;
    private bool pauseCoinParticlePlaying;
    private bool pauseGroundParticlePlaying;

    private Transform thisTransform;
    private CapsuleCollider capsuleCollider;
    private PlayerAnimation playerAnimation;
    private ProjectileManager projectileManager;
    private CameraController cameraController;
    private InfiniteObjectGenerator infiniteObjectGenerator;
    private PowerUpManager powerUpManager;
    private GameManager gameManager;

    public void Awake()
    {
        instance = this;
    }

    public void init()
    {
        // deprecated variables warnings:
        if (jumpForce != 0 && jumpHeight == 0) {
            Debug.LogError("PlayerController.jumpForce is deprecated. Use jumpHeight instead.");
            jumpHeight = jumpForce;
        }
        if (jumpDownwardForce != 0 && gravity == 0) {
            Debug.LogError("PlayerController.jumpDownwardForce is deprecated. Use gravity instead.");
            gravity = jumpDownwardForce;
        }
        // rigidbody should no longer use gravity, be kinematic, and freeze all constraints
        if (rigidbody != null) {
            if (rigidbody.useGravity) {
                Debug.LogError("The rigidbody no longer needs to use gravity. Disabling.");
                rigidbody.useGravity = false;
            }
            if (!rigidbody.isKinematic) {
                Debug.LogError("The rigidbody should be kinematic. Enabling.");
                rigidbody.isKinematic = true;
            }
            if (rigidbody.constraints != RigidbodyConstraints.FreezeAll) {
                Debug.LogError("The rigidbody should freeze all constraints. The PlayerController will take care of the physics.");
                rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        cameraController = CameraController.instance;
        infiniteObjectGenerator = InfiniteObjectGenerator.instance;
        powerUpManager = PowerUpManager.instance;
        gameManager = GameManager.instance;
        if (attackType == AttackType.Projectile) {
            projectileManager = GetComponent<ProjectileManager>();
        }

        platformLayer = 1 << LayerMask.NameToLayer("Platform");
        floorLayer = 1 << LayerMask.NameToLayer("Floor");
        wallLayer = LayerMask.NameToLayer("Wall");
        obstacleLayer = LayerMask.NameToLayer("Obstacle");

        thisTransform = transform;
        capsuleCollider = GetComponent<CapsuleCollider>();
        playerAnimation = GetComponent<PlayerAnimation>();
        playerAnimation.init();

        startPosition = thisTransform.position;
        startRotation = thisTransform.rotation;

        slideData = new CoroutineData();
        stumbleData = new CoroutineData();
        forwardSpeeds.init();
        // determine the fastest and the slowest forward speeds
        forwardSpeeds.getMinMaxValue(out minForwardSpeed, out maxForwardSpeed);
        forwardSpeedDelta = maxForwardSpeed - minForwardSpeed;
        if (forwardSpeedDelta == 0) {
            playerAnimation.setRunSpeed(1, 1);
        }

        // make sure the coin magnet trigger is deactivated
        activatePowerUp(PowerUpTypes.CoinMagnet, false);

        reset();
        enabled = false;
    }

    public void reset()
    {
        thisTransform.position = startPosition;
        thisTransform.rotation = startRotation;

        slideData.duration = 0;
        stumbleData.duration = 0;

        jumpSpeed = 0;
        isJumping = false;
        isJumpPending = false;
        isSliding = false;
        isStumbling = false;
        onGround = true;
        prevHitHashCode = -1;
        currentSlotPosition = SlotPosition.Center;
        targetSlotValue = (int)currentSlotPosition * infiniteObjectGenerator.slotDistance;
        playerAnimation.reset();
        if (projectileManager)
            projectileManager.reset();
        pauseCollisionParticlePlaying = false;
        totalMoveDistance = 0;
        turnOffset = Vector3.zero;
        curveOffset = Vector3.zero;
        turnTime = Time.time;
        jumpLandTime = Time.time;
        forwardSpeeds.reset();

        platformObject = null;
        curveTime = -1;
        curveMoveDistance = 0;
        curveDistanceMapIndex = 0;
        followCurve = false;

        targetRotation = startRotation;
        updateTargetPosition(targetRotation.eulerAngles.y);
    }

    public void startGame()
    {
        playerAnimation.run();
        enabled = true;
        gameManager.onPauseGame += gamePaused;
    }

    // There character doesn't move, all of the objects around it do. Make sure the character is in the correct position
    public void Update()
    {
        Vector3 moveDirection = Vector3.zero;
        float hitDistance = 0;
        RaycastHit hit;
        // cast a ray to see if we are over any platforms
        if (Physics.Raycast(thisTransform.position + capsuleCollider.center, -thisTransform.up, out hit, Mathf.Infinity, platformLayer)) {
            hitDistance = hit.distance;
            PlatformObject platform = null;
            // compare the has code to prevent having to look up GetComponent every frame
            if (prevHitHashCode != hit.GetHashCode()) {
                prevHitHashCode = hit.GetHashCode();
                // update the platform object
                if (((platform = hit.transform.GetComponent<PlatformObject>()) != null) || ((platform = hit.transform.parent.GetComponent<PlatformObject>()) != null)) {
                    if (platform != platformObject) {
                        platformObject = platform;
                        checkForCurvedPlatform();
                    }
                }
            }

            // we are over a platform, determine if we are on the ground of that platform
            if (hit.distance <= capsuleCollider.height / 2 + 0.0001f) {
                onGround = true;
                // we are on the ground. Get the platform object and check to see if we are on a curve
                // if we are sliding and the platform has a slope then stop sliding
                if (isSliding) {
                    if (platformObject != null && platformObject.slope != PlatformSlope.None) {
                        StopCoroutine("doSlide");
                        stopSlide(true);
                    }
                }

                // if we are jumping we either want to start jumping or land
                if (isJumping) {
                    if (isJumpPending) {
                        moveDirection.y += jumpSpeed;
                        cameraController.adjustVerticalOffset(jumpSpeed * Time.deltaTime);
                        jumpSpeed += gravity * Time.deltaTime;
                        onGround = false;
                    } else {
                        isJumping = false;
                        jumpLandTime = Time.time;
                        if (enabled)
                            playerAnimation.run();
                        groundCollisionParticleSystem.Play();
                    }
                } else {
                    // we are not jumping so our position should be the same as the hit point
                    Vector3 position = thisTransform.position;
                    position.y = hit.point.y;
                    thisTransform.position = position;
                }
                skipFrame = true;
                // a hit distance of -1 means that the platform is within distance
                hitDistance = -1;
            }
            // if we didn't hit a platform we may hit a floor
        } else if (Physics.Raycast(thisTransform.position + capsuleCollider.center, -thisTransform.up, out hit, Mathf.Infinity, floorLayer)) {
            hitDistance = hit.distance;
        }

        if (hitDistance != -1) {
            // a platform is beneith us but it is far away. If we are jumping apply the jump speed and gravity
            if (isJumping) {
                moveDirection.y += jumpSpeed;
                cameraController.adjustVerticalOffset(jumpSpeed * Time.deltaTime);
                jumpSpeed += gravity * Time.deltaTime;

                // the jump is no longer pending if we are in the air
                if (isJumpPending) {
                    isJumpPending = false;
                }
            } else if (!skipFrame) {
                // apply gravity if we are not jumping
                moveDirection.y = gravity * (powerUpManager.isPowerUpActive(PowerUpTypes.SpeedIncrease) ? 2 : 1); // the speed power up needs a little extra push
            }

            if (!skipFrame && hitDistance == 0) {
                platformObject = null;
            }
            if (skipFrame) {
                skipFrame = false;
            } else if (hitDistance != 0 && thisTransform.position.y + (moveDirection.y * Time.deltaTime) < hit.point.y) {
                // this transition should be instant so ignore Time.deltaTime
                moveDirection.y = (hit.point.y - thisTransform.position.y) / Time.deltaTime;
            }
            onGround = false;
        }

        float xStrafe = (targetPosition.x - thisTransform.position.x) * Mathf.Abs(Mathf.Cos(targetRotation.eulerAngles.y * Mathf.Deg2Rad)) / Time.deltaTime;
        float zStrafe = (targetPosition.z - thisTransform.position.z) * Mathf.Abs(Mathf.Sin(targetRotation.eulerAngles.y * Mathf.Deg2Rad)) / Time.deltaTime;
        moveDirection.x += Mathf.Clamp(xStrafe, -horizontalSpeed, horizontalSpeed);
        moveDirection.z += Mathf.Clamp(zStrafe, -horizontalSpeed, horizontalSpeed);
        thisTransform.position += moveDirection * Time.deltaTime;

        // Make sure we don't run into a wall
        if (Physics.Raycast(thisTransform.position + Vector3.up, thisTransform.forward, capsuleCollider.radius, 1 << wallLayer)) {
            gameManager.gameOver(GameOverType.Wall, true);
        }

        if (!gameManager.isGameActive()) {
            enabled = inAir(); // keep the character active for as long as they are in the air so gravity can keep pushing them down.
        }
    }

    // Move all of the objects within the LateObject to prevent jittering when the height transitions
    public void LateUpdate()
    {
        // don't move any objects if the game isn't active. The game may not be active if the character is in the air when they died
        if (!gameManager.isGameActive()) {
            return;
        }

        float forwardSpeed = forwardSpeeds.getValue(totalMoveDistance);
        if (isStumbling) {
            forwardSpeed -= stumbleSpeedDecrement;
        }
        if (powerUpManager.isPowerUpActive(PowerUpTypes.SpeedIncrease)) {
            forwardSpeed += powerUpSpeedIncrease;
        }

        // continue along the curve if over a curved platform
        if (curveTime != -1 && platformObject != null) {
            curveTime = Mathf.Clamp01(curveMoveDistance / platformObject.curveLength);
            if (curveTime < 1 && followCurve) {
                updateTargetPosition(thisTransform.eulerAngles.y);

                // compute a future curve time to determine which direction the player will be heading
                Vector3 curvePoint = getCurvePoint(curveMoveDistance, true);
                float futureMoveDistance = (curveMoveDistance + 2 * forwardSpeed * Time.deltaTime);
                Vector3 futureCurvePoint = getCurvePoint(futureMoveDistance, false);
                futureCurvePoint.y = curvePoint.y = targetPosition.y;
                Vector3 forwardDir = (futureCurvePoint - curvePoint).normalized;
                targetRotation = Quaternion.LookRotation(forwardDir);
                infiniteObjectGenerator.setMoveDirection(forwardDir);
            }
            curveMoveDistance += forwardSpeed * Time.deltaTime;
        }

        if (thisTransform.rotation != targetRotation) {
            thisTransform.rotation = Quaternion.RotateTowards(thisTransform.rotation, targetRotation,
                                        Mathf.Lerp(slowRotationSpeed, fastRotationSpeed, Mathf.Clamp01(Quaternion.Angle(thisTransform.rotation, targetRotation) / 45)));
        }

        playerAnimation.setRunSpeed(forwardSpeed, forwardSpeedDelta != 0 ? (forwardSpeed - minForwardSpeed) / (forwardSpeedDelta) : 1);
        forwardSpeed *= Time.deltaTime;
        totalMoveDistance += forwardSpeed;
        infiniteObjectGenerator.moveObjects(forwardSpeed);
    }

    public bool abovePlatform(bool aboveTurn /* if false, returns if above slope */)
    {
        if (platformObject != null) {
            if (aboveTurn) {
                return platformObject.rightTurn || platformObject.leftTurn;
            } else { // slope
                return platformObject.slope != PlatformSlope.None;
            }
        }

        return false;
    }

    // Turn left or right
    public bool turn(bool rightTurn, bool fromInputManager)
    {
        // prevent two turns from occurring really close to each other (for example, to prevent a 180 degree turn)
        if (Time.time - turnTime < simultaneousTurnPreventionTime) {
            return false;
        }

        RaycastHit hit;
        // ensure we are over the correct platform
        if (Physics.Raycast(thisTransform.position + capsuleCollider.center, -thisTransform.up, out hit, Mathf.Infinity, platformLayer)) {
            PlatformObject platform = null;
            // update the platform object
            if (((platform = hit.transform.GetComponent<PlatformObject>()) != null) || ((platform = hit.transform.parent.GetComponent<PlatformObject>()) != null)) {
                if (platform != platformObject) {
                    platformObject = platform;
                    checkForCurvedPlatform();
                }
            }
        }
        bool isAboveTurn = abovePlatform(true);

        // if we are restricting a turn, don't turn unless we are above a turn platform
        if (restrictTurns && (!isAboveTurn || restrictTurnsToTurnTrigger)) {
            if (fromInputManager) {
                turnRequestTime = Time.time;
                turnRightRequest = rightTurn;
                return false;
            }

            if (!powerUpManager.isPowerUpActive(PowerUpTypes.Invincibility) && !powerUpManager.isPowerUpActive(PowerUpTypes.SpeedIncrease) && !autoTurn) {
                // turn in the direction that the player swiped
                rightTurn = turnRightRequest;

                // don't turn if restrict turns is on and the player hasn't swipped within the grace period time or if the player isn't above a turn platform
                if (!gameManager.godMode && (Time.time - turnRequestTime > turnGracePeriod || !isAboveTurn)) {
                    return false;
                }
            }
        } else if (!fromInputManager && !autoTurn && !gameManager.godMode && (!restrictTurns || Time.time - turnRequestTime > turnGracePeriod) &&
                    !powerUpManager.isPowerUpActive(PowerUpTypes.Invincibility) && !powerUpManager.isPowerUpActive(PowerUpTypes.SpeedIncrease)) {
            return false;
        }

        turnTime = Time.time;
        Vector3 direction = platformObject.getTransform().right * (rightTurn ? 1 : -1);
        turnOffset = infiniteObjectGenerator.updateSpawnDirection(direction, platformObject.curveLength == 0, rightTurn, isAboveTurn);
        if (platformObject.curveLength > 0) {
            followCurve = true;
        } else {
            targetRotation = Quaternion.LookRotation(direction);
            curveOffset.x = (thisTransform.position.x - (startPosition.x + turnOffset.x)) * Mathf.Abs(Mathf.Sin(targetRotation.eulerAngles.y * Mathf.Deg2Rad));
            curveOffset.z = (thisTransform.position.z - (startPosition.z + turnOffset.z)) * Mathf.Abs(Mathf.Cos(targetRotation.eulerAngles.y * Mathf.Deg2Rad));
            if (isAboveTurn) {
                updateTargetPosition(targetRotation.eulerAngles.y);
            }
        }
        return true;
    }

    // There are three slots on a track. Move left or right if there is a slot available
    public void changeSlots(bool right)
    {
        SlotPosition targetSlot = (SlotPosition)Mathf.Clamp((int)currentSlotPosition + (right ? 1 : -1), (int)SlotPosition.Left, (int)SlotPosition.Right);

        changeSlots(targetSlot);
    }

    // There are three slots on a track. The accelorometer/swipes determine the slot position
    public void changeSlots(SlotPosition targetSlot)
    {
        if (targetSlot == currentSlotPosition)
            return;

        if (!inAir())
            playerAnimation.strafe((int)currentSlotPosition < (int)targetSlot);
        currentSlotPosition = targetSlot;
        targetSlotValue = (int)currentSlotPosition * infiniteObjectGenerator.slotDistance;

        updateTargetPosition(targetRotation.eulerAngles.y);
    }

    public SlotPosition getCurrentSlotPosition()
    {
        return currentSlotPosition;
    }

    // attack the object in front of the player if it can be destroyed
    public void attack()
    {
        if (attackType == AttackType.None)
            return;

        if (!inAir() && !isSliding) {
            if (attackType == AttackType.Fixed) {
                playerAnimation.attack();

                RaycastHit hit;
                if (Physics.Raycast(thisTransform.position + Vector3.up / 2, thisTransform.forward, out hit, farAttackDistance, 1 << obstacleLayer)) {
                    // the player will collide with the obstacle if they are too close
                    if (hit.distance > closeAttackDistance) {
                        ObstacleObject obstacle = hit.collider.GetComponent<ObstacleObject>();
                        if (obstacle.isDestructible) {
                            obstacle.obstacleAttacked();
                        }
                    }
                }
            } else if (projectileManager.canFire()) { // projectile
                playerAnimation.attack();
                projectileManager.fire();
            }
        }
    }

    private void updateTargetPosition(float yAngle)
    {
        if (curveTime != -1 && curveTime <= 1 && platformObject != null) {
            Vector3 curvePoint = getCurvePoint(curveMoveDistance, true);
            targetPosition.x = curvePoint.x;
            targetPosition.z = curvePoint.z;
        } else {
            targetPosition.x = startPosition.x * Mathf.Abs(Mathf.Sin(yAngle * Mathf.Deg2Rad));
            targetPosition.z = startPosition.z * Mathf.Abs(Mathf.Cos(yAngle * Mathf.Deg2Rad));
            targetPosition += (turnOffset + curveOffset);
        }
        targetPosition.x += targetSlotValue * Mathf.Cos(yAngle * Mathf.Deg2Rad);
        targetPosition.z += targetSlotValue * -Mathf.Sin(yAngle * Mathf.Deg2Rad);
    }

    private Vector3 getCurvePoint(float distance, bool updateMapIndex)
    {
        int index = curveDistanceMapIndex;
        float segmentDistance = platformObject.curveIndexDistanceMap[index];
        if (distance > segmentDistance && index < platformObject.curveIndexDistanceMap.Count - 1) {
            index++;
            if (updateMapIndex) {
                curveDistanceMapIndex = index;
            }
        }
        float time = 0;
        if (index > 0) {
            float prevDistance = platformObject.curveIndexDistanceMap[index - 1];
            time = (distance - prevDistance) / (platformObject.curveIndexDistanceMap[index] - prevDistance);
        } else {
            time = distance / platformObject.curveIndexDistanceMap[index];
        }
        time = Mathf.Clamp01(time);

        Vector3 p0, p1, p2;
        if (index == 0) {
            p0 = platformObject.controlPoints[index];
        } else {
            p0 = (platformObject.controlPoints[index] + platformObject.controlPoints[index + 1]) / 2;
        }
        p1 = platformObject.controlPoints[index + 1];
        if (index + 2 == platformObject.controlPoints.Count - 1) {
            p2 = platformObject.controlPoints[index + 2];
        } else {
            p2 = (platformObject.controlPoints[index + 1] + platformObject.controlPoints[index + 2]) / 2;
        }

        return platformObject.getTransform().TransformPoint(InfiniteRunnerStarterPackUtility.CalculateBezierPoint(p0, p1, p2, time));
    }

    public void jump(bool fromTrigger)
    {
        if (jumpHeight > 0 && !inAir() && !isSliding && !abovePlatform(false) && (fromTrigger || Time.time - jumpLandTime > 0.2f)) { // can't jump on a sloped platform
            // don't jump if coming from a trigger and auto jump, invincibility/speed increase and God mode are not activated
            if (fromTrigger && !autoJump && !powerUpManager.isPowerUpActive(PowerUpTypes.Invincibility) && !powerUpManager.isPowerUpActive(PowerUpTypes.SpeedIncrease) && !gameManager.godMode) {
                return;
            }

            jumpSpeed = jumpHeight;
            isJumping = isJumpPending = true;
            playerAnimation.jump();
        }
    }

    public bool inAir()
    {
        return !onGround;
    }

    public void slide()
    {
        if (slideDuration > 0 && !inAir() && !isSliding && !abovePlatform(false)) { // can't slide above a sloped platform
            isSliding = true;
            playerAnimation.slide();

            // adjust the collider bounds
            float height = capsuleCollider.height;
            height /= 2;
            Vector3 center = capsuleCollider.center;
            center.y = center.y - (capsuleCollider.height - height) / 2;
            capsuleCollider.height = height;
            capsuleCollider.center = center;

            slideData.duration = slideDuration;
            StartCoroutine("doSlide");
        }
    }

    // stay in the slide postion for a certain amount of time
    private IEnumerator doSlide()
    {
        slideData.startTime = Time.time;
        yield return new WaitForSeconds(slideData.duration);

        // only play the run animation if the player is still alive
        if (enabled) {
            playerAnimation.run();
            // let the run animation start
            yield return new WaitForSeconds(playerAnimation.runTransitionTime);
        }

        stopSlide(false);
    }

    private void stopSlide(bool force)
    {
        if (force)
            playerAnimation.run();

        isSliding = false;

        // adjust the collider bounds
        float height = capsuleCollider.height;
        height *= 2;
        capsuleCollider.height = height;
        Vector3 center = capsuleCollider.center;
        center.y = capsuleCollider.height / 2;
        capsuleCollider.center = center;
    }

    // the player collided with an obstacle, play some particle effects
    public void obstacleCollision(Transform obstacle, Vector3 position)
    {
        if (!enabled)
            return;

        // Make sure the particle system is active
#if UNITY_3_5
        if (!collisionParticleSystem.gameObject.active)
            collisionParticleSystem.gameObject.active = true;
#else
        if (!collisionParticleSystem.gameObject.activeSelf)
		    collisionParticleSystem.gameObject.SetActive(true);
#endif
        collisionParticleSystem.transform.position = position;
        collisionParticleSystem.transform.parent = obstacle;
        collisionParticleSystem.Clear();
        collisionParticleSystem.Play();

        // stumble
        if (stumbleDuration > 0) {
            isStumbling = true;
            stumbleData.duration = stumbleDuration;
            StartCoroutine("stumble");
        }

        // camera shake
        cameraController.shake();
    }

    private IEnumerator stumble()
    {
        stumbleData.startTime = Time.time;
        yield return new WaitForSeconds(stumbleData.duration);
        isStumbling = false;
    }

    private void checkForCurvedPlatform()
    {
        // find the closest curve point
        if (platformObject.curveLength > 0) {
            curveMoveDistance = curveTime = curveDistanceMapIndex = 0;

            // don't follow the curve initally, wait until the player has turned or hit a turn trigger.
            followCurve = !platformObject.rightTurn && !platformObject.leftTurn;
        } else if (curveTime > 0) { // done with a curved platform
            curveTime = -1;
            followCurve = false;
            turnOffset = infiniteObjectGenerator.getTurnOffset();
            Vector3 forward = platformObject.getTransform().forward;
            targetRotation = Quaternion.LookRotation(forward);
            float yAngle = targetRotation.eulerAngles.y;
            curveOffset.x = (thisTransform.position.x - (startPosition.x + turnOffset.x)) * Mathf.Abs(Mathf.Sin(yAngle * Mathf.Deg2Rad));
            curveOffset.z = (thisTransform.position.z - (startPosition.z + turnOffset.z)) * Mathf.Abs(Mathf.Cos(yAngle * Mathf.Deg2Rad));

            infiniteObjectGenerator.setMoveDirection(forward);
            updateTargetPosition(platformObject.getTransform().eulerAngles.y);
        }
    }

    public void transitionHeight(float amount)
    {
        Vector3 position = thisTransform.position;
        position.y -= amount;
        thisTransform.position = position;

        position = cameraController.transform.position;
        position.y -= amount;
        cameraController.transform.position = position;

        // adjust all of the projectiles
        if (projectileManager) {
            projectileManager.transitionHeight(amount);
        }
    }

    public void coinCollected()
    {
        coinCollectionParticleSystem.Play();
    }

    public void activatePowerUp(PowerUpTypes powerUpType, bool activate)
    {
        if (powerUpType != PowerUpTypes.None) {
            ParticleSystem particleSystem = powerUpParticleSystem[(int)powerUpType];
            if (activate) {
                particleSystem.Play();
            } else {
                particleSystem.Stop();
            }
            if (powerUpType == PowerUpTypes.CoinMagnet) {
#if UNITY_3_5
                coinMagnetTrigger.active = activate;
#else
			    coinMagnetTrigger.SetActive(activate);
#endif
            }
        }
    }

    public void gameOver(GameOverType gameOverType)
    {
        if (!isSliding && gameOverType != GameOverType.Pit)
            playerAnimation.gameOver(gameOverType);
        // ensure the player returns to their original color
        activatePowerUp(powerUpManager.getActivePowerUp(), false);
        collisionParticleSystem.transform.parent = null;
        gameManager.onPauseGame -= gamePaused;
        // let the character fall if they are still in the air
        jumpSpeed = 0;
        enabled = inAir();
    }

    // disable the script if paused to stop the objects from moving
    private void gamePaused(bool paused)
    {
        ParticleSystem particleSystem = null;
        PowerUpTypes activePowerUp = powerUpManager.getActivePowerUp();
        if (activePowerUp != PowerUpTypes.None) {
            particleSystem = powerUpParticleSystem[(int)activePowerUp];
        }

        if (paused) {
            if (coinCollectionParticleSystem.isPlaying) {
                pauseCoinParticlePlaying = true;
                coinCollectionParticleSystem.Pause();
            }
            if (collisionParticleSystem.isPlaying) {
                pauseCollisionParticlePlaying = true;
                collisionParticleSystem.Pause();
            }
            if (groundCollisionParticleSystem.isPlaying) {
                pauseGroundParticlePlaying = true;
                groundCollisionParticleSystem.Pause();
            }
            if (particleSystem != null)
                particleSystem.Pause();
        } else {
            if (pauseCoinParticlePlaying) {
                coinCollectionParticleSystem.Play();
                pauseCoinParticlePlaying = false;
            }
            if (pauseCollisionParticlePlaying) {
                collisionParticleSystem.Play();
                pauseCollisionParticlePlaying = false;
            }
            if (pauseGroundParticlePlaying) {
                groundCollisionParticleSystem.Play();
                pauseGroundParticlePlaying = false;
            }
            if (particleSystem != null)
                particleSystem.Play();
        }
        if (isSliding) {
            if (paused) {
                StopCoroutine("doSlide");
                slideData.calcuateNewDuration();
            } else {
                StartCoroutine("doSlide");
            }
        }
        if (isStumbling) {
            if (paused) {
                StopCoroutine("stumble");
                stumbleData.calcuateNewDuration();
            } else {
                StartCoroutine("stumble");
            }
        }
        enabled = !paused;
    }
}
