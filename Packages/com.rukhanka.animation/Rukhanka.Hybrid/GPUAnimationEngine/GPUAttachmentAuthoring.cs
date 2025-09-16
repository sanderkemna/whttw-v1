using Unity.Entities;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
[RequireComponent(typeof(Renderer))]
public class GPUAttachmentAuthoring: MonoBehaviour
{
	public int attachedBoneIndex = -1;
}

/////////////////////////////////////////////////////////////////////////////////

class GPUAttachmentBaker: Baker<GPUAttachmentAuthoring>
{
	public override void Bake(GPUAttachmentAuthoring a)
	{
	#if !RUKHANKA_NO_DEFORMATION_SYSTEM
		var e = GetEntity(a, TransformUsageFlags.Dynamic);
		
		var ga = new GPUAttachmentComponent()
		{
			attachedBoneIndex = GetComponentInParent<RigDefinitionAuthoring>(a) != null ? -1 : a.attachedBoneIndex
		};
		AddComponent(e, ga);
		AddComponent<GPUAttachmentBoneIndexMPComponent>(e);
		AddComponent<GPUAttachmentToBoneTransformMPComponent>(e);
		AddComponent<GPURigEntityLocalToWorldMPComponent>(e);
	#else
		Debug.LogWarning($"Looks like '{a.name}' object is configured as GPU attachment. But GPU animation system is disabled by RUKHANKA_NO_DEFORMATION_SYSTEM script symbol. Attachment mesh will be rendered incorrectly if it still uses GPUAttachment shader");
	#endif
	}
}
}
