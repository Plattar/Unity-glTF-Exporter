using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

public class GlTF_Parameters : GlTF_Writer {
	public GlTF_Accessor timeAccessor;
	public GlTF_Accessor translationAccessor;
	public GlTF_Accessor rotationAccessor;
	public GlTF_Accessor scaleAccessor;

	// Build a list instead of static var
	public Dictionary<string, GlTF_Accessor> paramAccessors = new Dictionary<string, GlTF_Accessor>();

	// seems like a bad place for this
	bool px, py, pz;
	bool sx, sy, sz;
	bool rx, ry, rz, rw;

	public GlTF_Parameters (string n) { name = n; }

	//FIXME this is broken
	//public void Populate(AnimationClipCurveData curveData)
	//{
	//    string propName = curveData.propertyName;
	//    float[] times = new float[curveData.curve.keys.Length]; // Should merge them if it's exactly the same for each curves
	//    //if (times == null) // allocate one array of times, assumes all channels have same number of keys
	//   // {
	//        timeAccessor = new GlTF_Accessor(target + "TimeAccessor", GlTF_Accessor.Type.SCALAR, GlTF_Accessor.ComponentType.FLOAT);
	//        timeAccessor.bufferView = GlTF_Writer.floatBufferView;
	//        GlTF_Writer.accessors.Add(timeAccessor);

	//        times = new float[curveData.curve.keys.Length];
	//        for (int i = 0; i < curveData.curve.keys.Length; i++)
	//            times[i] = curveData.curve.keys[i].time;
	//        timeAccessor.Populate(times);
	//    //}
	//    if (propName.Contains("m_LocalPosition"))
	//    {
	//        if (positions == null)
	//        {
	//            translationAccessor = new GlTF_Accessor(name + "TranslationAccessor_" + GlTF_Writer.accessors.Count, GlTF_Accessor.Type.VEC3, GlTF_Accessor.ComponentType.FLOAT);
	//            translationAccessor.bufferView = GlTF_Writer.vec3BufferView;
	//            GlTF_Writer.accessors.Add(translationAccessor);
	//            positions = new Vector3[curveData.curve.keys.Length];
	//        }
	//        else
	//        {
	//            // Update key values array size if needed
	//            if (positions.Length < curveData.curve.keys.Length)
	//            {
	//                int nbElt = curveData.curve.keys.Length - positions.Length;
	//                Vector3[] extension = new Vector3[nbElt];
	//                ArrayUtility.AddRange<Vector3>(ref positions, extension);
	//            }
	//        }

	//        if (propName.Contains(".x"))
	//        {
	//            px = true;
	//            for (int i = 0; i < curveData.curve.keys.Length; i++)
	//                positions[i].x = curveData.curve.keys[i].value;
	//        }
	//        else if (propName.Contains(".y"))
	//        {
	//            py = true;
	//            for (int i = 0; i < curveData.curve.keys.Length; i++)
	//                positions[i].y = curveData.curve.keys[i].value;
	//        }
	//        else if (propName.Contains(".z"))
	//        {
	//            pz = true;
	//            for (int i = 0; i < curveData.curve.keys.Length; i++)
	//                positions[i].z = curveData.curve.keys[i].value;
	//        }
	//        if (px && py && pz)
	//        {
	//            translationAccessor.Populate(positions);
	//        }
	//    }

	//    if (propName.Contains("m_LocalScale"))
	//    {
	//        if (scales == null)
	//        {
	//            scaleAccessor = new GlTF_Accessor(name + "ScaleAccessor_" + GlTF_Writer.accessors.Count, GlTF_Accessor.Type.VEC3, GlTF_Accessor.ComponentType.FLOAT);
	//            scaleAccessor.bufferView = GlTF_Writer.vec3BufferView;
	//            GlTF_Writer.accessors.Add(scaleAccessor);
	//            scales = new Vector3[curveData.curve.keys.Length];
	//        }
	//        else {
	//            // Update key values array size if needed
	//            if (scales.Length < curveData.curve.keys.Length)
	//            {
	//                int nbElt = curveData.curve.keys.Length - scales.Length;
	//                Vector3[] extension = new Vector3[nbElt];
	//                ArrayUtility.AddRange<Vector3>(ref scales, extension);
	//            }
	//        }


	//        if (propName.Contains(".x"))
	//        {
	//            sx = true;
	//            for (int i = 0; i < curveData.curve.keys.Length; i++)
	//                scales[i].x = curveData.curve.keys[i].value;
	//        }
	//        else if (propName.Contains(".y"))
	//        {
	//            sy = true;
	//            for (int i = 0; i < curveData.curve.keys.Length; i++)
	//                scales[i].y = curveData.curve.keys[i].value;
	//        }
	//        else if (propName.Contains(".z"))
	//        {
	//            sz = true;
	//            for (int i = 0; i < curveData.curve.keys.Length; i++)
	//                scales[i].z = curveData.curve.keys[i].value;
	//        }
	//        if (sx && sy && sz)
	//        {
	//            scaleAccessor.Populate(scales);
	//        }
	//    }

	//    if (propName.Contains("m_LocalRotation"))
	//    {
	//        if (rotations == null)
	//        {
	//            rotationAccessor = new GlTF_Accessor(name + "RotationAccessor_" + GlTF_Writer.accessors.Count, GlTF_Accessor.Type.VEC4, GlTF_Accessor.ComponentType.FLOAT);
	//            rotationAccessor.bufferView = GlTF_Writer.vec4BufferView;
	//            GlTF_Writer.accessors.Add(rotationAccessor);
	//            rotations = new Vector4[curveData.curve.keys.Length];
	//        }
	//        else
	//        {   // Update key values array size if needed
	//            if (rotations.Length < curveData.curve.keys.Length)
	//            {
	//                int nbElt = curveData.curve.keys.Length - rotations.Length;
	//                Vector4[] extension = new Vector4[nbElt];
	//                ArrayUtility.AddRange<Vector4>(ref rotations, extension);
	//            }
	//        }


	//        if (propName.Contains(".x"))
	//        {
	//            rx = true;
	//            for (int i = 0; i < curveData.curve.keys.Length; i++)
	//            {
	//                rotations[i].x = curveData.curve.keys[i].value;
	//            }
	//        }
	//        else if (propName.Contains(".y"))
	//        {
	//            ry = true;
	//            for (int i = 0; i < curveData.curve.keys.Length; i++)
	//                rotations[i].y = curveData.curve.keys[i].value;
	//        }
	//        else if (propName.Contains(".z"))
	//        {
	//            rz = true;
	//            for (int i = 0; i < curveData.curve.keys.Length; i++)
	//                rotations[i].z = curveData.curve.keys[i].value;
	//        }
	//        else if (propName.Contains(".w"))
	//        {
	//            rw = true;
	//            for (int i = 0; i < curveData.curve.keys.Length; i++)
	//                rotations[i].w = curveData.curve.keys[i].value;
	//        }
	//        if (rx && ry && rz && rw)
	//        {
	//            rotationAccessor.Populate(rotations);
	//        }
	//    }
	//}

	public override void Write()
	{
		Indent(); jsonWriter.Write("\"" + "parameters" + "\": {\n");
		IndentIn();
		foreach(KeyValuePair<string, GlTF_Accessor> param in paramAccessors)
		{
			CommaNL();
			Indent(); jsonWriter.Write("\"" + param.Key + "\": \"" + param.Value.id + "\"");
		}
		jsonWriter.WriteLine();
		IndentOut();
		Indent(); jsonWriter.Write("}");
	}
}
