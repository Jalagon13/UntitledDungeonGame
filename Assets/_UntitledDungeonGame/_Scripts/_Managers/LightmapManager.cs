using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace UntitledDungeonGame
{
    public class LightmapManager : MonoBehaviour
    {
        public static LightmapManager Instance { get; private set; }

        [SerializeField]
        private ComputeShader _lightmapComputeShader;

        [SerializeField]
        private RawImage _lightMapRawImage;

        [SerializeField] 
        private int _lightmapScale = 1;
        public int LightmapScale => _lightmapScale;
        

        [SerializeField] 
        private bool _usePointFilter;
        
        [SerializeField]
        private Tilemap _tilemap;

        [SerializeField, Range(0.9f, 1f)]
        private float _openStepTransmittance = 0.99f;

        [SerializeField, Range(0f, 1f)]
        private float _blockedStepTransmittance = 0.65f;

        [SerializeField, Range(0f, 1f)]
        private float _minContributionCutoff = 0.01f;
        

        
        private RenderTexture _lightmapRenderTexture;
        public RenderTexture LightMapRenderTexture => _lightmapRenderTexture;
        
        private RectTransform _overlayRect;
        private Vector2Int _minLoadedTilePos;
        private Vector2Int _maxLoadedTilePos;

        private List<LightSource> _lightSources = new List<LightSource>();

        private void Awake()
        {
            Instance = this;

            _overlayRect = _lightMapRawImage.rectTransform;
        }

        public void RegisterLightSource(LightSource lightSource)
        {
            if (!_lightSources.Contains(lightSource))
            {
                _lightSources.Add(lightSource);
                Debug.Log($"Light {lightSource.name} Registered");
                UpdateLightmap();
            }
        }

        public void DeregisterLightSource(LightSource lightSource)
        {
            if (_lightSources.Contains(lightSource))
            {
                _lightSources.Remove(lightSource);
                Debug.Log($"Light {lightSource.name} DeRegistered");
                UpdateLightmap();
            }
        }
        
        public void UpdateLightmap()
        {
            if (!HasValidConfiguration() || !HasValidBounds())
            {
                return;
            }
            Debug.Log($"Updating Lightmap");
            UpdateOverlayRect();
            UpdateRenderTexture();
            DispatchComputeShader();
        }

        public void UpdateLightMapBounds(Vector2Int minLoadedTilePos, Vector2Int maxLoadedTilePos)
        {
            _minLoadedTilePos = minLoadedTilePos;
            _maxLoadedTilePos = maxLoadedTilePos;

            UpdateLightmap();
        }

        private void UpdateOverlayRect()
        {
            // Convert tile positions to world space
            Vector2 minWorldPos = new Vector2(_minLoadedTilePos.x, _minLoadedTilePos.y);
            Vector2 maxWorldPos = new Vector2(_maxLoadedTilePos.x, _maxLoadedTilePos.y);

            // Calculate center and size in world space
            Vector2 centerWorldPos = (minWorldPos + maxWorldPos) / 2; // Center of the overlay
            Vector2 sizeWorld = new Vector2(maxWorldPos.x - minWorldPos.x, maxWorldPos.y - minWorldPos.y);

            // Update the RectTransform
            _overlayRect.position = centerWorldPos; // Center position in world space
            _overlayRect.sizeDelta = sizeWorld; // Set the scaled size in world units
            _overlayRect.localScale = Vector3.one; // Keep scale uniform
        }

        private void UpdateRenderTexture()
        {
            int renderTextureWidth = (_maxLoadedTilePos.x - _minLoadedTilePos.x) * _lightmapScale;
            int renderTextureHeight = (_maxLoadedTilePos.y - _minLoadedTilePos.y) * _lightmapScale;

            if (_lightmapRenderTexture != null &&
                _lightmapRenderTexture.width == renderTextureWidth &&
                _lightmapRenderTexture.height == renderTextureHeight &&
                _lightmapRenderTexture.filterMode == (_usePointFilter ? FilterMode.Point : FilterMode.Bilinear))
            {
                return;
            }

            // Release old render texture if it exists
            if (_lightmapRenderTexture != null)
            {
                _lightmapRenderTexture.Release();
                Destroy(_lightmapRenderTexture);
            }

            // Create a new render texture
            _lightmapRenderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 1)
            {
                enableRandomWrite = true,
                filterMode = _usePointFilter ? FilterMode.Point : FilterMode.Bilinear,
                format = RenderTextureFormat.ARGB32,
            };

            _lightmapRenderTexture.Create();
        }

        private void DispatchComputeShader()
        {
            int renderTextureWidth = _lightmapRenderTexture.width;
            int renderTextureHeight = _lightmapRenderTexture.height;
            int kernelIndex = _lightmapComputeShader.FindKernel("CSMain");

            // Create a list to hold the light data for all light sources
            List<Vector4> lightSourceList = CreateLightSourceGPUData();
            Vector4[] lightSourceData = lightSourceList.Count > 0
                ? lightSourceList.ToArray()
                : new[] { Vector4.zero };

            // Set up the tile visibility array and compute buffer
            TileVisibility[] tileVisibilityArray = new TileVisibility[renderTextureWidth * renderTextureHeight];
            PopulateTileVisibilityArray(_minLoadedTilePos, _maxLoadedTilePos, _lightmapScale, tileVisibilityArray, renderTextureWidth);

            ComputeBuffer lightSourceBuffer = null;
            ComputeBuffer tileDataBuffer = null;

            try
            {
                lightSourceBuffer = new ComputeBuffer(lightSourceData.Length, sizeof(float) * 4);
                lightSourceBuffer.SetData(lightSourceData);
                _lightmapComputeShader.SetBuffer(kernelIndex, "LightSources", lightSourceBuffer);

                tileDataBuffer = new ComputeBuffer(tileVisibilityArray.Length, sizeof(int));
                tileDataBuffer.SetData(tileVisibilityArray);
                _lightmapComputeShader.SetBuffer(kernelIndex, "TileData", tileDataBuffer);

                // Set shader parameters
                _lightmapComputeShader.SetInt("Width", renderTextureWidth);
                _lightmapComputeShader.SetInt("Height", renderTextureHeight);
                _lightmapComputeShader.SetInt("NumLights", lightSourceList.Count);
                _lightmapComputeShader.SetFloat("OpenStepTransmittance", _openStepTransmittance);
                _lightmapComputeShader.SetFloat("BlockedStepTransmittance", _blockedStepTransmittance);
                _lightmapComputeShader.SetFloat("MinContributionCutoff", _minContributionCutoff);

                // Set the output texture
                _lightmapComputeShader.SetTexture(kernelIndex, "Result", _lightmapRenderTexture);

                // Dispatch the compute shader
                int threadGroupsX = Mathf.CeilToInt((float)renderTextureWidth / 8f);
                int threadGroupsY = Mathf.CeilToInt((float)renderTextureHeight / 8f);
                _lightmapComputeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);
            }
            finally
            {
                tileDataBuffer?.Release();
                lightSourceBuffer?.Release();
            }

            // Set the texture on the RawImage component
            _lightMapRawImage.texture = _lightmapRenderTexture;
        }

        private void PopulateTileVisibilityArray(Vector2Int minLoadedTilePos, Vector2Int maxLoadedTilePos, int lightmapScale, TileVisibility[] tileVisibilityArray, int renderTextureWidth)
        {
            Dictionary<Vector3Int, TileVisibility> localVisibilityDict = new();

            // Build local visibility dictionary for the tiles on screen
            for (int x = minLoadedTilePos.x; x < maxLoadedTilePos.x; x++)
            {
                for (int y = minLoadedTilePos.y; y < maxLoadedTilePos.y; y++)
                {
                    Vector3Int tilePosition = new Vector3Int(x, y, 0);
                    localVisibilityDict[tilePosition] = new TileVisibility(_tilemap.HasTile(tilePosition) ? 0 : 1);
                }
            }

            // For future implementation: loop through all the resources on screen and add them to the local visibilitydict but for now just tilemap is fine for testing


            // Finally build the tile visiblity data for the gpu
            foreach (var kvp in localVisibilityDict)
            {
                Vector3Int tilePosition = kvp.Key;
                TileVisibility visibility = kvp.Value;

                int relativeX = (tilePosition.x - minLoadedTilePos.x) * lightmapScale;
                int relativeY = (tilePosition.y - minLoadedTilePos.y) * lightmapScale;

                for (int y = 0; y < lightmapScale; y++)
                {
                    for (int x = 0; x < lightmapScale; x++)
                    {
                        int index = (relativeY + y) * renderTextureWidth + (relativeX + x);
                        if (index >= 0 && index < tileVisibilityArray.Length)
                        {
                            tileVisibilityArray[index] = visibility;
                        }
                    }
                }
            }
        }

        private List<Vector4> CreateLightSourceGPUData()
        {
            List<Vector4> lightSourceList = new List<Vector4>();

            // Iterate over all light sources and populate the lightSourceList
            foreach (var lightSource in _lightSources)
            {
                float lightRadiusWorld = lightSource.LightRadius;
                Vector3 lightWorldPosition = lightSource.transform.position;

                if (lightWorldPosition.x + lightRadiusWorld < _minLoadedTilePos.x ||
                    lightWorldPosition.x - lightRadiusWorld > _maxLoadedTilePos.x ||
                    lightWorldPosition.y + lightRadiusWorld < _minLoadedTilePos.y ||
                    lightWorldPosition.y - lightRadiusWorld > _maxLoadedTilePos.y)
                {
                    continue;
                }

                // Convert world position to texture coordinates
                Vector2 worldPosition = new Vector2(lightWorldPosition.x, lightWorldPosition.y);
                Vector2 lightTextureCoord = WorldToRenderTextureCoords(worldPosition);

                // Adjust light radius to texture-space pixels.
                float adjustedLightRadius = lightSource.LightRadius * _lightmapScale;

                // Create light data (x, y position, intensity, adjusted radius)
                Vector4 lightData = new Vector4(lightTextureCoord.x, lightTextureCoord.y, lightSource.LightIntensity, adjustedLightRadius);

                // Add to the list
                lightSourceList.Add(lightData);
            }

            return lightSourceList;
        }

        public Vector2 WorldToRenderTextureCoords(Vector2 worldPos)
        {
            // Normalize to render texture coordinates
            float x = (worldPos.x - _minLoadedTilePos.x) / (_maxLoadedTilePos.x - _minLoadedTilePos.x);
            float y = (worldPos.y - _minLoadedTilePos.y) / (_maxLoadedTilePos.y - _minLoadedTilePos.y);

            // Scale to render texture dimensions
            Vector2 renderTextureCoord = new Vector2(x * _lightmapRenderTexture.width, y * _lightmapRenderTexture.height);

            // Snap to the nearest pixel
            renderTextureCoord.x = Mathf.Clamp(Mathf.Round(renderTextureCoord.x), 0, _lightmapRenderTexture.width - 1);
            renderTextureCoord.y = Mathf.Clamp(Mathf.Round(renderTextureCoord.y), 0, _lightmapRenderTexture.height - 1);

            return renderTextureCoord;
        }

        private bool HasValidConfiguration()
        {
            return _lightmapComputeShader != null &&
                   _lightMapRawImage != null &&
                   _tilemap != null &&
                   _lightmapScale > 0;
        }

        private bool HasValidBounds()
        {
            return _maxLoadedTilePos.x > _minLoadedTilePos.x &&
                   _maxLoadedTilePos.y > _minLoadedTilePos.y;
        }

    }
}
