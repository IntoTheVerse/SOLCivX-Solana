using UnityEngine;

public class SetupMainThread : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Setup()
    {
        MainThreadUtil.Setup();
    }
}