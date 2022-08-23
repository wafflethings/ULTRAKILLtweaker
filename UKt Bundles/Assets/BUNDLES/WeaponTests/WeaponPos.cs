using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000277 RID: 631
public class WeaponPos : MonoBehaviour
{
	// Token: 0x06000D5F RID: 3423 RVA: 0x00087C1B File Offset: 0x00085E1B
	private void Start()
	{
	}

	// Token: 0x06000D60 RID: 3424 RVA: 0x00087C1B File Offset: 0x00085E1B
	private void OnEnable()
	{
	}

	// Token: 0x06000D61 RID: 3425 RVA: 0x00087C24 File Offset: 0x00085E24
	public void CheckPosition()
	{
	}

	// Token: 0x04001602 RID: 5634
	private bool ready;

	// Token: 0x04001603 RID: 5635
	public Vector3 currentDefault;

	// Token: 0x04001604 RID: 5636
	private Vector3 defaultPos;

	// Token: 0x04001605 RID: 5637
	private Vector3 defaultRot;

	// Token: 0x04001606 RID: 5638
	private Vector3 defaultScale;

	// Token: 0x04001607 RID: 5639
	public Vector3 middlePos;

	// Token: 0x04001608 RID: 5640
	public Vector3 middleRot;

	// Token: 0x04001609 RID: 5641
	public Vector3 middleScale;

	// Token: 0x0400160A RID: 5642
	public Transform[] moveOnMiddlePos;

	// Token: 0x0400160B RID: 5643
	public Vector3[] middlePosValues;

	// Token: 0x0400160C RID: 5644
	private List<Vector3> defaultPosValues = new List<Vector3>();

	// Token: 0x0400160D RID: 5645
	public Vector3[] middleRotValues;

	// Token: 0x0400160E RID: 5646
	private List<Vector3> defaultRotValues = new List<Vector3>();
}
