using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class LevelSelector : MonoBehaviour
{
    public static LevelSelector instance;

    public int selectedWorld = 0;
    internal bool changing = false;

    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;
    public CubeWorldGenerator[] worlds;

    [Header("Enemies waves setup")]
    public Wave[][] enemies;

    public Button previousButton;
    public Button nextButton;
    public Button selectButton;
    public TMP_Text levelText;
    private int levelNum;
    public List<int> worldScores;
    public GameManager gameManager;
    public Image stars;
    public List<Sprite> starsSprites;
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

    private void Start()
    {
        worldScores = GameManager.instance.playerData.worldScores;

        ThemeInfo startTheme = worlds[0].GetComponent<ThemeSelector>().GetThemeInfo();
        RenderSettings.skybox.SetColor("_Tint", startTheme.backGroundColor);
        FindObjectOfType<Light>().color = startTheme.lightColor;

        if (!GameManager.instance.initiated)
            CreateWorldList();
        levelNum = 1;
        stars.sprite = starsSprites[selectedWorld];
        //levelText.text = "Nivel " + (levelNum);
        GoTo(selectedWorld);
        DoneChanging();
    }
    public void SelectLevel(int levelId)
    {
        SceneController.instance.LoadScene(levelId);
    }

    public void NextWorld()
    {
        if (!changing && selectedWorld < worlds.Length - 1)
        {
            changing = true;
            levelNum++;
            levelText.text = (selectedWorld + 2).ToString();
            GoTo(selectedWorld + 1);
            stars.sprite = starsSprites[gameManager.playerData.worldScores[levelNum - 1]];
        }
    }

    public void PreviousWorld()
    {
        if (!changing && selectedWorld > 0)
        {
            changing = true;
            levelNum--;
            levelText.text = selectedWorld.ToString();
            GoTo(selectedWorld - 1);
            stars.sprite = starsSprites[gameManager.playerData.worldScores[levelNum - 1]];
        }
    }

    public void CreateWorldList()
    {
        for (int i = 0; i < worlds.Length; i++)
        {
            //insert the level world settings in the list containing the different levels
            WorldInfo worldInfo = new WorldInfo();
            worldInfo.nPaths = worlds[i].nPaths;
            worldInfo.canMergePaths = worlds[i].canMergePaths;
            worldInfo.wallDensity = worlds[i].wallDensity;
            worldInfo.rocksVisualReduction = worlds[i].rocksVisualReduction;
            worldInfo.waterDensity = worlds[i].waterDensity;
            worldInfo.rockSize = worlds[i].rockSize;
            worldInfo.numberOfMidpoints = worlds[i].numberOfMidpoints;
            worldInfo.themeInfo = worlds[i].GetComponent<ThemeSelector>().themeInfo;
            worldInfo.waves = worlds[i].GetComponent<WaveInfo>().waves;
            GameManager.instance.worldList.Add(worldInfo);
        }
        GameManager.instance.initiated = true;
    }

    public void SelectWorld()
    {
        //Loads game scene with selected world
        GameManager.instance.currentWorldId = selectedWorld;
        ThemeInfo theme = GameManager.instance.GetCurrentWorld().themeInfo;
        SceneController.instance.fadeColor = theme.backGroundColor;
        SceneController.instance.LoadScene(1);
    }

    public void switchBetweenPanels(int panelId)
    {

        switch (panelId)
        {
            case 0:
                levelSelectPanel.SetActive(false);
                mainMenuPanel.SetActive(true);
                //MainMenuCamera.instance.MoveRight(); 
                break;
            case 1:
                mainMenuPanel.SetActive(false);
                levelSelectPanel.SetActive(true);
                //MainMenuCamera.instance.MoveLeft();
                gameManager.LoadData();
                gameManager.levelSelector.stars.sprite = gameManager.levelSelector.starsSprites[gameManager.playerData.worldScores[0]];
                break;
            default:
                break;
        }
    }

    public void GoTo(int nextIdx)
    {
        changing = true;
        nextButton.interactable = false;
        previousButton.interactable = false;
        selectButton.interactable = false;
        StartCoroutine(GoToCube(nextIdx));
    }

    void DoneChanging()
    {
        if (selectedWorld < worlds.Length - 1)
            nextButton.interactable = true;

        if (selectedWorld > 0)
            previousButton.interactable = true;

        if (selectedWorld == 0 || (selectedWorld > 0 && gameManager.playerData.worldScores[selectedWorld - 1] > 0))
            selectButton.interactable = true;
    }

    IEnumerator GoToCube(int nextIdx)
    {
        GameObject cameraObj = MainMenuCamera.instance.gameObject;
        MainMenuCamera camera = MainMenuCamera.instance;

        Light light = FindObjectOfType<Light>();

        Color lightColor;
        Color backGroundColor; ;

        ThemeInfo theme1 = worlds[nextIdx].GetComponent<ThemeSelector>().GetThemeInfo();
        ThemeInfo theme2 = worlds[selectedWorld].GetComponent<ThemeSelector>().GetThemeInfo();

        float waitTime = 1f;
        float doneTime = Time.time + waitTime;
        float delta;
        Vector3 position;

        while (Time.time < doneTime)
        {
            delta = ((doneTime - Time.time) / waitTime);
            position = Vector3.Lerp(worlds[nextIdx].center.position, worlds[selectedWorld].center.position, delta);

            cameraObj.transform.position = camera.offset + position;
            cameraObj.transform.LookAt(position + (Vector3.right * camera.offset.x), Vector3.up);

            lightColor = Color.Lerp(theme1.lightColor, theme2.lightColor, delta);
            backGroundColor = Color.Lerp(theme1.backGroundColor, theme2.backGroundColor, delta);

            light.color = lightColor;
            RenderSettings.skybox.SetColor("_Tint", backGroundColor);
            yield return null;
        }
        camera.idx = nextIdx;
        selectedWorld = nextIdx;
        changing = false;
        DoneChanging();
        yield return null;
    }

}
