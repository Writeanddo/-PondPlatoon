using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/*This manager inicialice the game */
//TODO: SPLIT MAP GENERATION AND PATH GENERATION INTO COMPONENTS 
public class GameManager : MonoBehaviour
{
    public int size = 20;
    CellInfo[,] cells; //0 walkable //1 can build //2 can't build //3 target
    GameObject[,] floor;
    Path[] paths;
    public int nPaths = 4;
    [Range(0.0f, 1.0f)]
    public float wallDensity = 0.3f;
    public float rockSize = 3f;
    public float seed = 0f;

    public GameObject floorPrefab;
    public GameObject weaponPrefab;
    public GameObject enemyPrefab;
    public Material[] materials;

    public Text text;
    // Start is called before the first frame update
    void Start()
    {
        if (seed == 0f)
            seed = Random.value * 10;
        Debug.Log("Seed: " + seed.ToString());

        int endX = size - 1;
        int endY = size - 1;

        cells = new CellInfo[size, size];
        floor = new GameObject[size, size];

        float diagonal = Mathf.Sqrt(2 * (size * size));
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                GameObject cell = GameObject.Instantiate(floorPrefab, new Vector3(i, j, -0.5f * Mathf.PerlinNoise(seed + (i / 3f), seed + (j / 3f))), Quaternion.identity);
                cell.GetComponent<CellId>().x = i;
                cell.GetComponent<CellId>().y = j;

                floor[i, j] = cell;
                cell.transform.parent = transform;
                cells[i, j] = new CellInfo(i, j);
                floor[i, j].name = "Floor_" + cells[i, j].id.ToString();

                //Rock generation
                float alpha = 1;
                float dist = Mathf.Sqrt(size * size + size * size) - Mathf.Sqrt(Mathf.Pow(endX - i, 2f) + Mathf.Pow(endY - j, 2f));
                if (dist > diagonal * 0.9f || i == 0 || j == 0)//|| diagonal - dist > size
                    alpha = 0;

                if (Mathf.PerlinNoise(seed + (i / rockSize), seed + (j / rockSize)) > (1 - (wallDensity * alpha)))//i == 0 || j == 0 || i == size - 1 || j == size - 1 ||//|| (i == j && i < size - 1)
                {
                    cells[i, j].state = 2;
                    floor[i, j].transform.position = new Vector3(i, j, -1);
                    cell.GetComponent<MeshRenderer>().material = materials[2];
                }
                else
                {
                    cells[i, j].state = 1;//Should use perlin noise?
                    cell.GetComponent<MeshRenderer>().material = materials[1];

                }
            }
        }

        paths = new Path[nPaths];
        floor[endX, endY].transform.Translate(-Vector3.forward);

        for (int i = 0; i < nPaths; i++)
        {
            int x = 0;
            int y = 0;

            int count = 0;
            while ((cells[x, y].state == 0 || cells[x, y].state == 2) && count <100)
            {
                if (i < nPaths / 2)
                {
                    x = Random.Range(1, size - 1);
                }
                else
                {
                    y = Random.Range(1, size - 1);
                }
                count++;
            }

            cells[x, y].state = 0;
            floor[x, y].GetComponent<MeshRenderer>().material = materials[0];
            floor[x, y].transform.position = new Vector3(x, y, 0);

            Node p = GeneratePaths(nPaths, cells[x, y], cells[endX, endY]);
            if (p != null)
            {
                List<CellInfo> pathCells = new List<CellInfo>();
                while (p != null)
                {
                    cells[p.x, p.y].state = 0;
                    floor[p.x, p.y].GetComponent<MeshRenderer>().material = materials[0];
                    floor[p.x, p.y].transform.position = new Vector3(p.x, p.y, 0);
                    pathCells.Add(cells[p.x, p.y]);
                    //floor[p.x, p.y].transform.Translate(-Vector3.forward * 0.1f);
                    p = p.Parent;
                }
                pathCells.Reverse();
                paths[i] = new Path(pathCells.ToArray());
            }
        }
    }

    Node GeneratePaths(int nPaths, CellInfo start, CellInfo end)
    {
        Node current;
        Node firstNodo;

        List<Node> openList = new List<Node>();
        List<Node> closedList = new List<Node>();

        firstNodo = new Node(start);

        //Primer nodo la posici�n incial con padre null
        firstNodo.ComputeHScore(end.x, end.y);
        firstNodo.Parent = null;
        openList.Add(firstNodo);



        int count = 0;
        while (openList.Count > 0 && count < 1000)
        {
            count++;
            //Ordenar la lista en orden ascendente de h
            openList = openList.OrderBy(o => o.h).ToList();

            //Mira el primer nodo de la lista
            current = openList[0];
            closedList.Add(current);
            openList.Remove(current);
            //Si el primer nodo es goal, returns current node
            if (current.x == end.x && current.y == end.y)
            {
                Debug.Log("Success: " + count.ToString());
                return current;
            }
            else
            {
                //Expande vecinos (calcula coste de cada uno, etc)y los a�ade en la lista
                CellInfo[] neighbours = WalkableNeighbours(current.cell);
                foreach (CellInfo neighbour in neighbours)
                {
                    if (neighbour != null)
                    {
                        //if neighbour no esta en open
                        bool IsInOpen = false;
                        foreach (Node nf in openList)
                        {
                            if (nf.cell.id == neighbour.id)
                            {
                                IsInOpen = true;
                                break;
                            }
                        }

                        bool IsInClosed = false;
                        foreach (Node nf in closedList)
                        {
                            if (nf.cell.id == neighbour.id)
                            {
                                IsInClosed = true;
                                break;
                            }
                        }

                        if (!IsInOpen && !IsInClosed)
                        {
                            Node n = new Node(neighbour);
                            n.ComputeHScore(end.x, end.y);
                            n.Parent = current;
                            n.cell = cells[n.x, n.y];

                            if (true)//n.h < current.h
                            {
                                openList.Add(n);
                                //floor[n.x, n.y].transform.position = new Vector3(n.x, n.y,-count/200f);
                                //floor[n.x, n.y].transform.Translate(-Vector3.forward);
                            }
                        }
                    }
                }
            }
        }
        Debug.Log("Fail: " + count.ToString());
        return null;
    }

    private CellInfo[] WalkableNeighbours(CellInfo current)
    {
        List<CellInfo> result = new List<CellInfo>();

        int[,] neighbours = new int[,] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } 
                                        ,{ -1, -1 }, { -1, 1 }, { 1, 1 }, { 1, -1 }};

        for (int i = 0; i < 4; i++)
        {
            int x = current.x + neighbours[i, 0];
            int y = current.y + neighbours[i, 1];
            if (x >= 0 && x < size && y >= 0 && y < size && (cells[x, y].state == 1 || cells[x, y].state == 0))
            {
                result.Add(cells[x, y]);
            }
        }


        return result.ToArray();
    }

    private void Update()
    {
        //transform.Rotate(Vector3.forward,Time.deltaTime*10);
        text.text = (1 / Time.deltaTime).ToString();

        if (Input.GetMouseButtonDown(0))
        {
            SpawnWeapon();
        }

        for (int i = 0; i < nPaths; i++)
        {
            if (paths[i] != null && paths[i].CheckSpawn())
            {
                GameObject.Instantiate(enemyPrefab, paths[i].GetStep(0), Quaternion.identity).GetComponent<EnemyBehaviour>().SetPath(paths[i]);
            }
        }
    }

    private void SpawnWeapon()
    {
        GameObject obj = CastRay();

        if (obj != null)
        {
            CellId cell;
            if (obj.TryGetComponent<CellId>(out cell))
            {
                if (cells[cell.x, cell.y].state == 1)
                {
                    GameObject.Instantiate(weaponPrefab, obj.transform.position - Vector3.forward, Quaternion.identity);
                    cells[cell.x, cell.y].state = 2;
                }
            }
        }
    }

    GameObject CastRay()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = Physics.RaycastAll(ray.origin, ray.direction, Mathf.Infinity)[0];// .Raycast(ray.origin, ray.direction, Mathf.Infinity);
        if (hit.collider != null)
        {
            return hit.collider.gameObject;
        }
        return null;
    }
}







