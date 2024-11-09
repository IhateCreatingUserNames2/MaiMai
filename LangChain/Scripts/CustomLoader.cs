using Puerts;
using System;
using System.IO;
using UnityEngine;

public class CustomLoader : ILoader
{

    private readonly string basePath;

    public CustomLoader(string basePath)
    {
        this.basePath = basePath;
    }

    public bool FileExists(string filepath)
    {
        string resourcePath = NormalizeResourcePath(filepath);
        return Resources.Load<TextAsset>(resourcePath) != null;
    }

    public string ReadFile(string filepath, out string debugpath)
    {
        string resourcePath = NormalizeResourcePath(filepath);
        debugpath = Path.Combine(Application.dataPath, "Resources", resourcePath);

        TextAsset file = Resources.Load<TextAsset>(resourcePath);

        if (file == null)
        {
            Debug.LogError($"File {filepath} not found at {debugpath}");
            throw new FileNotFoundException($"File {filepath} not found at {debugpath}");
        }

        Debug.Log($"Successfully loaded file: {filepath}");
        return file.text;
    }


    private string NormalizeResourcePath(string filepath)
    {
        // Remove extensions like .js, .cjs, .mjs
        if (filepath.EndsWith(".js") || filepath.EndsWith(".cjs") || filepath.EndsWith(".mjs"))
        {
            filepath = filepath.Substring(0, filepath.LastIndexOf('.'));
        }

        // Replace backslashes with forward slashes
        filepath = filepath.Replace('\\', '/');

        // Remove leading './' if present
        if (filepath.StartsWith("./"))
        {
            filepath = filepath.Substring(2);
        }

        return filepath;
    }
}
