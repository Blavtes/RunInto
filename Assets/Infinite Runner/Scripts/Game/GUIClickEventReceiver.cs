using UnityEngine;
using System.Collections;

/*
 * The user pressed a button, perform some action
 */
public enum ClickType { StartGame, Stats, Store, EndGame, Restart, MainMenu, MainMenuRestart, Pause, Resume, ToggleTutorial, Missions, 
                        StorePurchase, StoreNext, StorePrevious, StoreTogglePowerUps, MainMenuFromStore, EndGameFromStore, Facebook, Twitter }
public class GUIClickEventReceiver : MonoBehaviour {
	
	public ClickType clickType;
	
	public void OnClick()
	{
        bool playSoundEffect = true;
		switch (clickType) {
		case ClickType.StartGame:
			GameManager.instance.startGame(false);
			break;
		case ClickType.Store:
            GameManager.instance.showStore(true);
			GUIManager.instance.showGUI(GUIState.Store);
			break;
		case ClickType.Stats:
			GUIManager.instance.showGUI(GUIState.Stats);
			break;
		case ClickType.EndGame:
			GUIManager.instance.showGUI(GUIState.EndGame);
			break;
		case ClickType.Restart:
			GameManager.instance.restartGame(true);
            break;
        case ClickType.MainMenu:
            GameManager.instance.backToMainMenu(false);
            break;
		case ClickType.MainMenuRestart:
			GameManager.instance.backToMainMenu(true);
			break;
		case ClickType.Pause:
			GameManager.instance.pauseGame(true);
            playSoundEffect = false;
			break;
		case ClickType.Resume:
			GameManager.instance.pauseGame(false);
			break;
        case ClickType.ToggleTutorial:
            GameManager.instance.toggleTutorial();
            break;
        case ClickType.Missions:
            GUIManager.instance.showGUI(GUIState.Missions);
            break;
        case ClickType.StoreNext:
            GUIManager.instance.rotateStoreItem(true);
            break;
        case ClickType.StorePrevious:
            GUIManager.instance.rotateStoreItem(false);
            break;
        case ClickType.StorePurchase:
            GUIManager.instance.purchaseStoreItem();
            break;
        case ClickType.StoreTogglePowerUps:
            GUIManager.instance.togglePowerUpVisiblity();
            break;
        case ClickType.MainMenuFromStore:
            GameManager.instance.showStore(false);
            GUIManager.instance.removeStoreItemPreview();
            GameManager.instance.backToMainMenu(false);
            break;
        case ClickType.EndGameFromStore:
            GameManager.instance.showStore(false);
            GUIManager.instance.removeStoreItemPreview();
            GUIManager.instance.showGUI(GUIState.EndGame);
            break;
        case ClickType.Facebook:
            SocialManager.instance.openFacebook();
            break;
        case ClickType.Twitter:
            SocialManager.instance.openTwitter();
            break;
        }

        if (playSoundEffect)
            AudioManager.instance.playSoundEffect(SoundEffects.GUITapSoundEffect);
	}
}
