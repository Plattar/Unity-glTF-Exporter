using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;

public class GlTFExporterWindow : EditorWindow
{
    const string KEY_PATH = "GlTFPath";
    const string KEY_FILE = "GlTFFile";
    static public string path = "?";
    static string savedPath = EditorPrefs.GetString(KEY_PATH, "/");
    static string savedFile = EditorPrefs.GetString(KEY_FILE, "test.gltf");
    static XmlDocument xdoc;

    static Preset preset = new Preset();
    static UnityEngine.TextAsset presetAsset;
    GameObject exporterGo;
    SceneToGlTFWiz exporter;

    //EditorPrefs.SetString(KEY_PATH, savedPath);
	//EditorPrefs.SetString(KEY_FILE, savedFile);
    [MenuItem("File/Export/glTF")]
    static void CreateWizard()
    {
        savedPath = EditorPrefs.GetString(KEY_PATH, "/");
        savedFile = EditorPrefs.GetString(KEY_FILE, "test.gltf");
        path = savedPath + "/" + savedFile;
        //		ScriptableWizard.DisplayWizard("Export Selected Stuff to glTF", typeof(SceneToGlTFWiz), "Export");

        GlTFExporterWindow window = (GlTFExporterWindow)EditorWindow.GetWindow(typeof(GlTFExporterWindow));
        window.Show();
    }

    void OnWizardUpdate()
    {
        //		Texture[] txs = Selection.GetFiltered(Texture, SelectionMode.Assets);
        //		Debug.Log("found "+txs.Length);
    }

    void OnGUI()
    {
        GUILayout.Label("Export Options");
        GlTF_Writer.binary = GUILayout.Toggle(GlTF_Writer.binary, "Binary GlTF");
        // Force animation baking for now
        GlTF_Writer.bakeAnimation = GUILayout.Toggle(true, "Bake animations (forced for now)");
        presetAsset = EditorGUILayout.ObjectField("Preset file", presetAsset, typeof(UnityEngine.TextAsset), false) as UnityEngine.TextAsset;
        if (!exporterGo)
        {
            exporterGo = new GameObject("exporter");
        }
        if(!exporter)
        {
            exporter = exporterGo.AddComponent<SceneToGlTFWiz>();
        }
        GUI.enabled = (Selection.GetTransforms(SelectionMode.Deep).Length > 0);
        if (GUILayout.Button("Export to glTF"))
        {
            OnWizardCreate();
        }
        GUI.enabled = true;
    }

    void OnDestroy()
    {
        GameObject.DestroyImmediate(exporterGo);
        exporter = null;
    }

    void OnWizardCreate() // Create (Export) button has been hit (NOT wizard has been created!)
    {
        var ext = GlTF_Writer.binary ? "glb" : "gltf";
        path = EditorUtility.SaveFilePanel("Save glTF file as", savedPath, savedFile, ext);
        if (path.Length != 0)
        {
            exporter.ExportCoroutine(path, null, true);
        }
    }
}
