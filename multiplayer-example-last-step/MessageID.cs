public enum MessageID : byte
{
	// Server messages.
	PositionUpdate = 100,
	YourPlayer = 101,
	OtherPlayer = 102,
	OtherPlayerDespawned = 103,
	Gun = 104,

	// Client messages.
	RequestPositionUpdate = 200,
	RequestName = 201,
	RequestAttacheToPlayer = 202,
	RequestSpawnBullet = 203,
	RequestDestroyBullet = 204,
	RequestApplyDamage = 205,
	RequestPlayerDeath = 206
}