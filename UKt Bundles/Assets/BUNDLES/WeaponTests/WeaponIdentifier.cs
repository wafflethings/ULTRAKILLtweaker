using System;
using UnityEngine;

// Token: 0x02000274 RID: 628
public class WeaponIdentifier : MonoBehaviour
{
	// Token: 0x06000D53 RID: 3411 RVA: 0x000877C4 File Offset: 0x000859C4
	private void Start()
	{
		if (this.speedMultiplier == 0f)
		{
			this.speedMultiplier = 1f;
		}
	}

	// Token: 0x040015F2 RID: 5618
	public float delay;

	// Token: 0x040015F3 RID: 5619
	public float speedMultiplier;
}
