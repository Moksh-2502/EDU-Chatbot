using UnityEngine;

namespace SubwaySurfers
{
    public class GameDataLoader : MonoBehaviour
    {
        private static bool instanceExists = false;

        private void Awake()
        {
            // Ensure only one instance exists
            if (instanceExists)
            {
                Destroy(gameObject);
                return;
            }

            instanceExists = true;
            DontDestroyOnLoad(gameObject);

            //if we create the PlayerData, mean it's the very first call, so we use that to init the database
            //this allow to always init the database at the earlier we can, i.e. the start screen if started normally on device
            //or the Loadout screen if testing in editor
            CoroutineHandler.StartStaticCoroutine(CharacterDatabase.LoadDatabase());
            CoroutineHandler.StartStaticCoroutine(ThemeDatabase.LoadDatabase());
        }

        private void OnDestroy()
        {
            // Reset the flag when this instance is destroyed (e.g., when changing scenes without DontDestroyOnLoad)
            if (this != null)
            {
                instanceExists = false;
            }
        }
    }
}