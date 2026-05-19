using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace UntitledDungeonGame
{
    public class LightmapManager : MonoBehaviour
    {
        private static readonly Vector2Int[] CardinalDirections =
        {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1)
        };

        private static readonly Vector2Int[] DiagonalDirections =
        {
            new(1, 1),
            new(1, -1),
            new(-1, 1),
            new(-1, -1)
        };

        private struct PropagationNode
        {
            public int X;
            public int Y;
            public float Transmittance;

            public PropagationNode(int x, int y, float transmittance)
            {
                X = x;
                Y = y;
                Transmittance = transmittance;
            }
        }

        public static LightmapManager Instance { get; private set; }

        [SerializeField]
        private RawImage _lightMapRawImage;

        [SerializeField, Min(1)]
        private int _lightmapScale = 8;
        public int LightmapScale => _lightmapScale;

        [SerializeField]
        private bool _usePointFilter;

        [SerializeField]
        private Tilemap _tilemap;

        [SerializeField, Range(0f, 1f)]
        private float _ambientLight;

        [SerializeField, Range(0f, 1f)]
        private float _blockedCellTransmittance = 0.45f;

        [SerializeField, Range(0f, 1f)]
        private float _minLightThreshold = 0.02f;

        [SerializeField, Range(0, 4)]
        private int _blurPasses = 1;

        [SerializeField, Range(0f, 1f)]
        private float _blurStrength = 0.6f;

        [SerializeField, Range(0.1f, 2f)]
        private float _darknessStrength = 1f;

        private Texture2D _lightmapTexture;
        public Texture2D LightmapTexture => _lightmapTexture;

        private RectTransform _overlayRect;
        private Vector2Int _minLoadedTilePos;
        private Vector2Int _maxLoadedTilePos;
        private Vector2Int _gridSize;
        private readonly List<LightSource> _lightSources = new();

        private readonly Queue<PropagationNode> _propagationQueue = new();

        private TileVisibility[] _tileVisibilityGrid;
        private float[] _lightGrid;
        private float[] _workingLightGrid;
        private float[] _lightSourceWorkingGrid;
        private Color32[] _pixelBuffer;

        private void Awake()
        {
            Instance = this;

            if (_lightMapRawImage != null)
            {
                _overlayRect = _lightMapRawImage.rectTransform;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (_lightmapTexture != null)
            {
                Destroy(_lightmapTexture);
            }
        }

        public void RegisterLightSource(LightSource lightSource)
        {
            if (lightSource == null || _lightSources.Contains(lightSource))
            {
                return;
            }

            _lightSources.Add(lightSource);
            UpdateLightmap();
        }

        public void DeregisterLightSource(LightSource lightSource)
        {
            if (lightSource == null || !_lightSources.Remove(lightSource))
            {
                return;
            }

            UpdateLightmap();
        }

        public void UpdateLightMapBounds(Vector2Int minLoadedTilePos, Vector2Int maxLoadedTilePos)
        {
            _minLoadedTilePos = minLoadedTilePos;
            _maxLoadedTilePos = maxLoadedTilePos;

            UpdateLightmap();
        }

        public void UpdateLightmap()
        {
            if (!HasValidConfiguration() || !HasValidBounds())
            {
                return;
            }

            _gridSize = _maxLoadedTilePos - _minLoadedTilePos;

            UpdateOverlayRect();
            EnsureWorkingBuffers();
            BuildVisibilityGrid();
            SimulateLighting();
            ApplyBlurIfNeeded();
            RasterizeLightmap();
        }

        private void UpdateOverlayRect()
        {
            Vector2 minWorldPos = new(_minLoadedTilePos.x, _minLoadedTilePos.y);
            Vector2 maxWorldPos = new(_maxLoadedTilePos.x, _maxLoadedTilePos.y);
            Vector2 centerWorldPos = (minWorldPos + maxWorldPos) * 0.5f;
            Vector2 sizeWorld = maxWorldPos - minWorldPos;

            _overlayRect.position = centerWorldPos;
            _overlayRect.sizeDelta = sizeWorld;
            _overlayRect.localScale = Vector3.one;
        }

        private void EnsureWorkingBuffers()
        {
            int cellCount = _gridSize.x * _gridSize.y;
            int textureWidth = _gridSize.x * _lightmapScale;
            int textureHeight = _gridSize.y * _lightmapScale;

            if (_tileVisibilityGrid == null || _tileVisibilityGrid.Length != cellCount)
            {
                _tileVisibilityGrid = new TileVisibility[cellCount];
                _lightGrid = new float[cellCount];
                _workingLightGrid = new float[cellCount];
                _lightSourceWorkingGrid = new float[cellCount];
            }

            if (_pixelBuffer == null || _pixelBuffer.Length != textureWidth * textureHeight)
            {
                _pixelBuffer = new Color32[textureWidth * textureHeight];
            }

            FilterMode filterMode = _usePointFilter ? FilterMode.Point : FilterMode.Bilinear;
            if (_lightmapTexture != null &&
                _lightmapTexture.width == textureWidth &&
                _lightmapTexture.height == textureHeight &&
                _lightmapTexture.filterMode == filterMode)
            {
                return;
            }

            if (_lightmapTexture != null)
            {
                Destroy(_lightmapTexture);
            }

            _lightmapTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = filterMode
            };

            _lightMapRawImage.texture = _lightmapTexture;
        }

        private void BuildVisibilityGrid()
        {
            for (int y = 0; y < _gridSize.y; y++)
            {
                for (int x = 0; x < _gridSize.x; x++)
                {
                    Vector3Int tilePosition = new(_minLoadedTilePos.x + x, _minLoadedTilePos.y + y, 0);
                    _tileVisibilityGrid[GridIndex(x, y)] = new TileVisibility(_tilemap.HasTile(tilePosition) ? 0 : 1);
                }
            }
        }

        private void SimulateLighting()
        {
            Array.Fill(_lightGrid, _ambientLight);

            foreach (LightSource lightSource in _lightSources)
            {
                if (lightSource == null || !lightSource.isActiveAndEnabled)
                {
                    continue;
                }

                PropagateSingleLight(lightSource);
            }
        }

        private void PropagateSingleLight(LightSource lightSource)
        {
            Array.Fill(_lightSourceWorkingGrid, 0f);
            _propagationQueue.Clear();

            Vector2 localSourcePosition = new(
                lightSource.transform.position.x - _minLoadedTilePos.x,
                lightSource.transform.position.y - _minLoadedTilePos.y);

            int startX = Mathf.Clamp(Mathf.RoundToInt(localSourcePosition.x), 0, _gridSize.x - 1);
            int startY = Mathf.Clamp(Mathf.RoundToInt(localSourcePosition.y), 0, _gridSize.y - 1);
            int startIndex = GridIndex(startX, startY);

            float startLight = Mathf.Max(_ambientLight, lightSource.LightIntensity);
            _lightSourceWorkingGrid[startIndex] = startLight;
            _lightGrid[startIndex] = Mathf.Max(_lightGrid[startIndex], startLight);
            _propagationQueue.Enqueue(new PropagationNode(startX, startY, 1f));

            while (_propagationQueue.Count > 0)
            {
                PropagationNode currentNode = _propagationQueue.Dequeue();

                PropagateToNeighbors(lightSource, localSourcePosition, currentNode, CardinalDirections);

                // if (_allowDiagonalPropagation)
                // {
                //     PropagateToNeighbors(lightSource, localSourcePosition, currentNode, DiagonalDirections);
                // }
            }
        }

        private void PropagateToNeighbors(
            LightSource lightSource,
            Vector2 localSourcePosition,
            PropagationNode currentNode,
            IReadOnlyList<Vector2Int> directions)
        {
            for (int i = 0; i < directions.Count; i++)
            {
                Vector2Int direction = directions[i];
                int nextX = currentNode.X + direction.x;
                int nextY = currentNode.Y + direction.y;

                if (!IsInsideGrid(nextX, nextY))
                {
                    continue;
                }

                float distanceToSource = Vector2.Distance(localSourcePosition, new Vector2(nextX, nextY));
                if (distanceToSource > lightSource.LightRadius)
                {
                    continue;
                }

                float pathTransmittance = currentNode.Transmittance;
                if (IsBlocked(nextX, nextY))
                {
                    pathTransmittance *= _blockedCellTransmittance;
                }

                float candidateLight = CalculateDistanceFalloff(lightSource, distanceToSource) * pathTransmittance;
                if (candidateLight <= _minLightThreshold)
                {
                    continue;
                }

                int nextIndex = GridIndex(nextX, nextY);
                if (candidateLight <= _lightSourceWorkingGrid[nextIndex])
                {
                    continue;
                }

                _lightSourceWorkingGrid[nextIndex] = candidateLight;
                _lightGrid[nextIndex] = Mathf.Max(_lightGrid[nextIndex], candidateLight);
                _propagationQueue.Enqueue(new PropagationNode(nextX, nextY, pathTransmittance));
            }
        }

        private void ApplyBlurIfNeeded()
        {
            if (_blurPasses <= 0 || _blurStrength <= 0f)
            {
                return;
            }

            float[] source = _lightGrid;
            float[] destination = _workingLightGrid;

            for (int pass = 0; pass < _blurPasses; pass++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    for (int x = 0; x < _gridSize.x; x++)
                    {
                        int index = GridIndex(x, y);
                        float sum = source[index];
                        int sampleCount = 1;

                        for (int offsetY = -1; offsetY <= 1; offsetY++)
                        {
                            for (int offsetX = -1; offsetX <= 1; offsetX++)
                            {
                                if (offsetX == 0 && offsetY == 0)
                                {
                                    continue;
                                }

                                int sampleX = x + offsetX;
                                int sampleY = y + offsetY;
                                if (!IsInsideGrid(sampleX, sampleY))
                                {
                                    continue;
                                }

                                sum += source[GridIndex(sampleX, sampleY)];
                                sampleCount++;
                            }
                        }

                        float blurredValue = sum / sampleCount;
                        destination[index] = Mathf.Lerp(source[index], blurredValue, _blurStrength);
                    }
                }

                (source, destination) = (destination, source);
            }

            if (!ReferenceEquals(source, _lightGrid))
            {
                Array.Copy(source, _lightGrid, _lightGrid.Length);
            }
        }

        private void RasterizeLightmap()
        {
            int textureWidth = _lightmapTexture.width;
            int textureHeight = _lightmapTexture.height;

            for (int pixelY = 0; pixelY < textureHeight; pixelY++)
            {
                float sampleY = (pixelY + 0.5f) / _lightmapScale - 0.5f;

                for (int pixelX = 0; pixelX < textureWidth; pixelX++)
                {
                    float sampleX = (pixelX + 0.5f) / _lightmapScale - 0.5f;
                    float lightValue = SampleLightBilinear(sampleX, sampleY);
                    float darkness = Mathf.Clamp01((1f - lightValue) * _darknessStrength);
                    byte alpha = (byte)Mathf.RoundToInt(darkness * byte.MaxValue);
                    _pixelBuffer[pixelY * textureWidth + pixelX] = new Color32(0, 0, 0, alpha);
                }
            }

            _lightmapTexture.SetPixels32(_pixelBuffer);
            _lightmapTexture.Apply(false, false);
            _lightMapRawImage.texture = _lightmapTexture;
        }

        private float SampleLightBilinear(float x, float y)
        {
            float clampedX = Mathf.Clamp(x, 0f, _gridSize.x - 1f);
            float clampedY = Mathf.Clamp(y, 0f, _gridSize.y - 1f);

            int x0 = Mathf.FloorToInt(clampedX);
            int y0 = Mathf.FloorToInt(clampedY);
            int x1 = Mathf.Min(x0 + 1, _gridSize.x - 1);
            int y1 = Mathf.Min(y0 + 1, _gridSize.y - 1);

            float tx = clampedX - x0;
            float ty = clampedY - y0;

            float bottomLeft = _lightGrid[GridIndex(x0, y0)];
            float bottomRight = _lightGrid[GridIndex(x1, y0)];
            float topLeft = _lightGrid[GridIndex(x0, y1)];
            float topRight = _lightGrid[GridIndex(x1, y1)];

            float bottom = Mathf.Lerp(bottomLeft, bottomRight, tx);
            float top = Mathf.Lerp(topLeft, topRight, tx);
            return Mathf.Clamp01(Mathf.Lerp(bottom, top, ty));
        }

        private float CalculateDistanceFalloff(LightSource lightSource, float distanceToSource)
        {
            float normalizedDistance = distanceToSource / Mathf.Max(lightSource.LightRadius, 0.0001f);
            return lightSource.LightIntensity * Mathf.Clamp01(1f - normalizedDistance * normalizedDistance);
        }

        private int GridIndex(int x, int y)
        {
            return y * _gridSize.x + x;
        }

        private bool IsInsideGrid(int x, int y)
        {
            return x >= 0 && x < _gridSize.x && y >= 0 && y < _gridSize.y;
        }

        private bool IsBlocked(int x, int y)
        {
            return _tileVisibilityGrid[GridIndex(x, y)].Visibility == 1;
        }

        private bool HasValidConfiguration()
        {
            return _lightMapRawImage != null &&
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
