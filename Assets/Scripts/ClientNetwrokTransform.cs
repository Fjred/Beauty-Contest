using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetwrokTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
