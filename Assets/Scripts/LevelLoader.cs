using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking.Unity;
using BeardedManStudios.Forge.Networking;

[DisallowMultipleComponent]
public class LevelLoader : MonoBehaviour
{
    [SerializeField] private CanvasGroup _cgLoadingScreen;

    #region Singleton
    private static readonly object _instanceLock = new object();
    private static bool _shuttingDown = false;
    private static LevelLoader _instance;
    public static LevelLoader Instance
    {
        get
        {
            if (_shuttingDown)
            {
                Debug.Log("shutting down");
                return null;
            }

            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        var obj = new GameObject("LevelLoader");
                        _instance = obj.AddComponent<LevelLoader>();
                    }
                }
            }

            return _instance;
        }
    }
    #endregion

    #region Monobehaviour
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        _shuttingDown = true;
    }
    #endregion

    public void ChangeScene(int sceneIndex)
    {
        StartCoroutine(ChangeSceneInternal(sceneIndex));
    }

    private IEnumerator ChangeSceneInternal(int sceneIndex)
    {
        if (_cgLoadingScreen != null)
        {
            _cgLoadingScreen.alpha = 1.0f;
            _cgLoadingScreen.blocksRaycasts = true;
            _cgLoadingScreen.interactable = true;
        }

        SceneManager.LoadScene("LoadingScene");
        yield return null;
        if (InstanceManager.Instance.networkObject.IsServer)
        {
            InstanceManager.Instance.ToggleSceneReadyFlag(false);

            yield return StartCoroutine(LoadSceneDelay(sceneIndex));

            InstanceManager.Instance.ToggleSceneReadyFlag(true);
        }
        else
        {
            //! yielding one frame to make sure that the server can set sceneLoaded first
            yield return null;
        
            //! wait until the host finish loading the scene before loading in
            while (!InstanceManager.Instance.SceneReady)
            {
                yield return null;
            }
        
            StartCoroutine(LoadSceneDelay(sceneIndex));
        }
    }

    private IEnumerator LoadSceneDelay(int sceneIndex)
    {
        AsyncOperation asyncload = SceneManager.LoadSceneAsync(sceneIndex);
        asyncload.allowSceneActivation = false;

        while (!asyncload.isDone)
        {
            if (asyncload.progress >= 0.9f)
            {
                asyncload.allowSceneActivation = true;
            }
            yield return null;
        }
        
        yield return new WaitForSeconds(0.25f);

        if (_cgLoadingScreen != null)
        {
            _cgLoadingScreen.alpha = 0.0f;
            _cgLoadingScreen.blocksRaycasts = false;
            _cgLoadingScreen.interactable = false;
        }
    }
}
