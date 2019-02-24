using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The in scene version of this is a dummy that is cloned at runtime
/// </summary>
public class TrickManager
{

	private static TrickManager instance;
	Text m_trickTextBox;
	Text m_stanceTextBox;
	Text m_SpeedTextBox;

	private TrickManager() { }

	public static TrickManager GetInstance()
	{
		if (instance == null)
			instance = new TrickManager();

		return instance;
	}

	public void SetTrickBox(Text txtBox)
	{
		m_trickTextBox = txtBox;
	}

	public void SetStanceBox(Text txtBox)
	{
		m_stanceTextBox = txtBox;
	}

	public void SetSpeedBox(Text txtBox)
	{
		m_SpeedTextBox = txtBox;
	}

	public void WriteToTrickTextBox(string txt)
	{
		if (m_trickTextBox)
			m_trickTextBox.text += "\n" + txt;
	}

	public void WriteToStanceTextBox(string txt)
	{
		if (m_stanceTextBox)
			m_stanceTextBox.text = txt;
	}

	public void WriteToSpeedTextBox(string txt)
	{
		if (m_SpeedTextBox)
			m_SpeedTextBox.text = txt;
	}
}
