using System;
using UnityEngine;

public class WeaponIcon : MonoBehaviour
{
	private void OnEnable()
	{
	}
	public void UpdateIcon()
	{
	}

	public Sprite weaponIcon;

	public Sprite glowIcon;

	public int variationColor;

	[SerializeField]
	private Renderer[] variationColoredRenderers;

	[SerializeField]
	private Material[] variationColoredMaterials;
}
