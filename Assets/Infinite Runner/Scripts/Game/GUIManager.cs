using UnityEngine;
using System.Collections;

/*
 * The GUI manager is a singleton class which manages the NGUI objects
 */
public enum GUIState { MainMenu, InGame, EndGame, Store, Stats, Pause, Tutorial, Missions, Inactive }
public enum TutorialType { Jump, Slide, Strafe, Attack, Turn, GoodLuck }
public class GUIManager : MonoBehaviour {
	
	static public GUIManager instance;

    public GameObject mainMenuPanel;
    public GameObject logoPanel;
    public GameObject inGameLeftPanel;
    public GameObject inGameTopPanel;
    public GameObject inGameRightPanel;
	public GameObject endGamePanel;
    public GameObject storePanel;
    public GameObject statsPanel;
    public GameObject missionsPanel;
    public GameObject pausePanel;
    public GameObject tutorialPanel;
	
	// in game:
	public UILabel inGameScore;
    public UILabel inGameCoins;
    public Animation[] inGamePlayAnimation;
    public string inGamePlayAnimationName;
    public UISprite inGameActivePowerUp;
    public Renderer inGameActivePowerUpCutoff;
    public Material inGameActivePowerUpCutoffMaterial;
    private WaitForSeconds inGamePowerUpProgressWaitForSeconds;
    private CoroutineData inGamePowerUpData;
    private bool powerUpActive;
	
    // pause:
    public UILabel pauseScore;
    public UILabel pauseCoins;

	// end game:
	public UILabel endGameScore;
    public UILabel endGameCoins;
    public UILabel endGameMultiplier;
    public Animation endGamePlayAnimation;
    public string endGamePlayAnimationName;

    // store:
    public GameObject storeBackToMainMenuButton;
    public GameObject storeBackToEndGameButton;
    public UILabel storePowerUpSelectionButton;
    public UILabel storeTitle;
    public UILabel storeDescription;
    public UILabel storeCoins;
    public UIButton storeBuyButton;
    private bool storeSelectingPowerUp;
    private int storeItemIndex;

    public Transform storePowerUpPreviewTransform;
    public Transform storeCharacterPreviewTransform;
    private GameObject storeItemPreview;

	// stats:
	public UILabel statsHighScore;
	public UILabel statsCoins;
	public UILabel statsPlayCount;

    // tutorial:
    public UILabel tutorialLabel;

    // missions:
    public GameObject missionsBackToMainMenuButton;
    public GameObject missionsBackToEndGameButton;
    public UILabel missionsScoreMultiplier;
    public UILabel missionsActiveMission1;
    public UILabel missionsActiveMission2;
    public UILabel missionsActiveMission3;
	
	private GUIState guiState;

    private GameManager gameManager;
	private DataManager dataManager;
    private MissionManager missionManager;
    private CoinGUICollection coinGUICollection;

	public void Awake()
	{
		instance = this;	
	}
	
	public void Start ()
	{
        gameManager = GameManager.instance;
		dataManager = DataManager.instance;
        missionManager = MissionManager.instance;
        coinGUICollection = CoinGUICollection.instance;
		
		guiState = GUIState.MainMenu;
        inGamePowerUpData = new CoroutineData();
        gameManager.onPauseGame += gamePaused;	

		// hide everything except the main menu
#if UNITY_3_5
        mainMenuPanel.SetActiveRecursively(true);
        logoPanel.SetActiveRecursively(true);
        inGameLeftPanel.SetActiveRecursively(false);
		inGameTopPanel.SetActiveRecursively(false);
		inGameRightPanel.SetActiveRecursively(false);
		endGamePanel.SetActiveRecursively(false);
        storePanel.SetActiveRecursively(false);
		statsPanel.SetActiveRecursively(false);
        missionsPanel.SetActiveRecursively(false);
        pausePanel.SetActiveRecursively(false);
        tutorialPanel.SetActiveRecursively(false);
#else
        InfiniteRunnerStarterPackUtility.ActiveRecursively(mainMenuPanel.transform, true);
        InfiniteRunnerStarterPackUtility.ActiveRecursively(logoPanel.transform, true);
        InfiniteRunnerStarterPackUtility.ActiveRecursively(inGameLeftPanel.transform, false);
        InfiniteRunnerStarterPackUtility.ActiveRecursively(inGameTopPanel.transform, false);
        InfiniteRunnerStarterPackUtility.ActiveRecursively(inGameRightPanel.transform, false);
        InfiniteRunnerStarterPackUtility.ActiveRecursively(endGamePanel.transform, false);
        InfiniteRunnerStarterPackUtility.ActiveRecursively(storePanel.transform, false);
        InfiniteRunnerStarterPackUtility.ActiveRecursively(statsPanel.transform, false);
        InfiniteRunnerStarterPackUtility.ActiveRecursively(missionsPanel.transform, false);
        InfiniteRunnerStarterPackUtility.ActiveRecursively(pausePanel.transform, false);
        InfiniteRunnerStarterPackUtility.ActiveRecursively(tutorialPanel.transform, false);
#endif
    }

    private void activateObject(GameObject obj, bool activate)
    {
#if UNITY_3_5
        obj.SetActiveRecursively(activate);
#else
        InfiniteRunnerStarterPackUtility.ActiveRecursively(obj.transform, activate);
#endif
    }
    	
	public void showGUI(GUIState state)
	{		
		switch (state) {
        case GUIState.InGame:
            inGameActivePowerUp.enabled = powerUpActive;
            inGameActivePowerUpCutoff.enabled = powerUpActive;
            break;
		case GUIState.EndGame:
			endGameScore.text = dataManager.getScore().ToString();
			endGameCoins.text = dataManager.getLevelCoins().ToString();
            endGameMultiplier.text = string.Format("{0}x", missionManager.getScoreMultiplier());

            // only need to show the animation if we are coming from in game
            if (guiState == GUIState.InGame) {
                activateObject(endGamePanel, true);
                
                endGamePlayAnimation.enabled = true;
                endGamePlayAnimation.Stop();
                endGamePlayAnimation[endGamePlayAnimationName].speed = 1;
                endGamePlayAnimation.Play(endGamePlayAnimationName);
                for (int i = 0; i < inGamePlayAnimation.Length; ++i) {
                    inGamePlayAnimation[i].Stop();
                    inGamePlayAnimation[i][inGamePlayAnimationName].speed = 1;
                    inGamePlayAnimation[i].enabled = true;
                    inGamePlayAnimation[i].Play(inGamePlayAnimationName);
                }
                // NGUI 2 uses UIButtonPlayAnimation. NGUI 3 uses UIPlayAnimation. When the NGUI 3 version of the play animation activates it automatically disables
                // the animation component. We don't want that so we are just going to disable the NGUI component.
                System.Type playAnimationType = Types.GetType("UIPlayAnimation", "Assembly-CSharp");
                if (playAnimationType != null) {
                    var playAnimations = endGamePanel.GetComponentsInChildren(playAnimationType);
                    for (int i = 0; i < playAnimations.Length; ++i) {
                        ((MonoBehaviour)playAnimations[i]).enabled = false;
                    }
                    // enable the buttons again after the animation is done playing
                    StartCoroutine(enableComponents(endGamePlayAnimation[endGamePlayAnimationName].length, playAnimations));
                }
            }
			break;
			
		case GUIState.Store:			
			// go back to the correct menu that we came from
            if (guiState == GUIState.MainMenu) {
                activateObject(storeBackToEndGameButton, false);
                activateObject(storeBackToMainMenuButton, true);
            } else if (guiState == GUIState.EndGame) {
                activateObject(storeBackToMainMenuButton, false);
                activateObject(storeBackToEndGameButton, true);
            }
            storeSelectingPowerUp = false;
            storeItemIndex = dataManager.getSelectedCharacter();
            refreshStoreGUI();
            refreshStoreItem();
			break;

        case GUIState.Pause:
			pauseScore.text = dataManager.getScore().ToString();
			pauseCoins.text = (dataManager.getLevelCoins() + coinGUICollection.getAnimatingCoins()).ToString();
            break;
			
		case GUIState.Stats:
			statsHighScore.text = dataManager.getHighScore().ToString();
			statsCoins.text = dataManager.getTotalCoins().ToString();
			statsPlayCount.text = dataManager.getPlayCount().ToString();
			break;

        case GUIState.Missions:
            if (guiState == GUIState.MainMenu) {
                activateObject(missionsBackToEndGameButton, false);
            } else { // coming from GUIState.EndGame
                activateObject(missionsBackToMainMenuButton, false);
            }
            missionsScoreMultiplier.text = string.Format("{0}x", missionManager.getScoreMultiplier());
            missionsActiveMission1.text = dataManager.getMissionDescription(missionManager.getMission(0));
            missionsActiveMission2.text = dataManager.getMissionDescription(missionManager.getMission(1));
            missionsActiveMission3.text = dataManager.getMissionDescription(missionManager.getMission(2));

            break;
        }

		guiState = state;
	}

    private IEnumerator enableComponents(float length, Component[] components)
    {
        yield return new WaitForSeconds(length);

        for (int i = 0; i < components.Length; ++i) {
            ((MonoBehaviour)components[i]).enabled = true;
        }
    }

    public void setInGameScore(int score)
	{
		inGameScore.text = score.ToString();
	}
	
	public void setInGameCoinCount(int coins)
	{
		inGameCoins.text = coins.ToString();
	}

    public void activatePowerUp(PowerUpTypes powerUpType, bool active, float length)
    {
        if (active) {
            inGameActivePowerUpCutoffMaterial.SetFloat("_Cutoff", 0.0f);
            inGameActivePowerUp.spriteName = powerUpType.ToString();

            if (inGamePowerUpProgressWaitForSeconds == null) {
                inGamePowerUpProgressWaitForSeconds = new WaitForSeconds(0.05f);
            }

            inGamePowerUpData.duration = length;
            StartCoroutine("updatePowerUpProgress");
        } else {
            StopCoroutine("updatePowerUpProgress");
        }
        inGameActivePowerUp.enabled = active;
        inGameActivePowerUpCutoff.enabled = active;
        powerUpActive = active;
    }

    private IEnumerator updatePowerUpProgress()
    {
        inGamePowerUpData.startTime = Time.time;
        float cutoff = inGameActivePowerUpCutoffMaterial.GetFloat("_Cutoff");
        float cutoffStep = 0.05f / inGamePowerUpData.duration;
        while (cutoff < 1) {
            cutoff += cutoffStep;
            inGameActivePowerUpCutoffMaterial.SetFloat("_Cutoff", cutoff);
            yield return inGamePowerUpProgressWaitForSeconds;
        }
        inGameActivePowerUpCutoff.enabled = false;
    }

    public void showTutorial(bool show, TutorialType tutorial)
    {
        activateObject(tutorialPanel, show);
        if (show) {
            switch (tutorial) {
                case TutorialType.Jump:
#if (UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8)
                    tutorialLabel.text = "Swipe up to jump";
#else
                    tutorialLabel.text = "Press the up arrow\nto jump";
#endif
                    break;
                case TutorialType.Slide:
#if (UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8)
                    tutorialLabel.text = "Swipe down to slide";
#else
                    tutorialLabel.text = "Press the down arrow\nto slide";
#endif
                    break;
                case TutorialType.Strafe:
#if (UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8)
                    tutorialLabel.text = "Tilt to turn";
#else
                    tutorialLabel.text = "Left and right arrows\nwill change slots";
#endif
                    break;
                case TutorialType.Attack:
#if (UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8)
                    tutorialLabel.text = "Tap to attack";
#else
                    tutorialLabel.text = "Attack with the\nleft mouse button";
#endif
                    break;
                case TutorialType.Turn:
#if (UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8)
                    tutorialLabel.text = "Swipe left or right\nto turn";
#else
                    tutorialLabel.text = "Left and right arrows\nwill also turn";
#endif
                    break;
                case TutorialType.GoodLuck:
                    tutorialLabel.text = "Good luck!";
                    break;
            }
        }
    }
	
    // something has changed (item change, purchase, etc). Need to refresh the gui text
	public void refreshStoreGUI()
    {
        int cost = -1;
        if (storeSelectingPowerUp) {
            PowerUpTypes powerUp = (PowerUpTypes)storeItemIndex;

            storeTitle.text = dataManager.getPowerUpTitle(powerUp);
            storeDescription.text = dataManager.getPowerUpDescription(powerUp);

            cost = dataManager.getPowerUpCost(powerUp);
            if (cost != -1) {
                activateWidget(storeBuyButton, true);
            } else {
                activateWidget(storeBuyButton, false);
            }
            storePowerUpSelectionButton.text = "Characters";
        } else { // characters
            storeTitle.text = dataManager.getCharacterTitle(storeItemIndex);
            storeDescription.text = dataManager.getCharacterDescription(storeItemIndex);

            cost = dataManager.getCharacterCost(storeItemIndex);
            if (cost != -1) {
                activateWidget(storeBuyButton, true);
            } else {
                activateWidget(storeBuyButton, false);
            }
            storePowerUpSelectionButton.text = "Power Ups";
        }

        storeCoins.text = string.Format("{0}  ({1} Coins Available)", (cost == -1 ? "Purchased" : string.Format("Cost: {0}", cost.ToString())), dataManager.getTotalCoins());
	}

    public void togglePowerUpVisiblity()
    {
        storeSelectingPowerUp = !storeSelectingPowerUp;
        storeItemIndex = 0;
        refreshStoreGUI();
        refreshStoreItem();
    }

    // rotate through showing a preview of the item
    public void rotateStoreItem(bool next)
    {
        if (storeSelectingPowerUp) {
            storeItemIndex = (storeItemIndex + (next ? 1 : -1)) % (int)PowerUpTypes.None;
            if (storeItemIndex < 0) {
                storeItemIndex = (int)PowerUpTypes.None - 1;
            }
        } else {
            storeItemIndex = (storeItemIndex + (next ? 1 : -1)) % dataManager.getCharacterCount();
            if (storeItemIndex < 0) {
                storeItemIndex = dataManager.getCharacterCount() - 1;
            }
            GameManager.instance.selectCharacter(storeItemIndex);
        }
        refreshStoreGUI();
        refreshStoreItem();
    }

    // show a preview of the new item
    private void refreshStoreItem()
    {
        if (storeItemPreview != null) {
            Destroy(storeItemPreview);
        }
        Transform activePreviewTransform = null;
        if (storeSelectingPowerUp) {
            storeItemPreview = GameObject.Instantiate(dataManager.getPowerUpPrefab((PowerUpTypes)storeItemIndex)) as GameObject;
            storeItemPreview.transform.localScale = Vector3.one;
            activePreviewTransform = storePowerUpPreviewTransform;
        } else {
            // don't want to override PlayerController.instance when the new player controller gets instantiated
            PlayerController playerController = PlayerController.instance;
            storeItemPreview = GameObject.Instantiate(dataManager.getCharacterPrefab(storeItemIndex)) as GameObject;
            storeItemPreview.GetComponent<PlayerController>().activatePowerUp(PowerUpTypes.CoinMagnet, false);
            PlayerController.instance = playerController;
            storeItemPreview.transform.localScale = storeCharacterPreviewTransform.localScale; // set the character to be the same scale as the preview transform
            activePreviewTransform = storeCharacterPreviewTransform;
        }
        storeItemPreview.transform.position = activePreviewTransform.position;
        storeItemPreview.transform.rotation = activePreviewTransform.rotation;

        // disable all of the componenets to prevent any scripts from running. Also set the layer so only the UI camera can see it
        int layer = LayerMask.NameToLayer("UI");
        storeItemPreview.gameObject.layer = layer;
        changeLayers(storeItemPreview.transform, layer);
        foreach (Behaviour childCompnent in storeItemPreview.GetComponentsInChildren<Behaviour>()) {
            childCompnent.enabled = false;
        }
        if (storeItemPreview.rigidbody) {
            storeItemPreview.rigidbody.isKinematic = true;
        }
        PlayerAnimation playerAnimation = null;
        if ((playerAnimation = storeItemPreview.GetComponent<PlayerAnimation>()) != null) {
#if !UNITY_3_5
            if (playerAnimation.useMecanim) {
                storeItemPreview.GetComponent<Animator>().enabled = true;
            } else {
#endif
            storeItemPreview.animation.enabled = true;
            playerAnimation.idle();
#if !UNITY_3_5
            }
#endif
        }
    }

    // recursively change all of the child layers
    private void changeLayers(Transform obj, int layer)
    {
        foreach (Transform child in obj) {
            child.gameObject.layer = layer;
            changeLayers(child, layer);
        }
    }

    public void purchaseStoreItem()
    {
        if (storeSelectingPowerUp) {
            gameManager.upgradePowerUp((PowerUpTypes)storeItemIndex);
        } else {
            gameManager.purchaseCharacter(storeItemIndex);
            gameManager.selectCharacter(storeItemIndex);
        }
        refreshStoreGUI();
    }

    public void removeStoreItemPreview()
    {
        if (storeItemPreview != null) {
            Destroy(storeItemPreview);
        }
    }

    public void gamePaused(bool paused)
    {
        if (powerUpActive) {
            if (paused) {
                StopCoroutine("updatePowerUpProgress");
                inGamePowerUpData.calcuateNewDuration();
            } else {
                StartCoroutine("updatePowerUpProgress");
            }
        }
    }

    public void gameOver()
    {
#if UNITY_3_5
        if (tutorialPanel.active) {
            tutorialPanel.active = false;
        }
#else
		if (tutorialPanel.activeSelf) {
            tutorialPanel.SetActive(false);
        }
#endif
    }

    private void activateWidget(Behaviour widget, bool activate)
    {
#if UNITY_3_5
        widget.gameObject.SetActiveRecursively(activate);
#else
        widget.gameObject.SetActive(activate);
#endif
    }
}
