using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class DynamicBoneChainBaker: Baker<DynamicBoneChainAuthoring>
{
	public override void Bake(DynamicBoneChainAuthoring a)
	{
		if (a.tip == null)
		{
			Debug.LogError($"DynamicBoneChainAuthoring '{a.name}': No tip transform defined!");
			return;
		}
		
		var e = GetEntity(a, TransformUsageFlags.None);
		
		var dbcc = new DynamicBoneChainComponent()
		{
			inertia = a.inertia,
			damping = a.damping,
			elasticity = a.elasticity,
			stiffness = a.stiffness,
			timeAccumulator = 0,
			prevPosition = a.transform.position
		};

		AddComponent(e, dbcc);
		var nodeList = AddBuffer<DynamicBoneChainNode>(e);
		
		var allBones = new List<Transform>();
		allBones.AddRange(GetComponentsInChildren<Transform>());
		
		for (var i = 0; i < allBones.Count; ++i)
		{
			var currentTransform = allBones[i];
			var refLocalPos = new BoneTransform()
			{
				pos = currentTransform.localPosition,
				rot = currentTransform.localRotation,
				scale = currentTransform.localScale
			};
			
			var be = new DynamicBoneChainNode()
			{
				boneEntity = GetEntity(currentTransform, TransformUsageFlags.Dynamic),
				prevPosition = currentTransform.position,
				position = currentTransform.position,
				referenceLocalPose = refLocalPos,
				parentIndex = allBones.FindIndex(0, allBones.Count, x => x == currentTransform.parent)
			};
			nodeList.Add(be);
		}
	}
}
}
