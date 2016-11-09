using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using System.Runtime.Serialization.Formatters.Binary;
using System;

public enum ExporterState
{
    IDLE,
    REQUEST_CODE,
    PUBLISH_MODEL,
    GET_CATEGORIES
}

public class ExporterSKFB : EditorWindow {

    [MenuItem("File/Publish to Sketchfab")]
    static void Init()
    { 
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX // edit: added Platform Dependent Compilation - win or osx standalone
        ExporterSKFB window = (ExporterSKFB)EditorWindow.GetWindow(typeof(ExporterSKFB));
#else // and error dialog if not standalone
		EditorUtility.DisplayDialog("Error", "Your build target must be set to standalone", "Okay");
#endif
    }

    WWW www;
    WWW test;
    string access_token = "";
    string apiUrl;
    ExporterState state;
    GameObject exporterGo;
    ExporterScript publisher;
    SceneToGlTFWiz exporter;

    ////Account settings
    private string user_name = "";
    private string user_password = "";

    Dictionary<string, string> parameters = new Dictionary<string, string>();

    //Fields
    private string param_name = "Unity model";
    private string param_description = "Model exported from Unity Engine.";
    private string param_tags = "unity";
    private bool param_autopublish = true;
    private bool param_private = false;
    private string param_password = "";
    private string param_token = "";

    private bool isDirty = true;
    private string exportPath;
    private string zipPath;
    Dictionary<string, string> categories = new Dictionary<string, string>();
    List<string> categoriesNames = new List<string>();
    int categoryIndex = 0;

    // Oauth stuff
    private string credfile = Application.persistentDataPath + "/creds.txt";
    private string renewToken = "";
    private float expiresIn = 0;
    private float lastTokenTime = 0;
    DateTime date;

    void Awake()
    {
        zipPath = Application.temporaryCachePath + "/" + "Unity2Skfb.zip";
        exportPath = Application.temporaryCachePath + "/" + "Unity2Skfb.gltf";
        exporterGo = new GameObject("Exporter");
        publisher = exporterGo.AddComponent<ExporterScript>();
        exporter = exporterGo.AddComponent<SceneToGlTFWiz>();
        publisher.getCategories();
        if (File.Exists(credfile))
        {
            readToken();
        }
    }

    // Update is called once per frame
    void OnInspectorUpdate()
    {
        Repaint();
        date = DateTime.Now;
        float currentTimeSecond = date.Hour * 3600 + date.Minute * 60 + date.Second;
        if(currentTimeSecond - lastTokenTime > expiresIn)
            access_token = "";

        if (publisher != null && publisher.www != null && publisher.www.isDone)
        {
            state = publisher.getState();
            www = publisher.www;
            switch (state)
            {
                case ExporterState.REQUEST_CODE:
                    Debug.Log("---Access token---");
                    if (JSON.Parse(www.text)["access_token"] != null)
                    {
                        Debug.Log(www.text);
                        access_token = JSON.Parse(www.text)["access_token"];
                        renewToken = JSON.Parse(www.text)["refresh_token"];
                        expiresIn = JSON.Parse(www.text)["expires_in"].AsFloat;
                        lastTokenTime = DateTime.Now.Hour * 3600 + DateTime.Now.Minute * 60 + DateTime.Now.Second;
                        saveToken();
                    }
                        
                    publisher.setIdle();
                    break;
                case ExporterState.PUBLISH_MODEL:
                    if (www.responseHeaders["STATUS"].Contains("201"));
                    {
                        string urlid = www.responseHeaders["LOCATION"].Split('/')[www.responseHeaders["LOCATION"].Split('/').Length -1];
                        string url = "https://sketchfab-local.com/models/" + urlid;
                        Application.OpenURL(url);
                    }
                    publisher.setIdle();
                    break;
                case ExporterState.GET_CATEGORIES:
                    string jsonify = www.text.Replace("null", "\"null\"");
                    JSONArray categoriesArray = JSON.Parse(jsonify)["results"].AsArray;
                    foreach (JSONNode node in categoriesArray)
                    {
                        categories.Add(node["name"], node["uid"]);
                        categoriesNames.Add(node["name"]);
                    }

                    publisher.setIdle();
                    break;
            }
        }
    }

    public float progress()
    {
        if (www == null)
            return 0.0f;

        return 0.99f * www.uploadProgress + 0.01f * www.progress;
    }

    void OnSelectionChange()
    {
        isDirty = true;
    }

    void OnWizardCreate() // Create (Export) button has been hit (NOT wizard has been created!)
    {
        Debug.Log("ok");
    }

    void OnGUI()
    {
        // Account settings
        GUILayout.Label("Sketchfab authentication", EditorStyles.boldLabel);
        if (access_token.Length == 0 && renewToken.Length == 0)
        {
            user_name = EditorGUILayout.TextField("User name", user_name);
            user_password = EditorGUILayout.TextField("User password", user_password);
            GUILayout.Space(5);
            if (GUILayout.Button("Authorize exporter"))
            {
                www = publisher.www;
                publisher.oauth(user_name, user_password);
            }
        }
        else
        {
            GUILayout.Label("Sketchfab authentication status: OK");
            if (GUILayout.Button("Revoke autorization"))
            {
                renewToken = "";
                access_token = "";
                if (File.Exists(credfile))
                    File.Delete(credfile);
            }

            GUI.enabled = renewToken.Length > 0 && access_token.Length == 0;
            if (GUILayout.Button("Renew authorization"))
            {
                www = publisher.www;
                publisher.renewOauth(renewToken);
            }
        }

        GUILayout.Space(10);
        GUI.enabled = access_token.Length > 0;
        // Model settings
        GUILayout.Label("Model settings", EditorStyles.boldLabel);
        param_name = EditorGUILayout.TextField("Title (Scene name)", param_name); //edit: added name source
        param_description = EditorGUILayout.TextField("Description", param_description);
        param_tags = EditorGUILayout.TextField("Tags", param_tags);
        if(categories.Count > 0)
            categoryIndex = EditorGUILayout.Popup(categoryIndex, categoriesNames.ToArray());

        if (GUILayout.Button("Upload"))
        {
            if (isDirty)
            {
                if (System.IO.File.Exists(zipPath))
                {
                    System.IO.File.Delete(zipPath);
                }
                exporter.ExportCoroutine(exportPath, null, true, true);
            }

            if(File.Exists(zipPath))
            {
                publisher.setFilePath(zipPath);
                www = publisher.www;

                publisher.publish(parameters, access_token);
            }
            else
            {
                Debug.Log("Zip file has not been generated. Aborting publish.");
            }
        }

        if (publisher != null && publisher.getState() == ExporterState.PUBLISH_MODEL && publisher.www != null)
        {
            Rect r = EditorGUILayout.BeginVertical();
            EditorGUI.ProgressBar(r, progress(), "Upload progress");
            GUILayout.Space(16);
            EditorGUILayout.EndVertical();
        }
    }

    private Dictionary<string, string> buildParameterDict()
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters["name"] = param_name;
        parameters["description"] = param_description;
        parameters["tags"] = param_tags;
        parameters["private"] = param_private ? "1" : "0";
        parameters["isPublished"] = param_autopublish ? "1" : "0";
        if (param_private)
            parameters["password"] = param_password;

        return parameters;
    }

    void OnDestroy()
    {
        if (System.IO.File.Exists(zipPath))
            System.IO.File.Delete(zipPath);

        if (exporterGo)
        {
            DestroyImmediate(exporterGo);
            exporter = null;
            publisher = null;
        }
    }

    private void readToken()
    {
        FileStream file = File.Open(credfile, FileMode.Open, FileAccess.Read);
        BinaryFormatter bf = new BinaryFormatter();
        string renewToken = bf.Deserialize(file) as string;
        file.Close();
    }

    private void saveToken()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(credfile);
        bf.Serialize(file, renewToken);
        file.Close();
    }
}

public class ExporterScript : MonoBehaviour
{
    bool done = false;
    ExporterState state;
    public WWW www;
    string apiUrl;
    public string localFileName = "";

    public void Start()
    {
        state = ExporterState.IDLE;
        done = false;
    }

    public bool isDone()
    {
        return done;
    }

    public void oauth(string user_name, string user_password)
    {
        StartCoroutine(oauthCoroutine(user_name, user_password));
    }

    public void renewOauth(string renewToken)
    {
        StartCoroutine(renewOauthCoroutine(renewToken));
    }

    public void publish(Dictionary<string, string> para, string accessToken)
    {
        StartCoroutine(publishCoroutine(para, accessToken));
    }

    public ExporterState getState()
    {
        return state;
    }

    public void setIdle()
    {
        state = ExporterState.IDLE;
    }
    public void setFilePath(string exportFilepath)
    {
        localFileName = exportFilepath;
    }

    public void getCategories()
    {
        StartCoroutine(categoriesCoroutine());
    }

    private IEnumerator categoriesCoroutine()
    {
        state = ExporterState.GET_CATEGORIES;
        www = new WWW("https://sketchfab-local.com/v3/categories");
        yield return www;
    }
    string dummyClientId = "sMBE2QzkCPPfubljKi6zmRmV3yXzJppZXSORuSS6";
    // Request access_token
    private IEnumerator oauthCoroutine(string user_name, string user_password)
    {
        done = false;
        state = ExporterState.REQUEST_CODE;
        WWWForm oform = new WWWForm();
        oform.AddField("username", user_name);
        oform.AddField("password", user_password);
        www = new WWW("https://sketchfab-local.com/oauth2/token/?grant_type=password&client_id=" + dummyClientId, oform);
        yield return www;
    }

    private IEnumerator renewOauthCoroutine(string renew_token)
    {
        done = false;
        state = ExporterState.REQUEST_CODE;
        WWWForm oform = new WWWForm();
        oform.AddField("refresh_token", renew_token);
        www = new WWW("https://sketchfab-local.com/oauth2/token/?grant_type=password&client_id=" + dummyClientId, oform);
        yield return www;
    }

    // Publish file to Sketchfab
    private IEnumerator publishCoroutine(Dictionary<string, string> parameters, string accessToken)
    {
        Debug.Log("Publish");
        state = ExporterState.PUBLISH_MODEL;
        done = false;
        WWWForm postForm = new WWWForm();
        if (!System.IO.File.Exists(localFileName))
        {
            Debug.LogError("File has not been exported. Aborting publish.");
        }

        // Export
        byte[] data = File.ReadAllBytes(localFileName);
        postForm.AddBinaryData("modelFile", data, localFileName, "application/zip");
        foreach (string param in parameters.Keys)
        {
            postForm.AddField(param, parameters[param]);
        }

        postForm.AddField("source", "Unity-exporter");
        //postForm.AddField("isPublished", true ? "1" : "0");
        //postForm.AddField("private", true ? "1" : "0");

        Dictionary<string, string> headers = postForm.headers;
        headers["Authorization"] = "Bearer " + accessToken;
        www = new WWW("https://sketchfab-local.com/v3/models", postForm.data, headers);
        yield return www;
    }
}

