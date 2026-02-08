using UnityEngine;
using System.Collections;

public class PlayerPiece : MonoBehaviour
{
    public float moveSpeed = 3f;
    public Animator animator;

    public IEnumerator MoveToCell(Vector3 targetPos)
    {
        animator.SetBool("isMoving", true);

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPos;
        animator.SetBool("isMoving", false);
    }
    
}