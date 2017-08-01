
// forked from https://github.com/sketchfab/Unity-glTF-Exporter 

#if UNITY_EDITOR
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
	USER_ACCOUNT_TYPE,
	CHECK_VERSION
}


public class ExporterSKFB : EditorWindow {

	[MenuItem("Tools/Export to Plattar")]
	static void Init()
	{
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX // edit: added Platform Dependent Compilation - win or osx standalone
		ExporterSKFB window = (ExporterSKFB)EditorWindow.GetWindow(typeof(ExporterSKFB));
		window.titleContent.text = "Plattar Export";
		window.Show();
#else // and error dialog if not standalone
		EditorUtility.DisplayDialog("Error", "Your build target must be set to standalone", "Okay");
#endif
	}

	// Static data
	public static string skfbUrl = "https://plattar.com/";
	public static string latestReleaseUrl = "https://github.com/plattar/Unity-glTF-Exporter/releases";
	public static string helpUrl = "http://help.plattar.com/assets-and-content";
	public static string resetPasswordUrl = "";
	public static string createAccountUrl = "";
	public static string reportAnIssueUrl = "mailto:info@plattar.com";
	public static string privateUrl = "";
	public static string draftUrl = "";

	// UI dimensions (to be cleaned)
	[SerializeField]
	Vector2 loginSize = new Vector2(603, 190);
	[SerializeField]
	Vector2 fullSize = new Vector2(603, 690);
	[SerializeField]
	Vector2 descSize = new Vector2(603, 175);

	// Fields limits
	const int NAME_LIMIT = 48;
	const int DESC_LIMIT = 1024;
	const int TAGS_LIMIT = 50;
	const int PASSWORD_LIMIT = 64;
	const int SPACE_SIZE = 5;

	private string exporterVersion = GlTF_Writer.exporterVersion;
	private string latestVersion = "2.1.1a";

	// Keys used to save credentials in editor prefs
	const string usernameEditorKey = "UnityExporter_username";
	//const string passwordEditorKey = "UnityExporter_password";

	// Exporter UI: static elements
	[SerializeField]
	Texture2D header;
	GUIStyle exporterTextArea;
	GUIStyle exporterLabel;
	GUIStyle exporterClickableLabel;
	private string clickableLabelColor = "navy";
	//private Color clickableLabelColor =
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


	//Fields
	private string user_name = "";
	private string user_password = "";

	private bool opt_exportAnimation = true;
	private bool opt_exportVariations = false;
	private string param_name = "";
	private string param_description = "";
	private string param_tags = "";
	private bool param_autopublish = true;
	private bool param_private = false;
	private string param_password = "";

	// Exporter UI: dynamic elements
	private string status = "";
	private Color blueColor = new Color(69 / 255.0f, 185 / 255.0f, 223 / 255.0f);
	private Color redColor = new Color(0.8f, 0.0f, 0.0f);
	private Color greyColor = Color.white;
	private bool isUserPro = false;
	private string userDisplayName = "";
	Dictionary<string, string> categories = new Dictionary<string, string>();
	List<string> categoriesNames = new List<string>();
	//int categoryIndex = 0;
	Rect windowRect;

	// Oauth stuff
	private float expiresIn = 0;
	private int lastTokenTime = 0;

	//private List<String> tagList;
	void Awake()
	{
		zipPath = Application.dataPath + "/Exports/" + "PlattarExport.zip";
		exportPath = Application.dataPath + "/Exports/" + "PlattarExport.gltf";

		exporterGo = new GameObject("Exporter");
		publisher = exporterGo.AddComponent<ExporterScript>();
		exporter = exporterGo.AddComponent<SceneToGlTFWiz>();
		//FIXME: Make sure that object is deleted;
		exporterGo.hideFlags = HideFlags.HideAndDontSave;
		//publisher.getCategories();
		resizeWindow(fullSize);
		publisher.checkVersion();

		
	}

	void OnEnable()
	{
		
		// Try to load header image
		if(!header)
			header = (Texture2D)Resources.Load(Application.dataPath + "Plattar Exporter/plattar.png", typeof(Texture2D));


		//resizeWindow(fullSize);
	
	}

	int convertToSeconds(DateTime time)
	{
		return (int)(time.Hour * 3600 + time.Minute * 60 + time.Second);
	}

	void OnSelectionChange()
	{
		// do nothing for now
	}

	void resizeWindow(Vector2 size)
	{
		//this.maxSize = size;
		this.minSize = size;
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
		Repaint();
		float currentTimeSecond = convertToSeconds(DateTime.Now);


		if (publisher != null && publisher.www != null && publisher.www.isDone)
		{
			state = publisher.getState();
			www = publisher.www;
			switch (state)
			{
				case ExporterState.CHECK_VERSION:
					JSONNode githubResponse = JSON.Parse(this.jsonify(www.text));
					if(githubResponse != null && githubResponse[0]["tag_name"] != null)
					{
						latestVersion = githubResponse[0]["tag_name"];
						if (exporterVersion != latestVersion)
						{
							bool update = EditorUtility.DisplayDialog("Exporter update", "A new version is available \n(you have version " + exporterVersion + ")\nIt's strongly rsecommended that you update now. The latest version may include important bug fixes and improvements", "Update", "Skip");
							if (update)
							{
								Application.OpenURL(latestReleaseUrl);
							}
						}
						else
						{
							resizeWindow(fullSize);
						}
					}
					else
					{
						latestVersion = "";
						resizeWindow(fullSize + new Vector2(0, 15));
					}
					publisher.setIdle();
					break;

				
				case ExporterState.PUBLISH_MODEL:

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

			}
		}
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


			


		int nbSelectedObjects = Selection.GetTransforms(SelectionMode.Deep).Length;
		if (nbSelectedObjects == 0)
		{
			status = "No object selected to export";
			return false;
		}

		//if we only selected one item, name it that
		int topSelectedObjects = Selection.GetTransforms(SelectionMode.TopLevel).Length;
		if (topSelectedObjects == 1)
		{
			param_name = Selection.GetTransforms (SelectionMode.TopLevel) [0].gameObject.name;
		}

		status = "Export " + nbSelectedObjects + " object" + (nbSelectedObjects != 1 ? "s" : "");
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

		if(exporterClickableLabel == null)
		{
			exporterClickableLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
			exporterClickableLabel.richText = true;
		}
		//Header
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(header);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();


			if (latestVersion.Length == 0)
			{

				Color current = GUI.color;
				GUI.color = Color.red;
				GUILayout.Label("An error occured when looking for the latest exporter version\nYou might be using an old and not fully supported version", EditorStyles.centeredGreyMiniLabel);
				if (GUILayout.Button("Click here to be redirected to release page"))
				{
					Application.OpenURL(latestReleaseUrl);
				}
				GUI.color = current;
			}
			else if (exporterVersion != latestVersion)
			{
				Color current = GUI.color;
				GUI.color = redColor;
				GUILayout.Label("New version " + latestVersion + " available (current version is " + exporterVersion + ")", EditorStyles.centeredGreyMiniLabel);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Go to release page", GUILayout.Width(150), GUILayout.Height(25)))
				{
					Application.OpenURL(latestReleaseUrl);
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUI.color = current;
			}
			else
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Exporter is up to date (version:" + exporterVersion + ")", EditorStyles.centeredGreyMiniLabel);

				GUILayout.FlexibleSpace();
				if(GUILayout.Button("<color=" + clickableLabelColor + ">Help  -</color>", exporterClickableLabel, GUILayout.Height(20)))
				{
				Application.OpenURL(helpUrl);
				}

				if (GUILayout.Button("<color=" + clickableLabelColor + ">Report an issue</color>", exporterClickableLabel, GUILayout.Height(20)))
				{
					Application.OpenURL(reportAnIssueUrl);
				}
				GUILayout.EndHorizontal();
			}




		GUILayout.Space(SPACE_SIZE);


			// Model settings
			GUILayout.Label("Model properties", EditorStyles.boldLabel);



			GUILayout.Label("Options", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal();
			opt_exportAnimation = EditorGUILayout.Toggle("Export animation", opt_exportAnimation);
			GUILayout.FlexibleSpace();

			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			EditorGUIUtility.labelWidth = 310f;
			opt_exportVariations = EditorGUILayout.Toggle("Include each selected root object as a variation", opt_exportVariations);
			//GUILayout.FlexibleSpace();

			GUILayout.EndHorizontal();



			bool enable = updateExporterStatus();

			if (enable)
				GUI.color = blueColor;
			else
				GUI.color = greyColor;


				GUI.enabled = enable;
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(status, GUILayout.Width(250), GUILayout.Height(40)))
				{
					if (!enable)
					{
						EditorUtility.DisplayDialog("Error", status, "Ok");
					}
					else
					{



					 zipPath = EditorUtility.SaveFilePanel(
						"Export object to zip",
						"",
						param_name + "_" + System.DateTime.Now.ToString("yyyyMMdd") + ".zip",
						"zip");
										
						exportPath = zipPath.Replace(".zip", ".gltf");
					
						if (System.IO.File.Exists(zipPath))
						{
							System.IO.File.Delete(zipPath);
						}

						exporter.ExportCoroutine(exportPath, null, true, true, opt_exportAnimation, true);

						
					}
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();


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
	private string jsonify(string jsondata)
	{
		return jsondata.Replace("null", "\"null\"");
	}
}

public class ExporterScript : MonoBehaviour
{
	bool done = false;
	ExporterState state;
	public WWW www;
	public string localFileName = "";
	private string skfbUrl = "https://sketchfab.com/";
	private string latestVersionCheckUrl = "https://api.github.com/repos/plattar/Unity-glTF-Exporter/releases";

	public void Start()
	{
		state = ExporterState.IDLE;
		done = false;
	}

	public bool isDone()
	{
		return done;
	}
	public void checkVersion()
	{
		StartCoroutine(checkVersionCoroutine());
	}
		

	public void setState(ExporterState newState)
	{
		this.state = newState;
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




	private IEnumerator checkVersionCoroutine()
	{
		state = ExporterState.CHECK_VERSION;
		www = new WWW(latestVersionCheckUrl);
		yield return www;
	}




}
#endif