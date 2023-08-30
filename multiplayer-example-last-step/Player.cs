using Godot;
using System;

public partial class Player : Area2D, INetObject
{
	[Export]
	public float Speed = 50f;

	[Export]
	public float maxPlayerLife = 100;

	private float playerLife;

	public int NetworkID { get; set; }
	public string PlayerName { private set; get; }

	private NetworkManager _net;
	private Vector2 _velocity;
	private bool _isYou;

	public Gun attachedGun {get; private set;}

	private Slider hpBar;

	public void SetIdentity(int id, string name, bool isYou)
	{
		this.NetworkID = id;
		
		this.PlayerName = name;
		Label nameLabel = this.GetNode<Label>("%NameLabel");
		nameLabel.Text = name;
		this._isYou = isYou;

		playerLife = maxPlayerLife;
		hpBar = GetNode<Slider>("HP_Bar");
	}

	public override void _EnterTree()
	{
		this._net = this.GetNode<NetworkManager>("/root/NetworkManager");
		this._net.RegisterNetObject(this);

	}

	public override void _Process(double delta)
	{
		if(Input.IsActionJustPressed("leftMouse")  && _isYou && attachedGun != null)
		{
			var direction = (GetGlobalMousePosition() - attachedGun.GlobalPosition).Normalized();
			this._net.SendMessage_SpawnBullet(attachedGun.Position, attachedGun.NetworkID,this.NetworkID, this._net.bulletIDs, direction, true);
		}
		
	}

	public void SetAttachedGun(Gun _gun)
	{
		if(IsInstanceValid(_gun) && _isYou && attachedGun == null)
		{
			this.attachedGun = _gun;
			
		}
	}

	public void TakeDamage(int damage)
	{
		this.playerLife -= damage;
		hpBar.Value =  (int)this.playerLife;
		if(this.playerLife <= 0 && _isYou)
		{
			this._net.SendMessage_RequestPlayerDeath(NetworkID);
		}
	}

	public override void _ExitTree()
	{
		this._net.DeregisterNetObject(this);
	}


	public override void _PhysicsProcess(double delta)
	{
		if (!this._isYou)
			return;

		Vector2 movement = Vector2.Zero;

		if (Input.IsActionPressed("up"))
			movement += Vector2.Up;
		if (Input.IsActionPressed("down"))
			movement += Vector2.Down;
		if (Input.IsActionPressed("left"))
			movement += Vector2.Left;
		if (Input.IsActionPressed("right"))
			movement += Vector2.Right;

		movement = movement.Normalized();
		this._velocity = movement * this.Speed;

		Vector2 oldPosition = this.Position;
		this.Position += this._velocity * (float)delta;

		if (oldPosition != this.Position)
		{
			this._net.SendMessage_RequestPositionUpdate(this.Position);
		}
	}

	public void _on_area_entered(Area2D area)
	{
		Gun gun = area as Gun;
		if(gun != null && !gun._IsAttached && _isYou && attachedGun == null)
		{
			gun._IsAttached = true;
			this._net.SendMessage_RequestAttacheToPlayer(this.NetworkID, gun.NetworkID);
		}
	}

	public void KillPlayer()
	{
		this.QueueFree();
	}
}
