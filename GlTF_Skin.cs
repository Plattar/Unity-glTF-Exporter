using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlTF_Skin : GlTF_Writer {
	public GlTF_Matrix bindShapeMatrix;
	public Matrix4x4[] invBindMatrices;
	public string invBindMatricesAccessorName;
	public Transform node;
	public string[] jointNames;
	public Transform mesh;

	public GlTF_Skin() { }

	public static string GetNameFromObject(Object o)
	{
		return "skin_" + GlTF_Writer.GetNameFromObject(o, true);
	}

	public void setBindShapeMatrix(Transform mesh)
	{
		Matrix4x4 mat = mesh.worldToLocalMatrix;
		if (mesh.parent)
			mat = mat * mesh.parent.localToWorldMatrix;

		bindShapeMatrix = new GlTF_Matrix(mat);
		bindShapeMatrix.name = "bindShapeMatrix";
	}

	public void Populate (Transform m, ref GlTF_Accessor invBindMatricesAccessor)
	{
		SkinnedMeshRenderer skinMesh = m.GetComponent<SkinnedMeshRenderer>();
		if (!skinMesh)
			return;

		// Populate bind poses. From https://docs.unity3d.com/ScriptReference/Mesh-bindposes.html:
		// The bind pose is bone's inverse transformation matrix
		// In this case we also make this matrix relative to the root
		// So that we can move the root game object around freely
		Mesh mesh = skinMesh.sharedMesh;
		invBindMatricesAccessor.Populate(mesh.bindposes, m);
		invBindMatricesAccessorName = invBindMatricesAccessor.id;

		// Fill jointNames
		jointNames = new string[skinMesh.bones.Length];
		for(int i=0; i< skinMesh.bones.Length; ++i)
		{
			jointNames[i] = GlTF_Node.GetNameFromObject(skinMesh.bones[i]);
		}
	}

	public override void Write ()
	{
		Indent();	jsonWriter.Write ("\"" + name + "\": {\n");
		IndentIn();

		if (bindShapeMatrix != null)
		{
			CommaNL();
			bindShapeMatrix.Write();
		}

		Indent(); jsonWriter.Write(",\n");
		Indent(); jsonWriter.Write("\"inverseBindMatrices\": \""+ invBindMatricesAccessorName + "\",\n");
		Indent(); jsonWriter.Write ("\"jointNames\": [\n");

		IndentIn();
		foreach (string j in jointNames)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\""+ j + "\"");
		}
		IndentOut();
		jsonWriter.WriteLine();
		Indent(); jsonWriter.Write ("],\n");
		Indent(); jsonWriter.Write("\"name\": \"" + name + "\"\n");
		IndentOut();
		Indent();	jsonWriter.Write ("}");
	}
}
