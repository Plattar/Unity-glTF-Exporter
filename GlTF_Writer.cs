using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class GlTF_Writer {
	public static StreamWriter jsonWriter;
	public static Stream binFile;
	public static int indent = 0;
	public static string binFileName;
	static bool[] firsts = new bool[100];
	public static GlTF_BufferView ushortBufferView = new GlTF_BufferView("ushortBufferView");
	public static GlTF_BufferView floatBufferView = new GlTF_BufferView("floatBufferView");
	public static GlTF_BufferView vec2BufferView = new GlTF_BufferView("vec2BufferView");
	public static GlTF_BufferView vec3BufferView = new GlTF_BufferView("vec3BufferView");
	public static GlTF_BufferView vec4BufferView = new GlTF_BufferView("vec4BufferView");
	public static List<GlTF_BufferView> bufferViews = new List<GlTF_BufferView>();	
	public static List<GlTF_Camera> cameras = new List<GlTF_Camera>();
	public static List<GlTF_Light> lights = new List<GlTF_Light>();
	public static List<GlTF_Mesh> meshes = new List<GlTF_Mesh>();
	public static List<GlTF_Accessor> accessors = new List<GlTF_Accessor>();
	public static List<GlTF_Node> nodes = new List<GlTF_Node>();
	public static Dictionary<string, GlTF_Material> materials = new Dictionary<string, GlTF_Material>();
	public static Dictionary<string, GlTF_Texture> textures = new Dictionary<string, GlTF_Texture>();
	public static List<GlTF_Sampler> samplers = new List<GlTF_Sampler>();
	public static List<GlTF_Animation> animations = new List<GlTF_Animation>();
	// GlTF_Technique

	public void Init()
	{
		firsts = new bool[100];
		ushortBufferView = new GlTF_BufferView("ushortBufferView");
		floatBufferView = new GlTF_BufferView("floatBufferView");
		vec2BufferView = new GlTF_BufferView("vec2BufferView");
		vec3BufferView = new GlTF_BufferView("vec3BufferView");
		vec4BufferView = new GlTF_BufferView("vec4BufferView");
		bufferViews = new List<GlTF_BufferView>();	
		cameras = new List<GlTF_Camera>();
		lights = new List<GlTF_Light>();
		meshes = new List<GlTF_Mesh>();
		accessors = new List<GlTF_Accessor>();
		nodes = new List<GlTF_Node>();
		materials = new Dictionary<string, GlTF_Material>();
		textures = new Dictionary<string, GlTF_Texture>();
		samplers = new List<GlTF_Sampler>();
		animations = new List<GlTF_Animation>();
		// GlTF_Technique
	}

	public void Indent() {
		for (int i = 0; i < indent; i++)
			jsonWriter.Write ("\t");
	}

	public void IndentIn() {
		indent++;
		firsts[indent] = true;
	}

	public void IndentOut() {
		indent--;
	}

	public void CommaStart() {
		firsts[indent] = false;
	}

	public void CommaNL() {
		if (!firsts[indent])
			jsonWriter.Write (",\n");
		//		else
		//			jsonWriter.Write ("\n");
		firsts[indent] = false;
	}

	public string name; // name of this object

	public void OpenFiles (string filepath) {
		jsonWriter = new StreamWriter (filepath);
		binFileName = Path.GetFileNameWithoutExtension (filepath) + ".bin";
		binFile = File.Open(binFileName, FileMode.Create);
		//		binWriter = new BinaryWriter (File.Open(binFileName, FileMode.Create));
		//		binWriter = new BinaryWriter (File.Open(binFileName, FileMode.Create));
	}

	public void CloseFiles() {

		binFile.Close();
		jsonWriter.Close ();
	}

	public virtual void Write () {

		bufferViews.Add (ushortBufferView);
		bufferViews.Add (floatBufferView);
		bufferViews.Add (vec2BufferView);
		bufferViews.Add (vec3BufferView);
		bufferViews.Add (vec4BufferView);

		// write memory streams to binary file
		ushortBufferView.byteOffset = 0;
		ushortBufferView.memoryStream.WriteTo(binFile);
		floatBufferView.byteOffset = ushortBufferView.byteLength;
		floatBufferView.memoryStream.WriteTo(binFile);
		vec2BufferView.byteOffset = floatBufferView.byteOffset + floatBufferView.byteLength;
		vec2BufferView.memoryStream.WriteTo (binFile);
		vec3BufferView.byteOffset = vec2BufferView.byteOffset + vec2BufferView.byteLength;
		vec3BufferView.memoryStream.WriteTo (binFile);
		vec4BufferView.byteOffset = vec3BufferView.byteOffset + vec3BufferView.byteLength;
		vec4BufferView.memoryStream.WriteTo (binFile);

		jsonWriter.Write ("{\n");
		IndentIn();

		// FIX: Should support multiple buffers
		CommaNL();
		Indent();	jsonWriter.Write ("\"buffers\": {\n");
		IndentIn();
		Indent();	jsonWriter.Write ("\"" + Path.GetFileNameWithoutExtension(GlTF_Writer.binFileName) +"\": {\n");
		IndentIn();
		Indent();	jsonWriter.Write ("\"byteLength\": "+ (vec4BufferView.byteOffset+vec4BufferView.byteLength)+",\n");
		Indent();	jsonWriter.Write ("\"type\": \"arraybuffer\",\n");
		Indent();	jsonWriter.Write ("\"uri\": \"" + GlTF_Writer.binFileName + "\"\n");

		IndentOut();
		Indent();	jsonWriter.Write ("}\n");

		IndentOut();
		Indent();	jsonWriter.Write ("}");

		if (cameras != null && cameras.Count > 0)
		{
			CommaNL();
			Indent();		jsonWriter.Write ("\"cameras\": {\n");
			IndentIn();
			foreach (GlTF_Camera c in cameras)
			{
				CommaNL();
				c.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();		jsonWriter.Write ("}");
		}

		if (accessors != null && accessors.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"accessors\": {\n");
			IndentIn();
			foreach (GlTF_Accessor a in accessors)
			{
				CommaNL();
				a.Write ();
			}			
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (bufferViews != null && bufferViews.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"bufferViews\": {\n");
			IndentIn();
			foreach (GlTF_BufferView bv in bufferViews)
			{
				CommaNL();
				bv.Write ();
			}			
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (meshes != null && meshes.Count > 0)
		{
			CommaNL();
			Indent();
			jsonWriter.Write ("\"meshes\": {\n");
			IndentIn();
			foreach (GlTF_Mesh m in meshes)
			{
				CommaNL();
				m.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();
			jsonWriter.Write ("}");
		}

		// if (techniques != null && techniques.Count > 0)
		CommaNL();
		string tqs = @"
	'techniques': {
		'technique1': {
			'parameters': {
				'ambient': {
					'type': 35666
				},
				'diffuse': {
					'type': 35678
				},
				'emission': {
					'type': 35666
				},
				'light0Color': {
					'type': 35665,
					'value': [
					    1,
					    1,
					    1
					    ]
				},
				'light0Transform': {
					'semantic': 'MODELVIEW',
					'source': 'directionalLight1',
					'type': 35676
				},
				'modelViewMatrix': {
					'semantic': 'MODELVIEW',
					'type': 35676
				},
				'normal': {
					'semantic': 'NORMAL',
					'type': 35665
				},
				'normalMatrix': {
					'semantic': 'MODELVIEWINVERSETRANSPOSE',
					'type': 35675
				},
				'position': {
					'semantic': 'POSITION',
					'type': 35665
				},
				'projectionMatrix': {
					'semantic': 'PROJECTION',
					'type': 35676
				},
				'shininess': {
					'type': 5126
				},
				'specular': {
					'type': 35666
				},
				'texcoord0': {
					'semantic': 'TEXCOORD_0',
					'type': 35664
				}
			},
			'pass': 'defaultPass',
			'passes': {
				'defaultPass': {
					'details': {
						'commonProfile': {
							'extras': {
								'doubleSided': false
							},
							'lightingModel': 'Blinn',
							'parameters': [
							    'ambient',
							    'diffuse',
							    'emission',
							    'light0Color',
							    'light0Transform',
							    'modelViewMatrix',
							    'normalMatrix',
							    'projectionMatrix',
							    'shininess',
							    'specular'
							    ],
							'texcoordBindings': {
								'diffuse': 'TEXCOORD_0'
							}
						},
						'type': 'COLLADA-1.4.1/commonProfile'
					},
					'instanceProgram': {
						'attributes': {
							'a_normal': 'normal',
							'a_position': 'position',
							'a_texcoord0': 'texcoord0'
						},
						'program': 'program_0',
						'uniforms': {
							'u_ambient': 'ambient',
							'u_diffuse': 'diffuse',
							'u_emission': 'emission',
							'u_light0Color': 'light0Color',
							'u_light0Transform': 'light0Transform',
							'u_modelViewMatrix': 'modelViewMatrix',
							'u_normalMatrix': 'normalMatrix',
							'u_projectionMatrix': 'projectionMatrix',
							'u_shininess': 'shininess',
							'u_specular': 'specular'
						}
					},
					'states': {
						'enable': [
						    2884,
						    2929
						    ]
					}
				}
			}
		}
	}";
		tqs = tqs.Replace ("'", "\"");
		jsonWriter.Write (tqs);

		if (samplers.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"samplers\": {\n");
			IndentIn();
			foreach (GlTF_Sampler s in samplers)
			{
				CommaNL();
				s.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (textures.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"textures\": {\n");
			IndentIn();
			foreach (KeyValuePair<string,GlTF_Texture> t in textures)
			{
				CommaNL();
				t.Value.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (materials.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"materials\": {\n");
			IndentIn();
			foreach (KeyValuePair<string,GlTF_Material> m in materials)
			{
				CommaNL();
				m.Value.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (animations.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"animations\": {\n");
			IndentIn();
			foreach (GlTF_Animation a in animations)
			{
				CommaNL();
				a.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (nodes != null && nodes.Count > 0)
		{
			CommaNL();
			/*
		    "nodes": {
        "node-Alien": {
            "children": [],
            "matrix": [
*/
			Indent();			jsonWriter.Write ("\"nodes\": {\n");
			IndentIn();
			//			bool first = true;
			foreach (GlTF_Node n in nodes)
			{
				CommaNL();
				//				if (!first)
				//					jsonWriter.Write (",\n");
				n.Write ();
				//				first = false;
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();			jsonWriter.Write ("}");

		}
		CommaNL();

		Indent();			jsonWriter.Write ("\"scene\": \"defaultScene\",\n");
		Indent();			jsonWriter.Write ("\"scenes\": {\n");
		IndentIn();
		Indent();			jsonWriter.Write ("\"defaultScene\": {\n");
		IndentIn();
		CommaNL();
		Indent();			jsonWriter.Write ("\"nodes\": [\n");
		IndentIn();
		foreach (GlTF_Node n in nodes)
		{
			if (!n.hasParent)
			{
				CommaNL();
				Indent();		jsonWriter.Write ("\"node-" + n.name + "\"");
			}
		}
		jsonWriter.WriteLine();
		IndentOut();
		Indent();			jsonWriter.Write ("]\n");
		IndentOut();
		Indent();			jsonWriter.Write ("}\n");
		IndentOut();
		Indent();			jsonWriter.Write ("}\n");
		IndentOut();
		Indent();			jsonWriter.Write ("}");
	}
}
