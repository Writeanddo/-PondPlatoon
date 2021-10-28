//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


//TODO: End wave system
[RequireComponent(typeof(EnemySpawner))]
public class WaveController : MonoBehaviour
{
    public static WaveController instance;

    public int activeEnemies = 0;
    public List<EnemyBehaviour> enemies;


    [Range(0f, 0.5f)]
    public float randomRange = 0.25f;
    public Wave[] waves;

    public float timeBetweenWaves = 5f;
    public float timeBeforeRoundStarts = 3f;
    public float timeVariable;

    public bool isGameOver = false;
    public bool isWaveActive;
    public bool isBetweenWaves;
    public bool allWavesCleared;

    public int waveCount; // Wave its being played

 
    ///public Text waveText;

    EnemySpawner enemySpawner;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
        enemySpawner = GetComponent<EnemySpawner>();
        enemies = new List<EnemyBehaviour>();
    }

    public void Start()
    {
        isWaveActive = false;
        isBetweenWaves = true;

        waveCount = 0;

        timeVariable = Time.time + (.5f * timeBeforeRoundStarts);
        LevelManager.instance.OnGameLost += StopWave;
        LevelManager.instance.OnGameLost += LevelCompleted;

    }

    private void LevelCompleted()
    {
        if (LevelStats.instance.CurrentBaseHealthPoints > 0)
        {
            allWavesCleared = true;
        }
    }

    void Update()
    {
        if (isGameOver)
        {
            isBetweenWaves = false;
            isWaveActive = false;
            allWavesCleared = false;
            UIController.instance.EnableEndgameMenu();
        }
        if (allWavesCleared)
        {
            isBetweenWaves = false;
            isWaveActive = false;
            UIController.instance.EnableEndgameMenu();

        }
        else if (isBetweenWaves)
        {
            if (Time.time >= timeVariable)
            {
                isBetweenWaves = false;
                isWaveActive = true;
                StartCoroutine("SpawnWave");
                return;
            }
        }
        else if (isWaveActive)
        {
            if (activeEnemies <= 0)
            {
                isBetweenWaves = true;
                isWaveActive = false;

                timeVariable = Time.time + timeBetweenWaves;
                waveCount++;
            }
        }

    }

    public void AddToActiveEnemies(EnemyBehaviour enemy)
    {
        activeEnemies++;
        enemies.Add(enemy);
    }

    public void ReduceActiveEnemies(EnemyBehaviour enemy)
    {
        activeEnemies--;
        enemies.Remove(enemy);
    }

    IEnumerator SpawnWave()
    {
        Wave currentWave = new Wave();
        if (waveCount >= waves.Length)
        {
            LevelManager.instance.levelCompleted();
        }
        else
        {
            currentWave = waves[waveCount];
        }


        for (int i = 0; i < currentWave.packs.Length; i++)
        {
            Pack p = currentWave.packs[i];
            for (int j = 0; j < p.enemyAmount; j++)
            {
                int pathId = Random.Range(0, CubeWorldGenerator.instance.nPaths);
                enemySpawner.SpawnEnemy(p.enemyType, CubeWorldGenerator.instance.paths[pathId]);
                yield return new WaitForSeconds((1f / currentWave.spawnRate) + Random.Range(0f, randomRange)); //randomness between 
            }
        }


    }

    void StopWave()
    {
        StopCoroutine("SpawnWave");
        isGameOver = true;
    }
}
