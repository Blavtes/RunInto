using UnityEngine;
using System.Collections;

/*
 * Static data is a singleton class which holds any data which is not directly game related, such as the power up length and cost
 */
public class StaticData : MonoBehaviour {

	static public StaticData instance;

    public int totalPowerUpLevels;
    public string[] powerUpTitle;
    public string[] powerUpDescription;
    public GameObject[] powerUpPrefab;
	public float[] powerUpLength;
    public int[] powerUpCost;

    public int characterCount;
    public string[] characterTitle;
    public string[] characterDescription;
    public int[] characterCost;
    public GameObject[] characterPrefab;

    public string[] missionDescription;
    public int[] missionGoal;

    public GameObject chaseObjectPrefab;
	
	public void Awake()
	{
		instance = this;	
	}

    public string getPowerUpTitle(PowerUpTypes powerUpType)
    {
        return powerUpTitle[(int)powerUpType];
    }

    public string getPowerUpDescription(PowerUpTypes powerUpType)
    {
        return powerUpDescription[(int)powerUpType];
    }

    public GameObject getPowerUpPrefab(PowerUpTypes powerUpType)
    {
        return powerUpPrefab[(int)powerUpType];
    }
	
	public float getPowerUpLength(PowerUpTypes powerUpType, int level)
	{
        return powerUpLength[((int)powerUpType * (totalPowerUpLevels + 1)) + level];
	}
	
	public int getPowerUpCost(PowerUpTypes powerUpType, int level)
	{
		return powerUpCost[((int)powerUpType * totalPowerUpLevels) + level];
	}
	
	public int getTotalPowerUpLevels()
	{
		return totalPowerUpLevels;
	}

    public string getCharacterTitle(int character)
    {
        return characterTitle[character];
    }

    public string getCharacterDescription(int character)
    {
        return characterDescription[character];
    }

    public int getCharacterCost(int character)
    {
        return characterCost[character];
    }

    public GameObject getCharacterPrefab(int character)
    {
        return characterPrefab[character];
    }

    public string getMissionDescription(MissionType missionType)
    {
        return missionDescription[(int)missionType];
    }

    public int getMissionGoal(MissionType missionType)
    {
        return missionGoal[(int)missionType];
    }

    public GameObject getChaseObjectPrefab()
    {
        return chaseObjectPrefab;
    }
}
