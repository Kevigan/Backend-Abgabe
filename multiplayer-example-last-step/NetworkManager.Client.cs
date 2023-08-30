using Godot;
using LiteNetLib;
using LiteNetLib.Utils;

public partial class NetworkManager : Node
{
	public bool TryJoinGame(string ip)
	{
		EventBasedNetListener listener = new();
		this._net = new(listener);
		this._net.Start();
		this._net.Connect(ip, _gamePort, _gameNetKey);
		this.Mode = NetMode.Client;
		this.BindNetListenerEvents(listener);

		return true;
	}


	private void OnPeerConnectedClient(NetPeer peer)
	{
		GD.Print("Connection established.");
		this.EmitSignal(SignalName.Connected);
	}

	private void OnNetworkReceiveClient(NetPeer peer, NetPacketReader reader, DeliveryMethod _)
	{
		MessageID messageId = (MessageID)reader.GetByte();

		switch (messageId)
		{
			case MessageID.PositionUpdate:
				int movingPlayerId = reader.GetInt();
				Vector2 newPosition = new Vector2(reader.GetFloat(), reader.GetFloat());
				Player movingPlayer = this.GetNetObject<Player>(movingPlayerId);
				if (IsInstanceValid(movingPlayer))
				{
					movingPlayer.Position = newPosition;
				}
				break;

			case MessageID.YourPlayer:
				int playerId = reader.GetInt();
				string playerName = reader.GetString();
				Vector2 playerPosition = new Vector2(reader.GetFloat(), reader.GetFloat());
				this.SpawnPlayer(playerId, playerName, playerPosition, true);
				break;

			case MessageID.OtherPlayer:
				int otherPlayerId = reader.GetInt();
				
				string otherPlayerName = reader.GetString();
				Vector2 otherPlayerPosition = new Vector2(reader.GetFloat(), reader.GetFloat());
				this.SpawnPlayer(otherPlayerId, otherPlayerName, otherPlayerPosition);
				break;

			case MessageID.OtherPlayerDespawned:
				int disconnectedId = reader.GetInt();
				Player disconnectedPlayer = this.GetNetObject<Player>(disconnectedId);
				disconnectedPlayer?.QueueFree();
				break;
			case MessageID.Gun:
				int gunID = reader.GetInt();
				Vector2 gunPosition = new Vector2(reader.GetFloat(), reader.GetFloat());
				this.SPawnGun(gunID, gunPosition);
				break;
			case MessageID.RequestAttacheToPlayer:
				int playerID = reader.GetInt();
				Player playerToAttachTo = this.GetNetObject<Player>(playerID);
				gunID = reader.GetInt();
				Gun gun = this.GetNetObject<Gun>(gunID);
				this._world.RemoveChild(gun);

				playerToAttachTo.AddChild(gun);
				playerToAttachTo.SetAttachedGun(gun);
				gun.Position = Vector2.Zero;
				break;
			case MessageID.RequestSpawnBullet:
				gunID = reader.GetInt();
				playerID = reader.GetInt();
				int bulletID = reader.GetInt();
				
				Gun _gun = this.GetNetObject<Gun>(gunID);
				Vector2 direction = new Vector2(reader.GetFloat(), reader.GetFloat());
				bool isYou = reader.GetBool();
				this.SpawnBullet(_gun.NetworkID, playerID, _gun.GlobalPosition, direction, isYou);
				break;
			case MessageID.RequestDestroyBullet:
				bulletID = reader.GetInt();
				playerID = reader.GetInt();
				Player player;
				Bullet bullet;
				if(this._netObjects.ContainsKey(playerID))
				{
					player = this.GetNetObject<Player>(playerID);
					bullet = this.GetNetObject<Bullet>(bulletID);
					this.DestroyBulletClient(bullet);
				}
				
				break;
			case MessageID.RequestApplyDamage:
				playerID = reader.GetInt();
				bulletID = reader.GetInt();
				if(this._netObjects.ContainsKey(playerID))
				{
					player = this.GetNetObject<Player>(playerID);
					bullet = this.GetNetObject<Bullet>(bulletID);
					player.TakeDamage(bullet.damage);
				}

				break;
			case MessageID.RequestPlayerDeath:
				playerID = reader.GetInt();
				if(this._netObjects.ContainsKey(playerID))
				{
					player = this.GetNetObject<Player>(playerID);
					player.KillPlayer();
				}
				break;

		}

		reader.Recycle();
	}

	private void OnPeerDisconnectedClient(NetPeer peer, DisconnectInfo disconnectInfo)
	{
		GD.Print($"Disconnected ({peer.EndPoint}). Reason: {disconnectInfo.Reason}");

		this.Stop();
	}

	public void SendMessage_SpawnBullet(Vector2 gunPosition, int gunID, int playerID, int bulletID, Vector2 direction, bool _isYou)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.RequestSpawnBullet);
		writer.Put(gunID);
		writer.Put(playerID);
		writer.Put(bulletID);
		writer.Put(direction.X);
	 	writer.Put(direction.Y);
		writer.Put(_isYou);
		this._net.FirstPeer.Send(writer, DeliveryMethod.Unreliable);
	}

	public void SendMessage_RequestPositionUpdate(Vector2 myPosition)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.RequestPositionUpdate);
		writer.Put(myPosition.X);
		writer.Put(myPosition.Y);
		this._net.FirstPeer.Send(writer, DeliveryMethod.Unreliable);
	}

	public void SendMessage_RequestAttacheToPlayer(int parentID, int gunID)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.RequestAttacheToPlayer);
		writer.Put(parentID);
		writer.Put(gunID);
		this._net.FirstPeer.Send(writer, DeliveryMethod.Unreliable);
	}

	public void SendMessage_RequestDestroyBullet(int bulletID)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.RequestDestroyBullet);
		writer.Put(bulletID);
		this._net.FirstPeer.Send(writer, DeliveryMethod.Unreliable);
	}

	public void SendMessage_RequestApplyDamage(int _playerID, int _bulletID)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.RequestApplyDamage);
		writer.Put(_playerID);
		writer.Put(_bulletID);
		this._net.FirstPeer.Send(writer, DeliveryMethod.Unreliable);
	}

	public void SendMessage_RequestPlayerDeath(int _playerID)
	{
		NetDataWriter writer = new();
		writer.Put((byte)MessageID.RequestPlayerDeath);
		writer.Put(_playerID);
		this._net.FirstPeer.Send(writer, DeliveryMethod.Unreliable);
	}

	private void DestroyBulletClient(Bullet bullet)
	{
		bullet.QueueFree();
	}
}
