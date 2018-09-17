using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerManager {
		
	public enum LayerType
	{
		Intruder,
		Friend,
	    Wall,
		Count
	}

	private static LayerManager m_instance;
	public static LayerManager Instance
	{
		get
		{
			if (m_instance == null)
				m_instance = new LayerManager();

			return m_instance;
		}
	}

	int[] m_computedLayers;

	public LayerManager()
	{
		m_computedLayers = new int[(int)LayerType.Count];

		m_computedLayers[(int)LayerType.Intruder] = LayerMask.NameToLayer("Player");
		m_computedLayers[(int)LayerType.Friend] = LayerMask.NameToLayer("Enemy");
		m_computedLayers[(int)LayerType.Wall] = LayerMask.NameToLayer("Unmovable");
	}

	public int HitWith(params LayerType[] types)
	{
		if (types.Length == 0)
			return -1;

		int layer = m_computedLayers[(int)types[0]];
		for (int i = 1; i < types.Length; i++)
			layer |= m_computedLayers[(int)types[i]];

		return 1 << layer;
	}

	public int HitWithout(params LayerType[] types)
	{
		return ~HitWith(types);
	}
}
