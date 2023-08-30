using Godot;
using System;

public partial class JoinGame : Control
{
	[Signal]
	public delegate void SelectedEventHandler(string which);

	private void _on_back_pressed()
	{
		this.EmitSignal(SignalName.Selected, "main");
	}


	private void _on_connect_button_pressed()
	{
		string ip = this.GetNode<LineEdit>("%Address").Text;
		NetworkManager netManager = this.GetNode<NetworkManager>("/root/NetworkManager");
		netManager.TryJoinGame(ip);
	}
}
