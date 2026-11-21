using UnityEngine;

/// <summary>Displays ground cable artwork according to its source level's saved completion.</summary>
[DisallowMultipleComponent]
public sealed class LevelSelectCable : MonoBehaviour
{
    [System.Serializable]
    public struct Piece
    {
        public SpriteRenderer renderer;
        public Sprite inactiveSprite;
        public Sprite activeSprite;
    }

    [SerializeField] private LevelSelectPlate sourceLevel;
    [SerializeField] private Piece[] pieces = System.Array.Empty<Piece>();

    public bool IsPowered => sourceLevel != null && sourceLevel.IsBeaten;

    private void Start() => Refresh();
    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        bool powered = IsPowered;
        foreach (var piece in pieces)
            if (piece.renderer != null)
                piece.renderer.sprite = powered ? piece.activeSprite : piece.inactiveSprite;
    }
}
