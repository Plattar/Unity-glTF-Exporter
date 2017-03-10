#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlTF_Skin : GlTF_Writer {
	public Matrix4x4[] invBindMatrices;
	public int invBindMatricesAccessorIndex;
	public Transform node;
	public List<Transform> joints;
	public List<Transform> jointNames;
	public Transform mesh;
	public Transform rootBone;

	public GlTF_Skin() { }

	public static string GetNameFromObject(Object o)
	{
		return "skin_" + GlTF_Writer.GetNameFromObject(o, true);
	}

	public void Populate (Transform m, ref GlTF_Accessor invBindMatricesAccessor, int invBindAccessorIndex)
	{
		SkinnedMeshRenderer skinMesh = m.GetComponent<SkinnedMeshRenderer>();
		if (!skinMesh)
			return;

		// Populate bind poses. From https://docs.unity3d.com/ScriptReference/Mesh-bindposes.html:
		// The bind pose is bone's inverse transformation matrix
		// In this case we also make this matrix relative to the root
		// So that we can move the root game object around freely
		Mesh mesh = skinMesh.sharedMesh;
		joints = new List<Transform>();
		//Collect all bones from skin object. Order should be kept here since bones are referenced in the mesh
		foreach(Transform t in skinMesh.bones)
		{
			joints.Add(t);
		}

		// glTF expects a single hierarchy of bones, but Unity skips all the nodes that are not used.
		// Find the common ancestor of all used bones in order to get a valid bone herarchy
		rootBone = rebuildBoneHierarchy(skinMesh, ref joints);

		Matrix4x4[] invBindMatrices = new Matrix4x4[joints.Count];

		for (int i = 0; i < invBindMatrices.Length; ++i)
		{
			// Generates inverseWorldMatrix in right-handed coordinate system
			// Manually converts world translation and rotation from left to right handed coordinates systems
			Vector3 pos = joints[i].position;
			Quaternion rot = joints[i].rotation;
			convertQuatLeftToRightHandedness(ref rot);
			convertVector3LeftToRightHandedness(ref pos);

			invBindMatrices[i] = Matrix4x4.TRS(pos, rot, joints[i].lossyScale).inverse * sceneRootMatrix.inverse;
		}

		invBindMatricesAccessor.Populate(invBindMatrices, m);
		invBindMatricesAccessorIndex = invBindAccessorIndex;
	}

	// Rebuild hierarchy and returns root bone
	public Transform rebuildBoneHierarchy(SkinnedMeshRenderer skin, ref List<Transform> joints)
	{
		List<string> skeletons = new List<string>();
		List<Transform> tbones = new List<Transform>();
		List<Transform> traversed = new List<Transform>(); // Will be returned and contain all the nodes that are in the hierarchy but not used as bones (that need to be converted)
		Transform computedRoot = null;
		// Get bones
		foreach (Transform bone in skin.bones)
		{
			tbones.Add(bone);
		}
		List<Transform> haveBParents = new List<Transform>();
		// Check and list bones that have parents that are bon in this skin
		foreach (Transform b in tbones)
		{
			Transform temp = b.parent;
			while (temp.parent)
			{
				if (tbones.Contains(temp))
				{
					haveBParents.Add(b);
					break;
				}
				temp = temp.parent;
			}
		}

		// Remove bones having parents from the list
		foreach (Transform b in haveBParents)
		{
			tbones.Remove(b);
		}

		// if more than one root, find common ancestor
		if (tbones.Count > 1)
		{
			Transform rootSkeleton = null; //Will get the final root node
			List<Transform> visited = new List<Transform>(tbones); // internal list to detect parenting
			List<Transform> evol = new List<Transform>(tbones); // used to increment on each node
			// Get the parent of each bone
			while (evol.Count > 1)
			{
				for (int i = 0; i < evol.Count; ++i)
				{
					evol[i] = evol[i].parent;
					if (evol[i] != null)
					{
						if(!traversed.Contains(evol[i]))
						{
							traversed.Add(evol[i]);
						}

						if (visited.Contains(evol[i]))
						{
							rootSkeleton = evol[i];
							evol[i] = null;
						}
						else
						{
							visited.Add(evol[i]);
						}
					}
				}

				List<Transform> clean = new List<Transform>();
				foreach (Transform t in evol)
				{
					if (t)
						clean.Add(t);
				}

				evol = new List<Transform>(clean);
			}

			skeletons.Add(GlTF_Node.GetNameFromObject(rootSkeleton));
			computedRoot = rootSkeleton;
		}
		else if (tbones.Count == 1)
		{
			skeletons.Add(GlTF_Node.GetNameFromObject(tbones[0]));
			computedRoot = tbones[0];
		}

		joints.AddRange(traversed);

		return computedRoot;
	}

	public override void Write ()
	{
		Indent();	jsonWriter.Write ("{\n");
		IndentIn();

		Indent(); jsonWriter.Write("\"inverseBindMatrices\": "+ invBindMatricesAccessorIndex + ",\n");
		Indent(); jsonWriter.Write ("\"joints\": [\n");

		IndentIn();
		foreach (Transform j in joints)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("" + GlTF_Writer.nodeNames.IndexOf(GlTF_Node.GetNameFromObject(j)));
		}

		IndentOut();
		jsonWriter.WriteLine();
		Indent(); jsonWriter.Write ("],\n");
		Indent(); jsonWriter.Write("\"name\": \"" + name + "\",\n");
		Indent(); jsonWriter.Write("\"skeleton\": " + GlTF_Writer.nodeNames.IndexOf(GlTF_Node.GetNameFromObject(rootBone)) + "\n");
		IndentOut();
		Indent();	jsonWriter.Write ("}");
	}
}
#endif