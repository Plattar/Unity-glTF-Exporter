using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using UnityEditor.SceneManagement;

public enum ExporterState
{
    IDLE,
    REQUEST_CODE,
    PUBLISH_MODEL,
    GET_CATEGORIES,
    USER_ACCOUNT_TYPE
}

public class ExporterSKFB : EditorWindow {

    [MenuItem("File/Publish to Sketchfab")]
    static void Init()
    { 
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX // edit: added Platform Dependent Compilation - win or osx standalone
        ExporterSKFB window = (ExporterSKFB)EditorWindow.GetWindow(typeof(ExporterSKFB));
        window.title = "Sketchfab";
        window.Show();
#else // and error dialog if not standalone
		EditorUtility.DisplayDialog("Error", "Your build target must be set to standalone", "Okay");
#endif
    }

    // Keys used to save credentials in editor prefs
    const string usernameEditorKey = "UnityExporter_username";

    // UI dimensions (to be cleaned)
    [SerializeField]
    Vector2 loginSize = new Vector2(603, 145);
    [SerializeField]
    Vector2 fullSize = new Vector2(603, 610);
    [SerializeField]
    Vector2 descSize = new Vector2(603, 175);

    // Fields limits
    const int NAME_LIMIT = 48;
    const int DESC_LIMIT = 1024;
    const int TAGS_LIMIT = 50;
    const int PASSWORD_LIMIT = 64;
    const int SPACE_SIZE = 5;

    // Exporter UI: static elements
    [SerializeField]
    Texture2D header;
    GUIStyle exporterTextArea;
    GUIStyle exporterLabel;

    // Exporter objects and scripts
    WWW www;
    string access_token = "";
    ExporterState state;
    GameObject exporterGo;
    ExporterScript publisher;
    SceneToGlTFWiz exporter;
    private string exportPath;
    private string zipPath;

    ////Account settings
    private string skfbUrl = "https://sketchfab.com/";

    //Fields
    private string user_name = "";
    private string user_password = "";

    private string param_name = "";
    private string param_description = "";
    private string param_tags = "";
    private bool param_autopublish = true;
    private bool param_private = false;
    private string param_password = "";
    private string param_token = "";

    // Exporter UI: dynamic elements
    private string status = "";
    private Color blueColor = new Color(69 / 255.0f, 185 / 255.0f, 223 / 255.0f);
    private Color greyColor = Color.white;
    private bool isUserPro = false;
    private string userDisplayName = "";
    Dictionary<string, string> categories = new Dictionary<string, string>();
    List<string> categoriesNames = new List<string>();
    int categoryIndex = 0;
    Rect windowRect;

    // Oauth stuff
    private float expiresIn = 0;
    private int lastTokenTime = 0;

    //private List<String> tagList;
    void Awake()
    {
        zipPath = Application.temporaryCachePath + "/" + "Unity2Skfb.zip";
        exportPath = Application.temporaryCachePath + "/" + "Unity2Skfb.gltf";

        exporterGo = new GameObject("Exporter");
        publisher = exporterGo.AddComponent<ExporterScript>();
        exporter = exporterGo.AddComponent<SceneToGlTFWiz>();
        //FIXME: Make sure that object is deleted;
        exporterGo.hideFlags = HideFlags.HideInHierarchy;
        //publisher.getCategories();

        windowRect = position;
        windowRect.width = fullSize.x;
        windowRect.height = loginSize.y;
        position = windowRect;
    }

    void OnEnable()
    {
        // Try to load header image
        if(!header)
            header = (Texture2D)Resources.Load(Application.dataPath + "/Unity-glTF-Exporter/ExporterHeader.png", typeof(Texture2D));
        
        // Pre-fill model name with scene name if empty
        if (param_name.Length == 0)
        {
            param_name = EditorSceneManager.GetActiveScene().name;
        }
        this.maxSize = fullSize;
        windowRect = position;
        windowRect.width = fullSize.x;
        windowRect.height = loginSize.y;
        position = windowRect;
        //this.minSize = fullSize;
        // Try to login if username/password
        relog();
    }

    int convertToSeconds(DateTime time)
    {
        return (int)(time.Hour * 3600 + time.Minute * 60 + time.Second);
    }

    void OnSelectionChange()
    {
        // do nothing for now
    }

    void relog()
    {
        if(publisher && publisher.getState() == ExporterState.REQUEST_CODE)
        {
            return;
        }
        if (user_name.Length == 0)
        {
            user_name = EditorPrefs.GetString(usernameEditorKey);
            //user_password = EditorPrefs.GetString(passwordEditorKey);
        }

        if (publisher && user_name.Length > 0 && user_password.Length > 0)
        {
            publisher.oauth(user_name, user_password);
        }
    }

    void expandWindow(bool expand)
    {
        windowRect = this.position;
        windowRect.height = expand ? fullSize.y : loginSize.y;
        position = windowRect;
    }

    // Update is called once per frame
    void OnInspectorUpdate()
    {
        //if(Event.current != null && Event.current.type == EventType.keyDown && Event.current.keyCode == KeyCode.Escape)
        //{
        //    if (param_tags.Length > 0)
        //    {
        //        // Add tag
        //        string[] splitTags = param_tags.Split(' ');
        //        foreach(string tag in splitTags)
        //        {
        //            if(tag.Length > 0 && tagList.Contains(tag) == false)
        //            {
        //                tagList.Add(tag);
        //            }
        //        }
        //        Debug.Log("Added");
        //        param_tags = "";
        //    }
        //}

        Repaint();
        float currentTimeSecond = convertToSeconds(DateTime.Now);
        if (access_token.Length > 0 && currentTimeSecond - lastTokenTime > expiresIn)
        {
            access_token = "";
            relog();
        } 

        if (publisher != null && publisher.www != null && publisher.www.isDone)
        {
            state = publisher.getState();
            www = publisher.www;
            switch (state)
            {
                case ExporterState.REQUEST_CODE:
                    JSONNode accessResponse = JSON.Parse(this.jsonify(www.text));
                    if (accessResponse["access_token"] != null)
                    {
                        access_token = accessResponse["access_token"];
                        expiresIn = accessResponse["expires_in"].AsFloat;
                        lastTokenTime = convertToSeconds(DateTime.Now);
                        publisher.getAccountType(access_token);
                        expandWindow(true);
                    }
                    else
                    {
                        string errorDesc = accessResponse["error_description"];
                        EditorUtility.DisplayDialog("Authentication failed", "Failed to authenticate on Sketchfab.com.\nPlease check your credentials\n\nError: " + errorDesc, "Ok");
                        publisher.setIdle();
                    }

                    break;
                case ExporterState.PUBLISH_MODEL:
                    //foreach(string key in www.responseHeaders.Keys)
                    //{
                    //    Debug.Log("[" + key + "] = " + www.responseHeaders[key]);
                    //}
                    if (www.responseHeaders["STATUS"].Contains("201") == true)
                    {
                        string urlid = www.responseHeaders["LOCATION"].Split('/')[www.responseHeaders["LOCATION"].Split('/').Length -1];
                        string url = skfbUrl + "models/" + urlid;
                        Application.OpenURL(url);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Upload failed", www.responseHeaders["STATUS"], "Ok");
                    }
                    publisher.setIdle();
                    break;
                case ExporterState.GET_CATEGORIES:
                    string jsonify = this.jsonify(www.text);
                    if (!jsonify.Contains("results"))
                    {
                        Debug.Log(jsonify);
                        Debug.Log("Failed to retrieve categories");
                        publisher.setIdle();
                        break;
                    }
                        
                    JSONArray categoriesArray = JSON.Parse(jsonify)["results"].AsArray;
                    foreach (JSONNode node in categoriesArray)
                    {
                        categories.Add(node["name"], node["slug"]);
                        categoriesNames.Add(node["name"]);
                    }
                    publisher.setIdle();
                    break;

                case ExporterState.USER_ACCOUNT_TYPE:
                    string accountRequest = this.jsonify(www.text);
                    if(!accountRequest.Contains("account"))
                    {
                        Debug.Log(accountRequest);
                        Debug.Log("Failed to retrieve user account type");
                        publisher.setIdle();
                        break;
                    }

                    var userSettings = JSON.Parse(accountRequest);
                    isUserPro = userSettings["account"].ToString().Contains("free") == false;
                    userDisplayName = userSettings["displayName"];
                    publisher.setIdle();
                    break;
            }
        }
    }
    
    private string jsonify(string jsondata)
    {
        return jsondata.Replace("null", "\"null\"");
    }

    public float progress()
    {
        if (www == null)
            return 0.0f;

        return 0.99f * www.uploadProgress + 0.01f * www.progress;
    }

    private bool updateExporterStatus()
    {
        status = "";     

        if (param_name.Length > NAME_LIMIT)
        {
            status = "Model name is too long";
            return false;
        }
            

        if (param_name.Length == 0)
        {
            status = "Please give a name to your model";
            return false;
        }
            

        if (param_description.Length > DESC_LIMIT)
        {
            status = "Model description is too long";
            return false;
        }
            

        if (param_tags.Length > TAGS_LIMIT)
        {
            status = "Model tags are too long";
            return false;
        }
            

        int nbSelectedObjects = Selection.GetTransforms(SelectionMode.Deep).Length;
        if (nbSelectedObjects == 0)
        {
            status = "No object selected to export";
            return false;
        }

        status = "Upload " + nbSelectedObjects + " object" + (nbSelectedObjects != 1 ? "s" : "");
        return true;
    }

    void OnGUI()
    {
        if(exporterLabel == null)
        {
            exporterLabel = new GUIStyle(GUI.skin.label);
            exporterLabel.richText = true;
        }

        if(exporterTextArea == null)
        {
            exporterTextArea = new GUIStyle(GUI.skin.textArea);
            exporterTextArea.fixedWidth = descSize.x;
            exporterTextArea.fixedHeight = descSize.y;
        }
        //Header
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(header);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // Account settings
        if (access_token.Length == 0)
        {
            user_name = EditorGUILayout.TextField("Login", user_name);
            user_password = EditorGUILayout.PasswordField("Password", user_password);
            GUILayout.Space(SPACE_SIZE);
            if (GUILayout.Button("Login"))
            {
                www = publisher.www;
                publisher.oauth(user_name, user_password);
                EditorPrefs.SetString(usernameEditorKey, user_name);
                //EditorPrefs.SetString(passwordEditorKey, user_password);
            }
        }
        else
        {
            GUILayout.BeginHorizontal("Box");
            GUILayout.Label("Account: <b>" + userDisplayName + "</b> (" + (isUserPro ? "PRO" : "FREE") + " account)", exporterLabel);
            if (GUILayout.Button("Logout"))
            {
                access_token = "";
                //EditorPrefs.DeleteKey(usernameEditorKey);
                //EditorPrefs.DeleteKey(passwordEditorKey);
                expandWindow(false);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(SPACE_SIZE);

        if(access_token.Length > 0)
        {
            if(position.height != fullSize.y)
            {
               // this.maxSize = fullSize;
               // this.minSize = fullSize;
            }
            // Model settings
            GUILayout.Label("Model properties", EditorStyles.boldLabel);

            // Model name
            GUILayout.Label("Model name");
            param_name = EditorGUILayout.TextField(param_name);
            GUILayout.Label("(" + param_name.Length + "/" + NAME_LIMIT + ")", EditorStyles.centeredGreyMiniLabel);
            EditorStyles.textField.wordWrap = true;
            GUILayout.Space(SPACE_SIZE);
          
            GUILayout.Label("Description");
            param_description = EditorGUILayout.TextArea(param_description, exporterTextArea);
            GUILayout.Label("(" + param_description.Length + " / 1024)", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(SPACE_SIZE);
            GUILayout.Label("Tags (separated by spaces)");
            param_tags = EditorGUILayout.TextField(param_tags);
            GUILayout.Label("'unity' and 'unity3D' added automatically ("+ param_tags.Length + "/50)", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(SPACE_SIZE);
            // ENable only if user is pro

            GUILayout.Label("PRO only features", EditorStyles.centeredGreyMiniLabel);
            GUI.enabled = isUserPro;
            EditorGUILayout.BeginVertical("Box");
            param_private = EditorGUILayout.Toggle("Private model", param_private);
            GUI.enabled = isUserPro && param_private;
            GUILayout.Label("Password");
            param_password = EditorGUILayout.TextField(param_password);
            EditorGUILayout.EndVertical();
            GUI.enabled = true;
            param_autopublish = EditorGUILayout.Toggle("Publish immediately ", param_autopublish);
            GUILayout.Space(SPACE_SIZE);

            if (categories.Count > 0)
                categoryIndex = EditorGUILayout.Popup(categoryIndex, categoriesNames.ToArray());

            GUILayout.Space(SPACE_SIZE);
            bool enable = updateExporterStatus();
            
            if (enable)
                GUI.color = blueColor;
            else
                GUI.color = greyColor;

            if (publisher != null && publisher.getState() == ExporterState.PUBLISH_MODEL && publisher.www != null)
            {
                Rect r = EditorGUILayout.BeginVertical();
                EditorGUI.ProgressBar(r, progress(), "Upload progress");
                GUILayout.Space(18);
                EditorGUILayout.EndVertical();
            }
            else
            {
                GUI.enabled = enable;
                if (GUILayout.Button(status))
                {
                    if (!enable)
                    {
                        EditorUtility.DisplayDialog("Error", status, "Ok");
                    }
                    else
                    {
                        if (System.IO.File.Exists(zipPath))
                        {
                            System.IO.File.Delete(zipPath);
                        }

                        exporter.ExportCoroutine(exportPath, null, true, true);

                        if (File.Exists(zipPath))
                        {
                            publisher.setFilePath(zipPath);
                            publisher.publish(buildParameterDict(), access_token);
                            www = publisher.www;
                        }
                        else
                        {
                            Debug.Log("Zip file has not been generated. Aborting publish.");
                        }
                    }
                }
            }
        }
    }

    private Dictionary<string, string> buildParameterDict()
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters["name"] = param_name;
        parameters["description"] = param_description;
        parameters["tags"] = "unity unity3D " + param_tags;
        parameters["private"] = param_private ? "1" : "0";
        parameters["isPublished"] = param_autopublish ? "1" : "0";
        //string category = categories[categoriesNames[categoryIndex]];
        //Debug.Log(category);
        //parameters["categories"] = category;
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
}

public class ExporterScript : MonoBehaviour
{
    bool done = false;
    ExporterState state;
    public WWW www;
    public string localFileName = "";
    private string skfbUrl = "https://sketchfab.com/";

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

    public void getAccountType(string access_token)
    {
        StartCoroutine(userAccountCoroutine(access_token));
    }

    private IEnumerator categoriesCoroutine()
    {
        state = ExporterState.GET_CATEGORIES;
        www = new WWW(skfbUrl + "v3/categories");
        yield return www;
    }

    // FIXME need to put this somewhere else
    string clientId = "IUO8d5VVOIUCzWQArQ3VuXfbwx5QekZfLeDlpOmW";

    // Request access_token
    private IEnumerator oauthCoroutine(string user_name, string user_password)
    {
        done = false;
        state = ExporterState.REQUEST_CODE;
        WWWForm oform = new WWWForm();
        oform.AddField("username", user_name);
        oform.AddField("password", user_password);
        www = new WWW(skfbUrl + "oauth2/token/?grant_type=password&client_id=" + clientId, oform);
        yield return www;
    }

    private IEnumerator userAccountCoroutine(string access_token)
    {
        done = false;
        state = ExporterState.USER_ACCOUNT_TYPE;
        WWWForm oform = new WWWForm();
        Dictionary<string, string> headers = oform.headers;
        headers["Authorization"] = "Bearer " + access_token;
        www = new WWW(skfbUrl + "v3/me", null, headers);
        yield return www;
    }

    // Publish file to Sketchfab
    private IEnumerator publishCoroutine(Dictionary<string, string> parameters, string accessToken)
    {
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
        www = new WWW(skfbUrl + "v3/models", postForm.data, headers);
        yield return www;
    }
}
