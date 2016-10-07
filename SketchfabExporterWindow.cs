using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using SimpleJSON;
using Ionic.Zip;
using UnityEditor.SceneManagement;

public class SketchfabExporterWww : MonoBehaviour
{
	private WWW www = null;
	string api_url = "https://sketchfab.com/v2/models";
	public bool done = false;
	public IEnumerator UploadFileCo(string localFileName, string token, bool autopublish, bool model_private, string title, string description, string tags)
	{
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX // edit: added Platform Dependent Compilation - win or osx standalone - will not get called anyway if false
		byte[] data = File.ReadAllBytes(localFileName);
		if (data.Length > 0)
		{
			Debug.Log("Loaded file successfully : " + data.Length + " bytes");
		}
		else {
			Debug.Log("Open file error");
			yield break;
		}

		WWWForm postForm = new WWWForm();
		postForm.AddBinaryData("modelFile", data, localFileName, "application/zip");
		postForm.AddField("name", title);
		postForm.AddField("description", description);
		postForm.AddField("source", "Unity-exporter");
		postForm.AddField("tags", tags);
		postForm.AddField("token", token);
		postForm.AddField("isPublished", autopublish ? "1" : "0");
		postForm.AddField("private", model_private ? "1" : "0");
		www = new WWW(api_url, postForm);

#endif
		yield return www;
	}

	public string getUrlID()
	{
		if (www.error == null)
		{
			// test result
			var N = JSON.Parse(www.text);
			if (N["uid"].AsBool == true)
			{
				return N["uid"].Value;
			}
		}
		return null;
	}

	public float progress()
	{
		if (www == null)
			return 0.0f;

		return 0.99f * www.uploadProgress + 0.01f * www.progress;
	}

	public string getUid()
	{
		if (www.error == null)
		{
			return JSON.Parse(www.text)["uid"];
		}

		return "";
	}

	public void reset()
	{
		done = false;
		www = null;
	}

	public bool isDone()
	{
		return www != null && www.isDone;
	}

	public string getError()
	{
		return www.error;
	}

	public void upload(string filename, string token, bool autopublish, bool model_private, string title, string description, string tags)
	{
		done = false;
		StartCoroutine(UploadFileCo(filename, token, autopublish, model_private, title, description, tags));
	}
}

public class SketchfabExporter
{
	string api_url = "https://sketchfab.com/v2/models";
	public bool isActive = false;

	// Export parameters
	private string param_title;
	private string param_description;
	private string param_tags;
	private bool param_autopublish;
	private bool param_private;
	private string param_password;
	private string param_token;

	// Exporter objects
	public SketchfabExporterWww exporterWww = null;
	public SceneToGlTFWiz exporterParser = null;
	private static GameObject exporterGo;
	public bool isPublishing = false;

	private static ZipFile zip;
	private string zipname = "Unity2Skfb.zip";
	private string exportDirectory;

	public SketchfabExporter(string token, ArrayList m, string title, string description, string tags, bool autopublish, bool priv, string password)
	{
		param_token = token;
		param_title = title;
		param_description = description;
		param_tags = tags;
		param_autopublish = autopublish;
		param_private = priv;
		param_password = password;
	}

	public void export()
	{
		exportDirectory = Application.temporaryCachePath + "/" + "skfbexport";
		System.IO.Directory.CreateDirectory(exportDirectory);
		isActive = true;

		string filename = param_title + ".gltf";
		if (exporterGo == null)
			exporterGo = new GameObject("Exporter");
		if (exporterParser == null)
			exporterParser = exporterGo.AddComponent<SceneToGlTFWiz>();

		exporterParser.ExportCoroutine(exportDirectory + "/" + filename, null, true);
	}

	public void publish()
	{
		isPublishing = true;
		// Add generated files to zip
		cleanZip();
		zip = new ZipFile();
		zipname = "Unity2Skfb.zip";
		zip.AddDirectory(exportDirectory);
		zipname = exportDirectory + "/" + zipname;
		zip.Save(zipname);
		// upload zip to sketchfab
		if (exporterWww == null)
			exporterWww = exporterGo.AddComponent<SketchfabExporterWww>();
		exporterWww.upload(zipname, param_token, param_autopublish, param_private, param_title, param_description, param_tags);
	}

	public string getUid()
	{
		if (exporterWww == null)
			return null;
		return exporterWww.getUid();
	}

	public bool isParsingDone()
	{
		if (exporterParser == null)
			return true;

		return exporterParser.isDone();
	}

	public bool isUploadingDone()
	{
		if (exporterWww == null)
			return false;

		return exporterWww.isDone();
	}

	public bool done()
	{
		return !isActive;
	}

	public void resetParser()
	{
		if (exporterParser != null)
		{
			exporterParser.resetParser();
			exporterParser = null;
		}

	}

	public void resetPublisher()
	{
		if (exporterWww != null)
		{
			exporterWww.reset();
		}
	}

	public string getError()
	{
		return exporterWww.getError();
	}

	public string getUrlID()
	{
		if (exporterWww == null)
			return null;
		return exporterWww.getUrlID();
	}

	public float progress()
	{
		if (exporterWww == null)
			return 0.0f;
		return exporterWww.progress();
	}

	public void cleanZip()
	{
		if (System.IO.File.Exists(zipname))
			System.IO.File.Delete(zipname);
	}

	public void clean()
	{
		cleanZip();
		isPublishing = false;
		// Clean objects
		resetParser();
		resetPublisher();
		if (exporterGo != null)
		{
			GameObject.DestroyImmediate(exporterGo);
			exporterGo = null;
		}

		string[] generatedFiles = Directory.GetFiles(exportDirectory);
		if (!exportDirectory.Contains(Application.temporaryCachePath))
		{
			Debug.LogError("Directory '" + exportDirectory + "' has not been cleaned (not in cache directory) ");
		}
		else
		{
			for (int i = 0; i < generatedFiles.Length; ++i)
			{
				if (File.Exists(generatedFiles[i]))
				{
					File.Delete(generatedFiles[i]);
				}
			}
			Debug.Log("Files have been cleaned (" + exportDirectory + ")");
		}
	}
}

public class SketchfabExporterWindow : EditorWindow
{
	private string param_title = "Unity model";
	private string param_description = "Model exported from Unity Engine.";
	private string param_tags = "unity";
	private bool param_autopublish = true;
	private bool param_private = false;
	private string param_password = "";
	private string param_token = "";

	private static string dashboard_url = "https://sketchfab.com/settings/password";
	private SketchfabExporter exporter;
	private bool finished = true;

	[MenuItem("File/Publish to Sketchfab")]
	static void Init()
	{
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX // edit: added Platform Dependent Compilation - win or osx standalone
		SketchfabExporterWindow window = (SketchfabExporterWindow)EditorWindow.GetWindow(typeof(SketchfabExporterWindow));
		window.initialize();
#else // and error dialog if not standalone
		EditorUtility.DisplayDialog("Error", "Your build target must be set to standalone", "Okay");
#endif
	}


	void initialize()
	{
		FileInfo fi = new FileInfo(EditorSceneManager.GetActiveScene().name);
		param_title = Path.GetFileNameWithoutExtension(fi.Name);
	}

	void export()
	{
		finished = false;
		Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);
		if (selection.Length == 0)
		{
			EditorUtility.DisplayDialog("Selection is empty", "Please select one or more target objects", "OK");
			return;
		}

		if (param_token.Trim().Length == 0)
		{
			EditorUtility.DisplayDialog("Invalid API Token", "You can find your token at https://sketchfab.com/dashboard.", "OK");
			return;
		}

		// Check if selection contains meshes
		ArrayList meshList = new ArrayList();
		for (int i = 0; i < selection.Length; i++)
		{
			Component[] meshfilter = selection[i].GetComponentsInChildren(typeof(MeshFilter));
			for (int m = 0; m < meshfilter.Length; m++)
			{
				meshList.Add(meshfilter[m]);
			}

			Component[] skinnermeshrenderer = selection[i].GetComponentsInChildren(typeof(SkinnedMeshRenderer));
			for (int m = 0; m < skinnermeshrenderer.Length; m++)
			{
				meshList.Add(skinnermeshrenderer[m]);
			}
		}

		if (meshList.Count > 0)
		{
			exporter = new SketchfabExporter(param_token, meshList, param_title, param_description, param_tags, param_autopublish, param_private, param_password);
			exporter.export();
		}
		else {
			EditorUtility.DisplayDialog("Invalid selection", "Selection doesn't contain meshes. Ensure that your selection contains at least one mesh in order to have a valid Sketchfab scene", "Okay");
		}
	}

	void OnGUI()
	{
		GUILayout.Label("Model settings", EditorStyles.boldLabel);
		param_title = EditorGUILayout.TextField("Title (Scene name)", param_title); //edit: added name source
		param_description = EditorGUILayout.TextField("Description", param_description);
		param_tags = EditorGUILayout.TextField("Tags", param_tags);

		// edit: contained the password field in a toggle group
		EditorGUILayout.Separator();
		param_autopublish = EditorGUILayout.Toggle("Skip draft mode", param_autopublish);
		param_private = EditorGUILayout.BeginToggleGroup("Private", param_private);
		param_password = EditorGUILayout.PasswordField("Password", param_password);
		EditorGUILayout.EndToggleGroup();

		EditorGUILayout.Separator();
		GUILayout.Label("Sketchfab settings", EditorStyles.boldLabel);
		param_token = EditorGUILayout.PasswordField("API Token", param_token);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel("Find your token");
		if (GUILayout.Button("open dashboard"))
			Application.OpenURL(dashboard_url);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();
		if (exporter != null && !exporter.done())
		{
			GUI.enabled = false;
		}

		if (GUILayout.Button("Upload to Sketchfab"))
		{
			export();
		}

		GUI.enabled = true;
		if (exporter != null && exporter.isActive == true)
		{
			if(exporter.isPublishing == true)
			{
				Rect r = EditorGUILayout.BeginVertical();
				EditorGUI.ProgressBar(r, exporter.progress(), "Upload progress");
				GUILayout.Space(16);
				EditorGUILayout.EndVertical();
			}
			else
			{
				float progress = exporter.exporterParser.getCurrentIndex() / exporter.exporterParser.getNbSelectedObjects();
				Rect r = EditorGUILayout.BeginVertical();
				EditorGUI.ProgressBar(r, progress, "Exporting " + exporter.exporterParser.getCurrentObjectName());
				GUILayout.Space(16);
				EditorGUILayout.EndVertical();
			}
		}
		if (exporter != null && exporter.done() == false)
		{

		}
	}

	public void OnInspectorUpdate()
	{
		Repaint();
		if (exporter != null && exporter.isActive && finished == false)
		{
			if (exporter.isParsingDone() && exporter.isPublishing == false)
			{
				exporter.resetParser();
				Debug.Log("Scene has been exported");
				// publish
				exporter.publish();
			}
			else if (exporter.isUploadingDone() == true)
			{
				finished = true;
				string urlid = exporter.getUid();
				exporter.isActive = false;
				if (urlid.Length > 0)
				{
					string modelUrl = "http://sketchfab.com/models/" + urlid;
					EditorUtility.DisplayDialog("Success", "Model has been successfully uploaded to sketchfab", "View model");
					Application.OpenURL(modelUrl);
				}
				else {
					EditorUtility.DisplayDialog("Error", "The following error occured when uploading your model :" + exporter.getError(), "Close");
				}
				exporter.clean();
			}
		}
	}
}
