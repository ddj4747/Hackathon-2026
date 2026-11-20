using UnityEngine;

/// <summary>
/// Frame animation and facing for the player. Plays <see cref="walkFrames"/> while the player walks and
/// <see cref="pushFrames"/> while it is pushing objects. When the player stands still the animation
/// freezes on the last frame shown. The sprites face one way only (up); the player is rotated
/// to face the direction it moves in.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(PlayerGridMovement))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    [Tooltip("Frames played while walking (sheet frames 4-7).")]
    [SerializeField] private Sprite[] walkFrames;

    [Tooltip("Frames played while pushing objects (sheet frames 0-3).")]
    [SerializeField] private Sprite[] pushFrames;

    [Tooltip("Animation speed while moving, in frames per second.")]
    [SerializeField, Min(0.1f)] private float framesPerSecond = 12f;

    [Tooltip("Extra rotation in degrees, if the sprite does not face up in the sheet.")]
    [SerializeField] private float rotationOffset;

    private SpriteRenderer spriteRenderer;
    private PlayerGridMovement movement;
    private Sprite[] currentSet;
    private int frame;
    private float timer;
    private bool wasMoving;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        movement = GetComponent<PlayerGridMovement>();
        currentSet = walkFrames;
        Show();
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, FacingAngle(movement.Facing) + rotationOffset);

        bool moving = movement.IsMoving;
        if (moving)
        {
            Sprite[] set = movement.IsPushing ? pushFrames : walkFrames;
            if (set != currentSet)
            {
                // Switched between walking and pushing: restart on the new set's first frame.
                currentSet = set;
                frame = 0;
                timer = 0f;
            }
            else
            {
                // On the first moving frame advance at once so the start of a step feels responsive.
                timer = wasMoving ? timer + framesPerSecond * Time.deltaTime : 1f;
                while (timer >= 1f)
                {
                    timer -= 1f;
                    frame++;
                }
            }
            Show();
        }
        wasMoving = moving;   // standing still: keep the last frame
    }

    private void Show()
    {
        if (currentSet == null || currentSet.Length == 0)
            return;
        frame %= currentSet.Length;
        spriteRenderer.sprite = currentSet[frame];
    }

    /// <summary>Angle that turns an up-facing sprite toward a direction.</summary>
    private static float FacingAngle(Vector3Int dir)
    {
        if (dir == Vector3Int.left) return 90f;
        if (dir == Vector3Int.down) return 180f;
        if (dir == Vector3Int.right) return -90f;
        return 0f;
    }
}
