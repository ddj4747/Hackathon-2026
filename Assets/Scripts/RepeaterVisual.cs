using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Repeater), typeof(SpriteRenderer))]
public sealed class RepeaterVisual : MonoBehaviour
{
    [SerializeField] private Sprite unsuppliedSprite;
    [SerializeField] private Sprite suppliedSprite;

    private Repeater repeater;
    private SpriteRenderer spriteRenderer;
    private static Dictionary<string, Sprite> redSprites;

    private void OnEnable()
    {
        repeater = GetComponent<Repeater>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        Refresh();
    }

    private void Update() => Refresh();

    private void OnDisable() => SignalTypeVisual.Restore(spriteRenderer);

    private void Refresh()
    {
        SignalTypeVisual.Restore(spriteRenderer);
        bool playing = Application.IsPlaying(gameObject);
        // In the editor the Tilemap draws its preview; in Play mode the prefab draws itself.
        spriteRenderer.enabled = playing || GetComponentInParent<Tilemap>() == null;
        var sprite = playing && repeater.IsSignalSupplied ? suppliedSprite : unsuppliedSprite;
        if (repeater.Type == SignalType.Red && sprite != null)
        {
            if (redSprites == null)
            {
                redSprites = new Dictionary<string, Sprite>();
                foreach (var candidate in Resources.LoadAll<Sprite>("RedRepeaters"))
                    redSprites[candidate.name] = candidate;
            }
            if (redSprites.TryGetValue(sprite.name, out var redSprite))
                sprite = redSprite;
        }
        if (spriteRenderer.sprite != sprite)
            spriteRenderer.sprite = sprite;
    }
}
