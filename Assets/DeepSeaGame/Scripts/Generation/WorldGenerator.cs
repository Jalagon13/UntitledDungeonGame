using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeepSeaGame
{
    [RequireComponent(typeof(WorldGenerationData), typeof(WorldTileStreamingRenderer))]
    public class WorldGenerator : MonoBehaviour
    {
        [SerializeField]
        private Tilemap _backgroundTilemap;
        public Tilemap BackgroundTilemap => _backgroundTilemap;

        [SerializeField] 
        private Tilemap _forgroundTilemap;
        public Tilemap ForegroundTilemap => _forgroundTilemap;

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("_airTilemap")]
        private Tilemap _waterTilemap;
        public Tilemap WaterTilemap => _waterTilemap;
        
        [SerializeField] private Transform _multiTileRenderingTransform;
        public Transform MultiTileRenderingTransform => _multiTileRenderingTransform;

        public WorldGenerationState CurrentState { get; private set; } = WorldGenerationState.NotStarted;
        public float Progress { get; private set; }
        public Vector3Int SpawnTile { get; private set; }
        public bool IsGenerationComplete => CurrentState == WorldGenerationState.Completed;

        public event Action<WorldGenerationState, float> OnGenerationProgressChanged;
        public event Action<Vector3Int> OnGenerationCompleted;

        private WorldGenerationData _worldGenerationData;
        public WorldGenerationData WorldGenerationData => _worldGenerationData;
        
        private Coroutine _generationCoroutine;

        private void Awake() 
        {
            _worldGenerationData = GetComponent<WorldGenerationData>();
        }

        public void StartGeneration()
        {
            if (_generationCoroutine != null)
            {
                StopCoroutine(_generationCoroutine);
            }

            _generationCoroutine = StartCoroutine(GenerateWorldRoutine());
        }

        private IEnumerator GenerateWorldRoutine()
        {
            string resolvedSeed = _worldGenerationData.ResolvedSeed;
            int seedHash = ComputeStableSeedHash(resolvedSeed);
            float generationStartTime = Time.realtimeSinceStartup;

            Debug.Log($"Generating world data with seed '{resolvedSeed}' ({seedHash})");

            CurrentState = WorldGenerationState.Initializing;
            Progress = 0f;
            OnGenerationProgressChanged?.Invoke(CurrentState, Progress);

            List<GenerationStep> orderedSteps = GetOrderedSteps();
            WorldGenerationContext context = new(_worldGenerationData, WorldManager.Instance.WorldDataStore, seedHash, resolvedSeed, HandleProgressChanged);
            context.Begin(orderedSteps.Count);

            for (int i = 0; i < orderedSteps.Count; i++)
            {
                if(!orderedSteps[i].ExecuteStep)
                {
                    Debug.Log($"Skipping world gen step: {orderedSteps[i].GetType().Name} ({orderedSteps[i].State})");
                    continue;
                }
                
                GenerationStep step = orderedSteps[i];
                float stepStartTime = Time.realtimeSinceStartup;
                context.BeginStep(step.State, i);
                yield return StartCoroutine(step.Execute(context));

                float stepDuration = Time.realtimeSinceStartup - stepStartTime;
                Debug.Log($"World gen step complete: {step.GetType().Name} ({step.State}) in {stepDuration:F3}s");
            }

            context.Complete();
            SpawnTile = context.SpawnTile;
            _generationCoroutine = null;
            float totalGenerationTime = Time.realtimeSinceStartup - generationStartTime;
            Debug.Log($"World generation complete in {totalGenerationTime:F3}s");
            OnGenerationCompleted?.Invoke(SpawnTile);
        }

        private List<GenerationStep> GetOrderedSteps()
        {
            List<GenerationStep> orderedSteps = new();

            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out GenerationStep step))
                {
                    orderedSteps.Add(step);
                }
            }

            if (orderedSteps.Count == 0)
            {
                Debug.LogError("WorldGenerator has no GenerationStep children.");
            }

            return orderedSteps;
        }

        private void HandleProgressChanged(WorldGenerationState state, float progress)
        {
            CurrentState = state;
            Progress = progress;
            OnGenerationProgressChanged?.Invoke(CurrentState, Progress);
        }

        private int ComputeStableSeedHash(string seed)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < seed.Length; i++)
                {
                    hash = (hash * 31) + seed[i];
                }
                return hash;
            }
        }
    }
}
