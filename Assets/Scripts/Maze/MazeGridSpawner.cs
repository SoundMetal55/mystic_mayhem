using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGridSpawner : MonoBehaviour
{
    public int width;
    public int height;
    public int mazeCellSize;
    public GameObject origin;

    public Grid g;

    [HideInInspector] public MazeGrid grid;

    private void Awake()
    {
        grid = new MazeGrid(width, height, mazeCellSize, Vector3Int.CeilToInt(origin.transform.position));
    }
}
