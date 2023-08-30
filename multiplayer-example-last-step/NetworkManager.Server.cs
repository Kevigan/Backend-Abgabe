using System.Collections.Generic;
using System.Linq;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;

public partial class NetworkManager : Node
{
	private Dictionary<int, NetPeer> _peerList;

	public bool TryHostGame(string sessionName, Node2D world)
	{
		EventBasedNetListener listener = new();
		this._net = new(listener);
		this._net.Start(_gamePort);
		GD.Print($"Server running on port {this._net.LocalPort}");
		this.Mode = NetMode.Server;

		this.BindNetListenerEvents(listener);
		listener.ConnectionRequestEvent += this.OnConnectionRequest;

		this.EmitSignal(SignalName.Connected);

		this._peerList = new();

		world.Modulate = new Color(255,0,1);

		return true;
	}

	private void OnConnectionRequest(ConnectionRequest request)
	{
		if (this._net.ConnectedPeersCount < 10)
		{
			request.AcceptIfKey(_gameNetKey);
		}
		else
		{
			request.Reject();
		}
	}

	//
	private void OnPeerConnectedServer(NetPeer peer)
	{
		GD.Print($"We got connection: {peer.EndPoint}");
		GD.Print(peer.Id);
		this._peerList.Add(peer.Id, peer);
		
		//Telling other players that ths peer has spawned
		foreach (KeyValuePair<int, INetObject> pair in this._netObjects)
		{
			INetObject netObject = pair.Value;

			if (netObject is not Player castedPlayer)
				continue;

			this.SendMessage_OtherPlayer(peer, castedPlayer);
		}

		string playerName = GD.RandRange(10000, 99999).ToString();
		Vector2 spawnPosition = new Vector2(GD.Randf() * 1000, GD.Randf() * 500);
		Player player = this.SpawnPlayer(peer.Id, playerName, spawnPosition);
		this.SendMessage_YourPlayer(peer, player);
		GD.Print(peer.Id);
		//telling this player, which other players are allready in game
		foreach (KeyValuePair<int, INetObject> pair in this._netObjects)
		{
			if (pair.Value is not Player castedPlayer)
				continue;

			if (castedPlayer == player)
				continue;

			NetPeer otherPeer = this._peerList.First(peer => peer.Key == pair.Value.NetworkID).Value;
			
			this.SendMessage_OtherPlayer(otherPeer, player);
			
		}

		foreach(KeyValuePair<int, INetObject> pair in this._netObjects)
		{
			INetObject netObject = pair.Value;
			if(netObject is not Gun castedGun) continue;

			this.SendMessage_Spawnedguns(peer, castedGun);
		}
	}

	private void OnNetworkReceiveServer(NetPeer peer, NetPacketReader reader, DeliveryMethod _)
	{
		MessageID messageId = (MessageID)reader.GetByte();

		switch (messageId)
		{
			case MessageID.RequestPositionUpdate:
				float requestedX = reader.GetFloat();
				float requestedY = reader.GetFloat();
				Player requestingPlayer = this.GetNetObject<Player>(peer.Id);
				if (IsInstanceValid(requestingPlayer))
				{
					requestingPlayer.Position = new Vector2(requestedX, requestedY);

					foreach (NetPeer otherPeer in this._peerList.Values)
					{
						if (otherPeer == peer)
							continue;

						this.SendMessage_PositionUpdate(otherPeer, requestingPlayer);
					}
				}
				break;
			case MessageID.RequestName:
				break;
			case MessageID.RequestAttacheToPlayer:
				int playerID = reader.GetInt();
				int gunID = reader.GetInt();
				requestingPlayer = this.GetNetObject<Player>(playerID);
				Gun gunToAttach = this.GetNetObject<Gun>(gunID);
				foreach(NetPeer otherPeer in this._peerList.Values)
				{
					this.SendMessage_AttachGun(otherPeer, requestingPlayer, gunToAttach);
				}
				this._world.RemoveChild(gunToAttach);
				requestingPlayer.AddChild(gunToAttach);
				gunToAttach.Position = Vector2.Zero;
				break;
			case MessageID.RequestSpawnBullet:
				gunID = reader.GetInt();
				playerID = reader.GetInt();
				int bulletID = reader.GetInt();
				Vector2 direction = new Vector2(reader.GetFloat(), reader.GetFloat());
				bool isYou = reader.GetBool();
				foreach(NetPeer otherPeer in this._peerList.Values)
				{
					 if(peer.Id == otherPeer.Id) continue;
					this.SendMessage_SpawnBullet(otherPeer, gunID, playerID, bulletIDs, direction, false);
				}
				this.SendMessage_SpawnBullet(peer, gunID, playerID, bulletIDs, direction, true);
				Gun _gun = this.GetNetObject<Gun>(gunID);
				this.SpawnBullet(gunID, playerID, _gun.GlobalPosition, direction, false);
				break;
			case MessageID.RequestDestroyBullet:
				bulletID = reader.GetInt();
				Bullet bullet = this.GetNetObject<Bullet>(bulletID);
				Player player;
				foreach(NetPeer otherPeer in this._peerList.Values)
				{
					 if(peer.Id == otherPeer.Id) continue;
					 this.SendMessage_DestroyBullet(otherPeer, bulletID);
				}
				this.SendMessage_DestroyBullet(peer, bulletID);
				this.DestroyBulletServer(bullet);
				break;
			case MessageID.RequestApplyDamage:
				playerID = reader.GetInt();
				bulletID = reader.GetInt();
				requestingPlayer = this.GetNetObject<Player>(playerID);
				bullet = this.GetNetObject<Bullet>(bulletID);

				if (IsInstanceValid(requestingPlayer))
				{
					requestingPlayer.TakeDamage(bullet.damage);
					foreach (NetPeer otherPeer in this._peerList.Values)
					{
						this.SendMessage_ApplyDamage(otherPeer, playerID, bulletID);
					}
				}
				break;
			case MessageID.RequestPlayerDeath:
				playerID = reader.GetInt();
				requestingPlayer = this.GetNetObject<Player>(playerID);
				if (IsInstanceValid(requestingPlayer))
				{
					requestingPlayer.KillPlayer();
					foreach (NetPeer otherPeer in this._peerList.Values)
					{
						// if (otherPeer == peer)
						// 	continue;
						this.SendMessage_PlayerDeath(otherPeer, playerID);
					}
				}
				break;
		}

		reader.Recycle();
	}

	private void OnPeerDisconnectedServer(NetPeer peer, DisconnectInfo disconnectInfo)
	{
		GD.Print($"Disconnected ({peer.EndPoint}). Reason: {disconnectInfo.Reason}");

		this._peerList.Remove(peer.Id);
		Player disconnectedPlayer = this.GetNetObject<Player>(peer.Id);
		disconnectedPlayer?.QueueFree();

		foreach (NetPeer otherPeer in this._peerList.Values)
		{
			this.SendMessage_OtherPlayerDespawned(otherPeer, peer.Id);
		}
	}

	private void SendMessage_YourPlayer(NetPeer peer, Player player)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.YourPlayer);
		writer.Put(player.NetworkID);
		writer.Put(player.PlayerName);
		writer.Put(player.Position.X);
		writer.Put(player.Position.Y);
		peer.Send(writer, DeliveryMethod.ReliableOrdered);
	}

	private void SendMessage_OtherPlayer(NetPeer peer, Player player)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.OtherPlayer);
		writer.Put(player.NetworkID);
		writer.Put(player.PlayerName);
		writer.Put(player.Position.X);
		writer.Put(player.Position.Y);
		peer.Send(writer, DeliveryMethod.ReliableOrdered);
	}

	private void SendMessage_OtherPlayerDespawned(NetPeer peer, int netId)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.OtherPlayerDespawned);
		writer.Put(netId);
		peer.Send(writer, DeliveryMethod.ReliableOrdered);
	}

	private void SendMessage_PositionUpdate(NetPeer peer, Player player)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.PositionUpdate);
		writer.Put(player.NetworkID);
		writer.Put(player.Position.X);
		writer.Put(player.Position.Y);
		peer.Send(writer, DeliveryMethod.Unreliable);
	}

	private void SendMessage_Spawnedguns(NetPeer peer, Gun gun)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.Gun);
		writer.Put(gun.NetworkID);
		writer.Put(gun.Position.X);
		writer.Put(gun.Position.Y);
		peer.Send(writer, DeliveryMethod.Unreliable);

	}

	private void SendMessage_AttachGun(NetPeer peer, Player player, Gun gun)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.RequestAttacheToPlayer);
		writer.Put(player.NetworkID);
		writer.Put(gun.NetworkID);
		peer.Send(writer, DeliveryMethod.Unreliable);
		
	}

	private void SendMessage_SpawnBullet(NetPeer peer, int _gunID, int playerID, int bulletID, Vector2 dir, bool _isYou)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.RequestSpawnBullet);
		writer.Put(_gunID);
		writer.Put(playerID);
		writer.Put(bulletID);
		writer.Put(dir.X);
	 	writer.Put(dir.Y);
		writer.Put(_isYou);
		peer.Send(writer, DeliveryMethod.Unreliable);
	}

	private void SendMessage_DestroyBullet(NetPeer peer, int _BulletID)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.RequestDestroyBullet);
		writer.Put(_BulletID);
		writer.Put(peer.Id);
		peer.Send(writer, DeliveryMethod.Unreliable);
	}

	private void SendMessage_ApplyDamage(NetPeer peer, int _PlayerID, int _bulletID)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.RequestApplyDamage);
		writer.Put(_PlayerID);
		writer.Put(_bulletID);
		peer.Send(writer, DeliveryMethod.Unreliable);
	}

	private void SendMessage_PlayerDeath(NetPeer peer, int _PlayerID)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.RequestPlayerDeath);
		writer.Put(_PlayerID);
		peer.Send(writer, DeliveryMethod.Unreliable);
	}

	public Gun SpawnGunsAtServerStart()
	{
		int index = 0;
		Gun guns;
		
			PackedScene gunScene = ResourceLoader.Load<PackedScene>("res://Gun.tscn");
			Gun gun = gunScene.Instantiate<Gun>();
			Vector2 spawnPosition = new Vector2(GD.Randf() * 1000, GD.Randf() * 500);
			gun.Position = spawnPosition;
			gun.NetworkID = index+500;
			this._world.AddChild(gun);
			guns = gun;
			index ++;
		
		return guns;
	}

	private void DestroyBulletServer(Bullet bullet)
	{
		bullet.QueueFree();
	}
}