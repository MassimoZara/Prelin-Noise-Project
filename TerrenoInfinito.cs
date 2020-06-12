using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public LevelOfDetaildInfo[] detailLevels;           //numero di divisioni sul lato della mappa
    public static float maxViewDst;                     //massima distanza di veduta

    public Transform viewer;                            //funzione per le altezze
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;                          //vettore contenente la vecchia posizione

    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;


    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();         //dizionario dei chunck già caricati
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {

        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        chunkSize = MapGenerator.dimensione - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()                          //
    {

        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }

            }
        }
    }

    public class TerrainChunk
    {

        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        LevelOfDetaildInfo[] detailLevels;
        LevelOfDetailMesh[] lodMeshs;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        MapData mapData;
        bool mapDataReceived;

        int previousLODindex = -1;

        public TerrainChunk(Vector2 coord, int size, LevelOfDetaildInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible(false);

            lodMeshs = new LevelOfDetailMesh[detailLevels.Length];

            for(int i =0; i< detailLevels.Length;i++)
            {
                lodMeshs[i] = new LevelOfDetailMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.dimensione, MapGenerator.dimensione);
            meshRenderer.material.mainTexture = texture;
            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDst;

                if (visible)
                {
                    int index = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                            index = i + 1;

                        else
                            break;
                    }

                    if (index != previousLODindex)
                    {
                        LevelOfDetailMesh lodMesh = lodMeshs[index];
                        if (lodMesh.hasMesh)
                        {
                            previousLODindex = index;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                            lodMesh.RequestMesh(mapData);
                    }
                    terrainChunksVisibleLastUpdate.Add(this);
                }
                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

    }

    class LevelOfDetailMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        public int lod;

        System.Action updateCallBack;

        public LevelOfDetailMesh(int lod, System.Action updateCallBack)
        {
            this.lod = lod;
            this.updateCallBack = updateCallBack;
        }
        public void OnMeshDataRecieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallBack();
        }
        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataRecieved);
        }
    }
    [System.Serializable]
    public struct LevelOfDetaildInfo
    {
        public int lod;

        public float visibleDistanceThreshold;
    }
}
