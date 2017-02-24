using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class GlTF_Animation : GlTF_Writer {
	public List<GlTF_Channel> channels = new List<GlTF_Channel>();
	public GlTF_Parameters parameters;
	public List<GlTF_AnimSampler> animSamplers = new List<GlTF_AnimSampler>();
	//bool gotTranslation = false;
	//bool gotRotation = false;
	//bool gotScale = false;
	string targetName;

	int bakingFramerate = 30; // FPS

	public GlTF_Animation (string n, string target) {
		name = n;
		parameters = new GlTF_Parameters(n);
		targetName = target;
	}

	public void Populate(AnimationClip c, Transform tr, bool bake = true)
	{
		AnimationClipCurveData[] curveDatas = AnimationUtility.GetAllCurves(c, true);

		// Get current clip targets list
		List<string> targetPathList = new List<string>();

		// A clip contains curves that can affect different targets, collect them
		foreach(AnimationClipCurveData cu in curveDatas)
		{
			if(!targetPathList.Contains(cu.path))
				targetPathList.Add(cu.path);
		}

		if(bake)
		{
			// Bake animation for all animated nodes
			foreach(string targetPath in targetPathList)
			{
				Transform targetTr = targetPath.Length > 0 ? tr.transform.Find(targetPath) : tr;
				if (targetTr == null)
					continue;

				GameObject targetGo = targetTr.gameObject;
				Transform targetObject = targetGo.transform;
				string targetId = GlTF_Node.GetNameFromObject(targetObject);
				// Setup animation data: Samplers and Channel
				GlTF_AnimSampler sTranslation = new GlTF_AnimSampler(name + "_"+ targetId + "_TranslationSampler" , targetId + "_time", targetId + "_translation");
				animSamplers.Add(sTranslation);

				GlTF_Target targetTranslation = new GlTF_Target();
				targetTranslation.id = targetId;
				targetTranslation.path = "translation";

				GlTF_Channel chTranslation = new GlTF_Channel("translation", sTranslation);
				chTranslation.target = targetTranslation;
				channels.Add(chTranslation);

				// Rotation
				GlTF_AnimSampler sRotation = new GlTF_AnimSampler(name + "_" + targetId + "_RotationSampler", targetId + "_time", targetId + "_rotation");
				animSamplers.Add(sRotation);

				GlTF_Target targetRotation = new GlTF_Target();
				targetRotation.id = GlTF_Node.GetNameFromObject(targetObject);
				targetRotation.path = "rotation";

				GlTF_Channel chRotation = new GlTF_Channel("rotation", sRotation);
				chRotation.target = targetRotation;
				channels.Add(chRotation);


				// Scale
				GlTF_AnimSampler sScale = new GlTF_AnimSampler(name + "_" + targetId + "_ScaleSampler", targetId + "_time", targetId + "_scale");
				animSamplers.Add(sScale);
				GlTF_Target targetScale = new GlTF_Target();
				targetScale.id = GlTF_Node.GetNameFromObject(targetObject);
				targetScale.path = "scale";
				GlTF_Channel chScale = new GlTF_Channel("scale", sScale);
				chScale.target = targetScale;
				channels.Add(chScale);

				// Bake and populate animation data
				parameters.bakeAndPopulate(c, bakingFramerate, targetId, targetPath, targetObject);
			}
			if(channels.Count == 0)
			{
				Debug.Log("Error when parsing animation of node " + tr.name + " (" + (targetPathList.Count == 0 ? "Animation has no curves " : "curves paths are not valid" ) + ").");
			}
		}
		else
		{
			Debug.LogError("Only baked animation is supported for now. Skipping animation");
			//FIXME : I DONT WORK ANYMORE
			// AnimationUtility.GetCurveBindings(c);
			// look at each curve
			// if position, rotation, scale detected for first time
			//  create channel, sampler, param for it
			//  populate this curve into proper component
			//for (int i = 0; i < curveDatas.Length; i++)
			//{
			//    string propName = curveDatas[i].propertyName;
			//    string lastNodePath = curveDatas[i].path.Split('/')[curveDatas[i].path.Split('/').Length - 1];
			//    if (!lastNodePath.Contains(targetName))
			//        continue;

			//    if (propName.Contains("m_LocalPosition"))
			//    {
			//        if (!gotTranslation)
			//        {
			//            gotTranslation = true;
			//            GlTF_AnimSampler s = new GlTF_AnimSampler(name + "_AnimSampler", "translation");
			//            GlTF_Channel ch = new GlTF_Channel("translation", s);
			//            GlTF_Target target = new GlTF_Target();
			//            target.id = targetName;
			//            target.path = "translation";
			//            ch.target = target;
			//            channels.Add(ch);
			//            animSamplers.Add(s);
			//        }
			//    }
			//    if (propName.Contains("m_LocalRotation"))
			//    {
			//        if (!gotRotation)
			//        {
			//            gotRotation = true;
			//            GlTF_AnimSampler s = new GlTF_AnimSampler(name + "_RotationSampler", "rotation");
			//            GlTF_Channel ch = new GlTF_Channel("rotation", s);
			//            GlTF_Target target = new GlTF_Target();
			//            target.id = targetName;
			//            target.path = "rotation";
			//            ch.target = target;
			//            channels.Add(ch);
			//            animSamplers.Add(s);
			//        }
			//    }
			//    if (propName.Contains("m_LocalScale"))
			//    {
			//        if (!gotScale)
			//        {
			//            gotScale = true;
			//            GlTF_AnimSampler s = new GlTF_AnimSampler(name + "_ScaleSampler", "scale");
			//            GlTF_Channel ch = new GlTF_Channel("scale", s);
			//            GlTF_Target target = new GlTF_Target();
			//            target.id = targetName;
			//            target.path = "scale";
			//            ch.target = target;
			//            channels.Add(ch);
			//            animSamplers.Add(s);
			//        }
			//    }
			//    parameters.Populate(curveDatas[i]);
			   // Type propType = curveDatas[i].type;
			//}
		}


	}

	public override void Write()
	{
		if (channels.Count == 0)
			return;

		Indent();		jsonWriter.Write ("{\n");
		IndentIn();
		Indent();		jsonWriter.Write ("\"channels\": [\n");
		foreach (GlTF_Channel c in channels)
		{
			CommaNL();
			c.Write ();
		}
		jsonWriter.WriteLine();
		Indent();		jsonWriter.Write ("],\n");

		Indent();		jsonWriter.Write ("\"samplers\": [\n");
		IndentIn();
		foreach (GlTF_AnimSampler s in animSamplers)
		{
			CommaNL();
			s.Write ();
		}
		IndentOut();
		jsonWriter.WriteLine();
		Indent();		jsonWriter.Write ("]\n");

		IndentOut();
		Indent();		jsonWriter.Write ("}");
	}
}
