using UnityEngine;
using System.Collections;

/*
 * The input controller is a singleton class which interperates the input (from a keyboard or touch) and passes
 * it to the player controller
 */
public class InputController : MonoBehaviour
{

    static public InputController instance;

#if UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8
    // change slots with a swipe (true) or the accelerometer (false)
    public bool swipeToChangeSlots = false;
    // The number of pixels you must swipe in order to register a horizontal or vertical swipe
    public Vector2 swipeDistance = new Vector2(40, 40);
    // How sensitive the horizontal and vertical swipe are. The higher the value the more it takes to activate a swipe
    public float swipeSensitivty = 2;
    // More than this value and the player will move into the rightmost slot.
    // Less than the negative of this value and the player will move into the leftmost slot.
    // The accelerometer value in between these two values equals the middle slot.
    public float accelerometerRightSlotValue = 0.25f;
    // the higher the value the less likely the player will switch slots
    public float accelerometerSensitivity = 0.1f;
    private Vector2 touchStartPosition;
    private bool acceptInput; // ensure that only one action is performed per touch gesture
#endif
#if UNITY_EDITOR || !(UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8)
    public bool sameKeyForTurnChangeSlots = false;
    public bool moveMouseToChangeSlots = true;
    // Allow slot changes by moving the mouse left or right
    public float mouseXDeltaValue = 100f;
    private float mouseStartPosition;
    private float mouseStartTime;
#endif

    private PlayerController playerController;

    public void Awake()
    {
        instance = this;
    }

    public void startGame()
    {
        playerController = PlayerController.instance;

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8)
        touchStartPosition = Vector2.zero;
#else
        mouseStartPosition = Input.mousePosition.x;
        mouseStartTime = Time.time;
#endif

        enabled = true;
#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8)
        acceptInput = true;
#endif
    }

    public void gameOver()
    {
        enabled = false;
    }

    public void Update()
    {
#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8)
        if (Input.touchCount == 1) {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) {
                touchStartPosition = touch.position;
            } else if (touch.phase == TouchPhase.Moved && acceptInput) {
                Vector2 diff = touch.position - touchStartPosition;
                if (diff.x == 0f)
                    diff.x = 1f; // avoid divide by zero
                float verticalPercent = Mathf.Abs(diff.y / diff.x);

                if (verticalPercent > swipeSensitivty && Mathf.Abs(diff.y) > swipeDistance.y) {
                    if (diff.y > 0) {
                        playerController.jump(false);
                        acceptInput = false;
                    } else if (diff.y < 0) {
                        playerController.slide();
                        acceptInput = false;
                    }
                    touchStartPosition = touch.position;
                } else if (verticalPercent < (1 / swipeSensitivty) && Mathf.Abs(diff.x) > swipeDistance.x) {
                    // turn if above a turn, otherwise change slots
                    if (swipeToChangeSlots) {
                        if (playerController.abovePlatform(true)) {
                            playerController.turn(diff.x > 0 ? true : false, true);
                        } else {
                            playerController.changeSlots(diff.x > 0 ? true : false);
                        }
                    } else {
                        playerController.turn(diff.x > 0 ? true : false, true);
                    }
                    acceptInput = false;
                }
            } else if (touch.phase == TouchPhase.Stationary) {
                acceptInput = true;
            } else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
                if ((touch.position - touchStartPosition).sqrMagnitude < 100 && acceptInput) {
                    playerController.attack();
                }
                acceptInput = true;
            }
        }

        if (!swipeToChangeSlots)
            checkSlotPosition(Input.acceleration.x);
#else
        if (sameKeyForTurnChangeSlots) {
            bool hasTurned = false;
            if (Input.GetButtonUp("LeftTurn")) {
                hasTurned = playerController.turn(false, true);
            } else if (Input.GetButtonUp("RightTurn")) {
                hasTurned = playerController.turn(true, true);
            }

            // can change slots if the player hasn't turned
            if (!hasTurned) {
                if (Input.GetButtonUp("LeftSlot")) {
                    playerController.changeSlots(false);
                } else if (Input.GetButtonUp("RightSlot")) {
                    playerController.changeSlots(true);
                }
            }
        } else {
            if (Input.GetButtonUp("LeftSlot")) {
                playerController.changeSlots(false);
            } else if (Input.GetButtonUp("RightSlot")) {
                playerController.changeSlots(true);
            }

            if (Input.GetButtonUp("LeftTurn")) {
                playerController.turn(false, true);
            } else if (Input.GetButtonUp("RightTurn")) {
                playerController.turn(true, true);
            }
        }

        if (Input.GetButtonUp("Jump")) {
            playerController.jump(false);
        } else if (Input.GetButtonUp("Slide")) {
            playerController.slide();
        } else if (Input.GetButtonUp("Attack")) {
            playerController.attack();
        }

        // Change slots if the player moves their mouse more than mouseXDeltaValue within a specified amount of time
        if (moveMouseToChangeSlots) {
            if (Input.mousePosition.x != mouseStartPosition) {
                if (Time.time - mouseStartTime < 0.5f) {
                    float delta = Input.mousePosition.x - mouseStartPosition;
                    bool reset = false;
                    if (delta > mouseXDeltaValue) {
                        playerController.changeSlots(true);
                        reset = true;
                    } else if (delta < -mouseXDeltaValue) {
                        playerController.changeSlots(false);
                        reset = true;
                    }
                    if (reset) {
                        mouseStartTime = Time.time;
                        mouseStartPosition = Input.mousePosition.x;
                    }
                } else {
                    mouseStartTime = Time.time;
                    mouseStartPosition = Input.mousePosition.x;
                }
            }
        }
#endif
    }

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID || UNITY_BLACKBERRY || UNITY_WP8)
    private void checkSlotPosition(float tiltValue)
    {
        SlotPosition currentSlot = playerController.getCurrentSlotPosition();
        switch (currentSlot) {
            case SlotPosition.Center:
                if (tiltValue < -accelerometerRightSlotValue) {
                    playerController.changeSlots(SlotPosition.Left);
                } else if (tiltValue > accelerometerRightSlotValue) {
                    playerController.changeSlots(SlotPosition.Right);
                }
                break;
            case SlotPosition.Left:
                if (tiltValue > -accelerometerRightSlotValue + accelerometerSensitivity) {
                    playerController.changeSlots(SlotPosition.Center);
                }
                break;
            case SlotPosition.Right:
                if (tiltValue < accelerometerRightSlotValue - accelerometerSensitivity) {
                    playerController.changeSlots(SlotPosition.Center);
                }
                break;
        }
    }
#endif
}

