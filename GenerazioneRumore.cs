using UnityEngine;
using System.Collections;

public static class Noise
{
    public enum NormalizeMode { Local,Global}

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        float maxHeight = 0;        //altezza massima raggiungibile
        float amplitude = 1;        //amplitudine
        float frequency = 1;        //frequenza
        float noiseHeight = 0;      //altezza del rumore

        float[,] noiseMap = new float[mapWidth, mapHeight];         //height map

        System.Random rnd = new System.Random(seed);            //seed casuale 
        Vector2[] octOffs = new Vector2[octaves];



        for (int i = 0; i < octaves; i++)           
        {
            float offsetX = rnd.Next(-100000, 100000) + offset.x;
            float offsetY = rnd.Next(-100000, 100000) - offset.y;
            octOffs[i] = new Vector2(offsetX, offsetY);

            maxHeight = maxHeight + amplitude;
            amplitude = amplitude * persistance;
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;      //tener trccia dei valori minori e maggiori
        float minNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;


        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float coordX = (x - halfWidth + octOffs[i].x)  / scale * frequency ;
                    float coordY = (y - halfHeight + octOffs[i].y)  / scale * frequency ;

                    float perlinValue = Mathf.PerlinNoise(coordX, coordY) * 2 - 1;          //passo i valori alla funizone di Perlin implementata su Unity
                    noiseHeight += perlinValue * amplitude;

                    amplitude = amplitude * persistance;
                    frequency = frequency * lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if(normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);         //ritorna i valori tra 1 e 0 in base ai valori min e max passati
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxHeight) ;
                    noiseMap[x, y] = normalizedHeight;
                }
               
            }
        }
        return noiseMap;
    }

}
