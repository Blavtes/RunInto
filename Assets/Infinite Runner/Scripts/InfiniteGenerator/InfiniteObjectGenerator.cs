using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ObjectLocation { Center, Left, Right, Last }

/*
 * The InfiniteObjectGenerator is the controlling class of when different objects spawn.
 */
public class InfiniteObjectGenerator : MonoBehaviour
{
    static public InfiniteObjectGenerator instance;

    // How far out in the distance objects spawn (squared)
    public float sqrHorizon;
    // The distance behind the camera that the objects will be removed and added back to the object pool
    public float removeHorizon;
    // the number of units between the slots in the track
    public float slotDistance;
    // Spawn the full length of objects, useful when creating a tutorial or startup objects
    public bool spawnFullLength;
    // Do we want to reposition on height changes?
    public bool heightReposition;

    // the probability that no collidables will spawn on the platform
    [HideInInspector]
    public DistanceValueList noCollidableProbability;

    private SectionSelection sectionSelection;

    private Vector3 moveDirection;
    private Vector3 spawnDirection;

    private PlatformObject[] turnPlatform;

    private Vector3[] platformSizes;
    private Vector3[] sceneSizes;
    private float largestSceneLength;
    private Vector3[] platformStartPosition;
    private Vector3[] sceneStartPosition;

    private bool stopObjectSpawns;
    private ObjectSpawnData spawnData;

    private PlayerController playerController;
    private ChaseController chaseController;
    private Transform playerTransform;
    private DataManager dataManager;
    private InfiniteObjectManager infiniteObjectManager;
    private InfiniteObjectHistory infiniteObjectHistory;

    public void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        dataManager = DataManager.instance;
        infiniteObjectManager = InfiniteObjectManager.instance;
        infiniteObjectManager.init();
        infiniteObjectHistory = InfiniteObjectHistory.instance;
        infiniteObjectHistory.init(infiniteObjectManager.getTotalObjectCount());
        sectionSelection = SectionSelection.instance;
        chaseController = ChaseController.instance;

        moveDirection = Vector3.forward;
        spawnDirection = Vector3.forward;
        turnPlatform = new PlatformObject[(int)ObjectLocation.Last];

        infiniteObjectManager.getObjectSizes(out platformSizes, out sceneSizes, out largestSceneLength);
        infiniteObjectManager.getObjectStartPositions(out platformStartPosition, out sceneStartPosition);

        stopObjectSpawns = false;
        spawnData = new ObjectSpawnData();
        spawnData.largestScene = largestSceneLength;
        spawnData.useWidthBuffer = true;
        spawnData.section = 0;
        spawnData.sectionTransition = false;

        noCollidableProbability.init();

        showStartupObjects(GameManager.instance.showTutorial);

        spawnObjectRun(true);
    }

    // creates any startup objects, returns null if no prefabs are assigned
    public bool showStartupObjects(bool tutorial)
    {
        GameObject startupObjects = infiniteObjectManager.createStartupObjects(tutorial);
        if (startupObjects == null)
            return false;

        Transform objectTypeParent;
        Transform topObject;
        Transform transformParent;
        InfiniteObject parentInfiniteObject;
        bool isSceneObject;
        for (int i = 0; i < 2; ++i) {
            isSceneObject = i == 0;
            objectTypeParent = startupObjects.transform.FindChild((isSceneObject ? "Scene" : "Platforms"));
            // iterate high to low because assignParent is going to set a new parent thus changing the number of children in startup objects
            for (int j = objectTypeParent.childCount - 1; j >= 0; --j) {
                topObject = objectTypeParent.GetChild(j);
                infiniteObjectManager.assignParent(topObject.GetComponent<InfiniteObject>(), (isSceneObject ? ObjectType.Scene : ObjectType.Platform));

                InfiniteObject[] childObjects;
                if (isSceneObject) {
                    childObjects = topObject.GetComponentsInChildren<SceneObject>() as SceneObject[];
                } else {
                    childObjects = topObject.GetComponentsInChildren<PlatformObject>() as PlatformObject[];
                }
                for (int k = 0; k < childObjects.Length; ++k) {
                    childObjects[k].enableDestroyOnDeactivation();
                    transformParent = childObjects[k].getTransform().parent;
                    if ((parentInfiniteObject = transformParent.GetComponent<InfiniteObject>()) != null) {
                        childObjects[k].setInfiniteObjectParent(parentInfiniteObject);
                    }

                    if (!isSceneObject) { // platform
                        PlatformObject platformObject = ((PlatformObject)childObjects[k]);
                        if (platformObject.leftTurn || platformObject.rightTurn) {
                            turnPlatform[(int)ObjectLocation.Center] = platformObject;
                        }
                        // mark the coin objects as destructible so they get destroyed if they are collected
                        CoinObject[] coinObjects = platformObject.GetComponentsInChildren<CoinObject>() as CoinObject[];
                        for (int l = 0; l < coinObjects.Length; ++l) {
                            coinObjects[l].enableDestroyOnDeactivation();
                        }
                    }
                }
            }
        }

        // Get the persistent objects
        InfiniteObjectPersistence persistence = startupObjects.GetComponent<InfiniteObjectPersistence>();
        infiniteObjectHistory.loadInfiniteObjectPersistence(persistence);

        // All of the important objects have been taken out, destroy the game object
        Destroy(startupObjects.gameObject);

        return true;
    }

    public void startGame()
    {
        playerController = PlayerController.instance;
        playerTransform = playerController.transform;
    }

    // An object run contains many platforms strung together with collidables: obstacles, power ups, and coins. If spawnObjectRun encounters a turn,
    // it will spawn the objects in the correct direction
    public void spawnObjectRun(bool activateImmediately)
    {
        InfiniteObject prevPlatform = infiniteObjectHistory.getTopInfiniteObject(ObjectLocation.Center, false);
        while ((prevPlatform == null || (Vector3.Scale(prevPlatform.getTransform().position, spawnDirection)).sqrMagnitude < sqrHorizon) && turnPlatform[(int)ObjectLocation.Center] == null) {
            Vector3 position = Vector3.zero;
            if (prevPlatform != null) {
                int prevPlatformIndex = infiniteObjectHistory.getLastLocalIndex(ObjectLocation.Center, ObjectType.Platform);
                position = prevPlatform.getTransform().position - getPrevPlatformStartPosition(prevPlatform, prevPlatformIndex, spawnDirection) + platformSizes[prevPlatformIndex].z / 2 * spawnDirection + platformSizes[prevPlatformIndex].y * Vector3.up;
            }
            PlatformObject platform = spawnObjects(ObjectLocation.Center, position, spawnDirection, activateImmediately);

            if (platform == null)
                return;

            platformSpawned(platform, ObjectLocation.Center, spawnDirection, activateImmediately);
            prevPlatform = infiniteObjectHistory.getTopInfiniteObject(ObjectLocation.Center, false);

            if (spawnFullLength)
                spawnObjectRun(activateImmediately);
        }

        if (turnPlatform[(int)ObjectLocation.Center] != null) {
            Vector3 turnDirection = turnPlatform[(int)ObjectLocation.Center].getTransform().right;

            // spawn the platform and scene objects for the left and right turns
            for (int i = 0; i < 2; ++i) {
                ObjectLocation location = (i == 0 ? ObjectLocation.Right : ObjectLocation.Left);

                bool canTurn = (location == ObjectLocation.Right && turnPlatform[(int)ObjectLocation.Center].rightTurn) ||
                                (location == ObjectLocation.Left && turnPlatform[(int)ObjectLocation.Center].leftTurn);
                if (canTurn && turnPlatform[(int)location] == null) {
                    prevPlatform = infiniteObjectHistory.getTopInfiniteObject(location, false);
                    if (prevPlatform == null || (Vector3.Scale(prevPlatform.getTransform().position, turnDirection)).sqrMagnitude < sqrHorizon) {
                        infiniteObjectHistory.setActiveLocation(location);
                        Vector3 position = Vector3.zero;
                        if (prevPlatform != null) {
                            int prevPlatformIndex = infiniteObjectHistory.getLastLocalIndex(location, ObjectType.Platform);
                            position = prevPlatform.getTransform().position - getPrevPlatformStartPosition(prevPlatform, prevPlatformIndex, turnDirection) +
                                        platformSizes[prevPlatformIndex].z / 2 * turnDirection + platformSizes[prevPlatformIndex].y * Vector3.up;
                        } else {
                            PlatformObject centerTurn = turnPlatform[(int)ObjectLocation.Center];
                            int centerTurnIndex = infiniteObjectHistory.getLastLocalIndex(ObjectLocation.Center, ObjectType.Platform);
                            position = centerTurn.getTransform().position - platformStartPosition[centerTurnIndex].x * turnDirection - Vector3.up * platformStartPosition[centerTurnIndex].y - 
                                            platformStartPosition[centerTurnIndex].z * spawnDirection + centerTurn.centerOffset.x * turnDirection + centerTurn.centerOffset.z * spawnDirection;
                        }

                        PlatformObject platform = spawnObjects(location, position, turnDirection, activateImmediately);
                        if (platform == null)
                            return;

                        platformSpawned(platform, location, turnDirection, activateImmediately);
                    }
                }
                turnDirection *= -1;
            }

            // reset
            infiniteObjectHistory.setActiveLocation(ObjectLocation.Center);
        }
    }

    // it is a lot of work to adjust for the previous platform start position
    private Vector3 getPrevPlatformStartPosition(InfiniteObject platform, int platformIndex, Vector3 direction)
    {
        return platformStartPosition[platformIndex].x * platform.getTransform().right + platformStartPosition[platformIndex].y * Vector3.up + 
                    platformStartPosition[platformIndex].z * direction;
    }

    // spawn the platforms, obstacles, power ups, and coins
    private PlatformObject spawnObjects(ObjectLocation location, Vector3 position, Vector3 direction, bool activateImmediately)
    {
        setupSection(location, false);
        int localIndex = infiniteObjectManager.getNextObjectIndex(ObjectType.Platform, spawnData);
        if (localIndex == -1) {
            print("Unable to spawn platform. No platforms can be spawned based on the probability rules at distance " +
                infiniteObjectHistory.getTotalDistance(false) + " within section " + spawnData.section + (spawnData.sectionTransition ? (" (Transitioning from section " + spawnData.prevSection + ")") : ""));
            return null;
        }
        PlatformObject platform = spawnPlatform(localIndex, location, position, direction, activateImmediately);

        if (platform.canSpawnCollidable() && Random.value >= noCollidableProbability.getValue(infiniteObjectHistory.getTotalDistance(false))) {
            // First try to spawn an obstacle. If there is any space remaining on the platform, then try to spawn a coin.
            // If there is still some space remaing, try to spawn a powerup.
            // An extension of this would be to randomize the order of ObjectType, but this way works if the probabilities
            // are setup fairly
            spawnCollidable(ObjectType.Obstacle, position, direction, location, platform, localIndex, activateImmediately);
            if (platform.canSpawnCollidable()) {
                spawnCollidable(ObjectType.Coin, position, direction, location, platform, localIndex, activateImmediately);
                if (platform.canSpawnCollidable()) {
                    spawnCollidable(ObjectType.PowerUp, position, direction, location, platform, localIndex, activateImmediately);
                }
            }
        }

        return platform;
    }

    // returns the length of the created platform
    private PlatformObject spawnPlatform(int localIndex, ObjectLocation location, Vector3 position, Vector3 direction, bool activateImmediately)
    {
        PlatformObject platform = (PlatformObject)infiniteObjectManager.objectFromPool(localIndex, ObjectType.Platform);
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        platform.orient(position + (direction * platformSizes[localIndex].z / 2), lookRotation);
        if (activateImmediately)
            platform.activate();

        int objectIndex = infiniteObjectManager.localIndexToObjectIndex(localIndex, ObjectType.Platform);
        InfiniteObject prevTopPlatform = infiniteObjectHistory.objectSpawned(objectIndex, 0, location, lookRotation.eulerAngles.y, ObjectType.Platform, platform);
        // the current platform now becames the parent of the previous top platform
        if (prevTopPlatform != null) {
            prevTopPlatform.setInfiniteObjectParent(platform);
        } else {
            infiniteObjectHistory.setBottomInfiniteObject(location, false, platform);
        }
        infiniteObjectHistory.addTotalDistance(platformSizes[localIndex].z, location, false, spawnData.section);

        return platform;
    }

    // a platform has been spawned, now spawn the scene objects and setup for a turn if needed
    private void platformSpawned(PlatformObject platform, ObjectLocation location, Vector3 direction, bool activateImmediately)
    {
        int localIndex;
        bool isTurn = platform.leftTurn || platform.rightTurn;
        if (isTurn || spawnFullLength) {
            // set largestScene to 0 to prevent the scene spawner from waiting for space for the largest scene object
            spawnData.largestScene = 0;
            spawnData.useWidthBuffer = false;
        }

        // spawn all of the scene objects until we have spawned enough scene objects
        setupSection(location, true);
        while ((localIndex = infiniteObjectManager.getNextObjectIndex(ObjectType.Scene, spawnData)) != -1) {
            Vector3 position = Vector3.zero;
            SceneObject prevScene = infiniteObjectHistory.getTopInfiniteObject(location, true) as SceneObject;
            bool useZSize = true;
            // may be null if coming from a turn
            if (prevScene == null) {
                prevScene = infiniteObjectHistory.getTopInfiniteObject(ObjectLocation.Center, true) as SceneObject;
                useZSize = false;
            }
            if (prevScene) {
                int prevSceneIndex = infiniteObjectHistory.getLastLocalIndex(location, ObjectType.Scene);
                position = prevScene.getTransform().position - sceneStartPosition[prevSceneIndex] + (useZSize ? sceneSizes[prevSceneIndex].z : sceneSizes[prevSceneIndex].x) / 2 * direction + sceneSizes[prevSceneIndex].y * Vector3.up;
            }
            spawnSceneObject(localIndex, location, position, direction, activateImmediately);
            // the section may change because of the newly spawned scene object
            setupSection(location, true);
        }

        if (isTurn) {
            spawnData.largestScene = largestSceneLength;
            spawnData.useWidthBuffer = true;

            turnPlatform[(int)location] = platform;

            if (location == ObjectLocation.Center) {
                infiniteObjectHistory.resetTurnCount();
            }
        } else if (platform.sectionTransition) {
            infiniteObjectHistory.didSpawnSectionTranition(location, false);
        }
    }

    // before platforms are about to be spawned setup the section data to ensure the correct platforms are spawned
    private void setupSection(ObjectLocation location, bool isSceneObject)
    {
        int prevSection = infiniteObjectHistory.getPreviousSection(location, isSceneObject);
        spawnData.section = sectionSelection.getSection(infiniteObjectHistory.getTotalDistance(isSceneObject), isSceneObject);
        if (sectionSelection.useSectionTransitions) {
            if (spawnData.section != prevSection && !infiniteObjectHistory.hasSpawnedSectionTransition(location, isSceneObject)) {
                spawnData.sectionTransition = true;
                spawnData.prevSection = prevSection;
            } else {
                spawnData.sectionTransition = false;
                if (spawnData.section != prevSection && infiniteObjectHistory.hasSpawnedSectionTransition(location, isSceneObject))
                    infiniteObjectHistory.setPreviousSection(location, isSceneObject, spawnData.section);
            }
        }
    }

    // returns true if there is still space on the platform for a collidable object to spawn
    private void spawnCollidable(ObjectType objectType, Vector3 position, Vector3 direction, ObjectLocation location, PlatformObject platform, int platformLocalIndex, bool activateImmediately)
    {
        int collidablePositions = platform.collidablePositions;
        // can't do anything if the platform doesn't accept any collidable object spawns
        if (collidablePositions == 0)
            return;

        Vector3 offset = platformSizes[platformLocalIndex] * 0.1f;
        float zDelta = platformSizes[platformLocalIndex].z * .8f / (1 + collidablePositions);

        for (int i = 0; i < collidablePositions; ++i) {
            if (platform.canSpawnCollidable(i)) {
                spawnData.slotPositions = platform.getSlotsAvailable();
                int localIndex = infiniteObjectManager.getNextObjectIndex(objectType, spawnData);
                if (localIndex != -1) {
                    CollidableObject collidable = infiniteObjectManager.objectFromPool(localIndex, objectType) as CollidableObject;
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    Vector3 spawnSlot = collidable.getSpawnSlot(platform.getTransform().right * slotDistance, spawnData.slotPositions);
                    collidable.orient(platform, position + (offset.z + ((i + 1) * zDelta)) * direction + spawnSlot, lookRotation);
                    if (activateImmediately)
                        collidable.activate();
                    
                    int objectIndex = infiniteObjectManager.localIndexToObjectIndex(localIndex, objectType);
                    infiniteObjectHistory.objectSpawned(objectIndex, (offset.z + ((i + 1) * zDelta)), location, lookRotation.eulerAngles.y, objectType);
                    platform.collidableSpawned(i);

                    // don't allow any more of the same collidable type if we are forcing a different collidable
                    if (platform.forceDifferentCollidableTypes)
                        break;
                }
            }
        }
        spawnData.slotPositions = 0;
    }

    // spawn a scene object at the specified location
    private void spawnSceneObject(int localIndex, ObjectLocation location, Vector3 position, Vector3 direction, bool activateImmediately)
    {
        SceneObject scene = (SceneObject)infiniteObjectManager.objectFromPool(localIndex, ObjectType.Scene);
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        scene.orient(position + direction * sceneSizes[localIndex].z / 2, lookRotation);
        if (activateImmediately)
            scene.activate();

        int objectIndex = infiniteObjectManager.localIndexToObjectIndex(localIndex, ObjectType.Scene);
        InfiniteObject prevTopScene = infiniteObjectHistory.objectSpawned(objectIndex, 0, location, lookRotation.eulerAngles.y, ObjectType.Scene, scene);
        // the current scene now becames the parent of the previous top scene
        if (prevTopScene != null) {
            prevTopScene.setInfiniteObjectParent(scene);
        } else {
            infiniteObjectHistory.setBottomInfiniteObject(location, true, scene);
        }

        infiniteObjectHistory.addTotalDistance(sceneSizes[localIndex].z, location, true, spawnData.section);
        if (scene.sectionTransition) {
            infiniteObjectHistory.didSpawnSectionTranition(location, true);
        }
    }

    // move all of the active objects
    public void moveObjects(float moveDistance)
    {
        if (moveDistance == 0)
            return;

        // the distance to move the objects
        Vector3 delta = moveDirection * moveDistance;

        // only move the top most platform/scene of each ObjectLocation because all of the other objects are children of these two
        // objects. Only have to check the bottom-most platform/scene as well to determine if it should be removed
        InfiniteObject infiniteObject = null;
        Transform objectTransform = null;
        PlatformObject platformObject = null;
        for (int i = 0; i < 2; ++i) { // loop through the platform and scenes
            for (int j = 0; j < (int)ObjectLocation.Last; ++j) {
                // move
                infiniteObject = infiniteObjectHistory.getTopInfiniteObject((ObjectLocation)j, i == 0);
                if (infiniteObject != null) {
                    objectTransform = infiniteObject.getTransform();
                    Vector3 pos = objectTransform.position;
                    pos -= delta;
                    objectTransform.position = pos;

                    // check for removal.. there will always be a bottom object if there is a top object
                    infiniteObject = infiniteObjectHistory.getBottomInfiniteObject((ObjectLocation)j, i == 0);
                    if (playerTransform.InverseTransformPoint(infiniteObject.getTransform().position).z < removeHorizon) {
                        // if the infinite object is a platform and it has changes height, move everything down by that height
                        if (heightReposition && i == 1) { // 1 are platforms
                            platformObject = infiniteObject as PlatformObject;
                            if (platformObject.slope != PlatformSlope.None) {
                                transitionHeight(platformSizes[platformObject.getLocalIndex()].y);
                            }
                        }

                        infiniteObjectHistory.objectRemoved((ObjectLocation)j, i == 0);
                        infiniteObject.deactivate();
                    }
                }
            }

            // loop through all of the turn objects
            infiniteObject = infiniteObjectHistory.getTopTurnInfiniteObject(i == 0);
            if (infiniteObject != null) {
                objectTransform = infiniteObject.getTransform();
                Vector3 pos = objectTransform.position;
                pos -= delta;
                objectTransform.position = pos;

                infiniteObject = infiniteObjectHistory.getBottomTurnInfiniteObject(i == 0);
                if (playerTransform.InverseTransformPoint(infiniteObject.getTransform().position).z < removeHorizon) {
                    infiniteObjectHistory.turnObjectRemoved(i == 0);
                    infiniteObject.deactivate();
                }
            }
        }

        if (!stopObjectSpawns) {
            dataManager.addToScore(moveDistance);
            spawnObjectRun(true);
        }
    }

    // When a platform with delta height is removed, move all of the objects back to their original heights to reduce the chances
    // of floating point errors
    private void transitionHeight(float amount)
    {
        // Move the position of every object by -amount
        InfiniteObject infiniteObject;
        Transform infiniteObjectTransform;
        Vector3 position;
        for (int i = 0; i < 2; ++i) { // loop through the platform and scenes
            for (int j = 0; j < (int)ObjectLocation.Last; ++j) {
                infiniteObject = infiniteObjectHistory.getTopInfiniteObject((ObjectLocation)j, i == 0);
                if (infiniteObject != null) {
                    position = (infiniteObjectTransform = infiniteObject.getTransform()).position;
                    position.y -= amount;
                    infiniteObjectTransform.position = position;
                }
            }
        }

        
        playerController.transitionHeight(amount);
        chaseController.transitionHeight(amount);
    }

    // gradually turn the player for a curve
    public void setMoveDirection(Vector3 newDirection)
    {
        moveDirection = newDirection;
    }

    // the player hit a turn, start generating new objects
    public Vector3 updateSpawnDirection(Vector3 newDirection, bool setMoveDirection, bool rightTurn, bool playerAboveTurn)
    {
        if (setMoveDirection) {
            moveDirection = newDirection;
        }

        // don't change spawn directions if the player isn't above a turn. The game is about to be over anyway so there isn't a reason to keep generating objects
        if (!playerAboveTurn) {
            stopObjectSpawns = true;
            return Vector3.zero;
        }

        float yAngle = Quaternion.LookRotation(newDirection).eulerAngles.y;
        if ((rightTurn && Mathf.Abs(yAngle - infiniteObjectHistory.getObjectLocationAngle(ObjectLocation.Right)) < 0.01f) ||
            (!rightTurn && Mathf.Abs(yAngle - infiniteObjectHistory.getObjectLocationAngle(ObjectLocation.Left)) < 0.01f)) {
            spawnDirection = newDirection;
            ObjectLocation turnLocation = (rightTurn ? ObjectLocation.Right : ObjectLocation.Left);
            turnPlatform[(int)ObjectLocation.Center] = turnPlatform[(int)turnLocation];
            turnPlatform[(int)ObjectLocation.Right] = turnPlatform[(int)ObjectLocation.Left] = null;

            // The center objects and the objects in the location opposite of turn are grouped togeter with the center object being the top most object
            for (int i = 0; i < 2; ++i) {
                InfiniteObject infiniteObject = infiniteObjectHistory.getTopInfiniteObject((turnLocation == ObjectLocation.Right ? ObjectLocation.Left : ObjectLocation.Right), i == 0);
                // may be null if the turn only turns one direction
                if (infiniteObject != null) {
                    InfiniteObject centerObject = infiniteObjectHistory.getBottomInfiniteObject(ObjectLocation.Center, i == 0);
                    infiniteObject.setInfiniteObjectParent(centerObject);
                }
            }

            infiniteObjectHistory.turn(turnLocation);
            if (turnPlatform[(int)ObjectLocation.Center] != null) {
                infiniteObjectHistory.resetTurnCount();
            }

            return getTurnOffset();
        }
        return Vector3.zero;
    }

    public Vector3 getTurnOffset()
    {
        // add an offset so the character is always in the correct slot after a turn
        PlatformObject topPlatform = infiniteObjectHistory.getTopInfiniteObject(ObjectLocation.Center, false) as PlatformObject;
        Vector3 offset = Vector3.zero;
        Vector3 position = topPlatform.getTransform().position;
        int topPlatformIndex = infiniteObjectHistory.getLastLocalIndex(ObjectLocation.Center, ObjectType.Platform);
        Quaternion lookRotation = Quaternion.LookRotation(spawnDirection);
        offset.x = (position.x + platformStartPosition[topPlatformIndex].x * (spawnDirection.z > 0 ? -1 : 1)) * -Mathf.Cos(lookRotation.eulerAngles.y * Mathf.Deg2Rad) * (spawnDirection.z > 0 ? -1 : 1);
        offset.z = (position.z + platformStartPosition[topPlatformIndex].x * (spawnDirection.x < 0 ? -1 : 1)) * Mathf.Sin(lookRotation.eulerAngles.y * Mathf.Deg2Rad) * (spawnDirection.x < 0 ? -1 : 1);
        return offset;
    }

    // clear everything out and reset the generator back to the beginning, keeping the current set of objects activated before new objects are generated
    public void reset()
    {
        moveDirection = Vector3.forward;
        spawnDirection = Vector3.forward;

        for (int i = 0; i < (int)ObjectLocation.Last; ++i) {
            turnPlatform[i] = null;
        }

        stopObjectSpawns = false;
        spawnData.largestScene = largestSceneLength;
        spawnData.useWidthBuffer = true;
        spawnData.section = 0;
        spawnData.sectionTransition = false;

        infiniteObjectHistory.saveObjectsReset();
        sectionSelection.reset();
    }

    // remove the saved infinite objects and activate the set of objects for the next game
    public void readyFromReset()
    {
        // deactivate the saved infinite objects from the previous game
        InfiniteObject infiniteObject = infiniteObjectHistory.getSavedInfiniteObjects();
        InfiniteObject[] childObjects = null;
        for (int i = 0; i < 2; ++i) { // loop through the platform and scenes
            if (i == 0) { // scene
                childObjects = infiniteObject.GetComponentsInChildren<SceneObject>(true);
            } else {
                childObjects = infiniteObject.GetComponentsInChildren<PlatformObject>(true);
            }

            for (int j = 0; j < childObjects.Length; ++j) {
                childObjects[j].deactivate();
            }
        }

        // activate the objects for the current game
        for (int i = 0; i < 2; ++i) { // loop through the platform and scenes
            for (int j = 0; j < (int)ObjectLocation.Last; ++j) {
                infiniteObject = infiniteObjectHistory.getTopInfiniteObject((ObjectLocation)j, i == 0);
                if (infiniteObject != null) {
                    if (i == 0) { // scene
                        childObjects = infiniteObject.GetComponentsInChildren<SceneObject>(true);
                    } else {
                        childObjects = infiniteObject.GetComponentsInChildren<PlatformObject>(true);
                    }

                    for (int k = 0; k < childObjects.Length; ++k) {
                        childObjects[k].activate();
                    }
                }
            }
        }
    }
}