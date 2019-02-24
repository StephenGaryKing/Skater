using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TextBoxSetter : MonoBehaviour
{

	[SerializeField] Text m_textBox;
	[SerializeField] BoxType m_boxType;

    // Start is called before the first frame update
    void Start()
    {
		switch(m_boxType)
		{
			case (BoxType.Tricks):
				TrickManager.GetInstance().SetTrickBox(m_textBox);
				break;

			case (BoxType.Stance):
				TrickManager.GetInstance().SetStanceBox(m_textBox);
				break;

			case (BoxType.Speed):
				TrickManager.GetInstance().SetSpeedBox(m_textBox);
				break;
		}
    }

	enum BoxType
	{
		Tricks,
		Stance,
		Speed
	}
}
