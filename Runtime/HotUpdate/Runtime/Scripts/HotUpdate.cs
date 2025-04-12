using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HotUpdate : MonoBehaviour
{
    [SerializeField] private HotUpdateView _view;
    [SerializeField] private ConsoleToScreen _logger;
    
    private IHotUpdateHandler _handler;
    
    private void Awake()
    {
        _handler = new HotUpdateHandler(null);
    }

    private IEnumerator Start()
    {
        _logger.enabled = true;

        yield return _handler.CheckResources();
        
        yield return _handler.LoadMetaData();
        
        yield return _handler.LoadDll();
        
        yield return SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
    }
}