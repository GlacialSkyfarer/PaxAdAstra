using Godot;
using Godot.Collections;
using System;

public enum PlayerState {

	Standing,
	Jumping,
	Falling,
	GrappleStalling,
	Grappling

}

public partial class Player : Actor
{

	#region References

	private ShaderMaterial ditherMaterial;
	[Export] private NodePath grappleCastPath;
	private RayCast3D grappleCast;
	[Export] private NodePath standingShapePath;
	private CollisionShape3D standingShape;
	[Export] private NodePath grapplingShapePath;
	private CollisionShape3D grapplingShape;
	[Export] private NodePath grappleSkeletonPath;
	private Skeleton3D grappleSkeleton;

	#endregion

	#region Variables

	private PlayerState currentState = PlayerState.Standing;

	[ExportCategory("Movement")]
	[ExportGroup("Running")]
	[Export] private float movementSpeed = 20f;
	[Export] private float acceleration = 20f;
	[Export] private float deceleration = 15f;
	[Export] private float airAcceleration = 10f;
	[Export] private float airDeceleration = 3f;
	[Export] private float maxSpeed = 80f;

	[ExportGroup("Jumping, Falling")]
	[Export] private Array<PlayerState> jumpableStates = new();
	[Export] private float jumpHeight = 50f;
	[Export] private float fallGravityMultiplier = 1.25f;
	[Export] private float maxCoyoteTime = 0.15f;
	private float coyoteTime = 0f;
	[Export] private float jumpBuffer = 0.15f;
	private float jumpBufferTime = 0f;
	bool jumpLock = false;

	[ExportGroup("Grappling")]
	[Export] private Array<PlayerState> grappleableStates = new();
	[Export] private float grappleRange = 5f;
	[Export] private float grappleStall = 0.25f;
	private float grappleStallTime = 0f;
	[Export] private float grappleSpeed = 45f;
	[Export] private string ungrappleableGroup = "Ungrappleable";
	private Vector3 grappleTarget = Vector3.Zero;
	private bool canGrapple = true;

	[ExportCategory("Visual Effects")]
	[ExportGroup("Camera")]
	[Export] private float ditherVelocityMult = 0.1f;

    #endregion

    public override void _Ready()
    {

        base._Ready();

		grappleSkeleton = GetNode<Skeleton3D>(grappleSkeletonPath);
		ditherMaterial = GetNode<MeshInstance3D>("/root/GBFilter").Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
		grappleCast = GetNode<RayCast3D>(grappleCastPath);
		grapplingShape = GetNode<CollisionShape3D>(grapplingShapePath);
		standingShape = GetNode<CollisionShape3D>(standingShapePath);

	}

    public override void _Process(double delta)
    {

        base._Process(delta);

		jumpBufferTime = Mathf.Max(0, jumpBufferTime - (float)delta);
		grappleStallTime = Mathf.Max(0, grappleStallTime - (float)delta);

		if (jumpableStates.Contains(currentState)) {

			coyoteTime = maxCoyoteTime;
			canGrapple = true;

		} else {

			coyoteTime = Mathf.Max(0, coyoteTime - (float)delta);

		}

		if (Input.IsActionJustPressed("jump")) {

			jumpBufferTime = jumpBuffer;

		}

    }

    public override void _PhysicsProcess(double delta)
    {

        base._PhysicsProcess(delta);

		Vector2 inputVector = Input.GetVector("movement_left", "movement_right", "movement_down", "movement_up").Normalized();

		Vector3 velocity = Velocity;

		switch (currentState) {

			case PlayerState.Standing:
			case PlayerState.Jumping:
				velocity += GetGravity() * (float)delta;
				break;
			case PlayerState.Falling:
				velocity += GetGravity() * (float)delta * fallGravityMultiplier;
				break;

		}

		if (jumpBufferTime > 0 && coyoteTime > 0) 
		{

			jumpBufferTime = 0;
			coyoteTime = 0;
			velocity.Y = Math2.GetJumpVelocity(jumpHeight, GetGravity().Length());
			currentState = PlayerState.Jumping;
			jumpLock = true;

		}
		if (canGrapple && grappleableStates.Contains(currentState) && Input.IsActionJustPressed("grapple")) {

			Vector3 direction = -GlobalBasis.Z;

			if (inputVector != Vector2.Zero) {

				direction = new(inputVector.X, inputVector.Y, 0);

			}

			grappleCast.TargetPosition = direction * grappleRange;
			grappleCast.ForceRaycastUpdate();

			if (grappleCast.IsColliding() && !((Node)grappleCast.GetCollider()).IsInGroup(ungrappleableGroup)) {

				grappleTarget = grappleCast.GetCollisionPoint();

				grappleStallTime = grappleStall;

				currentState = PlayerState.GrappleStalling;

			} else {

				if (grappleCast.IsColliding()) {

					grappleTarget = grappleCast.GetCollisionPoint();

				} else {

					grappleTarget = GlobalPosition + grappleCast.TargetPosition;

				}


			}

			grappleSkeleton.LookAt(grappleTarget, Vector3.Forward);
			grappleSkeleton.SetBonePosePosition(1, Vector3.Forward * GlobalPosition.DistanceTo(grappleTarget));

			canGrapple = false;

		}

		velocity = StateProcess(delta, velocity);

		Velocity = velocity;

		switch (currentState) {

			case PlayerState.Falling:
			case PlayerState.Grappling:
			case PlayerState.Standing:
			case PlayerState.Jumping:

				MoveAndSlide();

			break;

		}

		ditherMaterial.SetShaderParameter("dither_offset", (Vector2)ditherMaterial.GetShaderParameter("dither_offset") + (new Vector2(Velocity.X, Velocity.Y) * ditherVelocityMult * (float)delta));

    }

	private Vector3 StateProcess(double delta, Vector3 velocity) {

		float inputX = Input.GetAxis("movement_left", "movement_right");

		SetCollider(currentState, delta);

		switch (currentState) {

			case PlayerState.Standing:

				if (!IsOnFloor()) {

					currentState = PlayerState.Falling;

				}

				velocity.X = Mathf.MoveToward(velocity.X, movementSpeed * inputX, (float)delta * GetTraction(inputX == 0 || velocity.X > movementSpeed && inputX > 0 || velocity.X < -movementSpeed && inputX < 0, currentState));

			break;
			case PlayerState.Falling:
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
			case PlayerState.GrappleStalling:

				if (grappleStallTime <= 0 || !Input.IsActionPressed("grapple")) {

					currentState = PlayerState.Grappling;

				}

			break;
			case PlayerState.Grappling:

				velocity = GlobalPosition.DirectionTo(grappleTarget) * grappleSpeed;

				if (!Input.IsActionPressed("grapple")) {

					currentState = PlayerState.Jumping;

				}

				if (GetSlideCollisionCount() > 0) {

					currentState = PlayerState.Falling;

				}

			break;

		}

		return velocity;

	}

	private void SetCollider(PlayerState state, double delta) {

		switch (state) {

			case PlayerState.GrappleStalling:
			case PlayerState.Grappling:

				grappleSkeleton.LookAt(grappleTarget, Vector3.Forward);
				grappleSkeleton.SetBonePosePosition(1, Vector3.Forward * GlobalPosition.DistanceTo(grappleTarget));

				Vector3 grappleDirection = GlobalPosition.DirectionTo(grappleTarget);

				grapplingShape.LookAt(GlobalPosition + grappleDirection.Cross(Vector3.Forward), grappleDirection);

				grapplingShape.Disabled = false;
				grapplingShape.Visible = true;
				standingShape.Disabled = true;
				standingShape.Visible = false;

				break;
			case PlayerState.Standing:
			case PlayerState.Jumping:
			case PlayerState.Falling:

				grappleSkeleton.SetBonePosePosition(1, grappleSkeleton.GetBonePosePosition(1).MoveToward(Vector3.Zero, 2 * grappleSpeed * (float)delta));

				grapplingShape.Disabled = true;
				grapplingShape.Visible = false;
				standingShape.Disabled = false;
				standingShape.Visible = true;

				break;

		}

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
