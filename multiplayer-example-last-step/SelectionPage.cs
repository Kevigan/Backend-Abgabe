using Godot;
using System;

public partial class SelectionPage : Control
{
	[Signal]
	public delegate void SelectedEventHandler(string which);


	private void _on_join_button_pressed()
	{
		this.EmitSignal(SignalName.Selected, "join");
	}


	private void _on_host_button_pressed()
	{
		this.EmitSignal(SignalName.Selected, "host");
	}


	private void _on_quit_button_pressed()
	{
		this.GetTree().Quit();
	}
}
