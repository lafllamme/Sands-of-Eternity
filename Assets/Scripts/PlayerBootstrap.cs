using UnityEngine;

public class PlayerBootstrap : MonoBehaviour
{
    void Start()
    {
        var hp = GetComponent<Health>();
        if (hp && GameManager.I) GameManager.I.RegisterPlayer(hp);
    }
}