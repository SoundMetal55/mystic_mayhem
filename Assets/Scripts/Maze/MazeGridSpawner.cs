using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGridSpawner : MonoBehaviour
{
    public int width;
    public int height;
    public int mazeCellSize;
    public GameObject origin;

    public Grid grid; // sorry, i know this variable name is terrible. it's temporary though; the MazeGrid class will be phased out p soon.
}
