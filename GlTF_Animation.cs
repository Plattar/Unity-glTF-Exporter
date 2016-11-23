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

	public void Populate (AnimationClip c)
	{
		AnimationClipCurveData[] curveDatas = AnimationUtility.GetAllCurves(c, true);

		// Get current clip targets list
		List<string> targetList = new List<string>();

		// A clip contains curves that can affect different targets
		foreach(AnimationClipCurveData cu in curveDatas)
		{
			string lastNodePath = cu.path.Split('/')[cu.path.Split('/').Length - 1];
			if (!targetList.Contains(lastNodePath))
				targetList.Add(lastNodePath);
		}

		if(bakeAnimation)
		{
			// Bake animation for all animated nodes
			foreach(string target in targetList)
			{
				GameObject targetGo = GameObject.Find(target);
				if (targetGo == null)
				{
					continue;
				}

				Transform targetObject = targetGo.transform;
				// Setup channels
				// Translation
				GlTF_AnimSampler sTranslation = new GlTF_AnimSampler(name + "_"+ target + "_TranslationSampler" , target +"_time", target + "_translation");
				GlTF_Channel chTranslation = new GlTF_Channel("translation", sTranslation);
				GlTF_Target targetTranslation = new GlTF_Target();
				targetTranslation.id = GlTF_Node.GetNameFromObject(targetObject);
				targetTranslation.path = "translation";
				chTranslation.target = targetTranslation;
				channels.Add(chTranslation);
				animSamplers.Add(sTranslation);

				// Rotation
				GlTF_AnimSampler sRotation = new GlTF_AnimSampler(name + "_" + target + "_RotationSampler", target + "_time", target + "_rotation");
				GlTF_Channel chRotation = new GlTF_Channel("rotation", sRotation);
				GlTF_Target targetRotation = new GlTF_Target();
				targetRotation.id = GlTF_Node.GetNameFromObject(targetObject);
				targetRotation.path = "rotation";
				chRotation.target = targetRotation;
				channels.Add(chRotation);
				animSamplers.Add(sRotation);

				// Scale
				GlTF_AnimSampler sScale = new GlTF_AnimSampler(name + "_" + target + "_ScaleSampler", target + "_time", target + "_scale");
				GlTF_Channel chScale = new GlTF_Channel("scale", sScale);
				GlTF_Target targetScale = new GlTF_Target();
				targetScale.id = GlTF_Node.GetNameFromObject(targetObject);
				targetScale.path = "scale";
				chScale.target = targetScale;
				channels.Add(chScale);
				animSamplers.Add(sScale);
				parameters.bakeAndPopulate(c, bakingFramerate, targetName, target);
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

		Indent();		jsonWriter.Write ("\"" + name + "\": {\n");
		IndentIn();
		Indent();		jsonWriter.Write ("\"channels\": [\n");
		foreach (GlTF_Channel c in channels)
		{
			CommaNL();
			c.Write ();
		}
		jsonWriter.WriteLine();
		Indent();		jsonWriter.Write ("]");
		CommaNL();

		parameters.Write ();
		CommaNL();

		Indent();		jsonWriter.Write ("\"samplers\": {\n");
		IndentIn();
		foreach (GlTF_AnimSampler s in animSamplers)
		{
			CommaNL();
			s.Write ();
		}
		IndentOut();
		jsonWriter.WriteLine();
		Indent();		jsonWriter.Write ("}\n");

		IndentOut();
		Indent();		jsonWriter.Write ("}");
	}
}
