using UnityEngine;
using Puerts;

public class InitializePuerts : MonoBehaviour
{
    private JsEnv jsEnv;

    void Start()
    {
        try
        {
            Debug.Log("Initializing PuerTS environment...");
            jsEnv = new JsEnv(new CustomLoader(), 9229);

            // Execute the testLangGraph module
            jsEnv.ExecuteModule("testLangGraph.mjs");

            Debug.Log("PuerTS environment initialized successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize PuerTS: {e}");
        }
    }

    void Update()
    {
        jsEnv?.Tick();
    }

    private void OnDestroy()
    {
        jsEnv?.Dispose();
    }
}
