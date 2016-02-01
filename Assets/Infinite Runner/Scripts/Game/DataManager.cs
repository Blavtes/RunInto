using UnityEngine;
using System.Collections;

/*
 * The data manager is a singleton which manages the data across games. It will persist any permanent data such as the
 * total number of coins or power up level
 */
public class DataManager : MonoBehaviour {
	
	static public DataManager instance;
	
	public float scoreMult;
	
	private float score;
	private int totalCoins;
	private int levelCoins;
	private int collisions;

	private int[] currentPowerupLevel;
	
	private GUIManager guiManager;
    private SocialManager socialManager;
    private MissionManager missionManager;
	private StaticData staticData;
	
	public void Awake()
	{
		instance = this;
	}
	
	public void Start()
	{	
		guiManager = GUIManager.instance;
        socialManager = SocialManager.instance;
        missionManager = MissionManager.instance;
		staticData = StaticData.instance;
		
		score = 0;
		levelCoins = 0;
		collisions = 0;
		totalCoins = PlayerPrefs.GetInt("Coins", 0);
		
		currentPowerupLevel = new int[(int)PowerUpTypes.None];
		for (int i = 0; i < (int)PowerUpTypes.None; ++i) {
            currentPowerupLevel[i] = PlayerPrefs.GetInt(string.Format("PowerUp{0}", i), 0);
            if (currentPowerupLevel[i] == 0 && GameManager.instance.enableAllPowerUps) {
                currentPowerupLevel[i] = 1;
            }
        }

        // first character is always available
        purchaseCharacter(0);
	}
	
    public void addToScore(float amount)
    {
        score += (amount * scoreMult);
        guiManager.setInGameScore(Mathf.RoundToInt(score));
    }
	
	public int getScore(bool withMultiplier = false)
	{
        return Mathf.RoundToInt(score * (withMultiplier ? missionManager.getScoreMultiplier() : 1));	
	}
	
	public void obstacleCollision()
	{
		collisions++;
	}
	
	public int getCollisionCount()
	{
		return collisions;	
	}
	
	public void addToCoins(int amount)
	{
		levelCoins += amount;
		guiManager.setInGameCoinCount(levelCoins);
	}
	
	public int getLevelCoins()
	{
		return levelCoins;	
	}

    public void adjustTotalCoins(int amount)
    {
        totalCoins += amount;
        PlayerPrefs.SetInt("Coins", totalCoins);
    }
	
	public int getTotalCoins()
	{
		return totalCoins;
	}
	
	public int getHighScore()
	{
		return PlayerPrefs.GetInt("HighScore", 0);
	}
	
	public int getPlayCount()
	{
		return PlayerPrefs.GetInt("PlayCount");	
	}
	
	public void gameOver()
    {
		// save the high score, coin count, and play count
		if (getScore() > getHighScore()) {
			PlayerPrefs.SetInt("HighScore", getScore());
            socialManager.recordScore(getScore());
		}
		
		totalCoins += levelCoins;
		PlayerPrefs.SetInt("Coins", totalCoins);
		
		int playCount = PlayerPrefs.GetInt("PlayCount", 0);
		playCount++;
        PlayerPrefs.SetInt("PlayCount", playCount);
	}
	
	public void reset()
	{
		score = 0;
		levelCoins = 0;
		collisions = 0;
		
		guiManager.setInGameScore(Mathf.RoundToInt(score));
		guiManager.setInGameCoinCount(levelCoins);
	}

    public int getCharacterCount()
    {
        return staticData.characterCount;
    }

    public string getCharacterTitle(int character)
    {
        return staticData.getCharacterTitle(character);
    }

    public string getCharacterDescription(int character)
    {
        return staticData.getCharacterDescription(character);
    }

    public int getCharacterCost(int character)
    {
        if (PlayerPrefs.GetInt(string.Format("CharacterPurchased{0}", character), 0) == 1)
            return -1; // -1 cost if the character is already purchased
        return staticData.getCharacterCost(character);
    }

    public void purchaseCharacter(int character)
    {
        PlayerPrefs.SetInt(string.Format("CharacterPurchased{0}", character), 1);
    }

    public void setSelectedCharacter(int character)
    {
        if (PlayerPrefs.GetInt(string.Format("CharacterPurchased{0}", character), 0) == 1) {
            PlayerPrefs.SetInt("SelectedCharacter", character);
        }
    }

    public int getSelectedCharacter()
    {
        return PlayerPrefs.GetInt("SelectedCharacter", 0);
    }

    public GameObject getCharacterPrefab(int character)
    {
        return staticData.getCharacterPrefab(character);
    }

    public GameObject getChaseObjectPrefab()
    {
        return staticData.getChaseObjectPrefab();
    }

    public string getPowerUpTitle(PowerUpTypes powerUpType)
    {
        return staticData.getPowerUpTitle(powerUpType);
    }

    public string getPowerUpDescription(PowerUpTypes powerUpType)
    {
        return staticData.getPowerUpDescription(powerUpType);
    }

    public GameObject getPowerUpPrefab(PowerUpTypes powerUpType)
    {
        return staticData.getPowerUpPrefab(powerUpType);
    }

    public int getPowerUpLevel(PowerUpTypes powerUpTypes)
    {
        return currentPowerupLevel[(int)powerUpTypes];
    }

    public float getPowerUpLength(PowerUpTypes powerUpType)
    {
        return staticData.getPowerUpLength(powerUpType, currentPowerupLevel[(int)powerUpType]);
    }

    public int getPowerUpCost(PowerUpTypes powerUpType)
    {
        if (currentPowerupLevel[(int)powerUpType] < staticData.getTotalPowerUpLevels()) {
            return staticData.getPowerUpCost(powerUpType, currentPowerupLevel[(int)powerUpType]);
        }
        return -1; // out of power up upgrades
    }

    public void upgradePowerUp(PowerUpTypes powerUpType)
    {
        currentPowerupLevel[(int)powerUpType]++;
        PlayerPrefs.SetInt(string.Format("PowerUp{0}", (int)powerUpType), currentPowerupLevel[(int)powerUpType]);
    }

    public int getMissionGoal(MissionType missionType)
    {
        return staticData.getMissionGoal(missionType);
    }

    public string getMissionDescription(MissionType missionType)
    {
        return staticData.getMissionDescription(missionType);
    }
}
