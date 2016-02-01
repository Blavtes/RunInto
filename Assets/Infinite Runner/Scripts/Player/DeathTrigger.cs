using UnityEngine;
using System.Collections;

// The player feel into a bad area. Die.
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class DeathTrigger : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player")) {
            GameManager.instance.gameOver(GameOverType.Pit, true);
        }
    }
}
