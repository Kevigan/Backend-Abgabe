using Godot;
using System;

public partial class HostPage : Control
{
	[Signal]
	public delegate void SelectedEventHandler(string which);
	NetworkManager netManager;

	[Export]
	public int maxGunsSpawn = 5;

	[Export]
	Node2D world;

	public override void _EnterTree()
	{
		netManager = this.GetNode<NetworkManager>("/root/NetworkManager");
		netManager.maxGunsSpawn = maxGunsSpawn;
	}

	private void _on_host_go_button_pressed()
	{
		string sessionName = this.GetNode<LineEdit>("%SessionNameInput").Text;
		netManager.TryHostGame(sessionName, world);
		SpawnSpawnables();
	}


	private void _on_back_button_pressed()
	{
		this.EmitSignal(SignalName.Selected, "main");
	}

	private void SpawnSpawnables()
	{
		netManager.SpawnGunsAtServerStart();
	}
}
