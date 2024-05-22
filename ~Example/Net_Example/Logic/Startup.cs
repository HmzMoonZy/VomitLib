using Geek.Server.Proto;
using MessagePack;
using MessagePack.Resolvers;

using UnityEngine;

namespace Demo.Net
{
    public class Startup : MonoBehaviour
    {
        Start()
        {
            PolymorphicRegister.Load();
            new GameObject("GameMain").AddComponent<GameMain>();
        }
    }
}
