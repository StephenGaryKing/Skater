using BansheeGz.BGSpline.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailData : MonoBehaviour
{
	[SerializeField] List<BansheeGz.BGSpline.Curve.BGCurve> m_rail;
	[HideInInspector] public List<BGCcMath> m_railData;

	// Start is called before the first frame update
	void Start()
    {
        foreach(var rail in m_rail)
			m_railData.Add(rail.GetComponent<BGCcMath>());
    }
}
