using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Desafio Opcional (Parte C):
/// Grid espacial 3D com celulas de ~2m utilizando Dictionary<Vector3Int, List<Boid>>.
/// Otimizado para evitar alocacoes no Garbage Collector (GC) atraves de reuso de listas.
/// </summary>
public class SpatialGrid
{
    private readonly float cellSize;
    private readonly float invCellSize;

    // Dicionario principal conforme especificado no enunciado
    private readonly Dictionary<Vector3Int, List<Boid>> grid = new Dictionary<Vector3Int, List<Boid>>(512);

    // Pool de listas para garantir zero geracao de lixo para o GC
    private readonly List<List<Boid>> listPool = new List<List<Boid>>(512);
    private readonly List<Vector3Int> activeKeys = new List<Vector3Int>(512);

    public SpatialGrid(float cellSize = 2.0f)
    {
        this.cellSize = Mathf.Max(0.5f, cellSize);
        this.invCellSize = 1.0f / this.cellSize;
    }

    /// <summary>
    /// Limpa o grid reaproveitando as listas no pool (Zero GC Alloc).
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < activeKeys.Count; i++)
        {
            Vector3Int key = activeKeys[i];
            if (grid.TryGetValue(key, out List<Boid> list))
            {
                list.Clear();
                listPool.Add(list);
            }
        }
        grid.Clear();
        activeKeys.Clear();
    }

    /// <summary>
    /// Converte uma posicao de mundo contínua em coordenadas inteiras de celula.
    /// </summary>
    public Vector3Int GetCellCoord(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x * invCellSize),
            Mathf.FloorToInt(position.y * invCellSize),
            Mathf.FloorToInt(position.z * invCellSize)
        );
    }

    /// <summary>
    /// Insere um boid na celula correspondente.
    /// </summary>
    public void Insert(Boid boid)
    {
        Vector3Int cell = GetCellCoord(boid.transform.position);

        if (!grid.TryGetValue(cell, out List<Boid> list))
        {
            if (listPool.Count > 0)
            {
                int lastIndex = listPool.Count - 1;
                list = listPool[lastIndex];
                listPool.RemoveAt(lastIndex);
            }
            else
            {
                list = new List<Boid>(16);
            }

            grid[cell] = list;
            activeKeys.Add(cell);
        }

        list.Add(boid);
    }

    /// <summary>
    /// Preenche o buffer fornecido com vizinhos encontrados nas 27 celulas adjacentes (3x3x3).
    /// Retorna a quantidade de vizinhos encontrados (Zero GC Alloc).
    /// </summary>
    public int GetNeighborsNonAlloc(Vector3 position, float radius, Boid[] buffer)
    {
        Vector3Int centerCell = GetCellCoord(position);
        float radiusSqr = radius * radius;
        int count = 0;
        int maxCapacity = buffer.Length;

        // Itera sobre as celulas vizinhas (-1 a +1 em cada eixo)
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3Int neighborCell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z + z);

                    if (grid.TryGetValue(neighborCell, out List<Boid> cellBoids))
                    {
                        int cellCount = cellBoids.Count;
                        for (int i = 0; i < cellCount; i++)
                        {
                            Boid candidate = cellBoids[i];
                            float distSqr = (candidate.transform.position - position).sqrMagnitude;

                            if (distSqr <= radiusSqr)
                            {
                                buffer[count] = candidate;
                                count++;
                                if (count >= maxCapacity)
                                    return count;
                            }
                        }
                    }
                }
            }
        }

        return count;
    }
}
