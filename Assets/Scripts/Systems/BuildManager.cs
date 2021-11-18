using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public static BuildManager instance;
    private StructureBlueprint structureBlueprint = null; //Structure is going to be built
    [SerializeField]
    private Structure selectedStructure; //Already Built structure 
    [SerializeField]
    private CellInfo selectedCell;
    [SerializeField]
    private UIController uIController;


    public bool canBuild;//Checks if a structure is selected to be built

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool CheckIfCanBuild(RaycastHit hit, out Vector3 intPos, out Vector3 sizeOffset)
    {
        //Position where structure will be build
        intPos = new Vector3(
            Mathf.RoundToInt(hit.point.x + (hit.normal.x / 2)),
            Mathf.RoundToInt(hit.point.y + (hit.normal.y / 2)),
            Mathf.RoundToInt(hit.point.z + (hit.normal.z / 2)));

        sizeOffset = Vector3Int.zero;

        if (!CheatManager.instance.infiniteMoney)
        {
            if (LevelStats.instance.CurrentMoney < structureBlueprint.creationCost)
                return false;
        }

        //Spell will always be 
        SpellBehaviour sb;
        if (structureBlueprint.structurePrefab.TryGetComponent<SpellBehaviour>(out sb))
            return true;

        //But for builidngs we need to check if the position is within the world bounds
        if (!LevelManager.instance.world.IsPosInBounds(Vector3Int.FloorToInt(intPos)))
            return false;

        //Position of the block under the block where the structure will be built
        Vector3Int intPosUnder = new Vector3Int(
            Mathf.RoundToInt(hit.point.x - (hit.normal.x / 2)),
            Mathf.RoundToInt(hit.point.y - (hit.normal.y / 2)),
            Mathf.RoundToInt(hit.point.z - (hit.normal.z / 2)));



        selectedCell = LevelManager.instance.world.GetCell(intPosUnder);
        Vector3Int cubeForward = Vector3Int.zero;
        
        if (hit.normal == Vector3Int.forward)
        {
            cubeForward = Vector3Int.right;
        }
        else if (hit.normal == Vector3Int.up)
        {
            cubeForward = Vector3Int.forward;
        }
        else if (hit.normal == Vector3Int.back)
        {
            cubeForward = Vector3Int.up;
        }
        else if (hit.normal == Vector3Int.down)
        {
            cubeForward = Vector3Int.right;
        }
        else if(hit.normal == Vector3Int.right)
        {
            cubeForward = Vector3Int.up;
        }
        else if (hit.normal == Vector3Int.left)
        {
            cubeForward = Vector3Int.forward;
        }


        Vector3Int cubeDotProduct = Vector3Int.FloorToInt(Vector3.Cross(hit.normal, cubeForward));

        int structureSize = structureBlueprint.structurePrefab.GetComponentInChildren<Structure>().Width;

        for (int i = 0; i < structureSize; i++)
        {
            for (int j = 0; j <structureSize; j++)
            {
 
                Vector3 sizeChecker = intPosUnder + (cubeForward * i) + (cubeDotProduct * j) ;
                Vector3 OnTopofSizeChecker = intPos + (cubeForward * i) + (cubeDotProduct * j) ;
                if (!LevelManager.instance.world.CheckCell(sizeChecker, structureBlueprint.structurePrefab.GetComponentInChildren<Structure>().blockType, OnTopofSizeChecker))
                    return false;
            }
        }

        if (structureSize > 1)
        {
            sizeOffset =(hit.normal + cubeForward + cubeDotProduct) / structureSize;
            Debug.Log("SO:"+sizeOffset);
        }

        if (!canBuild /*|| selectedCell.blockType != structureBlueprint.structurePrefab.GetComponentInChildren<Structure>().blockType*/)
            return false;

        Gatherer g;
        if (WorldManager.instance.IsPosInBounds((int)intPos.x, (int)intPos.y, (int)intPos.z) &&
            !LevelManager.instance.world.GetCell((int)intPos.x, (int)intPos.y, (int)intPos.z).isCloseToPath &&
            structureBlueprint.structurePrefab.TryGetComponent<Gatherer>(out g))
        {
            //Debug.Log("Can't add");
            return false;
        }

        //if (g != null)
        //LevelManager.instance.world.AddInterestPoint(intPos);


        return true;
    }

    public void PlaceObject(RaycastHit hit)
    {
        Vector3 pos;
        Vector3 offset;
        if (CheckIfCanBuild(hit, out pos, out offset))
        {
            pos += offset;
            Debug.Log("Pos:"+pos);
            BuildStructure(pos, hit.normal);
        }
    }

    internal void SetSelectedStructure(Structure structure)
    {
        selectedStructure = structure;
        structureBlueprint = selectedStructure.Blueprint;
        selectedCell = null;

    }

    public void SelectCell(CellInfo cell)
    {
        selectedCell = cell;
        selectedStructure = null;
        structureBlueprint = null;
    }

    public void SelectStructureToBuild(StructureBlueprint defense)
    {
        structureBlueprint = defense;
        selectedCell = null;
        canBuild = true;
    }

    public void ResetCanBuild()
    {
        canBuild = false;
    }

    public void BuildStructure(Vector3 position, Vector3 normal)
    {
        if (CheatManager.instance.infiniteMoney)
        {
            CreateTowerOnCell(position, normal);
            ResetCanBuild(); // after building an structure you have to select another one to be able to place it
        }
        else if (LevelStats.instance.CurrentMoney >= structureBlueprint.creationCost)
        {
            CreateTowerOnCell(position, normal);
            LevelStats.instance.SpendMoney(structureBlueprint.creationCost);
            ResetCanBuild(); // after building an structure you have to select another one to be able to place it
        }
        else
        {
            Debug.Log("Not enough Money");
            //TODO: Show on screen there is not enough money
        }
    }

    public void CreateTowerOnCell(Vector3 position, Vector3 normal)
    {
        GameObject structureGO = Instantiate(structureBlueprint.structurePrefab, position, Quaternion.Euler(normal));
        Structure structure = structureGO.GetComponentInChildren<Structure>();
        structure.gameObject.transform.localScale *= structure.Width;
        structure.SetNormal(normal);
        structure.Blueprint = structureBlueprint;

        //THIS SHOULDN'T BE NECESSARY
        //if (!LevelManager.instance.world.IsPosInBounds(position))
        //    return;

        //CellInfo cell = LevelManager.instance.world.GetCell(position);//Sometimes FAILS :(

        //If we are putting a bomb, apart from creating the model, we set the cell's structure associated in which we are creating it
        Bomb b;
        CellInfo cell = LevelManager.instance.world.GetCell((int)position.x, (int)position.y, (int)position.z);
        if (structure.TryGetComponent<Bomb>(out b))
        {
            if (cell.GetStructure() == null)
            {
                cell.SetStructure(b);
            }
        }

        cell.SetStructure(structure);
    }

    public void UpgradeStructure()
    {
        if (selectedStructure.GetLevel() < 3)
        {
            if (CheatManager.instance.infiniteMoney)
            {
                selectedCell.structure.UpgradeStrucrure(uIController);
            }
            else if ( LevelStats.instance.CurrentMoney >= structureBlueprint.upgrades[selectedStructure.GetLevel()].cost)
            {
                selectedStructure.UpgradeStrucrure(uIController);
                LevelStats.instance.SpendMoney(structureBlueprint.creationCost);

            }
            else
            {
                Debug.Log("Not enough Money");
                //TODO: Show on screen there is not enough money
            }
        }
    }

    public void SellStructure()
    {
        Debug.Log("Selling: " + selectedStructure.name);
        UIController.instance.ShowMenu(UIController.GameMenu.Game);
        selectedStructure.Sell();
        //selectedCell.structure = null;
        LevelStats.instance.EarnMoney(50);
    }
}
