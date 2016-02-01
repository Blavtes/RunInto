using UnityEngine;
using System.Collections;

/*
 * Basic class to manage the different animation states
 */
public class PlayerAnimation : MonoBehaviour
{
#if !UNITY_3_5
    public bool useMecanim = false;
#endif

    // the amount of time it takes to transition from the previous animation to a run
    public float runTransitionTime;

    // animation names
    public string runAnimationName = "Run";
    public string runJumpAnimationName = "RunJump";
    public string runSlideAnimationName = "RunSlide";
    public string runRightStrafeAnimationName = "RunRightStrafe";
    public string runLeftStrafeAnimationName = "RunLeftStrafe";
    public string attackAnimationName = "Attack";
    public string backwardDeathAnimationName = "BackwardDeath";
    public string forwardDeathAnimationName = "ForwardDeath";
    public string idleAnimationName = "Idle";

    // the speed of the run animation when the player is running
    public float slowRunSpeed = 1.0f;
    public float fastRunSpeed = 1.0f;

    private Animation thisAnimation;
#if !UNITY_3_5
    private Animator thisAnimator;
#endif

    public void init()
    {
#if !UNITY_3_5
        if (useMecanim) {
            thisAnimator = GetComponent<Animator>();
        } else {
#endif
            thisAnimation = animation;

            thisAnimation[runAnimationName].wrapMode = WrapMode.Loop;
            thisAnimation[runAnimationName].layer = 0;
            thisAnimation[runRightStrafeAnimationName].wrapMode = WrapMode.Once;
            thisAnimation[runRightStrafeAnimationName].layer = 1;
            thisAnimation[runLeftStrafeAnimationName].wrapMode = WrapMode.Once;
            thisAnimation[runLeftStrafeAnimationName].layer = 1;
            if (!runJumpAnimationName.Equals("")) {
                thisAnimation[runJumpAnimationName].wrapMode = WrapMode.ClampForever;
                thisAnimation[runJumpAnimationName].layer = 1;
            }
            if (!runSlideAnimationName.Equals("")) {
                thisAnimation[runSlideAnimationName].wrapMode = WrapMode.ClampForever;
                thisAnimation[runSlideAnimationName].layer = 1;
            }
            if (!attackAnimationName.Equals("")) {
                thisAnimation[attackAnimationName].wrapMode = WrapMode.Once;
                thisAnimation[attackAnimationName].layer = 1;
            }
            if (!backwardDeathAnimationName.Equals("")) {
                thisAnimation[backwardDeathAnimationName].wrapMode = WrapMode.ClampForever;
                thisAnimation[backwardDeathAnimationName].layer = 2;
            }
            if (!forwardDeathAnimationName.Equals("")) {
                thisAnimation[forwardDeathAnimationName].wrapMode = WrapMode.ClampForever;
                thisAnimation[forwardDeathAnimationName].layer = 2;
            }
            thisAnimation[idleAnimationName].wrapMode = WrapMode.Loop;
            thisAnimation[idleAnimationName].layer = 3;
#if !UNITY_3_5
        }
#endif
    }

    public void OnEnable()
    {
        GameManager.instance.onPauseGame += onPauseGame;
    }

    public void OnDisable()
    {
        GameManager.instance.onPauseGame -= onPauseGame;
    }

    public void run()
    {
#if !UNITY_3_5
        if (!useMecanim) {
#endif
            thisAnimation.CrossFade(runAnimationName, runTransitionTime, PlayMode.StopAll);
#if !UNITY_3_5
        }
#endif
    }

    public void setRunSpeed(float speed, float t)
    {
#if !UNITY_3_5
        if (useMecanim) {
            thisAnimator.SetFloat("Speed", speed);
        } else {
#endif
            thisAnimation[runAnimationName].speed = Mathf.Lerp(slowRunSpeed, fastRunSpeed, t);
#if !UNITY_3_5
        }
#endif
    }

    public void strafe(bool right)
    {
#if !UNITY_3_5
        if (useMecanim) {
            if (right) {
                StartCoroutine(playOnce(runRightStrafeAnimationName));
            } else {
                StartCoroutine(playOnce(runLeftStrafeAnimationName));
            }
        } else {
#endif
            if (right) {
                thisAnimation.CrossFade(runRightStrafeAnimationName, 0.05f);
            } else {
                thisAnimation.CrossFade(runLeftStrafeAnimationName, 0.05f);
            }
#if !UNITY_3_5
        }
#endif
    }

    public void jump()
    {
#if !UNITY_3_5
        if (useMecanim) {
            StartCoroutine(playOnce(runJumpAnimationName));
        } else {
#endif
            thisAnimation.CrossFade(runJumpAnimationName, 0.1f);
#if !UNITY_3_5
        }
#endif
    }

    public void slide()
    {
#if !UNITY_3_5
        if (useMecanim) {
            StartCoroutine(playOnce(runSlideAnimationName));
        } else {
#endif
            thisAnimation.CrossFade(runSlideAnimationName);
#if !UNITY_3_5
        }
#endif
    }

    public void attack()
    {
#if !UNITY_3_5
        if (useMecanim) {
            StartCoroutine(playOnce(attackAnimationName));
        } else {
#endif
            thisAnimation.CrossFade(attackAnimationName, 0.1f);
#if !UNITY_3_5
        }
#endif
    }

    public void idle()
    {
#if !UNITY_3_5
        if (!useMecanim) {
#endif
            if (thisAnimation == null) {
                thisAnimation = animation;
                thisAnimation[idleAnimationName].wrapMode = WrapMode.Loop;
            }
            thisAnimation.Play(idleAnimationName);
#if !UNITY_3_5
        }
#endif
    }

    public void gameOver(GameOverType gameOverType)
    {
#if !UNITY_3_5
        if (!useMecanim) {
#endif
            thisAnimation.Stop(runAnimationName);
#if !UNITY_3_5
        }
#endif

        if (gameOverType != GameOverType.Quit) {
            if (gameOverType == GameOverType.JumpObstacle) {
#if !UNITY_3_5
                if (useMecanim) {
                    StartCoroutine(playOnce(forwardDeathAnimationName));
                } else {
#endif
                    thisAnimation.Play(forwardDeathAnimationName);
#if !UNITY_3_5
                }
#endif
            } else {
#if !UNITY_3_5
                if (useMecanim) {
                    StartCoroutine(playOnce(backwardDeathAnimationName));
                } else {
#endif
                    thisAnimation.Play(backwardDeathAnimationName);
#if !UNITY_3_5
                }
#endif
            }
        }
    }

    public void reset()
    {
#if !UNITY_3_5
        if (useMecanim) {
            thisAnimator.SetFloat("Speed", 0);
        } else {
#endif
            thisAnimation.Play(idleAnimationName);
#if !UNITY_3_5
        }
#endif
    }

    public void onPauseGame(bool paused)
    {
        float speed = (paused ? 0 : 1);
#if !UNITY_3_5
        if (useMecanim) {
            thisAnimator.speed = speed;
        } else {
#endif
            foreach (AnimationState state in thisAnimation) {
                state.speed = speed;
            }
#if !UNITY_3_5
        }
#endif
    }

#if !UNITY_3_5
    public IEnumerator playOnce(string eventName)
    {
        thisAnimator.SetBool(eventName, true);
        yield return null;
        thisAnimator.SetBool(eventName, false);
    }
#endif
}
