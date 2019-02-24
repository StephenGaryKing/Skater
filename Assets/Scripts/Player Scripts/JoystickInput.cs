using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SkateboardingCharacterController))]
public class JoystickInput : MonoBehaviour
{

	[SerializeField] Joystick m_joystick;
	[SerializeField] OllieButton m_ollieBtn;
	SkateboardingCharacterController m_controller;

    // Start is called before the first frame update
	void Start()
	{
		//grab refferences
		m_controller = GetComponent<SkateboardingCharacterController>();
	}

    void Update()
    {
		//key down inputs
		if (m_ollieBtn.m_shouldJump)
		{
			if (m_controller.TouchingGround)
				m_controller.Jump();
			else
			{
				if (!m_controller.DropIn())
					m_controller.Grind();
			}
			m_ollieBtn.m_shouldJump = false;
		}
		//key up Inputs
		
    }

    // Update is called once per frame
    void FixedUpdate()
    {
		m_controller.UpdatePlayer();

		//axis inputs
		Vector2 input = new Vector2(m_joystick.Horizontal, m_joystick.Vertical);
		if (m_ollieBtn.m_pressing)
			input.y = 1;

		m_controller.Move(input.y);
		m_controller.Rotate(input.x);

    }
}
