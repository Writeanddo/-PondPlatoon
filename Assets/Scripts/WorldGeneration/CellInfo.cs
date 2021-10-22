using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CellInfo
{
    public int x, y, z;
    public int id { get { return x + (1000 * y) + (1000000 * z); } }
    public BlockType blockType = BlockType.Air;
    public bool explored = false;
    public bool isPath = false;
    public Vector3Int normalInt = Vector3Int.zero;
    internal bool isSurface = false;
    internal bool canWalk = false;
    internal bool endZone = false;

    public CellInfo(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}