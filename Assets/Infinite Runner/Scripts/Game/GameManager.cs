using UnityEngine;
using System.Collections;

/*
 * CoroutineData is a quick class used to save off any timinig information needed when the game is paused.
 */
public class CoroutineData { 
	public float startTime; 
	public float duration; 
	public CoroutineData() { startTime = 0; duration = 0; }
	public void calcuateNewDuration() { duration -= Time.time - startTime; }
}

/*
 * The game manager is a singleton which manages the game state. It coordinates with all of the other classes to tell them
 * when to start different game states such as pausing the game or ending the game.
 */
public enum GameOverType { Wall, JumpObstacle, DuckObstacle, Pit, Quit };
public class GameManager : MonoBehaviour {
	
	static public GameManager instance;

    public delegate void PlayerSpawnHandler();
    public event PlayerSpawnHandler onPlayerSpawn;
	public delegate void PauseHandler(bool paused);
	public event PauseHandler onPauseGame;

    public bool godMode;
    public bool enableAllPowerUps;
	public bool showTutorial;
    public bool runInBackground;

    private int activeCharacter;
    private GameObject character;

    private bool gamePaused;
    private bool gameActive;
	
	private InfiniteObjectGenerator infiniteObjectGenerator;
	private PlayerController playerController;
	private GUIManager guiManager;
    private DataManager dataManager;
    private AudioManager audioManager;
    private PowerUpManager powerUpManager;
    private MissionManager missionManager;
    private InputController inputController;
    private ChaseController chaseController;
    private CameraController cameraController;
    private CoinGUICollection coinGUICollection;
	
	public void Awake()
	{
		instance = this;	
	}
	
	public void Start ()
	{
		infiniteObjectGenerator = InfiniteObjectGenerator.instance;
		guiManager = GUIManager.instance;
		dataManager = DataManager.instance;
        audioManager = AudioManager.instance;
		powerUpManager = PowerUpManager.instance;
        missionManager = MissionManager.instance;
        inputController = InputController.instance;
        cameraController = CameraController.instance;
        coinGUICollection = CoinGUICollection.instance;

        Application.runInBackground = runInBackground;
        activeCharacter = -1;
        spawnCharacter();
        spawnChaseObject();
	}

    private void spawnCharacter()
    {
        if (activeCharacter == dataManager.getSelectedCharacter()) {
            return;
        }

        if (character != null) {
            Destroy(character);
        }

        activeCharacter = dataManager.getSelectedCharacter();
        character = GameObject.Instantiate(dataManager.getCharacterPrefab(activeCharacter)) as GameObject;
        playerController = PlayerController.instance;
        playerController.init();

        if (onPlayerSpawn != null) {
            onPlayerSpawn();
        }
    }

    private void spawnChaseObject()
    {
        GameObject prefab;
        if ((prefab = dataManager.getChaseObjectPrefab()) != null) {
            chaseController = (GameObject.Instantiate(prefab) as GameObject).GetComponent<ChaseController>();
        }
    }
	
	public void startGame(bool fromRestart)
	{
        gameActive = true;
        inputController.startGame();
		guiManager.showGUI(GUIState.InGame);
        audioManager.playBackgroundMusic(true);
        cameraController.startGame(fromRestart);
        infiniteObjectGenerator.startGame();
		playerController.startGame();
        if (chaseController != null)
            chaseController.startGame();
	}

    public bool isGameActive()
    {
        return gameActive;
    }

    public void toggleTutorial()
    {
        showTutorial = !showTutorial;
        infiniteObjectGenerator.reset();
        if (showTutorial) {
            infiniteObjectGenerator.showStartupObjects(true);
        } else {
            // show the startup objects if there are any
            if (!infiniteObjectGenerator.showStartupObjects(false))
                infiniteObjectGenerator.spawnObjectRun(false);
        }
        infiniteObjectGenerator.readyFromReset();
    }
	
	public void obstacleCollision(ObstacleObject obstacle, Vector3 position)
	{
        if (!powerUpManager.isPowerUpActive(PowerUpTypes.Invincibility) && !powerUpManager.isPowerUpActive(PowerUpTypes.SpeedIncrease) && !godMode && gameActive) {
            playerController.obstacleCollision(obstacle.getTransform(), position);
			dataManager.obstacleCollision();
            if (dataManager.getCollisionCount() == playerController.maxCollisions) {
                gameOver(obstacle.isJump ? GameOverType.JumpObstacle : GameOverType.DuckObstacle, true);
            } else {
                // the chase object will end the game
                if (playerController.maxCollisions == 0 && chaseController != null) {
                    if (chaseController.isVisible()) {
                        gameOver(obstacle.isJump ? GameOverType.JumpObstacle : GameOverType.DuckObstacle, true);
                    } else {
                        chaseController.approach();
                        audioManager.playSoundEffect(SoundEffects.ObstacleCollisionSoundEffect);
                    }
                } else {
                    // have the chase object approach the character when the collision count gets close
                    if (chaseController != null && dataManager.getCollisionCount() == playerController.maxCollisions - 1) {
                        chaseController.approach();
                    }
                    audioManager.playSoundEffect(SoundEffects.ObstacleCollisionSoundEffect);
                }
            }
		}
	}
	
    // initial collection is true when the player first collects a coin. It will be false when the coin is done animating to the coin element on the GUI
    // returns the value of the coin with the double coin power up considered
	public int coinCollected()
	{
        int coinValue = (powerUpManager.isPowerUpActive(PowerUpTypes.DoubleCoin) ? 2 : 1);
        audioManager.playSoundEffect(SoundEffects.CoinSoundEffect);
        return coinValue;
	}

    public void coinCollected(int coinValue)
    {
        dataManager.addToCoins(coinValue);
    }

    public void activatePowerUp(PowerUpTypes powerUpType, bool activate)
    {
        if (activate) {
            // deactivate the current power up (if a power up is active) and activate the new one
            powerUpManager.deactivatePowerUp();
            powerUpManager.activatePowerUp(powerUpType);
            audioManager.playSoundEffect(SoundEffects.PowerUpSoundEffect);
        }
        playerController.activatePowerUp(powerUpType, activate);
        guiManager.activatePowerUp(powerUpType, activate, dataManager.getPowerUpLength(powerUpType));
    }
	
	public void gameOver(GameOverType gameOverType, bool waitForFrame)
	{
        if (!gameActive && waitForFrame)
            return;
        gameActive = false;

        if (waitForFrame) {
            // mecanim doesn't trigger the event if we wait until the frame is over
            playerController.gameOver(gameOverType);
            StartCoroutine(waitForFrameGameOver(gameOverType));
        } else {
            inputController.gameOver();
            // Mission Manager's gameOver must be called before the Data Manager's gameOver so the Data Manager can grab the 
            // score multiplier from the Mission Manager to determine the final score
            missionManager.gameOver();
            coinGUICollection.gameOver();
            dataManager.gameOver();
            if (playerController.enabled)
                playerController.gameOver(gameOverType);
            if (chaseController != null)
                chaseController.gameOver(gameOverType);
            audioManager.playBackgroundMusic(false);
            if (gameOverType != GameOverType.Quit)
                audioManager.playSoundEffect(SoundEffects.GameOverSoundEffect);
            guiManager.gameOver();
            cameraController.gameOver(gameOverType);
        }
	}
	
	// Game over may be called from a trigger so wait for the physics loop to end
    private IEnumerator waitForFrameGameOver(GameOverType gameOverType)
	{
		yield return new WaitForEndOfFrame();

        gameOver(gameOverType, false);

        // Wait a second for the ending animations to play
        yield return new WaitForSeconds(1.0f);

        guiManager.showGUI(GUIState.EndGame);
	}
	
	public void restartGame(bool start)
	{
        if (gamePaused) {
            if (onPauseGame != null)
                onPauseGame(false);
            gameOver(GameOverType.Quit, false);
        }

		dataManager.reset();
		infiniteObjectGenerator.reset();
		powerUpManager.reset();
		playerController.reset();
        cameraController.reset();
        if (chaseController != null)
            chaseController.reset();
        if (showTutorial) {
            infiniteObjectGenerator.showStartupObjects(true);
        } else {
            // show the startup objects if there are any
            if (!infiniteObjectGenerator.showStartupObjects(false))
                infiniteObjectGenerator.spawnObjectRun(false);
        }
        infiniteObjectGenerator.readyFromReset();

        if (start)
            startGame(true);
	}

    public void backToMainMenu(bool restart)
	{
        if (gamePaused) {
            if (onPauseGame != null)
                onPauseGame(false);
            gameOver(GameOverType.Quit, false);
        }

        if (restart)
            restartGame(false);
		guiManager.showGUI(GUIState.MainMenu);
	}

    // activate/deactivate the character when going into the store. The GUIManager will manage the preview
    public void showStore(bool show)
    {
        // ensure the correct character is used
        if (!show) {
            spawnCharacter();
        }
#if UNITY_3_5
        character.SetActiveRecursively(!show);
#else
        InfiniteRunnerStarterPackUtility.ActiveRecursively(character.transform, !show);
#endif
    }
	
	public void pauseGame(bool pause)
    {
        guiManager.showGUI(pause ? GUIState.Pause : GUIState.InGame);
        audioManager.playBackgroundMusic(!pause);
		if (onPauseGame != null)
            onPauseGame(pause);
        inputController.enabled = !pause;
        gamePaused = pause;
	}
	
	public void upgradePowerUp(PowerUpTypes powerUpType)
	{
        // Can't upgrade if the player can't afford the power up
        int cost = dataManager.getPowerUpCost(powerUpType);
        if (dataManager.getTotalCoins() < cost) {
            return;
        }
		dataManager.upgradePowerUp(powerUpType);
        dataManager.adjustTotalCoins(-cost);
	}

    public void selectCharacter(int character)
    {
        int characterCost = dataManager.getCharacterCost(character);
        if (characterCost == -1) { // can only select a character if it has been purchased
            if (dataManager.getSelectedCharacter() != character) {
                dataManager.setSelectedCharacter(character);
            }
        }
    }

    public void purchaseCharacter(int character)
    {
        int cost = dataManager.getCharacterCost(character);
        if (dataManager.getTotalCoins() < cost) {
            return;
        }
        dataManager.purchaseCharacter(character);
        dataManager.setSelectedCharacter(character);
        dataManager.adjustTotalCoins(-cost);
    }
	
	public void OnApplicationPause(bool pause)
	{
        if (gamePaused)
            return;

		if (onPauseGame != null)
			onPauseGame(pause);
	}
}
