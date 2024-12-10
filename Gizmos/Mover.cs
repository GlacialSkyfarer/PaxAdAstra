using Godot;
using System;
using System.Collections.Generic;

public partial class Mover : CharacterBody3D
{

	[Export] public Godot.Collections.Dictionary func_godot_properties;

	Vector3 openPos;
	Vector3 closedPos;

	bool open = false;

	float speed = 1.5f;

	GameManager gameManager;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		GameManager gameManager = GetNode<GameManager>("/root/GameManager");

		closedPos = GlobalPosition;
		openPos = closedPos + (Vector3)func_godot_properties["open_position"];

		if ((bool)func_godot_properties["start_open"]) {
			open = true;
			GlobalPosition = openPos;
		}

		if (func_godot_properties.ContainsKey("targetname")) {

			gameManager.SetTargetName((string)func_godot_properties["targetname"], this);

		}

		speed = (float)func_godot_properties["speed"];

	}

	public override void _Process(double delta) {

		GlobalPosition = GlobalPosition.MoveToward(open ? openPos : closedPos, speed * (float)delta);

	}

	public void Open() {

		open = true;

	}
	
	public void Close() {
		
		open = false;
		
	}

}
