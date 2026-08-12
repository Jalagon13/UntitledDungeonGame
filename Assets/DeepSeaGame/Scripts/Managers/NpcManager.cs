using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace DeepSeaGame
{
    public class NpcManager : NetworkBehaviour
    {
        public static NpcManager Instance { get; private set; }

        [SerializeField] private bool _enableSpawning = true;
        [SerializeField] private float _startSpawnDelay;
        [SerializeField, Tooltip("Inner rectangle bounds where mobs CANNOT spawn (No-Spawn Zone). Also used as the camera frustum zone for despawn timer.")]
        private Vector2Int _innerNoSpawnDimensions = new Vector2Int(124, 70);

        [SerializeField, Tooltip("Outer rectangle bounds within which mobs CAN spawn. NPCs outside all players' outer zones are instantly despawned.")]
        private Vector2Int _outerSpawnDimensions = new Vector2Int(168, 94);

        [SerializeField, Tooltip("Maximum number of NPCs that can exist in the world at once.")]
        private int _globalMaxNpcCap = 200;

        [SerializeField, Tooltip("How often the server checks and updates NPC visibility for clients (in seconds).")]
        private float _visibilityUpdateInterval = 0.2f;

        [SerializeField, Tooltip("How many seconds an NPC can remain off-screen (outside all inner zones) before being despawned.")]
        private float _npcDespawnDuration = 14f;

        [SerializeField] private List<BiomeSO> _biomes;

        private readonly List<ServerCharacter> _activeNpcs = new();
        private readonly Dictionary<ServerCharacter, Timer> _despawnTimers = new();
        private readonly List<Timer> _timerTickBuffer = new();
        private readonly float _tickTime = 1f / 60f; // 60 ticks per second
        private readonly int _maxSpawnAttempts = 50;
        private readonly Dictionary<ulong, PlayerSpawnData> _playerSpawnData = new();
        private class PlayerSpawnData
        {
            public ulong PlayerId;
            public float CurrentCapacity;
            public BiomeSO CurrentBiome;
            public readonly List<ServerCharacter> SpawnedNpcs = new();
        }

        #region Start / Clean up

        private void Awake()
        {
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
                NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;

                // Initialize spawn data for already connected players (e.g. host)
                foreach (var client in NetworkManager.ConnectedClientsList)
                {
                    if (!_playerSpawnData.ContainsKey(client.ClientId))
                    {
                        _playerSpawnData[client.ClientId] = new PlayerSpawnData
                        {
                            PlayerId = client.ClientId,
                            CurrentCapacity = 0
                        };
                    }
                }

                InvokeRepeating(nameof(TryToSpawnNpc), _startSpawnDelay, _tickTime);
                InvokeRepeating(nameof(UpdateNpcVisibility), _startSpawnDelay + 0.1f, _visibilityUpdateInterval);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                CancelInvoke(nameof(TryToSpawnNpc));
                CancelInvoke(nameof(UpdateNpcVisibility));

                if (NetworkManager != null)
                {
                    NetworkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
                    NetworkManager.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
                }
            }
        }

        private void NetworkManager_OnClientConnectedCallback(ulong clientId)
        {
            if (!IsServer) return;

            if (!_playerSpawnData.ContainsKey(clientId))
            {
                _playerSpawnData[clientId] = new PlayerSpawnData
                {
                    PlayerId = clientId,
                    CurrentBiome = GetCurrentBiomeForPlayer(clientId)
                };
            }
        }

        private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
        {
            if (!IsServer) return;
            _playerSpawnData.Remove(clientId);
        }

        private void Update()
        {
            if (!IsServer) return;

            // Snapshot the timer list before ticking — Tick can fire OnTimerEnd → DespawnNpc →
            // _despawnTimers.Remove(), which would mutate the dictionary mid-foreach.
            _timerTickBuffer.Clear();
            _timerTickBuffer.AddRange(_despawnTimers.Values);

            foreach (var timer in _timerTickBuffer)
            {
                timer.Tick(Time.deltaTime);
            }
        }

        #endregion



        #region Spawning

        public void TryToSpawnNpc()
        {
            if (!IsServer || !_enableSpawning || !WorldManager.Instance.IsWorldReady) return;

            // Check global NPC cap
            if (GetGlobalActiveNpcCount() >= _globalMaxNpcCap) return;

            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                TryToSpawnNpcForClient(client);
            }
        }
        
        private void TryToSpawnNpcForClient(NetworkClient client)
        {
            if (client.PlayerObject == null) return;

            ulong playerId = client.ClientId;
            Transform playerTransform = client.PlayerObject.transform;

            if (!_playerSpawnData.TryGetValue(playerId, out PlayerSpawnData spawnData))
            {
                spawnData = new PlayerSpawnData
                {
                    PlayerId = playerId,
                    CurrentBiome = GetCurrentBiomeForPlayer(playerId)
                };

                _playerSpawnData[playerId] = spawnData;
            }

            // Clean up and recalculate this player's active NPC capacity
            RecalculatePlayerCapacity(spawnData);

            if(spawnData.CurrentBiome == null)
            {
                spawnData.CurrentBiome = GetCurrentBiomeForPlayer(playerId);
            }
            
            if (spawnData.CurrentCapacity >= spawnData.CurrentBiome.MaxNpcSlotAmount) return;

            // Calculate spawn probability per tick (Terraria-style)
            float spawnModifier = GetSpawnModifier(spawnData.CurrentCapacity, spawnData.CurrentBiome.MaxNpcSlotAmount);
            float spawnProbability = (spawnData.CurrentBiome.SpawnsPerMinute / 3600f) * spawnModifier; // Convert spawns per minute to probability per tick

            // Roll for spawn attempt
            if (Random.value >= spawnProbability) return;

            // Try to find a valid spawn spot (Terraria-style: limited attempts per tick)
            for (int attempt = 0; attempt < _maxSpawnAttempts; attempt++)
            {
                Vector2 potentialSpawnPoint = GetRandomTileInSpawnArea(playerTransform.position);
                
                if(SpawnAttempt(potentialSpawnPoint, spawnData))
                {
                    break; // Successfully spawned, exit attempts loop
                }
            }

        }
        
        private bool SpawnAttempt(Vector2 potentialSpawnPoint, PlayerSpawnData spawnData)
        {
            if (SpawnSpotIsValid(potentialSpawnPoint))
            {
                float remainingNpcSlotSpace = spawnData.CurrentBiome.MaxNpcSlotAmount - spawnData.CurrentCapacity;
                CharacterSO npcToSpawn = GetNpcFromPosition(potentialSpawnPoint);

                if (npcToSpawn != null && npcToSpawn.SlotAmount <= remainingNpcSlotSpace)
                {
                    SpawnNpcOnServer(potentialSpawnPoint, npcToSpawn, spawnData.PlayerId);
                    return true; // Successfully spawned, exit attempts loop
                }
            }
            return false;
        }
        
        private CharacterSO GetNpcFromPosition(Vector2 position)
        {
            BiomeType biomeType = WorldManager.Instance.WorldDataStore.GetBiomeAt(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));

            foreach (BiomeSO biomeSO in _biomes)
            {
                if (biomeSO.BiomeType == biomeType)
                {
                    return biomeSO.GetRandomNpc();
                }
            }
            return null;
        }
        
        private BiomeSO GetCurrentBiomeForPlayer(ulong playerId)
        {
            if (NetworkManager.ConnectedClients.TryGetValue(playerId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    Vector2 playerPos = client.PlayerObject.transform.position;
                    BiomeType biomeType = WorldManager.Instance.WorldDataStore.GetBiomeAt(Mathf.FloorToInt(playerPos.x), Mathf.FloorToInt(playerPos.y));

                    foreach (var biome in _biomes)
                    {
                        if (biome.BiomeType == biomeType)
                        {
                            return biome;
                        }
                    }
                }
            }

            // Fallback to default biome if none found
            Debug.LogWarning($"Could not determine biome for player {playerId}. Defaulting to first biome in list.");
            return _biomes.Count > 0 ? _biomes[0] : null;
        }

        private int GetGlobalActiveNpcCount()
        {
            int count = 0;
            foreach (var kvp in _playerSpawnData)
            {
                count += kvp.Value.SpawnedNpcs.Count;
            }
            return count;
        }

        private void SpawnNpcOnServer(Vector2 position, CharacterSO npcToSpawn, ulong playerId)
        {
            if (!IsServer) return;

            var spawnPosition = new Vector2(Mathf.FloorToInt(position.x) + 0.5f, Mathf.FloorToInt(position.y) + 0.5f);
            GameObject npcPrefab = Instantiate(npcToSpawn.NpcPrefab.gameObject, spawnPosition, Quaternion.identity);

            NetworkObject npcPrefabNetworkObject = npcPrefab.GetComponent<NetworkObject>();
            npcPrefabNetworkObject.SpawnWithObservers = false;
            npcPrefabNetworkObject.Spawn();

            if (npcPrefab.TryGetComponent<ServerCharacter>(out var serverCharacter))
            {
                _activeNpcs.Add(serverCharacter);

                // Create a despawn timer for this NPC; it starts paused since it just spawned in the outer zone
                var despawnTimer = new Timer(_npcDespawnDuration);
                despawnTimer.IsPaused = true;
                despawnTimer.OnTimerEnd += (_, _) => DespawnNpc(serverCharacter);
                _despawnTimers[serverCharacter] = despawnTimer;

                if (_playerSpawnData.TryGetValue(playerId, out PlayerSpawnData spawnData))
                {
                    spawnData.SpawnedNpcs.Add(serverCharacter);
                    RecalculatePlayerCapacity(spawnData);
                }

                // Immediately run visibility check on spawn for all connected clients to prevent latency
                foreach (var client in NetworkManager.ConnectedClientsList)
                {
                    if (client.PlayerObject == null) continue;
                    Vector2 playerPos = client.PlayerObject.transform.position;

                    if (IsNpcInOuterZone(spawnPosition, playerPos))
                    {
                        npcPrefabNetworkObject.NetworkShow(client.ClientId);
                    }
                }
            }

            _playerSpawnData.TryGetValue(playerId, out PlayerSpawnData data);
            // Debug.Log($"Spawning NPC: {npcPrefab.name} at {position} for player {playerId}, with {data.CurrentCapacity}/{data.MaxNpcSlotAmount} capacity");
        }

        private void RecalculatePlayerCapacity(PlayerSpawnData spawnData)
        {
            float cap = 0f;
            for (int i = spawnData.SpawnedNpcs.Count - 1; i >= 0; i--)
            {
                ServerCharacter npc = spawnData.SpawnedNpcs[i];
                if (npc == null || npc.LifeState == LifeState.Dead)
                {
                    spawnData.SpawnedNpcs.RemoveAt(i);
                    continue;
                }
                cap += npc.CharacterData.SlotAmount;
            }
            spawnData.CurrentCapacity = cap;
        }

        private bool SpawnSpotIsValid(Vector2 potentialSpawnPoint)
        {
            WorldDataStore worldDataStore = WorldManager.Instance.WorldDataStore;
            Vector2Int spawnPosition = new(Mathf.FloorToInt(potentialSpawnPoint.x), Mathf.FloorToInt(potentialSpawnPoint.y));
            
            bool isThereForegroundTile = worldDataStore.GetTileId(spawnPosition.x, spawnPosition.y, WorldTm.ForegroundTilemap) != GameDataRegistry.INVALID_ID;
            if(isThereForegroundTile)
            {
                return false;
            }

            if(IsInAnyPlayerInnerZone(potentialSpawnPoint))
            {
                return false;
            }
        
            return true;
        }
        
        private bool IsInAnyPlayerInnerZone(Vector2 potentialSpawnPoint)
        {
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;

                Vector2 playerPos = client.PlayerObject.transform.position;
                if (IsNpcInInnerZone(potentialSpawnPoint, playerPos))
                {
                    return true; // Point is visible/rendered on this client's screen!
                }
            }
            
            return false;
        }

        private Vector2 GetRandomTileInSpawnArea(Vector2 playerPos)
        {
            float halfInnerX = _innerNoSpawnDimensions.x / 2f;
            float halfInnerY = _innerNoSpawnDimensions.y / 2f;
            float halfOuterX = _outerSpawnDimensions.x / 2f;
            float halfOuterY = _outerSpawnDimensions.y / 2f;

            // Attempt to find a point in the donut area
            for (int i = 0; i < 10; i++)
            {
                float rx = Random.Range(-halfOuterX, halfOuterX);
                float ry = Random.Range(-halfOuterY, halfOuterY);

                // If it falls inside the inner no-spawn rectangle, re-roll
                if (Mathf.Abs(rx) < halfInnerX && Mathf.Abs(ry) < halfInnerY)
                {
                    continue;
                }

                return new Vector2(playerPos.x + rx, playerPos.y + ry);
            }

            return default;
        }

        private float GetSpawnModifier(float currentCapacity, int maxCapacity)
        {
            if (maxCapacity <= 0) return 0.1f;
            float activeRatio = currentCapacity / maxCapacity;

            // Terraria-style: More mobs = lower spawn rate, fewer mobs = higher spawn rate
            if (activeRatio < 0.2f)
            {
                return 1.5f; // 50% faster when area is mostly empty
            }
            else if (activeRatio < 0.4f)
            {
                return 1.3f; // 30% faster when area is 20-40% full
            }
            else if (activeRatio < 0.6f)
            {
                return 1.1f; // 10% faster when area is 40-60% full
            }
            else if (activeRatio < 0.8f)
            {
                return 0.9f; // 10% slower when area is 60-80% full
            }
            else if (activeRatio < 0.95f)
            {
                return 0.5f; // 50% slower when area is 80-95% full
            }

            return 0.1f; // 90% slower when area is nearly full
        }

        #endregion



        #region Npc Rendering

        private void UpdateNpcVisibility()
        {
            if (!IsServer) return;

            // 1. Clean up dead or null NPCs from active list
            for (int i = _activeNpcs.Count - 1; i >= 0; i--)
            {
                ServerCharacter npc = _activeNpcs[i];
                if (npc == null)
                {
                    _activeNpcs.RemoveAt(i);
                    continue;
                }

                if (npc.LifeState == LifeState.Dead)
                {
                    DespawnNpc(npc);
                }
            }

            // Build a snapshot of connected player positions once per visibility update
            var playerPositions = new List<Vector2>(NetworkManager.ConnectedClientsList.Count);
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    playerPositions.Add(client.PlayerObject.transform.position);
                }
            }

            // 2. Evaluate each active NPC
            var toInstantDespawn = new List<ServerCharacter>();
            foreach (var npc in _activeNpcs)
            {
                if (npc == null) continue;

                Vector2 npcPos = npc.transform.position;

                bool isInsideAnyOuterZone = false;
                bool isInsideAnyInnerZone = false;

                foreach (var playerPos in playerPositions)
                {
                    if (IsNpcInOuterZone(npcPos, playerPos))
                    {
                        isInsideAnyOuterZone = true;
                    }
                    if (IsNpcInInnerZone(npcPos, playerPos))
                    {
                        isInsideAnyInnerZone = true;
                    }

                    // Early exit — can't be more true than true
                    if (isInsideAnyOuterZone && isInsideAnyInnerZone) break;
                }

                // Instant despawn if outside ALL players' outer zones
                if (!isInsideAnyOuterZone)
                {
                    toInstantDespawn.Add(npc);
                    continue;
                }

                // Manage despawn timer:
                // If any player can see this NPC (inner zone), pause and reset the timer.
                // Otherwise let it count down.
                if (_despawnTimers.TryGetValue(npc, out Timer timer))
                {
                    if (isInsideAnyInnerZone)
                    {
                        timer.IsPaused = true;
                        timer.Reset();
                    }
                    else
                    {
                        timer.IsPaused = false;
                    }
                }

                // 3. Update Netcode observer visibility per client based on outer zone
                foreach (var client in NetworkManager.ConnectedClientsList)
                {
                    if (client.PlayerObject == null) continue;

                    ulong clientId = client.ClientId;
                    Vector2 playerPos = client.PlayerObject.transform.position;
                    bool shouldBeVisible = IsNpcInOuterZone(npcPos, playerPos);
                    bool isCurrentlyVisible = npc.NetworkObject.IsNetworkVisibleTo(clientId);

                    if (shouldBeVisible && !isCurrentlyVisible)
                    {
                        npc.NetworkObject.NetworkShow(clientId);
                    }
                    else if (!shouldBeVisible && isCurrentlyVisible)
                    {
                        npc.NetworkObject.NetworkHide(clientId);
                    }
                }
            }

            // Process instant despawns after iteration to avoid mutating the list mid-loop
            foreach (var npc in toInstantDespawn)
            {
                DespawnNpc(npc);
            }
        }

        public void DespawnNpc(ServerCharacter npc)
        {
            if (!IsServer || npc == null) return;

            // Remove from active list
            _activeNpcs.Remove(npc);

            // Remove and unsubscribe despawn timer
            if (_despawnTimers.TryGetValue(npc, out Timer timer))
            {
                timer.IsPaused = true;
                _despawnTimers.Remove(npc);
            }

            // Remove from every player's spawn data list and recalculate their capacity
            foreach (var kvp in _playerSpawnData)
            {
                PlayerSpawnData spawnData = kvp.Value;
                if (spawnData.SpawnedNpcs.Remove(npc))
                {
                    RecalculatePlayerCapacity(spawnData);
                }
            }

            // Despawn the NetworkObject (true = destroy the GameObject)
            if (npc.NetworkObject != null && npc.NetworkObject.IsSpawned)
            {
                npc.NetworkObject.Despawn(true);
            }
        }

        private bool IsNpcInInnerZone(Vector2 npcPos, Vector2 playerPos)
        {
            float dx = Mathf.Abs(npcPos.x - playerPos.x);
            float dy = Mathf.Abs(npcPos.y - playerPos.y);
            return dx <= _innerNoSpawnDimensions.x * 0.5f && dy <= _innerNoSpawnDimensions.y * 0.5f;
        }

        private bool IsNpcInOuterZone(Vector2 npcPos, Vector2 playerPos)
        {
            float dx = Mathf.Abs(npcPos.x - playerPos.x);
            float dy = Mathf.Abs(npcPos.y - playerPos.y);
            return dx <= _outerSpawnDimensions.x * 0.5f && dy <= _outerSpawnDimensions.y * 0.5f;
        }

        #endregion

    }
}
