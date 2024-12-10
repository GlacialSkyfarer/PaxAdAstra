using Godot;
using System;

public partial class LogicDelay : LogicRelay
{

	bool retriggerBeforeDone = false;

	bool running = false;

	float delay = 1;

	public override void _Ready()
	{

		base._Ready();

		delay = (float)func_godot_properties["delay"];

		retriggerBeforeDone = (bool)func_godot_properties["retrigger_before_done"];

	}

	public override void Trigger() {

		if (!running || retriggerBeforeDone) {
			
			Delay();

			running = true;

		}

	}

	async void Delay() {

		await ToSignal(GetTree().CreateTimer((float)func_godot_properties["delay"]), "timeout");

		foreach (string n in targets) {

			gameManager.CallTarget(n, triggerFunction);

		}

		running = false;

	}

}
