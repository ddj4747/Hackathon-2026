using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Repeater), typeof(SpriteRenderer))]
public sealed class RepeaterVisual : MonoBehaviour
{
    [SerializeField] private Sprite unsuppliedSprite;
    [SerializeField] private Sprite suppliedSprite;

    private Repeater repeater;
    private SpriteRenderer spriteRenderer;

    private void OnEnable()
    {
        repeater = GetComponent<Repeater>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        Refresh();
    }

    private void Update() => Refresh();

    private void Refresh()
    {
        bool playing = Application.IsPlaying(gameObject);
        // In the editor the Tilemap draws its preview; in Play mode the prefab draws itself.
        spriteRenderer.enabled = playing || GetComponentInParent<Tilemap>() == null;
        var sprite = playing && repeater.IsSignalSupplied ? suppliedSprite : unsuppliedSprite;
        if (spriteRenderer.sprite != sprite)
            spriteRenderer.sprite = sprite;
    }
}
