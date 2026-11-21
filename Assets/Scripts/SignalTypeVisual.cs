using System.Collections.Generic;
using UnityEngine;

// Share one shader material while preserving each renderer's original material.
internal static class SignalTypeVisual
{
    private static readonly Dictionary<Renderer, Material> Originals = new();
    private static Material redMaterial;

    internal static void Apply(Renderer renderer, SignalType type)
    {
        if (renderer == null) return;
        if (type != SignalType.Red) { Restore(renderer); return; }
        if (redMaterial == null)
        {
            var shader = Resources.Load<Shader>("RedSignal");
            if (shader == null) return;
            redMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        if (renderer.sharedMaterial == redMaterial) return;
        if (!Originals.ContainsKey(renderer)) Originals.Add(renderer, renderer.sharedMaterial);
        renderer.sharedMaterial = redMaterial;
    }

    internal static void Restore(Renderer renderer)
    {
        if (renderer == null || !Originals.TryGetValue(renderer, out var original)) return;
        if (renderer.sharedMaterial == redMaterial) renderer.sharedMaterial = original;
        Originals.Remove(renderer);
    }
}
