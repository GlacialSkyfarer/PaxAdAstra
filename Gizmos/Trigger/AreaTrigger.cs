using Godot;
using System;

public partial class AreaTrigger : Area3D
{

	GameManager gameManager;
	
	[Export] public Godot.Collections.Dictionary<string, Variant> func_godot_properties;
	Godot.Collections.Array<string> targets;

	string triggerFunction = "Trigger";
	string detriggerFunction = "Detrigger";

	bool triggered = false;
	bool repeat = false;

	bool enabled = true;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
		gameManager = GetNode<GameManager>("/root/GameManager");
		
		repeat = (bool)func_godot_properties["repeat"];
		
		targets = gameManager.GetTargets(func_godot_properties);
		
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		if ((string)func_godot_properties["targetname"] != "") {

			gameManager.SetTargetName((string)func_godot_properties["targetname"], this);

		}
		
	}
	
	public void OnBodyEntered(Node3D body) {
		
		if (!triggered && enabled) {
			
			triggered = true;
			
			foreach (string n in targets) {

				gameManager.CallTarget(n, triggerFunction);

			}
			
		}
		
	}
	
	public void OnBodyExited(Node3D body) {
		
		if (GetOverlappingAreas().Count == 0) {
			
			if (repeat) {
				
				triggered = false;
				
			}
			
			if (enabled) {

				foreach (string n in targets) {

					gameManager.CallTarget(n, detriggerFunction);

				}

			}

		}
		
	}

	public void Reset() {

		triggered = false;

	}

	public void Enable() {

		enabled = true;

	}

	public void Disable() {

		enabled = false;

	}
	
}
