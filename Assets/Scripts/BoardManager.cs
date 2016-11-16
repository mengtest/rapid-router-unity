﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Road;
using Zenject;
using DG.Tweening;

public enum Direction : int
{
    North = 0,
    East = -90,
    South = -180,
    West = -270
};

public class Level
{
    public PathNode[] path;
    public OriginNode origin;

    public int[][] destinations;
    
    public Coordinate[] destinationCoords
    {
        get
        {
            Coordinate[] coordinates = new Coordinate[destinations.Length];
            for (int i = 0; i < destinations.Length; i++)
            {
                int[] dest = destinations[i];
                coordinates[i] = new Coordinate(dest[0], dest[1]);
            }
            return coordinates;
        }
    }
}

public class BoardManager : MonoBehaviour, IInitializable
{
    public float rows;
    public float columns;

    public static Level currentLevel;

    [Inject]
	Installer.Settings.FloorTiles floorTiles;
	[Inject]
	Installer.Settings.RoadTiles roadTiles;

    [Inject]
    RoadDrawer roadDrawer;

    private static Transform boardHolder;
    private List<Vector3> gridPositions = new List<Vector3>();

    [Inject]
    BoardTranslator translator;

    [Inject]
    Installer.Settings.MapSettings mapDimensions;

    [PostInject]
    public void Initialize() {
        gridPositions = new List<Vector3>();
        rows = mapDimensions.rows;
        columns = mapDimensions.columns;
    }

	public Transform GetBoardHolder() {
		return boardHolder;
	}

	public void SetupScene(int level)
	{
		InitialiseList();
		GameObject currentBoard = GameObject.Find ("Board");
		if (currentBoard != null) {
			GameObject.DestroyObject (currentBoard);
		}
		boardHolder = new GameObject("Board").transform;
		SetupLevel(level);
	}

    void InitialiseList()
    {
        gridPositions.Clear();
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                gridPositions.Add(new Vector3(x, y, 0f));
            }
        }
    }

    private void SetupLevel(int levelNumber)
    {
        currentLevel = LevelReader.ReadLevelFromFile(levelNumber);

        SetupBoard();
		SetupRoute ();
		SetupVan ();
    }

	private void SetupBoard()
	{
		for (int x = 0; x < columns; x++)
		{
			for (int y = 0; y < rows; y++)
			{
				SetStaticWithBoardAsParent(
					Instantiate(floorTiles.grassTile, 
								new Vector3(translator.translateRow(x), translator.translateColumn(y), 0f),
								Quaternion.identity) as GameObject);
			}
		}
	}

	private void SetupRoute() {
		GameObject cfcOrigin = SetupOrigin (currentLevel.origin);
		GameObject[] roadObjects = roadDrawer.SetupRoadSegments(currentLevel.path);
		GameObject homeDestination = SetupDestinations(currentLevel.destinationCoords);

		List<GameObject> roadObjectsList = new List<GameObject>(roadObjects);
		roadObjectsList.Add (cfcOrigin);
		roadObjectsList.Add (homeDestination);
		foreach (GameObject roadObject in roadObjectsList) {
			SetStaticWithBoardAsParent (roadObject);
		}
	}

	private GameObject SetupOrigin(OriginNode origin)
	{
		Direction direction = RoadDrawer.StringToDirection(origin.direction);
		Coordinate coords = origin.coords;
		return Instantiate(roadTiles.cfcTile, new Vector3(translator.translateRow(coords.x), translator.translateColumn(coords.y), 0f),
			Quaternion.Euler(0, 0, (float)direction)) as GameObject;
	}

	private GameObject SetupDestinations(Coordinate[] destinationNodes)
	{
		return new GameObject ();
	}
		
	private void SetupVan() 
	{
		GameObject van = GameObject.Find ("Van");
		van.transform.position = translator.translateVector(currentLevel.origin.coords.vector);
		int direction = (int)RoadDrawer.StringToDirection(currentLevel.origin.direction);

		van.transform.rotation = Quaternion.identity;
		van.transform.Rotate(new Vector3(0, 0, direction));
		van.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
		van.transform.position += VehicleMover.ForwardABit(van.transform, 0.5f);
		DOTween.defaultEaseOvershootOrAmplitude = 0;

		BoardManager.SetBoardAsParent (van);
	}

	private static void SetStaticWithBoardAsParent(GameObject childObject) {
		SetStatic (childObject);
		SetBoardAsParent (childObject);
	}

	private static void SetStatic(GameObject staticObject) {
		staticObject.isStatic = true;
	}

	public static void SetBoardAsParent(GameObject childObject) {
		childObject.transform.SetParent(boardHolder);
	}
}