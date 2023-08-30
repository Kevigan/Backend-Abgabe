using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public enum NetMode
{
	Client,
	Server,
	None
}

public partial class NetworkManager : Node
{
	public NetMode Mode = NetMode.None;

	[Signal]
	public delegate void ConnectedEventHandler();

	public int bulletIDs = 1000;

	private const int _gamePort = 11337;
	private const string _gameNetKey = "MultiplayerExampleKey";
	private NetManager _net;
	private Node _world;

	private Dictionary<int, INetObject> _netObjects;

	public int maxGunsSpawn {get; set;}

	public override void _EnterTree()
	{
		this._netObjects = new();
	}


	public void SetWorldReference(Node2D worldNode)
	{
		this._world = worldNode;
	}

	private void BindNetListenerEvents(EventBasedNetListener listener)
	{
		if (this.Mode == NetMode.Client)
		{
			listener.PeerConnectedEvent += this.OnPeerConnectedClient;
			listener.NetworkReceiveEvent += this.OnNetworkReceiveClient;
			listener.PeerDisconnectedEvent += this.OnPeerDisconnectedClient;
		}
		else if (this.Mode == NetMode.Server)
		{
			listener.PeerConnectedEvent += this.OnPeerConnectedServer;
			listener.NetworkReceiveEvent += this.OnNetworkReceiveServer;
			listener.PeerDisconnectedEvent += this.OnPeerDisconnectedServer;
		}

		listener.NetworkErrorEvent += this.OnNetworkError;
	}


	private Player SpawnPlayer(int id, string name, Vector2 position, bool isYou = false)
	{
		PackedScene playerScene = ResourceLoader.Load<PackedScene>("res://Player.tscn");
		Player player = playerScene.Instantiate<Player>();
		player.Position = position;
		player.SetIdentity(id, name, isYou);
		this._world.AddChild(player);

		return player;
	}

	private Bullet SpawnBullet(int gunID, int playerID, Vector2 position, Vector2 direction, bool isYou = false)
	{
		PackedScene playerScene = ResourceLoader.Load<PackedScene>("res://Bullet.tscn");
		Bullet bullet = playerScene.Instantiate<Bullet>();
		bullet.Position = position;
		bullet.SetDirection(direction);
		bool bla = false;
		bullet.SetID(gunID, playerID, bulletIDs, isYou);
		this._world.AddChild(bullet);
		bulletIDs++;

		return bullet;
	}

	

	private void SPawnGun(int id, Vector2 position)
	{
		PackedScene gunScene = ResourceLoader.Load<PackedScene>("res://Gun.tscn");
		Gun gun = gunScene.Instantiate<Gun>();
		gun.Position = position;
		gun.NetworkID = id;
		this._world.AddChild(gun);
	}


	private void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
	{
		GD.PrintErr($"Network error from {endPoint} - {socketError}");
	}

	private T GetNetObject<T>(int id) where T : Node, INetObject
	{
		if (!this._netObjects.ContainsKey(id))
		{
			GD.Print($"Net object with id {id} not found.");
			return null;
		}

		return (T)this._netObjects[id];
	}

	public void Stop()
	{
		this._net.Stop();
		this._net = null;
		this.Mode = NetMode.None;
	}

	public override void _Process(double delta)
	{
		this._net?.PollEvents();
	}

	public override void _ExitTree()
	{
		this.Stop();
	}

	public void RegisterNetObject(INetObject obj)
	{
		if(this._netObjects.ContainsKey(obj.NetworkID)) return;
		this._netObjects.Add(obj.NetworkID, obj);
		GD.Print("Onregister ID: "+obj.NetworkID);
	}


	public void DeregisterNetObject(INetObject obj)
	{
		this._netObjects.Remove(obj.NetworkID);
		if(obj is Bullet && bulletIDs > 2000)
		{
			bulletIDs = 1000;
		}
	}
}
