using System;
using System.Collections.Generic;
using UnityEngine;
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

                UpdateLightmap();
            }
        }

        public void DeregisterLightSource(LightSource lightSource)
        {
            if (_lightSources.Contains(lightSource))
            {
                _lightSources.Remove(lightSource);

                UpdateLightmap();
            }
        }
        
        public void UpdateLightmap()
        {
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

            // Release old render texture if it exists
            if (_lightmapRenderTexture != null)
            {
                _lightmapRenderTexture.Release();
            }

            // Create a new render texture
            _lightmapRenderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 1)
            {
                enableRandomWrite = true,
                filterMode = _usePointFilter ? FilterMode.Point : FilterMode.Bilinear,
            };

            _lightmapRenderTexture.Create();
        }

        private void DispatchComputeShader()
        {
            // int renderTextureWidth = _lightmapRenderTexture.width;
            // int renderTextureHeight = _lightmapRenderTexture.height;
            // int kernelIndex = _lightmapComputeShader.FindKernel("CSMain");

            // // Create a list to hold the light data for all light sources
            // List<Vector4> lightSourceList = CreateLightSourceGPUData();

            // // Create and set structured buffer for light sources if there are any
            // ComputeBuffer lightSourceBuffer = new ComputeBuffer(lightSourceList.Count == 0 ? 1 : lightSourceList.Count, sizeof(float) * 4);
            // lightSourceBuffer.SetData(lightSourceList.ToArray());
            // _lightmapComputeShader.SetBuffer(kernelIndex, "LightSources", lightSourceBuffer);

            // // Set up the tile visibility array and compute buffer
            // TileVisibility[] tileVisibilityArray = new TileVisibility[renderTextureWidth * renderTextureHeight];
            // PopulateTileVisibilityArray(_minLoadedTilePos, _maxLoadedTilePos, _lightmapScale, tileVisibilityArray, renderTextureWidth);

            // // Create and set the compute buffer for tile visibility
            // ComputeBuffer tileDataBuffer = new ComputeBuffer(tileVisibilityArray.Length, sizeof(uint));
            // tileDataBuffer.SetData(tileVisibilityArray);
            // _lightmapComputeShader.SetBuffer(kernelIndex, "TileData", tileDataBuffer);

            // // Set shader parameters
            // _lightmapComputeShader.SetInt("Width", renderTextureWidth);
            // _lightmapComputeShader.SetInt("Height", renderTextureHeight);
            // _lightmapComputeShader.SetInt("OpaqueTileTolerance", _lightmapScale / 2);
            // _lightmapComputeShader.SetInt("NumLights", lightSourceList.Count);
            // _lightmapComputeShader.SetVector("BaseLight", GetBaseLight());
            // // Set the output texture
            // _lightmapComputeShader.SetTexture(kernelIndex, "Result", _lightmapRenderTexture);

            // // Dispatch the compute shader
            // int threadGroupsX = Mathf.CeilToInt((float)renderTextureWidth / 8f);
            // int threadGroupsY = Mathf.CeilToInt((float)renderTextureHeight / 8f);
            // _lightmapComputeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);

            // // Release buffers after use
            // tileDataBuffer.Release();
            // lightSourceBuffer.Release();

            // // Set the texture on the RawImage component
            // _lightMapRawImage.texture = _lightmapRenderTexture;
        }

        private List<Vector4> CreateLightSourceGPUData()
        {
            List<Vector4> lightSourceList = new List<Vector4>();

            // Iterate over all light sources and populate the lightSourceList
            foreach (var lightSource in _lightSources)
            {
                // Convert world position to texture coordinates
                Vector2 worldPosition = new Vector2(lightSource.transform.position.x, lightSource.transform.position.y);
                Vector2 lightTextureCoord = WorldToRenderTextureCoords(worldPosition);

                // Adjust light radius based on the lightmap scale (invert the scale to keep the radius consistent in world space)
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
            renderTextureCoord.x = Mathf.Round(renderTextureCoord.x);
            renderTextureCoord.y = Mathf.Round(renderTextureCoord.y);

            return renderTextureCoord;
        }

    }
}
