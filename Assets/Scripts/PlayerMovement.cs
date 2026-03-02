using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float repeatDelay = 0.15f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask obstacleLayer;

    private bool isMoving = false;
    private float nextMoveTime;

    private void Start()
    {
        SnapToGrid();
    }

    private void Update()
    {
        if (isMoving) return;

        Vector2 input = GetHeldInput();

        if (input != Vector2.zero && Time.time >= nextMoveTime)
        {
            Vector3 targetPosition = transform.position +
                                     new Vector3(input.x, input.y, 0) * tileSize;

            if (CanMoveTo(targetPosition))
            {
                nextMoveTime = Time.time + repeatDelay;
                StartCoroutine(MoveTo(targetPosition));
            }
        }
    }

    private Vector2 GetHeldInput()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            return Vector2.up;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            return Vector2.down;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            return Vector2.left;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            return Vector2.right;

        return Vector2.zero;
    }

    private float GetCurrentSpeed()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            ? sprintSpeed
            : moveSpeed;
    }

    private bool CanMoveTo(Vector3 targetPosition)
    {
        Collider2D hit = Physics2D.OverlapBox(
            targetPosition,
            Vector2.one * (tileSize * 0.8f),
            0f,
            obstacleLayer
        );

        return hit == null;
    }

    private IEnumerator MoveTo(Vector3 targetPosition)
    {
        isMoving = true;

        while ((targetPosition - transform.position).sqrMagnitude > 0.001f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                GetCurrentSpeed() * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;
    }

    private void SnapToGrid()
    {
        Vector3 pos = transform.position;

        float snappedX = Mathf.Floor(pos.x / tileSize) * tileSize + tileSize / 2f;
        float snappedY = Mathf.Floor(pos.y / tileSize) * tileSize + tileSize / 2f;

        transform.position = new Vector3(snappedX, snappedY, pos.z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            transform.position,
            Vector3.one * (tileSize * 0.8f)
        );
    }
}