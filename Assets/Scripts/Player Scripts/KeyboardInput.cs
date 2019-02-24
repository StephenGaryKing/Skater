using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SkateboardingCharacterController))]
public class KeyboardInput : MonoBehaviour
{

	SkateboardingCharacterController m_controller;

    // Start is called before the first frame update
	void Start()
	{
		//grab refferences
		m_controller = GetComponent<SkateboardingCharacterController>();
	}

    void Update()
    {
		//key inputs
		if (Input.GetKeyUp(KeyCode.Space))
			if (m_controller.TouchingGround)
				m_controller.Jump();

		if (Input.GetKeyUp(KeyCode.LeftShift))
			if (!m_controller.TouchingGround)
				if (!m_controller.DropIn())
					m_controller.Grind();
	}

    // Update is called once per frame
    void FixedUpdate()
    {
		m_controller.UpdatePlayer();

		//axis inputs
		Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Mathf.Clamp(Input.GetAxisRaw("Vertical"), -1, 0));
		if (Input.GetKey(KeyCode.Space))
			input.y = 1;

		m_controller.Move(input.y);
		m_controller.Rotate(input.x);

    }
}
