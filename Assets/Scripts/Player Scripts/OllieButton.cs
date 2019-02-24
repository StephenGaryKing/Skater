using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OllieButton : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{

	[HideInInspector] public bool m_pressing = false;
	[HideInInspector] public bool m_shouldJump = false;

	public void OnPointerDown(PointerEventData eventData)
	{
		m_pressing = true;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		m_pressing = false;
		m_shouldJump = true;
	}

}
