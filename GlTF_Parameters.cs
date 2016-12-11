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

	public void bakeAndPopulate(AnimationClip clip, int bakingFramerate, string targetId, string targetName)
	{
		int nbSamples = (int)(clip.length * 30);
		float deltaTime = clip.length / nbSamples;

		AnimationClipCurveData[] translationCurves = new AnimationClipCurveData[3]; // ordered curves X,Y,Z
		AnimationClipCurveData[] rotationCurves = new AnimationClipCurveData[4]; // ordered curves X,Y,Z,W
		AnimationClipCurveData[] scaleCurves = new AnimationClipCurveData[3]; // ordered curves X,Y,Z

		Transform targetObject = GameObject.Find(targetName).transform;
		retrieveCurvesFromClip(clip, targetName, ref translationCurves, ref rotationCurves, ref scaleCurves);
		checkAndFixMissingCurvesWithTransform(targetObject, clip.length, ref translationCurves, ref rotationCurves, ref scaleCurves);

		// Initialize accessors for current animation
		GlTF_Accessor timeAccessor = new GlTF_Accessor(targetId + "_TimeAccessor_" + clip.name, GlTF_Accessor.Type.SCALAR, GlTF_Accessor.ComponentType.FLOAT);
		timeAccessor.bufferView = GlTF_Writer.floatBufferView;
		GlTF_Writer.accessors.Add(timeAccessor);

		GlTF_Accessor translationAccessor = new GlTF_Accessor(targetId + "_TranslationAccessor_" + clip.name, GlTF_Accessor.Type.VEC3, GlTF_Accessor.ComponentType.FLOAT);
		translationAccessor.bufferView = GlTF_Writer.vec3BufferView;
		GlTF_Writer.accessors.Add(translationAccessor);

		GlTF_Accessor rotationAccessor = new GlTF_Accessor(targetId + "_RotationAccessor_" + clip.name, GlTF_Accessor.Type.VEC4, GlTF_Accessor.ComponentType.FLOAT);
		rotationAccessor.bufferView = GlTF_Writer.vec4BufferView;
		GlTF_Writer.accessors.Add(rotationAccessor);

		GlTF_Accessor scaleAccessor = new GlTF_Accessor(targetId + "_ScaleAccessor_" + clip.name, GlTF_Accessor.Type.VEC3, GlTF_Accessor.ComponentType.FLOAT);
		scaleAccessor.bufferView = GlTF_Writer.vec3BufferView;
		GlTF_Writer.accessors.Add(scaleAccessor);

		// Initialize Arrays
		float[] times = new float[nbSamples];
		Vector3[] positions = new Vector3[nbSamples];
		Vector3[] scales = new Vector3[nbSamples];
		Vector4[] rotations = new Vector4[nbSamples];

		// Assuming all the curves exist now
		for (int i = 0; i < nbSamples; ++i)
		{
			float currentTime = i * deltaTime;
			times[i] = currentTime;
			positions[i] = new Vector3(translationCurves[0].curve.Evaluate(currentTime), translationCurves[1].curve.Evaluate(currentTime), translationCurves[2].curve.Evaluate(currentTime));
			scales[i] = new Vector3(scaleCurves[0].curve.Evaluate(currentTime), scaleCurves[1].curve.Evaluate(currentTime), scaleCurves[2].curve.Evaluate(currentTime));
			rotations[i] = new Vector4(rotationCurves[0].curve.Evaluate(currentTime), rotationCurves[1].curve.Evaluate(currentTime), rotationCurves[2].curve.Evaluate(currentTime), rotationCurves[3].curve.Evaluate(currentTime));
		}
		// Populate data into accessors
		timeAccessor.Populate(times);
		translationAccessor.Populate(positions);
		rotationAccessor.Populate(rotations);
		scaleAccessor.Populate(scales, true);

		// For now, one time parameter for each target.
		// Time could be merged inside a single parameter/accessor for baking since sampling is the same for every curve in a given clip
		paramAccessors.Add(targetId + "_time", timeAccessor);
		paramAccessors.Add(targetId + "_translation", translationAccessor);
		paramAccessors.Add(targetId + "_rotation", rotationAccessor);
		paramAccessors.Add(targetId + "_scale", scaleAccessor);
	}
	public void retrieveCurvesFromClip(AnimationClip clip, string target, ref AnimationClipCurveData[] translationCurves, ref AnimationClipCurveData[] rotationCurves, ref AnimationClipCurveData[] scaleCurves)
	{
		AnimationClipCurveData[] curveDatas = AnimationUtility.GetAllCurves(clip, true);
		foreach (AnimationClipCurveData curve in curveDatas)
		{
			string lastNodePath = curve.path.Split('/')[curve.path.Split('/').Length - 1];
			if (!lastNodePath.Equals(target))
			{
				continue;
			}

			if (curve.propertyName.Contains("m_LocalPosition"))
			{
				if (curve.propertyName.Contains(".x"))
					translationCurves[0] = curve;
				if (curve.propertyName.Contains(".y"))
					translationCurves[1] = curve;
				if (curve.propertyName.Contains(".z"))
					translationCurves[2] = curve;
			}
			else if (curve.propertyName.Contains("m_LocalScale"))
			{
				if (curve.propertyName.Contains(".x"))
					scaleCurves[0] = curve;
				if (curve.propertyName.Contains(".y"))
					scaleCurves[1] = curve;
				if (curve.propertyName.Contains(".z"))
					scaleCurves[2] = curve;
			}
			else if (curve.propertyName.Contains("m_LocalRotation"))
			{
				if (curve.propertyName.Contains(".x"))
					rotationCurves[0] = curve;
				if (curve.propertyName.Contains(".y"))
					rotationCurves[1] = curve;
				if (curve.propertyName.Contains(".z"))
					rotationCurves[2] = curve;
				if (curve.propertyName.Contains(".w"))
					rotationCurves[3] = curve;
			}
		}
	}

	// Assumption: If a property (T/R/S) has at least one curve, other parameters are also given (if x, then y and z are here too)
	public void checkAndFixMissingCurvesWithTransform(Transform targetObject, float endTime, ref AnimationClipCurveData[] translationCurves, ref AnimationClipCurveData[] rotationCurves, ref AnimationClipCurveData[] scaleCurves)
	{
		if (translationCurves[0] == null)
		{
			translationCurves[0] = createConstantCurve("m_LocalPosition.x", targetObject.name, targetObject.localPosition.x, endTime);
			translationCurves[1] = createConstantCurve("m_LocalPosition.y", targetObject.name, targetObject.localPosition.y, endTime);
			translationCurves[2] = createConstantCurve("m_LocalPosition.z", targetObject.name, targetObject.localPosition.z, endTime);
		}

		if (scaleCurves[0] == null)
		{
			scaleCurves[0] = createConstantCurve("m_LocalScale.x", targetObject.name, targetObject.localScale.x, endTime);
			scaleCurves[1] = createConstantCurve("m_LocalScale.y", targetObject.name, targetObject.localScale.y, endTime);
			scaleCurves[2] = createConstantCurve("m_LocalScale.z", targetObject.name, targetObject.localScale.z, endTime);
		}

		if (rotationCurves[0] == null)
		{
			rotationCurves[0] = createConstantCurve("m_LocalRotation.x", targetObject.name, targetObject.localRotation.x, endTime);
			rotationCurves[1] = createConstantCurve("m_LocalRotation.y", targetObject.name, targetObject.localRotation.y, endTime);
			rotationCurves[2] = createConstantCurve("m_LocalRotation.z", targetObject.name, targetObject.localRotation.z, endTime);
			rotationCurves[3] = createConstantCurve("m_LocalRotation.w", targetObject.name, targetObject.localRotation.w, endTime);
		}
	}

	public AnimationClipCurveData createConstantCurve(string propertyName, string targetName, float value, float endTime)
	{
		// No translation curves, adding them
		AnimationClipCurveData curveData = new AnimationClipCurveData();
		curveData.propertyName = propertyName;
		curveData.path = targetName;
		AnimationCurve curve = new AnimationCurve();
		curve.AddKey(0, value);
		curve.AddKey(endTime, value);
		curveData.curve = curve;
		return curveData;
	}

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
