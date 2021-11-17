using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;
    bool choosingWhereToBuild = false; //A structure card has been selected
    bool zooming = false;//Is zooming

    GameObject selectedCard;

    [SerializeField]
    private float mouseSensitivity = 3.0f;
    [SerializeField]
    private float scrollSensitivity = 15.0f;
    [SerializeField]
    private float pinchSensitivity = 15.0f;
    //Gameobject that will be placed where structure is about to be built
    public GameObject cursor;
    private GameObject cursorBase;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        //If no cursor is assigned, a cube will be created and used
        if (cursor == null)
        {
            cursor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cursor.GetComponent<Collider>());
        }
        else
        {
            cursor = GameObject.Instantiate(cursor);
            cursor.SetActive(false);
        }

        cursorBase = cursor.transform.GetChild(0).gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        //Zoom
        CheckPinch();
        CameraBehaviour.instance.Zoom(Input.mouseScrollDelta.y * scrollSensitivity); //Zoom with mouse wheel

        //Click
        if (Input.GetMouseButtonDown(0))
        {
            MouseDown();
        }

        //Click release
        if (Input.GetMouseButtonUp(0))
        {
            MouseUp();
        }

        //Drag
        if (Input.GetMouseButton(0))
        {
            MouseDrag();
        }


    }

    private void MouseDrag()
    {
        if (choosingWhereToBuild)
        {
            //Casts a ray to find out where does the player want to place the structure
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                if (hit.collider.tag == "World")//If mouse is over world
                {
                    //If can't build on selected cell, cursor turns red
                    Vector3Int pos;
                    if (BuildManager.instance.CheckIfCanBuild(hit, out pos))
                    {
                        cursor.GetComponent<MeshRenderer>().material.color = Color.white;
                    }
                    else
                    {
                        cursor.GetComponent<MeshRenderer>().material.color = Color.red;
                    }

                    //Cursor activates and moves to selected cell
                    cursor.SetActive(true);
                    cursor.transform.position = pos;
                    cursor.transform.up = hit.normal;
                }
                //Card is hidden if poiting at anything in the world
                selectedCard.SetActive(false);
            }
            else
            {
                //If mouse isn't over the world, cursor is hidden and card is shown again
                cursor.SetActive(false);
                selectedCard.SetActive(true);
                //Card is moved with mouse
                selectedCard.transform.position = GetMouseAsWorldPoint() + mOffset;
            }
        }
        else if (!zooming)
        {
            //If not zooming, camera will be moved
            CameraBehaviour.instance.Rotate(Input.GetAxis("Mouse X") * mouseSensitivity, Input.GetAxis("Mouse Y") * mouseSensitivity);
        }
    }

    //https://www.patreon.com/posts/unity-3d-drag-22917454
    private Vector3 mOffset;
    private float mZCoord;
    private Vector3 defaultPos;
    private Vector3 worldPos;

    private void MouseDown()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = new RaycastHit();

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            switch (hit.collider.tag)
            {
                case "Card":
                    choosingWhereToBuild = true;

                    //Select clicked card
                    selectedCard = hit.collider.gameObject;
                    selectedCard.GetComponent<Collider>().enabled = false;

                    //Select structure
                    Shop.instance.setShopIndex(selectedCard.GetComponent<Card>().index);

                    //Getting offset between camera and card
                    defaultPos = selectedCard.transform.localPosition;
                    worldPos = selectedCard.transform.position;
                    mZCoord = Camera.main.WorldToScreenPoint(worldPos).z;
                    mOffset = worldPos - GetMouseAsWorldPoint();

                    DefenseBehaviour db;
                    if (Shop.instance.selectedDefenseBlueprint.structurePrefab.TryGetComponent<DefenseBehaviour>(out db))
                        cursorBase.transform.localScale = new Vector3(2 * db.attackRange, 2 * db.attackRange, 1);

                    break;
                case "Structure":
                    GameObject structureHitted = hit.collider.gameObject;
                    //Interact with existing defenses
                    UIController.instance.ShowMenu(UIController.GameMenu.UpgradeMenu);

                    //check the structure type
                    if (structureHitted.GetComponent<ShootingDefenseBehaviour>() != null)
                    {
                        if (structureHitted.GetComponent<ShootingDefenseBehaviour>().GetEffect() == 1)
                        {
                            UIController.instance.SetUpgradeMenu(1, structureHitted.GetComponent<ShootingDefenseBehaviour>().GetStructureName(), structureHitted.GetComponent<ShootingDefenseBehaviour>().GetLevel(),
                                structureHitted.GetComponent<ShootingDefenseBehaviour>().GetTarget(), structureHitted.GetComponent<ShootingDefenseBehaviour>().GetRange(), structureHitted.GetComponent<ShootingDefenseBehaviour>().GetFireRate(),
                                structureHitted.GetComponent<ShootingDefenseBehaviour>().GetDamage(), 0);
                        }
                        else
                        {
                            UIController.instance.SetUpgradeMenu(0, structureHitted.GetComponent<ShootingDefenseBehaviour>().GetStructureName(), structureHitted.GetComponent<ShootingDefenseBehaviour>().GetLevel(),
                                structureHitted.GetComponent<ShootingDefenseBehaviour>().GetTarget(), structureHitted.GetComponent<ShootingDefenseBehaviour>().GetRange(), structureHitted.GetComponent<ShootingDefenseBehaviour>().GetFireRate(),
                                structureHitted.GetComponent<ShootingDefenseBehaviour>().GetDamage(), 0);
                        }
                    }
                    else if (structureHitted.GetComponent<AOEDefenseBehaviour>() != null)
                    {
                        UIController.instance.SetUpgradeMenu(2, structureHitted.GetComponent<AOEDefenseBehaviour>().GetStructureName(), structureHitted.GetComponent<AOEDefenseBehaviour>().GetLevel(), structureHitted.GetComponent<AOEDefenseBehaviour>().GetTarget(),
                                structureHitted.GetComponent<AOEDefenseBehaviour>().GetRange(), structureHitted.GetComponent<AOEDefenseBehaviour>().GetFireRate(), structureHitted.GetComponent<AOEDefenseBehaviour>().GetDamage(), 0);
                    }
                    else if (structureHitted.GetComponent<Bomb>() != null)
                    {
                        UIController.instance.SetUpgradeMenu(3, structureHitted.GetComponent<Bomb>().GetStructureName(), structureHitted.GetComponent<Bomb>().GetLevel(), structureHitted.GetComponent<Bomb>().GetTarget(),
                                structureHitted.GetComponent<Bomb>().GetRange(), structureHitted.GetComponent<Bomb>().GetFireRate(), structureHitted.GetComponent<Bomb>().GetDamage(), 0);
                    }
                    BuildManager.instance.SetSelectedStructure(structureHitted.GetComponent<Structure>());
                    break;

                case "Gatherer":
                    GameObject gathererHitted = hit.collider.gameObject;
                    UIController.instance.ShowMenu(UIController.GameMenu.UpgradeMenu);
                    UIController.instance.SetUpgradeMenu(4, gathererHitted.GetComponent<MoneyGatherer>().GetStructureName(), gathererHitted.GetComponent<MoneyGatherer>().GetLevel(), "", "", "", "",
                        gathererHitted.GetComponent<MoneyGatherer>().GetResourceGatheredAmount());
                    break;
                default:
                    break;
            }
        }
    }

    private void MouseUp()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = new RaycastHit();

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            switch (hit.collider.tag)
            {
                case "World":
                    //If mouse released over world while choosing where to build, the structure will be built if possible
                    if (choosingWhereToBuild)
                    {
                        BuildManager.instance.PlaceObject(hit);
                    }
                    break;
                default:
                    break;
            }
        }

        if (choosingWhereToBuild)
        {
            //Card gets released, so everything resets
            selectedCard.GetComponent<Collider>().enabled = true;
            selectedCard.SetActive(true);
            selectedCard.transform.localPosition = defaultPos;
            selectedCard = null;
            choosingWhereToBuild = false;
            cursor.SetActive(false);
        }
    }

    bool CheckPinch()
    {
        if (choosingWhereToBuild)
            return false;

        int activeTouches = Input.touchCount;

        if (activeTouches < 2)//If less than two touches, can't zoom
        {
            zooming = false;
            return false;
        }

        zooming = true;

        Vector2 touch0 = Input.GetTouch(0).position;
        Vector2 touch1 = Input.GetTouch(1).position;
        //Deltas are the position change since las frame
        Vector2 delta0 = Input.GetTouch(0).deltaPosition;
        Vector2 delta1 = Input.GetTouch(1).deltaPosition;

        if (Vector2.Dot(delta0, delta1) < 0)//If deltas form an angle greater than 90 degrees
        {
            float currentDist = Vector2.Distance(touch0, touch1);
            float previousDist = Vector2.Distance(touch0 - delta0, touch1 - delta1);
            float difference = previousDist - currentDist;

            if (difference < 50)
            {
                CameraBehaviour.instance.Zoom((difference) * Time.deltaTime * pinchSensitivity);
            }
        }

        return true;
    }

    private Vector3 GetMouseAsWorldPoint()
    {
        // Pixel coordinates of mouse (x,y)
        Vector3 mousePoint = Input.mousePosition;

        // z coordinate of game object on screen
        mousePoint.z = mZCoord;

        // Convert it to world points
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}
//https://answers.unity.com/questions/1698508/detect-mobile-client-in-webgl.html?childToView=1698985#answer-1698985
