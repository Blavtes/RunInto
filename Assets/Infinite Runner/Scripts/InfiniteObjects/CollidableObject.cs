using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * A collidable object attaches itself to the platform when activated, and moves back to the original parent on deactivation
 */
public class CollidableObject : InfiniteObject {

    public bool canSpawnInLeftSlot;
    public bool canSpawnInCenterSlot;
    public bool canSpawnInRightSlot;

    private PlatformObject platformParent;

    private List<int> slotPositions;
    private List<int> slotPositionsPow;
    private int slotPositionsMask;

    public override void init() {
        base.init();

        determineSlotPositions();
    }

    public override void Awake()
    {
        base.Awake();

        // need to determine the slot positions again because the cloned object doesn't get inited
        determineSlotPositions();
    }

    private void determineSlotPositions()
    {
        slotPositions = new List<int>();
        slotPositionsPow = new List<int>();
        slotPositionsMask = 0;
        if (canSpawnInLeftSlot) {
            slotPositions.Add(-1);
            slotPositionsPow.Add(0);
            slotPositionsMask |= 1;
        }
        if (canSpawnInCenterSlot) {
            slotPositions.Add(0);
            slotPositionsPow.Add(1);
            slotPositionsMask |= 2;
        }
        if (canSpawnInRightSlot) {
            slotPositions.Add(1);
            slotPositionsPow.Add(2);
            slotPositionsMask |= 4;
        }
    }

	public override void setParent(Transform parent)
	{
		base.setParent(parent);

        CollidableObject childCollidableObject = null;
        for (int i = 0; i < thisTransform.childCount; ++i) {
            if ((childCollidableObject = thisTransform.GetChild(i).GetComponent<CollidableObject>()) != null) {
                childCollidableObject.setStartParent(thisTransform);
            }
        }
	}

    public int getSlotPositionsMask()
    {
        return slotPositionsMask;
    }

    public Vector3 getSpawnSlot(Vector3 platformPosition, int platformSlots)
    {
        if (slotPositions.Count > 0 && platformSlots != 0) {
            List<int> slotPow = slotPositionsPow;
            while (slotPow.Count > 0) {
                int index = Random.Range(0, slotPow.Count);
                int mask = (int)Mathf.Pow(2, slotPositionsPow[index]);
                // return the position if the platform can spawn with the given slot
                if ((platformSlots & mask) == mask) {
                    return platformPosition * slotPositions[index];
                }
                // can't spawn yet
                slotPow.RemoveAt(index);
            }
        }
        return platformPosition;
    }

    public override void orient(PlatformObject parent, Vector3 position, Quaternion rotation)
	{
        base.orient(parent, position, rotation);
		
		platformParent = parent;
		platformParent.onPlatformDeactivation += collidableDeactivation;
	}
	
	public virtual void collidableDeactivation()
	{
        if (platformParent)
		    platformParent.onPlatformDeactivation -= collidableDeactivation;
		
		base.deactivate();
	}
}
