using UnityEngine;

[DisallowMultipleComponent]
public sealed class ConnectionManagerHost : MonoBehaviour
{
    public ConnectionManager Manager => ConnectionManager.ForScene(gameObject.scene);

    private void Awake()
    {
        ConnectionManager.ForScene(gameObject.scene);
    }
}
