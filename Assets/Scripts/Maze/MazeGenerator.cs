﻿/**
 * MazeGenerator.cs
 * Gavin Gee
 * 
 * Randomly generates a maze utilizing Eller's Algorithm.
 * 
 * Based on @ShenShaw on Youtube's great series on generating mazes with
 * Eller's Algorithm (https://www.youtube.com/watch?v=5nWUX2TMJrY).
 * The algorithm is adapted here to be a bit more readable and work with 
 * 2d tilemaps.
 * 
 * TODO LIST
 * - TODO: implement step 6
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;
using System.Linq;

public class MazeGenerator : MonoBehaviour
{
    Grid grid;                 // the actual grid that tiles exist on
    Tilemap tilemap;			                // the tilemap that tiles exist on
    [SerializeField] TileBase[] wallTiles;         // the tile used to generate walls
    [SerializeField] bool generate = false;     // Used for testing, determines whether the map should be generated.
    [SerializeField] bool bottomRowEmpty = false;

    [SerializeField] MazeGridSpawner _mSpawner; // the spawner object
    [SerializeField] private MazeObjectSpawner objSpawner; // object spawner object
    private int _maxSetValue = 0;               // the current max set value

    [SerializeField] float rightWallProb = 0.5f;
    [SerializeField] float bottomWallProb = 0.5f;

    [SerializeField] bool doDebug = false;

    //private MazeCell keyLocation;

    bool doneGenerating = false;

    // Use this for initialization
    void Start()
    {
        grid = _mSpawner.grid;
        tilemap = grid.GetComponentInChildren<Tilemap>();

        if (generate)
        {
            if (bottomRowEmpty) GenerateMazeBottomRowEmpty();
            else GenerateMaze();
        }

        //keyLocation = new MazeCell(new Vector3Int(_mSpawner.keySpawnLocation.x, _mSpawner.keySpawnLocation.y));

        doneGenerating = true;

        // spawn key & boss
        objSpawner.SpawnKey();
        objSpawner.SpawnBoss();

    }

    #region MazeGeneration

    /** 
     * Generates the maze via the steps of Eller's Algorithm
     * 
     * NOTE: this function does generate the entine maze, 
     * but currently exhibits some buggy behavior on the final
     * row. 
     * 
     * For testing with pathfinding, use GenerateMazeBottomRowEmpty().
     */
    public void GenerateMaze()
    {
        // Step 1 -- create the first row of empty cells ( this also includes step 2 for the first run... )
        List<MazeCell> firstRow = new List<MazeCell>();

        for (int x = 0; x < _mSpawner.width; x++)
        {
            Vector3Int coords = new Vector3Int(x, 0);
            MazeCell newCell = new MazeCell(coords, _maxSetValue);

            firstRow.Add(newCell);
            _maxSetValue++;
        }

        // place walls at the top of the maze...
        foreach (MazeCell cell in firstRow)
        {
            Vector3Int gridCoords = MazeToGridCoords(cell.GetCoords());
            PlaceBottomWall(gridCoords.x, gridCoords.y + _mSpawner.mazeCellSize);
        }

        List<MazeCell> current = firstRow;
        // generation loop
        for (int i = 0; i < _mSpawner.height; i++)// _mSpawner.height; i++)
        {
            // step 3 -- create right walls & merge sets
            List<MazeCell> currentJoined = Step3(current);

            // step 4 -- generate bottom walls
            Step4(current);

            // if we're at the end...
            if (i == _mSpawner.height - 1)
            {
                foreach (MazeCell cell in current)
                {
                    if (!cell.GetBottomWallStatus())
                    {
                        cell.SetBottomWallStatus(true);
                        Vector3Int gridCoords = MazeToGridCoords(cell.GetCoords());
                        PlaceBottomWall(gridCoords.x, gridCoords.y);
                    }
                }
            }
            // otherwise do step 5
            else
            {
                // step 5 (+ 2) -- create the next row & clear walls
                current = Step5(current);
            }            
        }

        // step 6 -- create boundary walls & bottom walls // TODO: make this remove impossible walls
        // Step6()
    }

    public void GenerateMazeBottomRowEmpty()
    {
        // Step 1 -- create the first row of empty cells ( this also includes step 2 for the first run... )
        List<MazeCell> firstRow = new List<MazeCell>();

        for (int x = 0; x < _mSpawner.width; x++)
        {
            Vector3Int coords = new Vector3Int(x, 0);
            MazeCell newCell = new MazeCell(coords, _maxSetValue);
            firstRow.Add(newCell);
            _maxSetValue++;
        }

        // place walls at the top of the maze...
        foreach (MazeCell cell in firstRow)
        {
            Vector3Int gridCoords = MazeToGridCoords(cell.GetCoords());
            PlaceBottomWall(gridCoords.x, gridCoords.y + _mSpawner.mazeCellSize);
        }

        List<MazeCell> current = firstRow;
        // generation loop
        for (int i = 0; i < _mSpawner.height; i++)
        {
            if (i != _mSpawner.height - 1)
            {
                // step 3 -- create right walls & merge sets
                List<MazeCell> currentJoined = Step3(current);

                // step 4 -- generate bottom walls
                Step4(current);
                current = Step5(current);
            }
        }

        // place leftmost wall for last row
        Vector3Int wallCoords = MazeToGridCoords(current[0].GetCoords());
        PlaceRightWall(wallCoords.x - _mSpawner.mazeCellSize, wallCoords.y);

        // places the bottommost walls
        foreach (MazeCell cell in current)
        {
            if (!cell.GetBottomWallStatus())
            {
                cell.SetBottomWallStatus(true);
                Vector3Int gridCoords = MazeToGridCoords(cell.GetCoords());
                PlaceBottomWall(gridCoords.x, gridCoords.y);
            }
        }

        // place the rightmost wall
        wallCoords = MazeToGridCoords(current[current.Count - 1].GetCoords());
        PlaceRightWall(wallCoords.x, wallCoords.y);
    }

    #endregion

    #region Steps

    /*
     * TODO:
     * - document me
     * - test me
     */
    private List<MazeCell> Step3(List<MazeCell> row)
    {
        List<MazeCell> target = new List<MazeCell>(row);

        // place the leftmost wall on this row
        Vector3Int coords = MazeToGridCoords(row[0].GetCoords());
        PlaceRightWall(coords.x - _mSpawner.mazeCellSize, coords.y);

        for (int i = 0; i < _mSpawner.width-1; i++)
        {
            MazeCell current = target[i];
            MazeCell rightAdjacent = target[i + 1];

            if (current.GetSetValue() == rightAdjacent.GetSetValue())
            {
                // create a wall
                Vector3Int gridCoords = MazeToGridCoords(current.GetCoords());
                PlaceRightWall(gridCoords.x, gridCoords.y);
            }
            else
            {
                // 50/50 create a wall
                if (Random.value < rightWallProb)
                {
                    // create a wall
                    Vector3Int gridCoords = MazeToGridCoords(current.GetCoords());
                    PlaceRightWall(gridCoords.x, gridCoords.y);
                }
                else
                {
                    // union c & r.a to the same set
                    rightAdjacent.SetSetVal(current.GetSetValue());
                }
            }
        }

        // place the rightmost wall
        coords = MazeToGridCoords(row[row.Count - 1].GetCoords());
        PlaceRightWall(coords.x, coords.y);

        return target;
    }

    /**
     * TODO:
     * - test me
     * - document me
     */
    private void Step4(List<MazeCell> row)
    {
        List<MazeCell> target = new List<MazeCell>(row);
        List<List<MazeCell>> sets = SeparateIntoSets(target);
        List<List<MazeCell>> shuffled = ShuffleSets(sets);

        foreach (List<MazeCell> set in shuffled) // for each set
        {
            for (int i = 0; i < set.Count; i++)  // iterate through its elements.
            {
                if (i == 0) continue; // if its the first element skip it

                // otherwise, 50/50 chance whether to add a bottom wall or not.
                if (Random.value < bottomWallProb)
                {
                    set[i].SetBottomWallStatus(true);
                }
            }
        }

        // now actually generate the walls
        foreach (MazeCell current in row)
        {
            if (current.GetBottomWallStatus())
            {
                Vector3Int gridCoords = MazeToGridCoords(current.GetCoords());
                //Debug.Log("Placing bottom wall @ " + current.GetCoords());
                PlaceBottomWall(gridCoords.x, gridCoords.y);
            }
        }
    }

    /*
     * TODO:
     * - document me
     * - test me
     */
    private List<MazeCell> Step5(List<MazeCell> row)
    {
        List<MazeCell> next = new List<MazeCell>(row); // no need to remove right walls.. they technically aren't ever stored anywhere

        foreach (MazeCell cell in next)
        {
            if (cell.GetBottomWallStatus())
            {
                // remove the bottom wall
                cell.SetBottomWallStatus(false);

                // remove the cell from its set (give it a new distinct set..)
                cell.SetSetVal(_maxSetValue);
                _maxSetValue++;
            }
            cell.SetCoords(new Vector3Int(cell.GetCoords().x, cell.GetCoords().y - 1)); // increment the axis by 1
        }
        
        return next;   
    }

    private void Step6(List<MazeCell> row)
    {
        // place the rightmost wall
        Vector3Int gridCoords = MazeToGridCoords(row[0].GetCoords());
        PlaceRightWall(gridCoords.x - _mSpawner.mazeCellSize, gridCoords.y);

        for (int i = 0; i < row.Count - 1; i++)
        {
            MazeCell current = row[i];
            MazeCell rightAdjacent = row[i + 1];

            // make sure that every cell has a bottom wall
            current.SetBottomWallStatus(true);
            if (!rightAdjacent.GetBottomWallStatus()) rightAdjacent.SetBottomWallStatus(true);

            if (current.GetSetValue() != rightAdjacent.GetSetValue())
            {
                // remove the right wall... TODO: figure out if i need to do anything here? more likely, I need to do something elsewhere to add a right wall to others...

                // union the two sets
                rightAdjacent.SetSetVal(current.GetSetValue());
            }
            else
            {
                // create a wall if you want...
                if (Random.value < rightWallProb)
                {
                    // create a wall
                    Vector3Int coords = MazeToGridCoords(current.GetCoords());
                    PlaceRightWall(coords.x, coords.y);
                }
            }
        }

        // place the rightmost wall
        gridCoords = MazeToGridCoords(row[row.Count - 1].GetCoords());
        PlaceRightWall(gridCoords.x, gridCoords.y);

        // places the bottom walls
        foreach (MazeCell current in row)
        {
            if (current.GetBottomWallStatus())
            {
                Vector3Int coords = MazeToGridCoords(current.GetCoords());
                if (doDebug) Debug.Log("Placing bottom wall @ " + current.GetCoords());
                PlaceBottomWall(coords.x, coords.y);
            }
        }
    }

    #endregion

    #region HelperFunctions


    private Vector3Int MazeToGridCoords(Vector3Int coords)
    {
        return coords * _mSpawner.mazeCellSize + Vector3Int.FloorToInt(_mSpawner.origin.transform.position);
    }

    /*
     * Places a right wall at the cell at grid coordinates x, y. 
     * TODO: make this take maze coords? looks a little more readable.
     */
    private void PlaceRightWall(int x, int y)
    {
        for (int i = 0; i < _mSpawner.mazeCellSize; i++) // if there's overlap, try mazeCellSize-1
        {
            Vector3Int pos = new Vector3Int(x + _mSpawner.mazeCellSize, y+i);
            tilemap.SetTile(pos, wallTiles[Random.Range(0,wallTiles.Length)]);
        }
    }

    /*
     * Places a bottom wall at the cell at grid coordinates x, y. 
     * TODO: make this take maze coords? looks a little more readable.
     */
    private void PlaceBottomWall(int x, int y)
    {
        for (int i = 0; i < _mSpawner.mazeCellSize + 1; i++)
        {
            Vector3Int pos = new Vector3Int(x + i, y);
            tilemap.SetTile(pos, wallTiles[Random.Range(0, wallTiles.Length)]);
        }
    }


    /*
     * Separates a list of MazeCells into a list of lists of MazeCells, where
     * each List contains only Cells of one set.
     */
    private List<List<MazeCell>> SeparateIntoSets(List<MazeCell> row)
    {
        List<List<MazeCell>> sets = new List<List<MazeCell>>();
        List<MazeCell> currentSet = new List<MazeCell>();

        sets.Add(currentSet);
        currentSet.Add(row[0]);
        int lastSetID = currentSet[0].GetSetValue();

        for (int i = 1; i < row.Count; i++)
        {
            if (row[i].GetSetValue() != lastSetID)
            {
                // create a new set and add this element to it
                currentSet = new List<MazeCell>();
                sets.Add(currentSet);
                currentSet.Add(row[i]);
                lastSetID = currentSet[0].GetSetValue();
            }
            else
            {
                // add this element to the current set
                currentSet.Add(row[i]);
            }
        }

        return sets;
    }

    public bool isDoneGenerating()
    {
        return doneGenerating;
    }

    private List<List<MazeCell>> ShuffleSets(List<List<MazeCell>> sets)
    {
        List<List<MazeCell>> shuffled = new List<List<MazeCell>>();
        foreach (List<MazeCell> set in sets)
        {
            shuffled.Add(set.OrderBy(a => Random.Range(0, 101)).ToList()); // maybe replace w/ rng.Next()...
        }
        return shuffled;
    }


    // here in case the above function doesn't work w/ Random.Range...
    // private static System.Random rng = new System.Random();

    #endregion
}
