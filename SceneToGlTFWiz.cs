/***************************************************************************
GlamExport
 - Unity3D Scriptable Wizard to export Hierarchy or Project objects as glTF


****************************************************************************/
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Reflection;
using Ionic.Zip;

public enum IMAGETYPE
{
	GRAYSCALE,
	RGB,
	RGBA,
	RGBA_OPAQUE,
	RG, // Metal/Roughness texture
	NORMAL_MAP,
	IGNORE
}

public class SceneToGlTFWiz : MonoBehaviour
{
	public int jpgQuality = 85;

	public GlTF_Writer writer;
	string savedPath = "";
	int nbSelectedObjects = 0;

	static bool done = true;
	bool parseLightmaps = false;

	public static void parseUnityCamera(Transform tr)
	{
		if (tr.GetComponent<Camera>().orthographic)
		{
			GlTF_Orthographic cam;
			cam = new GlTF_Orthographic();
			cam.type = "orthographic";
			cam.zfar = tr.GetComponent<Camera>().farClipPlane;
			cam.znear = tr.GetComponent<Camera>().nearClipPlane;
			cam.name = GlTF_Writer.cleanNonAlphanumeric(tr.name);
			//cam.orthographic.xmag = tr.camera.
			GlTF_Writer.cameras.Add(cam);
		}
		else
		{
			GlTF_Perspective cam;
			cam = new GlTF_Perspective();
			cam.type = "perspective";
			cam.zfar = tr.GetComponent<Camera>().farClipPlane;
			cam.znear = tr.GetComponent<Camera>().nearClipPlane;
			cam.aspect_ratio = tr.GetComponent<Camera>().aspect;
			cam.yfov = tr.GetComponent<Camera>().fieldOfView;
			cam.name = GlTF_Writer.cleanNonAlphanumeric(tr.name);
			GlTF_Writer.cameras.Add(cam);
		}
	}

	public bool isDone()
	{
		return done;
	}

	public void resetParser()
	{
		done = false;
	}

	public static void parseUnityLight(Transform tr)
	{
		switch (tr.GetComponent<Light>().type)
		{
			case LightType.Point:
				GlTF_PointLight pl = new GlTF_PointLight();
				pl.color = new GlTF_ColorRGB(tr.GetComponent<Light>().color);
				pl.name = GlTF_Writer.cleanNonAlphanumeric(tr.name);
				GlTF_Writer.lights.Add(pl);
				break;

			case LightType.Spot:
				GlTF_SpotLight sl = new GlTF_SpotLight();
				sl.color = new GlTF_ColorRGB(tr.GetComponent<Light>().color);
				sl.name = GlTF_Writer.cleanNonAlphanumeric(tr.name);
				GlTF_Writer.lights.Add(sl);
				break;

			case LightType.Directional:
				GlTF_DirectionalLight dl = new GlTF_DirectionalLight();
				dl.color = new GlTF_ColorRGB(tr.GetComponent<Light>().color);
				dl.name = GlTF_Writer.cleanNonAlphanumeric(tr.name);
				GlTF_Writer.lights.Add(dl);
				break;

			case LightType.Area:
				GlTF_AmbientLight al = new GlTF_AmbientLight();
				al.color = new GlTF_ColorRGB(tr.GetComponent<Light>().color);
				al.name = GlTF_Writer.cleanNonAlphanumeric(tr.name);
				GlTF_Writer.lights.Add(al);
				break;
		}
	}

	public void ExportCoroutine(string path, Preset presetAsset, bool buildZip, bool exportPBRMaterials, bool exportAnimation = true, bool doConvertImages = true)
	{
		StartCoroutine(Export(path, presetAsset, buildZip, exportPBRMaterials, exportAnimation, doConvertImages));
	}

	public int getNbSelectedObjects()
	{
		return nbSelectedObjects;
	}

	public IEnumerator Export(string path, Preset presetAsset, bool buildZip, bool exportPBRMaterials, bool exportAnimation = true, bool doConvertImages = false)
	{
		writer = new GlTF_Writer();
		writer.Init ();
		done = false;
		bool debugRightHandedScale = false;
		bool splitTextures = false;
		GlTF_Writer.exportedFiles.Clear();
		if (debugRightHandedScale)
			GlTF_Writer.convertRightHanded = false;

		writer.extraString.Add("exporterVersion", GlTF_Writer.exporterVersion );

		// Create rootNode
		GlTF_Node correctionNode = new GlTF_Node();
		correctionNode.id = "UnityGlTF_correctionMatrix";
		correctionNode.name = "UnityGlTF_correctionMatrix";

		// Add correction matrix to reorient scene for left to right-handed coordinate systems
		Quaternion correctionQuat = Quaternion.Euler(0, 180, 0);
		writer.convertQuatLeftToRightHandedness(ref correctionQuat);
		Matrix4x4 correctionMat = Matrix4x4.TRS(Vector3.zero, correctionQuat, Vector3.one);
		GlTF_Writer.sceneRootMatrix = correctionMat;
		correctionNode.matrix = new GlTF_Matrix(correctionMat, false);
		GlTF_Writer.nodes.Add(correctionNode);
		GlTF_Writer.nodeNames.Add(correctionNode.name);
		GlTF_Writer.rootNodes.Add(correctionNode);


		// Check if scene has lightmap data
		bool hasLightmap = LightmapSettings.lightmaps.Length != 0;

		//path = toGlTFname(path);
		savedPath = Path.GetDirectoryName(path);

		// Temp list to keep track of skeletons
		Dictionary<string, GlTF_Skin> parsedSkins = new Dictionary<string, GlTF_Skin>();
		parsedSkins.Clear();

		// first, collect objects in the scene, add to lists
		Transform[] transforms = Selection.GetTransforms (SelectionMode.Deep);
		List<Transform> trs = new List<Transform>(transforms);
		// Prefilter selected nodes and look for skinning in order to list "bones" nodes
		//FIXME: improve this
		List<Transform> bones = new List<Transform>();
		foreach(Transform tr in trs)
		{
			if (!tr.gameObject.activeSelf)
				continue;

			SkinnedMeshRenderer skin = tr.GetComponent<SkinnedMeshRenderer>();
			if (skin)
			{
				foreach(Transform bone in skin.bones)
				{
					bones.Add(bone);
				}
			}
		}

		nbSelectedObjects = trs.Count;
		int nbDisabledObjects = 0;
		foreach (Transform tr in trs)
		{
			if (tr.gameObject.activeInHierarchy == false)
			{
				nbDisabledObjects++;
				continue;
			}

			// Initialize the node
			GlTF_Node node = new GlTF_Node();
			node.id = GlTF_Node.GetNameFromObject(tr);
			node.name = GlTF_Writer.cleanNonAlphanumeric(tr.name);

			if (tr.GetComponent<Camera>() != null)
				parseUnityCamera(tr);

			if (tr.GetComponent<Light>() != null)
				parseUnityLight(tr);

			Mesh m = GetMesh(tr);
			if (m != null)
			{
				GlTF_Mesh mesh = new GlTF_Mesh();
				mesh.name = GlTF_Writer.cleanNonAlphanumeric(GlTF_Mesh.GetNameFromObject(m) + tr.name);

				GlTF_Accessor positionAccessor = new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(m, "position"), GlTF_Accessor.Type.VEC3, GlTF_Accessor.ComponentType.FLOAT);
				positionAccessor.bufferView = GlTF_Writer.vec3BufferView;
				GlTF_Writer.accessors.Add (positionAccessor);

				GlTF_Accessor normalAccessor = null;
				if (m.normals.Length > 0)
				{
					normalAccessor = new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(m, "normal"), GlTF_Accessor.Type.VEC3, GlTF_Accessor.ComponentType.FLOAT);
					normalAccessor.bufferView = GlTF_Writer.vec3BufferView;
					GlTF_Writer.accessors.Add (normalAccessor);
				}

				GlTF_Accessor colorAccessor = null;
				if (m.colors.Length > 0)
				{
					colorAccessor = new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(m, "color"), GlTF_Accessor.Type.VEC4, GlTF_Accessor.ComponentType.FLOAT);
					colorAccessor.bufferView = GlTF_Writer.vec4BufferView;
					GlTF_Writer.accessors.Add(colorAccessor);
				}

				GlTF_Accessor uv0Accessor = null;
				if (m.uv.Length > 0) {
					uv0Accessor =  new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(m, "uv0"), GlTF_Accessor.Type.VEC2, GlTF_Accessor.ComponentType.FLOAT);
					uv0Accessor.bufferView = GlTF_Writer.vec2BufferView;
					GlTF_Writer.accessors.Add (uv0Accessor);
				}

				GlTF_Accessor uv1Accessor = null;
				if (m.uv2.Length > 0) {
					// check if object is affected by a lightmap
					uv1Accessor =  new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(m, "uv1"), GlTF_Accessor.Type.VEC2, GlTF_Accessor.ComponentType.FLOAT);
					uv1Accessor.bufferView = GlTF_Writer.vec2BufferView;
					GlTF_Writer.accessors.Add (uv1Accessor);
				}

				GlTF_Accessor uv2Accessor = null;
				if (m.uv3.Length > 0) {
					uv2Accessor =  new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(m, "uv2"), GlTF_Accessor.Type.VEC2, GlTF_Accessor.ComponentType.FLOAT);
					uv2Accessor.bufferView = GlTF_Writer.vec2BufferView;
					GlTF_Writer.accessors.Add (uv2Accessor);
				}

				GlTF_Accessor uv3Accessor = null;
				if (m.uv4.Length > 0) {
					uv3Accessor =  new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(m, "uv3"), GlTF_Accessor.Type.VEC2, GlTF_Accessor.ComponentType.FLOAT);
					uv3Accessor.bufferView = GlTF_Writer.vec2BufferView;
					GlTF_Writer.accessors.Add (uv3Accessor);
				}

				GlTF_Accessor jointAccessor = null;
				if (exportAnimation && m.boneWeights.Length > 0)
				{
					jointAccessor = new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(m, "joints"), GlTF_Accessor.Type.VEC4, GlTF_Accessor.ComponentType.FLOAT);
					jointAccessor.bufferView = GlTF_Writer.vec4BufferView;
					GlTF_Writer.accessors.Add(jointAccessor);
				}

				GlTF_Accessor weightAccessor = null;
				if (exportAnimation && m.boneWeights.Length > 0)
				{
					weightAccessor = new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(m, "weights"), GlTF_Accessor.Type.VEC4, GlTF_Accessor.ComponentType.FLOAT);
					weightAccessor.bufferView = GlTF_Writer.vec4BufferView;
					GlTF_Writer.accessors.Add(weightAccessor);
				}

				GlTF_Accessor tangentAccessor = null;
				if (m.tangents.Length > 0)
				{
					tangentAccessor = new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(m, "tangents"), GlTF_Accessor.Type.VEC4, GlTF_Accessor.ComponentType.FLOAT);
					tangentAccessor.bufferView = GlTF_Writer.vec4BufferView;
					GlTF_Writer.accessors.Add(tangentAccessor);
				}

				var smCount = m.subMeshCount;
				for (var i = 0; i < smCount; ++i)
				{
					GlTF_Primitive primitive = new GlTF_Primitive();
					primitive.name = GlTF_Primitive.GetNameFromObject(m, i);
					primitive.index = i;
					GlTF_Attributes attributes = new GlTF_Attributes();
					attributes.positionAccessor = positionAccessor;
					attributes.normalAccessor = normalAccessor;
					attributes.colorAccessor = colorAccessor;
					attributes.texCoord0Accessor = uv0Accessor;
					attributes.texCoord1Accessor = uv1Accessor;
					attributes.texCoord2Accessor = uv2Accessor;
					attributes.texCoord3Accessor = uv3Accessor;
					attributes.jointAccessor = jointAccessor;
					attributes.weightAccessor = weightAccessor;
					attributes.tangentAccessor = tangentAccessor;
					primitive.attributes = attributes;
					GlTF_Accessor indexAccessor = new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(m, "indices_" + i), GlTF_Accessor.Type.SCALAR, GlTF_Accessor.ComponentType.USHORT);
					indexAccessor.bufferView = GlTF_Writer.ushortBufferView;
					GlTF_Writer.accessors.Add (indexAccessor);
					primitive.indices = indexAccessor;

					var mr = GetRenderer(tr);
					var sm = mr.sharedMaterials;
					if (i < sm.Length) {
						var mat = sm[i];
						var matName = GlTF_Material.GetNameFromObject(mat);
						if(GlTF_Writer.materialNames.Contains(matName))
						{
							primitive.materialIndex = GlTF_Writer.materialNames.IndexOf(matName); // THIS INDIRECTION CAN BE REMOVED!
						}
						else
						{
							GlTF_Material material = new GlTF_Material();
							material.name = GlTF_Writer.cleanNonAlphanumeric(mat.name);
							primitive.materialIndex = GlTF_Writer.materials.Count;
							GlTF_Writer.materialNames.Add(matName);
							GlTF_Writer.materials.Add (material);

							//technique
							var s = mat.shader;
							var techName = GlTF_Technique.GetNameFromObject(s);
							if(GlTF_Writer.techniqueNames.Contains(techName))
							{
								material.instanceTechniqueIndex = GlTF_Writer.techniqueNames.IndexOf(techName);// THIS INDIRECTION CAN BE REMOVED!
							}
							else
							{
								GlTF_Technique tech = new GlTF_Technique();
								tech.name = techName;
								GlTF_Technique.Parameter tParam = new GlTF_Technique.Parameter();
								tParam.name = "position";
								tParam.type = GlTF_Technique.Type.FLOAT_VEC3;
								tParam.semantic = GlTF_Technique.Semantic.POSITION;
								tech.parameters.Add(tParam);
								GlTF_Technique.Attribute tAttr = new GlTF_Technique.Attribute();
								tAttr.name = "a_position";
								tAttr.param = tParam.name;
								tech.attributes.Add(tAttr);

								if (normalAccessor != null)
								{
									tParam = new GlTF_Technique.Parameter();
									tParam.name = "normal";
									tParam.type = GlTF_Technique.Type.FLOAT_VEC3;
									tParam.semantic = GlTF_Technique.Semantic.NORMAL;
									tech.parameters.Add(tParam);
									tAttr = new GlTF_Technique.Attribute();
									tAttr.name = "a_normal";
									tAttr.param = tParam.name;
									tech.attributes.Add(tAttr);
								}

								if (uv0Accessor != null)
								{
									tParam = new GlTF_Technique.Parameter();
									tParam.name = "texcoord0";
									tParam.type = GlTF_Technique.Type.FLOAT_VEC2;
									tParam.semantic = GlTF_Technique.Semantic.TEXCOORD_0;
									tech.parameters.Add(tParam);
									tAttr = new GlTF_Technique.Attribute();
									tAttr.name = "a_texcoord0";
									tAttr.param = tParam.name;
									tech.attributes.Add(tAttr);
								}

								if (uv1Accessor != null)
								{
									tParam = new GlTF_Technique.Parameter();
									tParam.name = "texcoord1";
									tParam.type = GlTF_Technique.Type.FLOAT_VEC2;
									tParam.semantic = GlTF_Technique.Semantic.TEXCOORD_1;
									tech.parameters.Add(tParam);
									tAttr = new GlTF_Technique.Attribute();
									tAttr.name = "a_texcoord1";
									tAttr.param = tParam.name;
									tech.attributes.Add(tAttr);
								}

								if (uv2Accessor != null)
								{
									tParam = new GlTF_Technique.Parameter();
									tParam.name = "texcoord2";
									tParam.type = GlTF_Technique.Type.FLOAT_VEC2;
									tParam.semantic = GlTF_Technique.Semantic.TEXCOORD_2;
									tech.parameters.Add(tParam);
									tAttr = new GlTF_Technique.Attribute();
									tAttr.name = "a_texcoord2";
									tAttr.param = tParam.name;
									tech.attributes.Add(tAttr);
								}

								if (uv3Accessor != null)
								{
									tParam = new GlTF_Technique.Parameter();
									tParam.name = "texcoord3";
									tParam.type = GlTF_Technique.Type.FLOAT_VEC2;
									tParam.semantic = GlTF_Technique.Semantic.TEXCOORD_3;
									tech.parameters.Add(tParam);
									tAttr = new GlTF_Technique.Attribute();
									tAttr.name = "a_texcoord3";
									tAttr.param = tParam.name;
									tech.attributes.Add(tAttr);
								}

								tech.AddDefaultUniforms();

								// Populate technique with shader data
								GlTF_Writer.techniqueNames.Add (techName);
								GlTF_Writer.techniques.Add (tech);

								// create program
								GlTF_Program program = new GlTF_Program();
								program.name = GlTF_Program.GetNameFromObject(s);
								tech.program = program.name;
								foreach (var attr in tech.attributes)
								{
									program.attributes.Add(attr.name);
								}
								GlTF_Writer.programs.Add(program);
							}

							unityToPBRMaterial(mat, ref material, doConvertImages, splitTextures);
							// Handle lightmap
							if(parseLightmaps && hasLightmap)
							{
								KeyValuePair<GlTF_Texture,GlTF_Image> lightmapdata = exportLightmap(tr, ref primitive, ref material);
								if(lightmapdata.Key != null)
								{
									GlTF_Writer.textureNames.Add(lightmapdata.Key.name);
									GlTF_Writer.textures.Add(lightmapdata.Key);
									GlTF_Writer.images.Add(lightmapdata.Value);
								}
							}
						}
					}
					mesh.primitives.Add(primitive);
				}

				SkinnedMeshRenderer skin = tr.GetComponent<SkinnedMeshRenderer>();
				Mesh baked = m;
				// If skinned, bake mesh in order to end with good transforms
				// (Unity skinning directly uses mesh to deform it, and doesn't care about transform anymore)
				// Baking allow to take the current transform into account.
				// FIXME: could also avoid baking and use the mesh directly and reset the transform
				if (exportAnimation && skin)
				{
					baked = new Mesh();
					skin.BakeMesh(baked);
					baked.uv = m.uv;
					baked.uv2 = m.uv2;
					baked.uv3 = m.uv3;
					baked.uv4 = m.uv4;

					baked.bindposes = m.bindposes;
					baked.boneWeights = m.boneWeights;

					Matrix4x4 correction = Matrix4x4.TRS(tr.localPosition, tr.localRotation, tr.lossyScale).inverse * Matrix4x4.TRS(tr.localPosition, tr.localRotation, Vector3.one);
					if(!correction.isIdentity)
					{
						Vector3[] verts = baked.vertices;
						Vector3[] norms = baked.normals;
						Vector4[] tangents = baked.tangents;
						for (int i = 0; i < verts.Length; ++i)
						{
							verts[i] = correction.MultiplyPoint3x4(verts[i]);
							norms[i] = correction.MultiplyVector(norms[i]);
							norms[i].Normalize();
						}
						baked.vertices = verts;
						baked.normals = norms;
						baked.RecalculateBounds();
					}
				}

				mesh.Populate(baked);
				GlTF_Writer.meshes.Add(mesh);
				node.meshIndex = GlTF_Writer.meshes.IndexOf(mesh);
			}

			// Parse animations
			if (exportAnimation)
			{
				Animator a = tr.GetComponent<Animator>();
				if (a != null)
				{
					AnimationClip[] clips = AnimationUtility.GetAnimationClips(tr.gameObject);
					for (int i = 0; i < clips.Length; i++)
					{
						//FIXME It seems not good to generate one animation per animator.
						GlTF_Animation anim = new GlTF_Animation(GlTF_Writer.cleanNonAlphanumeric(a.name));
						anim.Populate(clips[i], tr, GlTF_Writer.bakeAnimation);
						if(anim.channels.Count > 0)
							GlTF_Writer.animations.Add(anim);
					}
				}

				Animation animation = tr.GetComponent<Animation>();
				if (animation != null)
				{
					AnimationClip clip = animation.clip;
					//FIXME It seems not good to generate one animation per animator.
					GlTF_Animation anim = new GlTF_Animation(GlTF_Writer.cleanNonAlphanumeric(animation.name));
					anim.Populate(clip, tr, GlTF_Writer.bakeAnimation);
					if (anim.channels.Count > 0)
						GlTF_Writer.animations.Add(anim);
				}
			}

			// Parse transform
			if (tr.parent == null)
			{
				Matrix4x4 mat = Matrix4x4.identity;
				if(debugRightHandedScale)
					mat.m22 = -1;
				mat = mat * Matrix4x4.TRS(tr.localPosition, tr.localRotation, tr.localScale);
				node.matrix = new GlTF_Matrix(mat);
			}
			// Use good transform if parent object is not in selection
			else if (!trs.Contains(tr.parent))
			{
				node.hasParent = false;
				Matrix4x4 mat = Matrix4x4.identity;
				if(debugRightHandedScale)
					mat.m22 = -1;
				mat = mat * tr.localToWorldMatrix;
				node.matrix = new GlTF_Matrix(mat);
			}
			else
			{
				node.hasParent = true;
				if (tr.localPosition != Vector3.zero)
					node.translation = new GlTF_Translation (tr.localPosition);
				if (tr.localScale != Vector3.one)
					node.scale = new GlTF_Scale (tr.localScale);
				if (tr.localRotation != Quaternion.identity)
					node.rotation = new GlTF_Rotation (tr.localRotation);
			}

			if(!node.hasParent)
				correctionNode.childrenNames.Add(node.id);

			if (tr.GetComponent<Camera>() != null)
			{
				node.cameraName = GlTF_Writer.cleanNonAlphanumeric(tr.name);
			}
			else if (tr.GetComponent<Light>() != null)
				node.lightName = GlTF_Writer.cleanNonAlphanumeric(tr.name);

			// Parse node's skin data
			GlTF_Accessor invBindMatrixAccessor = null;
			SkinnedMeshRenderer skinMesh = tr.GetComponent<SkinnedMeshRenderer>();
			if (exportAnimation && skinMesh != null && skinMesh.enabled && checkSkinValidity(skinMesh, trs) && skinMesh.rootBone != null)
			{
				node.skeletons = GlTF_Skin.findRootSkeletons(skinMesh);
				GlTF_Skin skin = new GlTF_Skin();
				skin.setBindShapeMatrix(tr);
				skin.name = GlTF_Writer.cleanNonAlphanumeric(skinMesh.rootBone.name) + "_skeleton_" + GlTF_Writer.cleanNonAlphanumeric(node.name) + tr.GetInstanceID();

				// Create invBindMatrices accessor
				invBindMatrixAccessor = new GlTF_Accessor(skin.name + "invBindMatrices", GlTF_Accessor.Type.MAT4, GlTF_Accessor.ComponentType.FLOAT);
				invBindMatrixAccessor.bufferView = GlTF_Writer.mat4BufferView;
				GlTF_Writer.accessors.Add(invBindMatrixAccessor);

				// Generate skin data
				skin.Populate(tr, ref invBindMatrixAccessor, GlTF_Writer.accessors.Count -1);
				GlTF_Writer.skins.Add(skin);
				node.skinIndex = GlTF_Writer.skins.IndexOf(skin);
			}

			// The node is a bone?
			if (exportAnimation && bones.Contains(tr))
				node.jointName = GlTF_Node.GetNameFromObject(tr);

			foreach (Transform t in tr.transform)
			{
				if(t.gameObject.activeInHierarchy)
					node.childrenNames.Add(GlTF_Node.GetNameFromObject(t));
			}

			GlTF_Writer.nodeNames.Add(node.id);
			GlTF_Writer.nodes.Add (node);
		}

		if (GlTF_Writer.meshes.Count == 0)
		{
			Debug.Log("No visible objects have been exported. Aboring export");
			yield return false;
		}

		writer.OpenFiles(path);
		writer.Write ();
		writer.CloseFiles();

		if(nbDisabledObjects > 0)
			Debug.Log(nbDisabledObjects + " disabled object ignored during export");

		Debug.Log("Scene has been exported to " + path);
		if(buildZip)
		{
			ZipFile zip = new ZipFile();
			Debug.Log(GlTF_Writer.exportedFiles.Count + " files generated");
			string zipName = Path.GetFileNameWithoutExtension(path) + ".zip";
			foreach(string originFilePath in GlTF_Writer.exportedFiles.Keys)
			{
				zip.AddFile(originFilePath, GlTF_Writer.exportedFiles[originFilePath]);
			}
			
			zip.Save(savedPath + "/" + zipName);

			// Remove all files
			foreach (string pa in GlTF_Writer.exportedFiles.Keys)
			{
				if (System.IO.File.Exists(pa))
					System.IO.File.Delete(pa);
			}

			Debug.Log("Files have been cleaned");
		}
		done = true;

		yield return true;
	}

	// Check if all the bones referenced by the skin are in the selection
	public bool checkSkinValidity(SkinnedMeshRenderer skin, List<Transform> selection)
	{
		string unselected = "";
		foreach(Transform t in skin.bones)
		{
			if (!selection.Contains(t))
			{
				unselected = unselected + "\n" + t.name;
			}
		}

		if(unselected.Length > 0)
		{
			Debug.LogError("Error while exportin skin for " + skin.name + " (skipping skinning export).\nClick for more details:\n \nThe following bones are used but are not selected" + unselected + "\n");
			return false;
		}

		return true;
	}

	public KeyValuePair<GlTF_Texture, GlTF_Image> exportLightmap(Transform tr, ref GlTF_Primitive primitive, ref GlTF_Material material)
	{
		MeshRenderer meshRenderer = tr.GetComponent<MeshRenderer>();
		KeyValuePair<GlTF_Texture, GlTF_Image> lightmapKV = new KeyValuePair<GlTF_Texture, GlTF_Image>();
		if(!meshRenderer || meshRenderer.lightmapIndex == -1)
		{
			Debug.Log("[ExportLightmap] No mesh renderer, return");
			return lightmapKV;
		}

		//FIXME what if object has no lightmap ?
		LightmapData lightmap = LightmapSettings.lightmaps[meshRenderer.lightmapIndex];
		Texture2D lightmapTex = lightmap.lightmapLight;

		// Handle UV lightmaps
		MeshFilter meshfilter = tr.GetComponent<MeshFilter>();
		if (meshfilter)
		{
			GlTF_Accessor lightmapUVAccessor = new GlTF_Accessor(GlTF_Accessor.GetNameFromObject(GetMesh(tr), "uv4"), GlTF_Accessor.Type.VEC2, GlTF_Accessor.ComponentType.FLOAT);
			lightmapUVAccessor.bufferView = GlTF_Writer.vec2BufferView;
			GlTF_Writer.accessors.Add(lightmapUVAccessor);
			Vector4 scaleOffset = meshRenderer.lightmapScaleOffset;
			lightmapUVAccessor.scaleValues = new Vector2(scaleOffset[0], scaleOffset[1]);
			lightmapUVAccessor.offsetValues = new Vector2(scaleOffset[2], scaleOffset[3]);
			primitive.attributes.lightmapTexCoordAccessor = lightmapUVAccessor;
		}

		string lightmapTexName = GlTF_Texture.GetNameFromObject(lightmapTex);
		if(!GlTF_Writer.textureNames.Contains(lightmapTexName))
		{
			//Generate lightmap
			Texture2D convertedLightmap = new Texture2D(lightmapTex.width, lightmapTex.height, TextureFormat.RGB24, false);
			Color[] lightmapPixels;
			getPixelsFromTexture(ref lightmapTex, out lightmapPixels, IMAGETYPE.RGB);

			convertedLightmap.SetPixels(lightmapPixels);
			convertedLightmap.Apply();
			string filename = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(lightmapTex)) + ".jpg";
			string filepath = savedPath + "/" + filename;
			byte[] lightmapData = convertedLightmap.EncodeToJPG();
			File.WriteAllBytes(filepath, lightmapData);
			GlTF_Writer.exportedFiles.Add(filepath, "");
			GlTF_Image lightmapImg = new GlTF_Image();
			lightmapImg.name = GlTF_Image.GetNameFromObject(lightmapTex);
			lightmapImg.uri = filename;

			GlTF_Texture lmTex = new GlTF_Texture();
			lmTex.name = lightmapTexName;

			var valLightmap = new GlTF_Material.DictValue();
			valLightmap.name = "aoTexture";
			valLightmap.intValue.Add("texture", GlTF_Writer.textures.IndexOf(lmTex));
			valLightmap.stringValue.Add("semantic", "TEXCOORD_4");
			material.values.Add(valLightmap);

			// Both images use the same sampler
			GlTF_Sampler sampler;
			var samplerName = GlTF_Sampler.GetNameFromObject(lightmapTex);
			if (!GlTF_Writer.samplerNames.Contains(samplerName))
			{
				sampler = new GlTF_Sampler(lightmapTex);
				sampler.name = samplerName;
				GlTF_Writer.samplers.Add(sampler);
				GlTF_Writer.samplerNames.Add(samplerName);
			}

			lmTex.samplerIndex = GlTF_Writer.samplerNames.IndexOf(samplerName);
			GlTF_Writer.textureNames.Add(lmTex.name);
			GlTF_Writer.textures.Add(lmTex);

			lmTex.source = GlTF_Writer.imageNames.Count;
			GlTF_Writer.imageNames.Add(lightmapImg.name);
			GlTF_Writer.images.Add(lightmapImg);

			return new KeyValuePair<GlTF_Texture, GlTF_Image>(lmTex, lightmapImg);
		}
		else
		{
			var valLightmap = new GlTF_Material.DictValue();
			valLightmap.name = "aoTexture";
			valLightmap.intValue.Add("texture", GlTF_Writer.textureNames.IndexOf(lightmapTexName));
			valLightmap.stringValue.Add("semantic", "TEXCOORD_4");
			material.values.Add(valLightmap);
		}
		return lightmapKV;
	}

	private string toGlTFname(string name)
	{
		// remove spaces and illegal chars, replace with underscores
		string correctString = name.Replace(" ", "_");
		// make sure it doesn't start with a number
		return correctString;
	}

	private bool isInheritedFrom (Type t, Type baseT)
	{
		if (t == baseT)
			return true;
		t = t.BaseType;
		while (t != null && t != typeof(System.Object))
		{
			if (t == baseT)
				return true;
			t = t.BaseType;
		}
		return false;
	}

	private Renderer GetRenderer(Transform tr)
	{
		Renderer mr = tr.GetComponent<MeshRenderer>();
		if (mr == null) {
			mr = tr.GetComponent<SkinnedMeshRenderer>();
		}
		return mr;
	}

	private void clampColor(ref Color c)
	{
		c.r = c.r > 1.0f ? 1.0f : c.r;
		c.g = c.g > 1.0f ? 1.0f : c.g;
		c.b = c.b > 1.0f ? 1.0f : c.b;
		//c.a = c.a > 1.0f ? 1.0f : c.a;
	}

	private Mesh GetMesh(Transform tr)
	{
		var mr = GetRenderer(tr);
		Mesh m = null;
		if (mr != null && mr.enabled)
		{
			var t = mr.GetType();
			if (t == typeof(MeshRenderer))
			{
				MeshFilter mf = tr.GetComponent<MeshFilter>();
				if(!mf)
				{
					Debug.Log("The gameObject " + tr.name + " will be exported as Transform (object has no MeshFilter component attached)");
					return null;
				}
				m = mf.sharedMesh;
			} else if (t == typeof(SkinnedMeshRenderer))
			{
				SkinnedMeshRenderer smr = mr as SkinnedMeshRenderer;
				m = smr.sharedMesh;
			}
		}
		return m;
	}

	// Convert unity Material to glTF PBR Material
	// FIXME: this function became messy with all the updates. It needs to be refactored and cleaned
	private void unityToPBRMaterial(Material mat, ref GlTF_Material material, bool doConvertImages = false, bool splitTextures=false)
	{
		Dictionary<string, string> UnityToGltfPBRMetalChannels= new Dictionary<string, string>
		{
			{"_MainTex", "baseColorTexture" },
			{"_Color","baseColorFactor" },
			{"_MetallicGlossMap", "metallicRoughnessTexture" },
			{"_Metallic", "metallicFactor" },
			{"_GlossMapScale", "roughnessFactor" }, // Smoothness factor is given by glossMapScale if there is a MetalGloss/SpecGloss texture, glossiness otherwise
			{"_Glossiness", "roughnessFactor" },
		};

		Dictionary<string, string> UnityToGltfPBRSpecularChannels = new Dictionary<string, string>
		{
			{"_MainTex", "diffuseTexture" },
			{"_SpecGlossMap", "specularGlossinessTexture" },
			{"_Color","diffuseFactor" },
			{"_SpecColor", "specularFactor" },
			{"_GlossMapScale", "glossinessFactor" },
			{"_Glossiness", "glossinessFactor" }, // Smoothness factor is given by glossMapScale if there is a MetalGloss/SpecGloss texture, glossiness otherwise
		};

		Dictionary<string, string> UnityToGltfAdditionalChannels = new Dictionary<string, string>
		{
			{"_BumpMap","normalTexture" },
			{"_OcclusionMap","occlusionTexture" },
			{"_EmissionMap", "emissiveTexture" },
			{"_EmissionColor","emissiveFactor" }
		};


		Shader s = mat.shader;
		int spCount2 = ShaderUtil.GetPropertyCount(s);
		Dictionary<string, string> workflowChannelMap = UnityToGltfPBRMetalChannels;
		bool isMaterialPBR = true;
		bool hasPBRMap = false;
		bool usesPBRTextureAlpha = false;
		bool isMetal = true;

		// Unity materials are single sided by default
		GlTF_Material.BoolValue doubleSided = new GlTF_Material.BoolValue();
		doubleSided.name = "doubleSided";
		doubleSided.value = false;
		material.values.Add(doubleSided);

		if (mat.HasProperty("_Mode") && mat.GetFloat("_Mode") != 0)
		{
			string mode = mat.GetFloat("_Mode") == 1 ? "MASK" : "BLEND";
			GlTF_Material.StringValue alphaMode = new GlTF_Material.StringValue();
			alphaMode.name = "alphaMode";
			alphaMode.value = mode;

			GlTF_Material.FloatValue alphaCutoff = new GlTF_Material.FloatValue();
			alphaCutoff.name = "alphaCutoff";
			alphaCutoff.value = mat.GetFloat("_Cutoff");

			material.values.Add(alphaMode);
			material.values.Add(alphaCutoff);
		}

		if (!mat.shader.name.Contains("Standard"))
		{
			Debug.Log("Material " + mat.shader + " is not fully supported");
			isMaterialPBR = false;
		}

		if (isMaterialPBR)
		{
			// Is metal workflow used
			isMetal = mat.shader.name == "Standard";
			GlTF_Writer.hasSpecularMaterials = GlTF_Writer.hasSpecularMaterials || !isMetal;
			material.isMetal = isMetal;

			// Is smoothness is defined by diffuse/albedo alpha or metal/specular texture alpha
			usesPBRTextureAlpha = mat.GetFloat("_SmoothnessTextureChannel") == 0;
			if (!usesPBRTextureAlpha)
				Debug.LogWarning("Smoothness from Albedo texture's alpha is not supported yet");

			workflowChannelMap = isMetal ? UnityToGltfPBRMetalChannels : UnityToGltfPBRSpecularChannels;
			hasPBRMap = (!isMetal && mat.GetTexture("_SpecGlossMap") != null || isMetal && mat.GetTexture("_MetallicGlossMap") != null);
		}

		Dictionary<string, string> currentDict;
		bool isPBRChannel = false;
		string gltfPName;
		for (var j = 0; j < spCount2; ++j)
		{
			var pName = ShaderUtil.GetPropertyName(s, j);
			var pType = ShaderUtil.GetPropertyType(s, j);

			if(workflowChannelMap.ContainsKey(pName))
			{
				isPBRChannel = true;
				currentDict = workflowChannelMap;
				gltfPName = workflowChannelMap[pName];
			}
			else if(UnityToGltfAdditionalChannels.ContainsKey(pName))
			{
				isPBRChannel = false;
				currentDict = UnityToGltfAdditionalChannels;
				gltfPName = UnityToGltfAdditionalChannels[pName];
			}
			else
			{
				//Unknown or unsupported value
				continue;
			}

			// Smoothness factor is given by glossMapScale if there is a MetalGloss/SpecGloss texture, glossiness otherwise
			if (pName == "_Glossiness" && hasPBRMap || pName == "_GlossMapScale" && !hasPBRMap)
				continue;

			if (pType == ShaderUtil.ShaderPropertyType.Color)
			{
				var matCol = new GlTF_Material.ColorValue();
				matCol.name = gltfPName;
				matCol.color = mat.GetColor(pName);
				clampColor(ref matCol.color);
				//FIXME: Unity doesn't use albedo color when there is no specular texture
				if (pName.CompareTo("_SpecColor") == 0)
				{
					matCol.color.a = 1.0f;
				}

				if (pName.CompareTo("_EmissionColor") == 0)
				{
					matCol.isRGB = true;
				}

				if (isPBRChannel)
					material.pbrValues.Add(matCol);
				else
					material.values.Add(matCol);

			}
			else if (pType == ShaderUtil.ShaderPropertyType.Vector)
			{
				var matVec = new GlTF_Material.VectorValue();
				matVec.name = gltfPName;
				matVec.vector = mat.GetVector(pName);

				if (isPBRChannel)
					material.pbrValues.Add(matVec);
				else
					material.values.Add(matVec);

			}
			else if (pType == ShaderUtil.ShaderPropertyType.Float || pType == ShaderUtil.ShaderPropertyType.Range)
			{
				var matFloat = new GlTF_Material.FloatValue();
				matFloat.name = gltfPName;
				matFloat.value = mat.GetFloat(pName);

				// Roughness = 1 - smoothness. Gloss map scale is not supported for now.
				if (isMetal && !hasPBRMap && pName.CompareTo("_Glossiness") == 0)
					matFloat.value = 1 - matFloat.value;

				// If metallic texture, set the factor to 1.0
				if(hasPBRMap && pName.CompareTo("_Metallic") == 0)
				{
					matFloat.value = 1.0f;
				}

				if (isPBRChannel)
					material.pbrValues.Add(matFloat);
				else
					material.values.Add(matFloat);

			}
			else if (pType == ShaderUtil.ShaderPropertyType.TexEnv && (workflowChannelMap.ContainsKey(pName) || UnityToGltfAdditionalChannels.ContainsKey(pName)))
			{
				var td = ShaderUtil.GetTexDim(s, j);
				if (td == UnityEngine.Rendering.TextureDimension.Tex2D)
				{
					var t = mat.GetTexture(pName);
					if (t != null)
					{
						Texture2D t2d = t as Texture2D;
						bool isBumpTexture = pName.CompareTo("_BumpMap") == 0;
						bool isBumpMap = false;
						var texName = GlTF_Texture.GetNameFromObject(t);

						IMAGETYPE format = doConvertImages ? IMAGETYPE.RGB : IMAGETYPE.IGNORE;

						// Force psd conversion
						if(t2d != null)
						{
							string ext = Path.GetExtension(AssetDatabase.GetAssetPath(t2d));
							if(ext == ".psd")
								format = IMAGETYPE.RGB;
						}
						var val = new GlTF_Material.DictValue();

						if (isBumpTexture)
						{
							TextureImporter im = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)) as TextureImporter;
							if(im)
							{
								isBumpMap = im.convertToNormalmap;
								val.name = isBumpMap ? "bumpTexture" : "normalTexture";
							}
						}
						else
						{
							val.name = currentDict[pName];
						}
						if (doConvertImages && pName.CompareTo("_MainTex") == 0)
						{
							if (mat.HasProperty("_Mode") && mat.GetFloat("_Mode") != 0)
								format = IMAGETYPE.RGBA;
							else
								format = IMAGETYPE.RGBA_OPAQUE;
						}

						if (pName.CompareTo("_MetallicGlossMap") == 0)
						{
							format = IMAGETYPE.RG;
						}
						else if(pName.CompareTo("_SpecGlossMap") == 0)
						{
							format = IMAGETYPE.RGBA;
						}

						if (!GlTF_Writer.textureNames.Contains(texName) && AssetDatabase.GetAssetPath(t).Length > 0)
						{
							if (doConvertImages && isBumpTexture && !isBumpMap)
							{
								format = IMAGETYPE.NORMAL_MAP;
							}

							var texPath = ExportTexture(t, savedPath, false, format);
							if(texPath.Length == 0)
							{
								Debug.Log("Failed to process texture for property '" + pName + "' in material '" + mat.name + "'");
								continue;
							}

							GlTF_Texture texture = new GlTF_Texture();
							texture.name = texName;

							GlTF_Image img = new GlTF_Image();
							img.name = GlTF_Image.GetNameFromObject(t);
							img.uri = texPath;
							texture.source = GlTF_Writer.imageNames.Count;
							GlTF_Writer.imageNames.Add(img.name);
							GlTF_Writer.images.Add(img);

							GlTF_Sampler sampler;
							var samplerName = GlTF_Sampler.GetNameFromObject(t);
							if (!GlTF_Writer.samplerNames.Contains(samplerName))
							{
								sampler = new GlTF_Sampler(t);
								sampler.name = samplerName;
								GlTF_Writer.samplers.Add(sampler);
								GlTF_Writer.samplerNames.Add(samplerName);
							}

							texture.samplerIndex = GlTF_Writer.samplerNames.IndexOf(samplerName);

							val.intValue.Add("index", GlTF_Writer.textures.Count);
							val.intValue.Add("texCoord", 0);
							if(isBumpTexture && !isBumpMap && mat.HasProperty("_BumpScale"))
							{
								val.floatValue.Add("scale", mat.GetFloat("_BumpScale"));
							}
							if(pName.CompareTo("_OcclusionMap") == 0 && mat.HasProperty("_OcclusionStrength"))
							{
								val.floatValue.Add("strength", mat.GetFloat("_OcclusionStrength"));
							}

							if(isPBRChannel)
								material.pbrValues.Add(val);
							else
								material.values.Add(val);

							GlTF_Writer.textures.Add(texture);
							GlTF_Writer.textureNames.Add(texName);
						}
						else
						{
							val.intValue.Add("index", GlTF_Writer.textureNames.IndexOf(texName));
							val.intValue.Add("texCoord", 0);
							if (isPBRChannel)
								material.pbrValues.Add(val);
							else
								material.values.Add(val);
						}

						if (AssetDatabase.GetAssetPath(t).Length == 0)
						{
							Debug.LogWarning("Texture '" + t.name + "' has not been exported (Asset not found)");
						}
					}
				}
			}
		}
	}

	private bool getPixelsFromTexture(ref Texture2D texture, out Color[] pixels, IMAGETYPE imageFormat)
	{
		//Make texture readable
		TextureImporter im = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
		if(!im)
		{
			pixels = new Color[1];
			return false;
		}
		bool readable = im.isReadable;
		TextureImporterCompression format = im.textureCompression;
		TextureImporterType type = im.textureType;
		bool isConvertedBump = im.convertToNormalmap;

		if (!readable)
			im.isReadable = true;
		if (type != TextureImporterType.Default)
			im.textureType = TextureImporterType.Default;

		im.textureCompression = TextureImporterCompression.Uncompressed;
		im.SaveAndReimport();

		pixels = texture.GetPixels();

		if (!readable)
			im.isReadable = false;
		if (type != TextureImporterType.Default)
			im.textureType = type;

		if (isConvertedBump)
			im.convertToNormalmap = true;

		im.textureCompression = format;
		im.SaveAndReimport();

		return true;
	}

	// Flip all images on Y and
	public string convertTexture(ref Texture2D inputTexture, string assetPath, string outputDir, IMAGETYPE format)
	{
		int height = inputTexture.height;
		int width = inputTexture.width;
		Color[] textureColors = new Color[inputTexture.height * inputTexture.width];
		if(!getPixelsFromTexture(ref inputTexture, out textureColors, format))
		{
			Debug.Log("Failed to convert texture " + inputTexture.name + " (unsupported type or format)");
			return "";
		}
		Color[] newTextureColors = new Color[inputTexture.height * inputTexture.width];

		for (int i = 0; i < height; ++i)
		{
			for (int j = 0; j < width; ++j)
			{
				if (format == IMAGETYPE.RG)
				{
					newTextureColors[i * width + j] = new Color(textureColors[(height - i - 1) * width + j].r, 1.0f - textureColors[(height - i - 1) * width + j].a, 0.0f, 0.0f);
				}
				else
				{
					newTextureColors[i * width + j] = textureColors[(height - i - 1) * width + j];
					if (format == IMAGETYPE.RGBA_OPAQUE)
						newTextureColors[i * width + j].a = 1.0f;
				}
			}
		}

		Texture2D newtex = new Texture2D(inputTexture.width, inputTexture.height);
		newtex.SetPixels(newTextureColors);
		newtex.Apply();

		string pathInArchive = Path.GetDirectoryName(assetPath);
		string exportDir = Path.Combine(outputDir, pathInArchive);

		if (!Directory.Exists(exportDir))
			Directory.CreateDirectory(exportDir);

		string outputFilename = Path.GetFileNameWithoutExtension(assetPath) + (format == IMAGETYPE.RG ? "_converted_metalRoughness" : "") + (format == IMAGETYPE.RGBA ? ".png" : ".jpg");
		string exportPath = exportDir + "/" + outputFilename;  // relative path inside the .zip
		string pathInGltfFile = pathInArchive + "/" + outputFilename;
		File.WriteAllBytes(exportPath, (format == IMAGETYPE.RGBA ? newtex.EncodeToPNG() : newtex.EncodeToJPG( format== IMAGETYPE.NORMAL_MAP ? 95 : jpgQuality)));

		if (!GlTF_Writer.exportedFiles.ContainsKey(exportPath))
			GlTF_Writer.exportedFiles.Add(exportPath, pathInArchive);
		else
			Debug.LogError("Texture '" + inputTexture + "' already exists");

		return pathInGltfFile;
	}

	private string ExportTexture(Texture texture, string path, bool forceRGBA32=false, IMAGETYPE format=IMAGETYPE.IGNORE)
	{
		var assetPath = AssetDatabase.GetAssetPath(texture);
		var fn = Path.GetFileName(assetPath);
		var t = texture as Texture2D;
		if (t != null)
		{
			// All the textures need to be converted and flipped in Y
			return convertTexture(ref t, assetPath, path, format);
		}
		return fn;
	}
}
#endif