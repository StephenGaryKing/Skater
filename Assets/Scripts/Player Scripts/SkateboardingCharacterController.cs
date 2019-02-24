using BansheeGz.BGSpline.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct DropInPoint
{
	public Vector3 position;
	public Vector3 normal;
}

[RequireComponent(typeof(Rigidbody))]
public class SkateboardingCharacterController : MonoBehaviour
{

	// player behaviour
	[SerializeField] float m_acceleration = 0.2f;
	[SerializeField] float m_turnAmount = 3;
	[SerializeField] float m_jumpAmount = 6;
	[SerializeField] float m_maxAdhesionDelta = 60f;
	[SerializeField] float m_smoothing = 0.2f;
	[SerializeField] float m_cruisingSpeed = 2f;
	[SerializeField] Cinemachine.CinemachineFreeLook m_cameraLogic;

	// utility
	Rigidbody m_rb;
	Vector3 m_currentUp = Vector3.up;
	Vector3 m_currentGroundPos;
	Collider m_lastRamp;
	[SerializeField] float m_currentSpeed = 0;
	float m_currentRotation;
	float m_targetRotation;
	BGCcMath m_currentQuaterPipe;
	BGCcMath m_currentRail;
	DropInPoint m_dropInPoint = new DropInPoint();
	Vector3 m_moveDir;

	//flags
	[SerializeField] bool m_touchingGround = false;
	[SerializeField] bool m_vert = false;
	[SerializeField] bool m_leavingGround = false;
	[SerializeField] bool m_grinding = false;
	[SerializeField] bool m_switch = false;
	[SerializeField] bool m_droppingIn = false;

	public bool TouchingGround { get { return m_touchingGround; } }

	// Start is called before the first frame update
	void Start()
    {
		m_currentRotation = transform.rotation.eulerAngles.y;
		m_rb = GetComponent<Rigidbody>();
    }

	/// <summary>
	/// Update the player's controller
	/// </summary>
	public void UpdatePlayer()
	{
		SetPlayerUp();
		UpdateRotation();
		UpdateCamera();
		//DebugDropIn();

		TrickManager.GetInstance().WriteToSpeedTextBox(m_rb.velocity.magnitude + "");
	}

	/// <summary>
	/// Rotate according to it's frame of reference
	/// </summary>
	void UpdateRotation()
	{
		// Determine the current rotation based on linear interpolation
		m_currentRotation = Mathf.Lerp(m_currentRotation, m_targetRotation, 0.5f);
		Quaternion rotation = Quaternion.Euler(0, m_currentRotation, 0);
		// Find the tilt around based on the player's relative up
		Quaternion tilt = Quaternion.FromToRotation(Vector3.up, m_currentUp);
		// Apply the rotation combined with the tilt to the player's rotation
		m_rb.MoveRotation(Quaternion.Lerp(m_rb.rotation, tilt * rotation, m_smoothing));
	}

	/// <summary>
	/// Relocate the camera depending on the gameplay
	/// </summary>
	void UpdateCamera()
	{
		if (m_vert || m_droppingIn)
			m_cameraLogic.m_YAxis.Value = Mathf.Lerp(m_cameraLogic.m_YAxis.Value, 1f, 0.1f);
		else
			m_cameraLogic.m_YAxis.Value = Mathf.Lerp(m_cameraLogic.m_YAxis.Value, 0.5f, 0.05f);
	}

	/// <summary>
	/// Find the correct up for the player based on it's surroundings
	/// </summary>
	void SetPlayerUp()
	{
		// If the player is dropping in force the transition between current rotation/position (should really start a new function, this one's getting phat)
		if (m_droppingIn)
		{
			m_currentUp = Vector3.Lerp(m_currentUp, m_dropInPoint.normal, 0.02f);
			Vector3 newPos = m_dropInPoint.position;
			newPos.y = transform.position.y;
			transform.position = Vector3.Lerp(transform.position, newPos, 0.05f);
		}

		// Check if the player is touching the ground
		RaycastHit hit = CheckIfGrounded();
		if (hit.collider != null)
			DoBehaviour(hit);
		else
		{
			if (m_touchingGround)
				LeaveGround();
			if (m_leavingGround)
				m_leavingGround = false;
		}
	}

	/// <summary>
	/// Check if the player should be considered grounded
	/// </summary>
	/// <returns>
	/// The location to ground the player
	/// </returns>
	RaycastHit CheckIfGrounded()
	{
		// if the player is falling and leaving the ground
		if (m_leavingGround && m_rb.velocity.y <= 0)
			m_leavingGround = false;
		int layerMask = LayerMask.GetMask("Ground");
		RaycastHit hit;
		float lookDist = 1.5f;

		if (!m_touchingGround && !m_leavingGround)
			// Handle sharp transitions using a spherical cast 
			if (Physics.SphereCast(transform.position + transform.up, 0.6f, -transform.up, out hit, 0.6f, layerMask))
				return hit;

		// Check to see if the player is still Currently on the ground
		if (Physics.Raycast(transform.position + transform.up, -transform.up, out hit, lookDist, layerMask))
		{
			// Set this on the raycast to avoid unwanted disengagement when using quarter pipes
			m_lastRamp = hit.collider;

			// Handle sharp transitions using a spherical cast 
			if (Physics.SphereCast(transform.position + transform.up, 0.6f, -transform.up, out hit, 0.7f, layerMask))
				return hit;
		}

		return new RaycastHit();
	}

	void DoBehaviour(RaycastHit hit)
	{
		if (!m_leavingGround)
		{
			// If the player is facing away from its movement vector, change to switch. Otherwise, they are in the regular stance
			float fwdVelDot = Vector3.Dot(transform.forward, m_rb.velocity);
			if (m_switch && fwdVelDot > 0)
			{
				m_switch = false;
				TrickManager.GetInstance().WriteToStanceTextBox("Regular");
			}
			else if (!m_switch && fwdVelDot < 0)
			{
				m_switch = true;
				TrickManager.GetInstance().WriteToStanceTextBox("Switch");
			}

			// Find the vector in front of the player or under the player that best suits
			m_moveDir = (m_switch) ? -transform.forward : transform.forward;

			// Find the degree to which the curent terrain and the previous differ
			Vector3 lastUp = m_currentUp;
			float transitionAngle = Vector3.Angle(lastUp, hit.normal);

			// If that angle is too high, stop sticking to the ground and instead, leave the ground.
			if (transitionAngle >= m_maxAdhesionDelta)
				LeaveGround();

			SetGroundedData(hit);
		}
	}

	void SetGroundedData(RaycastHit hit)
	{
		// Update player info
		m_currentUp = hit.normal;
		m_currentGroundPos = hit.point;
		m_leavingGround = false;
		m_touchingGround = true;
		m_vert = false;
		m_rb.useGravity = false;
		m_droppingIn = false;
	}

	public void Fall()
	{
		Debug.Log("The player stacked it! " + Time.time);
	}

	/// <summary>
	/// Make the player jump
	/// </summary>
	public void Jump()
	{
		if (m_touchingGround)
		{
			m_rb.velocity += Vector3.up * m_jumpAmount;
		}

		m_grinding = false;
		LeaveGround();
		TrickManager.GetInstance().WriteToTrickTextBox("Ollie");
	}

	void DebugDropIn()
	{
		float spacing = 0.5f;
		int numOfRaysToCast = 20;

		// Find the vector in front of the player or under the player that best suits
		Vector3 bestVector;
		float angleDelta = Vector3.Angle(m_currentUp, Vector3.up);
		if (angleDelta <= 45)
			bestVector = m_moveDir;
		else
			bestVector = -transform.up;

		// Make the vector only use X and Z, then normalize
		bestVector.y = 0;
		bestVector.Normalize();

		if (!m_touchingGround && m_rb.velocity.y > 0.5f)
			Debug.DrawLine(transform.position, transform.position + bestVector * 10, Color.blue);

		// Perform a series of raycasts in front of the player
		for (int i = 1; i <= numOfRaysToCast; i++)
			if (!m_touchingGround && m_rb.velocity.y > 0.5f)
				Debug.DrawLine(transform.position + (bestVector * spacing * i), transform.position + (bestVector * spacing * i) + Vector3.down * 10, Color.red);
	}

	/// <summary>
	/// Drop into a bank or other sufficiently sloped surface
	/// </summary>
	public bool DropIn()
	{
		if (!m_droppingIn && !m_touchingGround && m_rb.velocity.y > 0.5f)
		{
			bool rampFound = false;
			float spacing = 0.5f;
			int numOfRaysToCast = 30;

			// Pick the best direction to look for a slope
			Vector3 bestVector;
			float angleDelta = Vector3.Angle(m_currentUp, Vector3.up);
			if (angleDelta <= 45)
				bestVector = m_moveDir;
			else
				bestVector = -transform.up;

			// Make the vector only use X and Z, then normalize
			bestVector.y = 0;
			bestVector.Normalize();

			// Perform a series of raycasts in front of the player
			for (int i = 1; i <= numOfRaysToCast; i++)
			{
				if (!rampFound)
				{
					if (Physics.Raycast(transform.position + (bestVector * spacing * i), Vector3.down, out RaycastHit hit))
					{
						rampFound = TryDropinLocation(hit);
						if (rampFound)
						{
							TrickManager.GetInstance().WriteToTrickTextBox("Transfer");
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	bool TryDropinLocation(RaycastHit hit)
	{
		// If the angle between the current rotation and the drop in normal is large enough and if the angle between vect.up and the drop in normal is large enough
		if (Vector3.Angle(m_currentUp, hit.normal) > 45 && Vector3.Angle(hit.normal, Vector3.up) >= m_maxAdhesionDelta)
		{
			// Check that the player has enough velocity to perform the transition
			// Find the distance from the players position to the hit location along the horizontal axis' (dist)
			Vector3 distVector = hit.point - transform.position;
			distVector.y = 0;
			float dist = distVector.magnitude;

			// Find the half circumfrence for a circle with the diameter of dist
			dist = (2 * (Mathf.PI * dist))/2;

			// If the player's velocity is more than or equal to the half circumfrence, drop in worked
			if (m_rb.velocity.magnitude > dist)
			{
				// Lock the Horizontal motion and perform the transfer 
				m_droppingIn = true;
				m_dropInPoint.position = hit.point;
				m_dropInPoint.normal = hit.normal;

				Vector3 velocity = m_rb.velocity;
				velocity.x = 0;
				velocity.z = 0;
				m_rb.velocity = velocity;

				m_vert = false;
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Update the state of the player once they are no longer in contact with the ground 
	/// </summary>
	void LeaveGround()
	{
		m_leavingGround = true;
		m_touchingGround = false;
		m_rb.useGravity = true;

		float angleDelta = Vector3.Angle(m_currentUp, Vector3.up);
		if (angleDelta <= m_maxAdhesionDelta)
			m_currentUp = Vector3.up;

		if (angleDelta >= m_maxAdhesionDelta + 5)
		{
			// If the player is not in vert mode
			if (!m_vert)
			{
				// Get the quarterpipe data from the last ramp touched (if possible)
				QuarterPipeData data = m_lastRamp.GetComponent<QuarterPipeData>();
				Transform parent = m_lastRamp.transform.parent;
				while (!data && parent != null)
				{
					parent = parent.parent;
					if (parent)
						data = parent.GetComponent<QuarterPipeData>();
				}

				// If this ramp has coping
				if (data && data.m_rampData.Count > 0)
				{
					// Default to the first coping
					BGCcMath closestLip = data.m_rampData[0];
					float closestDist = Vector3.Distance(transform.position, data.m_rampData[0].CalcPositionByClosestPoint(transform.position));

					// For all the copings that belong to this ramp
					foreach (var d in data.m_rampData)
					{
						// Find the coping that is nearest to the player
						float newClosestDist = Vector3.Distance(transform.position, d.CalcPositionByClosestPoint(transform.position));
						if (newClosestDist < closestDist)
						{
							closestLip = d;
							closestDist = newClosestDist;
						}
					}

					// Set the current quarter pipe to the on that was found
					m_currentQuaterPipe = closestLip;

					// Grab a refference to the player's current velocity
					Vector3 velocity = m_rb.velocity;

					float signedAngle = Vector3.SignedAngle(m_moveDir, Vector3.up, transform.up);
					if (Vector3.Dot(m_moveDir.normalized, Vector3.up) >= 0.85f)
					{
						// Rotate to make quarter pipes easier to ride
						m_targetRotation += signedAngle;
						velocity = Vector3.zero;
						velocity.y = m_rb.velocity.y;
						m_rb.velocity = velocity;
					}
					else
					{
						// Reduce horizontal velocity
						velocity.y = 0;
						velocity /= 2;
						velocity.y = m_rb.velocity.y;
						m_rb.velocity = velocity;
					}

					// The player is now in the vert mode
					m_vert = true;
				}
			}
		}
	}

	/// <summary>
	/// Update the player's velocity and direction of movement based on the current state
	/// </summary>
	/// <param name="input"></param>
	public void Move(float input)
	{
		if (!m_touchingGround && !m_droppingIn)
			m_currentSpeed = Mathf.Lerp(m_currentSpeed, m_rb.velocity.magnitude, 0.05f);
		if (m_touchingGround && m_rb.velocity.magnitude > m_currentSpeed)
			m_currentSpeed = m_rb.velocity.magnitude;

		//in vert
		if (m_vert)
		{
			Vector3 m_currentRampPos;
			// Find the position on the coping spline that is closest to the player
			m_currentRampPos = m_currentQuaterPipe.CalcPositionByClosestPoint(transform.position, out float dist, out Vector3 tangent);

			// Calculate the player's new up based on the tangent of the current position on the spline
			Vector3 norm = Vector3.Cross(tangent, Vector3.up).normalized;
			float signedAngle = Vector3.SignedAngle(norm, m_currentUp, Vector3.up);
			m_targetRotation -= signedAngle;
			m_currentUp = norm;

			// Correct the position's y value based on the player's y position
			m_currentRampPos.y = transform.position.y; 

			// If the player is still within the bounds of the ramp's coping spline
			if (dist > 0 && dist < m_currentQuaterPipe.GetDistance())
				m_rb.MovePosition(Vector3.Lerp(m_rb.position, m_currentRampPos, 0.5f));

			// Grab a reference to the player's velocity
			Vector3 velocity = m_rb.velocity;
			// remove the vertical component for now
			velocity.y = 0;
			// Find the direction on the spline that the player is moving (tangent)
			Vector3 tan = (Vector3.Dot(velocity.normalized, tangent) > 0) ? tangent : -tangent;
			// Take out the vertical component
			tan.y = 0;
			tan.Normalize();
			// Force the velocity to coincide with the tangent
			velocity = tan * velocity.magnitude;
			// Re-introduce the vertical component of the velocity
			velocity.y = m_rb.velocity.y;
			m_rb.velocity = velocity;
			return;
		}

		//grinding
		if (m_grinding)
		{
			Vector3 m_currentRailPos;
			// Find the position on the coping spline that is closest to the player
			m_currentRailPos = m_currentRail.CalcPositionByClosestPoint(transform.position, out float dist, out Vector3 tangent);

			// Calculate the player's new up based on the tangent of the current position on the spline
			Vector3 tanOfTangent = Vector3.Cross(tangent, Vector3.up).normalized;
				m_currentUp = -Vector3.Cross(tangent, tanOfTangent).normalized;

			// If the player is still within the bounds of the rail's spline
			if (dist > 0 && dist < m_currentRail.GetDistance())
				m_rb.MovePosition(Vector3.Lerp(m_rb.position, m_currentRailPos, 0.5f));

			// Grab a reference to the player's velocity
			Vector3 velocity = m_rb.velocity;
			// Find the direction on the spline that the player is moving (tangent)
			Vector3 tan = (Vector3.Dot(velocity.normalized, tangent) > 0) ? tangent : -tangent;
			// Force the velocity to coincide with the tangent
			velocity = tan * m_currentSpeed;
			m_rb.velocity = velocity;
			m_touchingGround = true;

			// Rotate to face the correct direction
			float signedAngle = Vector3.SignedAngle(m_moveDir, tan, m_currentUp);
			m_targetRotation += signedAngle/2;
			m_currentRotation = m_targetRotation;

			Quaternion rotation = Quaternion.Euler(0, m_currentRotation, 0);
			// Find the tilt around based on the player's relative up
			Quaternion tilt = Quaternion.FromToRotation(Vector3.up, m_currentUp);
			// Apply the rotation combined with the tilt to the player's rotation
			transform.rotation = tilt * rotation;

			return;
		}

		//grounded
		if (m_touchingGround)
		{
			//holding faster
			if (input > 0)
				if (m_currentSpeed < m_cruisingSpeed * 2)
					m_currentSpeed = Mathf.Lerp(m_currentSpeed, m_cruisingSpeed * 2, 0.01f);
			//holding nothing
			if (input == 0)
			{
				if (m_currentSpeed < m_cruisingSpeed)
					m_currentSpeed = Mathf.Lerp(m_currentSpeed, m_cruisingSpeed, 0.01f);
				else
					m_currentSpeed -= 0.1f;
			}
			//holding slower
			if (input < 0)
				m_currentSpeed = Mathf.Lerp(m_currentSpeed, 0, 0.1f);

			// Set the player's velocity
			m_rb.velocity = m_currentSpeed * m_moveDir;

			// Move the player to the ground
			m_rb.MovePosition(Vector3.Lerp(m_rb.position, m_currentGroundPos, 0.05f));
		}
	}

	public void Grind()
	{
		float sphereRad = 2f;
		// Perform a spherecast. If anything the cast touches has a rail, add it to a list of possible rails.
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, sphereRad);
		if (hitColliders.Length > 0)
		{
			// Look through the list of rails and find the spline that is closest to the player.	
			List<RailData> rails = new List<RailData>();

			foreach(var c in hitColliders)
			{
				Transform parent = c.transform;
				while (parent != null)
				{
					RailData railData = parent.GetComponent<RailData>();					
					if (railData)
					{
						bool sameRail = false;
						foreach (var rail in rails)
						{
							if (rail == railData)
							{
								sameRail = true;
								break;
							}
						}
						if (!sameRail)
							rails.Add(railData);
						break;
					}
					else
						parent = parent.parent;
				}
			}

			if (rails.Count > 0)
			{
				// Default to the first rail
				BGCcMath closestRail = rails[0].m_railData[0];
				float closestDist = Vector3.Distance(transform.position, rails[0].m_railData[0].CalcPositionByClosestPoint(transform.position));

				foreach (var rail in rails)
				{
					foreach (var r in rail.m_railData)
					{
						// Find the coping that is nearest to the player
						float newClosestDist = Vector3.Distance(transform.position, r.CalcPositionByClosestPoint(transform.position));
						if (newClosestDist < closestDist)
						{
							closestRail = r;
							closestDist = newClosestDist;
						}
					}
				}
				if (closestDist <= sphereRad)
				{
					// If the player is facing away from its movement vector, change to switch. Otherwise, they are in the regular stance
					float fwdVelDot = Vector3.Dot(transform.forward, m_rb.velocity);
					if (m_switch && fwdVelDot > 0)
					{
						m_switch = false;
						TrickManager.GetInstance().WriteToStanceTextBox("Regular");
					}
					else if (!m_switch && fwdVelDot < 0)
					{
						m_switch = true;
						TrickManager.GetInstance().WriteToStanceTextBox("Switch");
					}

					// Find the vector in front of the player or under the player that best suits
					m_moveDir = (m_switch) ? -transform.forward : transform.forward;
					m_currentRail = closestRail;
					m_grinding = true;
				}
			}
		}
	}

	/// <summary>
	/// Cause the player to rotate
	/// </summary>
	/// <param name="input">
	/// Value between -1 and 1 denoting left or right
	/// </param>
	public void Rotate(float input)
	{
		if (m_touchingGround)
			m_targetRotation += input * m_turnAmount;
		else
			m_targetRotation += input * m_turnAmount * 2;
	}

	public void OnCollisionEnter(Collision col)
	{
		foreach (var c in col.contacts)
		{
			float dotW = Vector3.Dot(c.normal, Vector3.up);
			float dotL = Vector3.Dot(c.normal, transform.up);
			if (dotW <= 0 && dotL < 0.5f)
			{
				float dotF = Vector3.Dot(-c.normal, m_moveDir);
				float speedDissipation = 1 - dotF;
				m_currentSpeed *= speedDissipation;
				Vector3 reflection = Vector3.Reflect(m_moveDir, c.normal);
				float angle = Vector3.SignedAngle(m_moveDir, reflection, transform.up);
				m_targetRotation += angle;
				m_currentRotation = m_targetRotation;
				return;
			}

		}
	}
}
