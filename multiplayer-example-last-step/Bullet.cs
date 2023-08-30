using Godot;
using System;

public partial class Bullet : Node2D, INetObject
{
	[Export]
	public float _speed = 5f;

	[Export]
	public int damage = 5;

	private float destroyTimer = 5f;

	private int gunID;
	private int playerID;
	private NetworkManager _net;

	private Vector2 velocity = Vector2.Right;

    public int NetworkID { get ; set ; }

	private bool hasEnteredPlayer = false;
	private bool _isYou;

    public override void _EnterTree()
	{
		this._net = this.GetNode<NetworkManager>("/root/NetworkManager");
		this._net.RegisterNetObject(this);
	}

	public override void _ExitTree()
	{
		this._net.DeregisterNetObject(this);
	}

	public override void _Process(double delta)
	{
		if(destroyTimer > 0)
		{
			destroyTimer -= (float)delta;
			if(destroyTimer <= 0)
			{
				QueueFree();
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 movement = Vector2.Zero;
		movement += velocity;
		movement = movement.Normalized() * _speed;

		this.Position += (float)delta * movement;
	}

	public void SetDirection(Vector2 dir)
	{
		this.velocity = dir;
	}

	public void SetID(int gunID, int playerID, int bulletID, bool _isYou)
	{
		this.gunID = gunID;
		this.playerID = playerID;
		this.NetworkID = bulletID;
		this._isYou = _isYou;
	}

	public void _on_area_2d_area_entered(Area2D area)
	{
		if(area is Player && !hasEnteredPlayer && _isYou)
		{
			Player player = area as Player;
			if(player.NetworkID == this.playerID) return;
			this._net.SendMessage_RequestApplyDamage(player.NetworkID, NetworkID);
			this._net.SendMessage_RequestDestroyBullet(this.NetworkID);
			hasEnteredPlayer = true;
		}
	}
}
