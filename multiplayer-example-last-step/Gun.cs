using Godot;
using System;

public partial class Gun : Area2D, INetObject
{
    public int NetworkID { get; set;  }
	private NetworkManager _net;

	public bool _IsAttached{get; set;}

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Shoot()
	{
		
	}

	public override void _EnterTree()
	{
		this._net = this.GetNode<NetworkManager>("/root/NetworkManager");
		this._net.RegisterNetObject(this);

	}


	public override void _ExitTree()
	{
		this._net.DeregisterNetObject(this);
	}

	
}
