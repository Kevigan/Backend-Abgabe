using Godot;
using System;

public partial class MainMenu : Node
{
	private Control _selectionPage;
	private Control _hostPage;
	private Control _joinPage;
	private Node2D _world;


	public override void _Ready()
	{
		this._selectionPage = this.GetNode<Control>("%SelectionPage");
		this._hostPage = this.GetNode<Control>("%HostPage");
		this._joinPage = this.GetNode<Control>("%JoinGame");
		this._world = this.GetNode<Node2D>("%World");

		NetworkManager netManager = this.GetNode<NetworkManager>("/root/NetworkManager");
		netManager.SetWorldReference(this._world);
		netManager.Connect(NetworkManager.SignalName.Connected, new Callable(this, MethodName._on_connected));
	}


	private void _on_connected()
	{
		this._on_selection("game");
	}


	private void _on_selection(string which)
	{
		foreach (Node node in this.GetChildren())
		{
			if (node is not CanvasItem canvasItemNode)
			{
				continue;
			}

			canvasItemNode.Hide();
		}

		switch (which)
		{
			case "host":
				this._hostPage.Show();
				break;

			case "main":
				this._selectionPage.Show();
				break;

			case "join":
				this._joinPage.Show();
				break;

			case "game":
				this._world.Show();
				break;
		}
	}
}
