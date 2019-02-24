using BansheeGz.BGSpline.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuarterPipeData : MonoBehaviour
{
	[SerializeField] List<BansheeGz.BGSpline.Curve.BGCurve> m_rampLip;
	[HideInInspector] public List<BGCcMath> m_rampData = new List<BGCcMath>();

	// Start is called before the first frame update
	void Start()
    {
        foreach(var lip in m_rampLip)
			m_rampData.Add(lip.GetComponent<BGCcMath>());
    }
}
