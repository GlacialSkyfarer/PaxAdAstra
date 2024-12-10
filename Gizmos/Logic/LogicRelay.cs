using Godot;
using System;

public partial class LogicRelay : Node
{

	protected GameManager gameManager;

	[Export] public Godot.Collections.Dictionary<string, Variant> func_godot_properties;
	
	protected Godot.Collections.Array<string> targets;

	protected string triggerFunction = "Trigger";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		gameManager = GetNode<GameManager>("/root/GameManager");

		targets = gameManager.GetTargets(func_godot_properties);

		gameManager.SetTargetName((string)func_godot_properties["targetname"], this);

		triggerFunction = (string)func_godot_properties["trigger_function"];

	}

	public virtual void Trigger() {

		foreach (string n in targets) {

			gameManager.CallTarget(n, triggerFunction);

		}

	}

}
