using Godot;
using Godot.Collections;
using System;

public enum PlayerState {

	Standing,
	Jumping,
	Falling

}

public partial class Player : Actor
{

	#region Variables

	private PlayerState currentState = PlayerState.Standing;

	[ExportCategory("Movement")]
	[ExportGroup("Running")]
	[Export] private float movementSpeed = 20f;
	[Export] private float acceleration = 20f;
	[Export] private float deceleration = 15f;
	[Export] private float airAcceleration = 10f;
	[Export] private float airDeceleration = 3f;
	[Export] private float maxSpeed = 60f;

	[ExportGroup("Jumping, Falling")]
	[Export] private Array<PlayerState> jumpableStates = new();
	[Export] private float jumpHeight = 50f;
	[Export] private float fallGravityMultiplier = 1.25f;
	[Export] private float maxCoyoteTime = 0.15f;
	private float coyoteTime = 0f;
	[Export] private float maxJumpBuffer = 0.15f;
	private float jumpBuffer = 0f;
	bool jumpLock = false;

    #endregion

    public override void _Process(double delta)
    {

        base._Process(delta);

		jumpBuffer = Mathf.Max(0, jumpBuffer - (float)delta);

		if (IsOnFloor()) {

			coyoteTime = maxCoyoteTime;

		} else {

			coyoteTime = Mathf.Max(0, coyoteTime - (float)delta);

		}

		if (Input.IsActionJustPressed("jump")) {

			jumpBuffer = maxJumpBuffer;

		}

    }

    public override void _PhysicsProcess(double delta)
    {

        base._PhysicsProcess(delta);

		Vector3 velocity = Velocity;

		velocity += GetGravity() * (float)delta;

		if (jumpBuffer > 0 && coyoteTime > 0 && jumpableStates.Contains(currentState)) 
		{

			jumpBuffer = 0;
			coyoteTime = 0;
			velocity.Y = Math2.GetJumpVelocity(jumpHeight, GetGravity().Length());
			currentState = PlayerState.Jumping;
			jumpLock = true;

		}

		velocity = StateProcess(delta, velocity);

		Velocity = velocity;

		MoveAndSlide();

    }

	private Vector3 StateProcess(double delta, Vector3 velocity) {

		float inputX = Input.GetAxis("movement_left", "movement_right");

		switch (currentState) {

			case PlayerState.Standing:

				if (!IsOnFloor()) {

					currentState = PlayerState.Falling;

				}

				velocity.X = Mathf.MoveToward(velocity.X, movementSpeed * inputX, (float)delta * GetTraction(inputX == 0 || velocity.X > movementSpeed && inputX > 0 || velocity.X < -movementSpeed && inputX < 0, currentState));

			break;
			case PlayerState.Falling:

				velocity += GetGravity() * (fallGravityMultiplier - 1) * (float)delta;

				if (IsOnFloor()) {

					currentState = PlayerState.Standing;

				}

				velocity.X = Mathf.MoveToward(velocity.X, movementSpeed * inputX, (float)delta * GetTraction(velocity.X > movementSpeed && inputX > 0 || velocity.X < -movementSpeed && inputX < 0, currentState));

			break;
			case PlayerState.Jumping:

				if (!Input.IsActionPressed("jump") || velocity.Y <= 0) {

					currentState = PlayerState.Falling;

				}

				if (IsOnFloor() && !jumpLock) {

					currentState = PlayerState.Standing;

				}

				velocity.X = Mathf.MoveToward(velocity.X, movementSpeed * inputX, (float)delta * GetTraction(velocity.X > movementSpeed && inputX > 0 || velocity.X < -movementSpeed && inputX < 0, currentState));

				jumpLock = false;

			break;

		}

		return velocity;

	}

	private float GetTraction(bool isDecel, PlayerState state) {

		switch (state) {

			case PlayerState.Standing:
				return isDecel ? deceleration : acceleration;
			case PlayerState.Falling:
			case PlayerState.Jumping:
				return isDecel ? airDeceleration : airAcceleration;

			default:
				return 0f;

		}

	}

}
