using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DeepSeaGame
{
    public class WorldManager : NetworkBehaviour
    {
        public static WorldManager Instance { get; private set; }

        [field: SerializeField] 
        public WorldGenerator WorldGenerator { get; private set; }
        public WorldDataStore WorldDataStore { get; private set; }
        public WorldTileStreamingRenderer TileStreamingRenderer { get; private set; }
        public bool IsWorldReady { get; private set; }
        
        public event Action OnWorldReady;

        [Header("Ocean Visuals")]
        [SerializeField] private ParallaxLayer _undergroundLayer;

        private WorldGenerationData _worldGenerationData;

        private void Awake()
        {
            Instance = this;

            _worldGenerationData = WorldGenerator.GetComponent<WorldGenerationData>();
            TileStreamingRenderer = WorldGenerator.GetComponent<WorldTileStreamingRenderer>();
            WorldDataStore = new(_worldGenerationData);

            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
            }
        }
        
        public override void OnDestroy() 
        {
            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.LocalClientId != clientId) return;

            StartCoroutine(InitializeRuntimeWorldRoutine());
        }

        private IEnumerator InitializeRuntimeWorldRoutine()
        {
            IsWorldReady = false;
            WorldGenerator.StartGeneration();

            while (!WorldGenerator.IsGenerationComplete)
            {
                yield return null;
            }

            TileStreamingRenderer.Initialize(WorldDataStore, WorldGenerator.ForegroundTilemap, WorldGenerator.BackgroundTilemap, WorldGenerator.AirTilemap, WorldGenerator.MultiTileRenderingTransform);

            yield return new WaitUntil(() => Player.Instance != null);

            Vector3 spawnPosition = ResolveSpawnWorldPosition(WorldGenerator.SpawnTile);
            Player.Instance.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            Player.Instance.SpawnPoint = spawnPosition;
            
            _undergroundLayer.Initialize(_worldGenerationData);

            IsWorldReady = true;
            OnWorldReady?.Invoke();
        }

        private Vector3 ResolveSpawnWorldPosition(Vector3Int spawnTile)
        {
            if (WorldGenerator.ForegroundTilemap != null)
            {
                Vector3 center = WorldGenerator.ForegroundTilemap.GetCellCenterWorld(spawnTile);
                return new Vector3(center.x, center.y, 0f);
            }

            return spawnTile;
        }
    }
}
