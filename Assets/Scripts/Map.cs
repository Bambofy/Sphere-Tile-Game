using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;

public class Map : Bolt.EntityBehaviour<IMapState>
{
    private int _randomSeed = 0;

    private Tilemap _groundTileMap;
    private Tilemap _walkablesTileMap;
    private Tilemap _objectsTileMap;
    private Tilemap _playersTileMap;
    private Tilemap _solidsMaskTileMap;

    private FastNoise _noiseFunction;

    private Dictionary<string, Tile> _tiles = new Dictionary<string, Tile>();

    /// <summary>
    /// This is what is written to when we change a tile, whenever we want to save we write to the file whats in this buffer.
    /// </summary>
    private HashSet<string> _changesToSaveBuffer = new HashSet<string>();

    public override void Attached()
    {
        // get references to the tilemaps
        _groundTileMap = GameObject.FindGameObjectWithTag("TILESET_GROUND").GetComponent<Tilemap>();
        _walkablesTileMap = GameObject.FindGameObjectWithTag("TILESET_WALKABLES").GetComponent<Tilemap>();
        _objectsTileMap = GameObject.FindGameObjectWithTag("TILESET_OBJECTS").GetComponent<Tilemap>();
        _playersTileMap = GameObject.FindGameObjectWithTag("TILEMAP_PLAYERS").GetComponent<Tilemap>();
        _solidsMaskTileMap = GameObject.FindGameObjectWithTag("TILEMAP_SOLIDS_MASK").GetComponent<Tilemap>();

        // load tiles from resources folder.
        Tile[] tileFiles = Resources.LoadAll<Tile>("Tiles");
        foreach (Tile tile in tileFiles)
        {
            _tiles.Add(tile.name, tile);
        }

        // if we are the player, set the random seed.
        if (BoltNetwork.IsServer)
        {
            System.Random r = new System.Random(DateTime.Now.Second);

            _randomSeed = r.Next(0, 10000);

            state.Random_Seed = _randomSeed;
        }
        else
        {
            _randomSeed = state.Random_Seed;
        }

        // seed has been loaded.
        // setup our noise function from the seed.
        _noiseFunction = new FastNoise(_randomSeed);
        _noiseFunction.SetFrequency(0.1f);

        Debug.Log("Current working directory: " + Application.persistentDataPath);

        GenerateMap(0, 0);
        bool isMapLoaded = LoadMap();

        SaveMap();
    }

    private void GenerateMap(int pScreenCenterTileX, int pScreenCenterTileY)
    {
        // generate 32 x 32 tiles from the noise funciton.
        for (int x = -32; x < 32; x++)
        {
            for (int y = -32; y < 32; y++)
            {
                int tileX = pScreenCenterTileX + x;
                int tileY = pScreenCenterTileY + y;

                float noiseValue = (1.0f + _noiseFunction.GetCubic(tileX, tileY)) / 2.0f;

                if (noiseValue > 0.5f)
                {
                    // floor
                }
                else
                {
                    // walls
                    this.SetTile(tileX, tileY, "colored_32", "ground", false);
                    this.SetTile(tileX, tileY, "colored_1", "solids_mask", false);
                }
            }
        }
    }

    public void SetTile(int pTileX, int pTileY, string pTileName, string pDestTileMap, bool pShouldSave)
    {
        Vector3Int tilePosition = new Vector3Int(pTileX, pTileY, 0);

        Tile tile = null;
        if (!_tiles.TryGetValue(pTileName, out tile))
        {
            tile = null;
        }

        // assign to the tilemap.
        switch (pDestTileMap)
        {
            case "ground":
                _groundTileMap.SetTile(tilePosition, tile);
                break;
            case "walkables":
                _walkablesTileMap.SetTile(tilePosition, tile);
                break;
            case "objects":
                _objectsTileMap.SetTile(tilePosition, tile);
                break;
            case "players":
                _playersTileMap.SetTile(tilePosition, tile);
                break;
            case "solids_mask":
                _solidsMaskTileMap.SetTile(tilePosition, tile);
                break;
        }

        // we only save when the tilemaps change from their default generated state.
        // e.g. when a player removes a generated tree.
        if (pShouldSave)
        {
            // add this change to the buffer so we aren't restricted by file IO.
            string changesBufferCommand = pTileX + "," + pTileY + "," + pTileName + "," + pDestTileMap;
            _changesToSaveBuffer.Add(changesBufferCommand);
        }
    }

    public TileBase GetTile(int pTileX, int pTileY, string pDestTileMap)
    {
        Vector3Int tilePosition = new Vector3Int(pTileX, pTileY, 0);

        // assign to the tilemap.
        switch (pDestTileMap)
        {
            case "ground":
                return _groundTileMap.GetTile(tilePosition);
            case "walkables":
                return _walkablesTileMap.GetTile(tilePosition);
            case "objects":
                return _objectsTileMap.GetTile(tilePosition);
            case "players":
                return _playersTileMap.GetTile(tilePosition);
            case "solids_mask":
                return _solidsMaskTileMap.GetTile(tilePosition);
            default:
                Debug.Log("Could not get tile @ " + pTileX + ", " + pTileY);
                return null;
        }
    }

    /// <summary>
    /// saving is done by INSERT OR UPDATE method
    /// </summary>
    public void SaveMap()
    {
        // create directory if it doesn't exist yet.
        if (!Directory.Exists(Application.persistentDataPath + "/saves"))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/saves");
        }

        // get the previous saved file.
        long currentBiggestUnixTimestamp = 0;
        string mostRecentSaveFilename = "";
        foreach (string saveFileNamePath in Directory.GetFiles(Application.persistentDataPath + "/saves", "*.json"))
        {
            string fileName = Path.GetFileNameWithoutExtension(saveFileNamePath);
            long thisFileSaveTimestamp = Convert.ToInt64(fileName);

            if (thisFileSaveTimestamp > currentBiggestUnixTimestamp)
            {
                mostRecentSaveFilename = fileName;
            }
        }



        // if we have a save file.
        if (mostRecentSaveFilename != "")
        {
            // current time for the filename
            string newSaveFileName = Convert.ToString(DateTimeOffset.UtcNow.ToUnixTimeSeconds()) + ".json";

            HashSet<string> newSaveFileChanges = new HashSet<string>();

            // copy the most recent save.
            File.Copy(Application.persistentDataPath + "/saves/" + mostRecentSaveFilename + ".json", Application.persistentDataPath + "/saves/" + newSaveFileName);

            // merge conflicts with the new file.
            using (StreamReader streamReader = new StreamReader(Application.persistentDataPath + "/saves/" + mostRecentSaveFilename + ".json"))
            {
                // if the change location here exists in the current changes, overwrite it.
                while (!streamReader.EndOfStream)
                {
                    string changeCommand = streamReader.ReadLine();

                    string[] commandComponents = changeCommand.Split(',');

                    if (commandComponents.Length == 4)
                    {
                        string tileX = commandComponents[0];
                        string tileY = commandComponents[1];
                        string tileName = commandComponents[2];
                        string destTileMapName = commandComponents[3];

                        // is this command overwritten?
                        bool isBeingOverwritten = false;
                        foreach (string newChange in _changesToSaveBuffer)
                        {
                            string[] newCommandComponents = newChange.Split(',');
                            string newTileX = newCommandComponents[0];
                            string newTileY = newCommandComponents[1];
                            string newTileName = newCommandComponents[2];
                            string newDestTileMapName = newCommandComponents[3];

                            if (Convert.ToInt32(tileX) == Convert.ToInt32(newTileX))
                            {
                                if (Convert.ToInt32(tileY) == Convert.ToInt32(newTileY))
                                {
                                    isBeingOverwritten = true;
                                }
                            }
                        }

                        // if we aren't overwriting this command, then add it to the new file.
                        // this means that changes will continue through consequtive save files.
                        if (!isBeingOverwritten)
                        {
                            newSaveFileChanges.Add(changeCommand);
                        }
                    }
                    else
                    {
                        throw new Exception("Tried to load a save file that was corrupt. Please delete: " + Application.persistentDataPath + "/saves/" + " folder.");
                    }
                }
            }

            // add the waiting changes buffer
            foreach (var changeCommand in this._changesToSaveBuffer)
            {
                newSaveFileChanges.Add(changeCommand);
            }

            // write the changes to the new file.
            using (StreamWriter streamWriter = new StreamWriter(Application.persistentDataPath + "/saves/" + newSaveFileName))
            {
                foreach (var changeCommand in newSaveFileChanges)
                {
                    streamWriter.WriteLine(changeCommand);
                }
            }
        }
        else
        {
            // current time for the filename
            string newSaveFileName = Convert.ToString(DateTimeOffset.UtcNow.ToUnixTimeSeconds()) + ".json";

            // write the changes to the new file.
            using (StreamWriter streamWriter = new StreamWriter(Application.persistentDataPath + "/saves/" + newSaveFileName))
            {
                foreach (var changeCommand in _changesToSaveBuffer)
                {
                    streamWriter.WriteLine(changeCommand);
                }
            }
        }

  

        // overwrite existing changes with this new buffer value

        _changesToSaveBuffer.Clear();
    }

    public bool LoadMap()
    {
        // load file into memory.
        Debug.Log("Loading map from file:");

        // create directory if it doesn't exist yet.
        if (!Directory.Exists(Application.persistentDataPath + "/saves"))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/saves");
        }

        // get the previous saved file.
        long currentBiggestUnixTimestamp = 0;
        string mostRecentSaveFilename = "";
        foreach (string saveFileNamePath in Directory.GetFiles(Application.persistentDataPath + "/saves", "*.json"))
        {
            string fileName = Path.GetFileNameWithoutExtension(saveFileNamePath);
            long thisFileSaveTimestamp = Convert.ToInt64(fileName);

            if (thisFileSaveTimestamp > currentBiggestUnixTimestamp)
            {
                mostRecentSaveFilename = fileName;
            }
        }
        Debug.Log("     " + mostRecentSaveFilename);


        // for each change in the file, apply them.
        if (mostRecentSaveFilename != "")
        {
            HashSet<string> saveFileChangesBuffer = new HashSet<string>();
            using (StreamReader streamReader = new StreamReader(Application.persistentDataPath + "/saves/" + mostRecentSaveFilename + ".json"))
            {
                while (!streamReader.EndOfStream)
                {
                    string changeCommand = streamReader.ReadLine();

                    saveFileChangesBuffer.Add(changeCommand);
                }
            }

            Debug.Log("      Completed with " + saveFileChangesBuffer.Count);

            float timeNow = Time.time;
            Debug.Log("     Processing the change buffer...");

            foreach (string changeCommand in saveFileChangesBuffer)
            {
                string[] newCommandComponents = changeCommand.Split(',');
                string newTileX = newCommandComponents[0];
                string newTileY = newCommandComponents[1];
                string newTileName = newCommandComponents[2];
                string newDestTileMapName = newCommandComponents[3];

                int tileX = Convert.ToInt32(newTileX);
                int tileY = Convert.ToInt32(newTileY);

                this.SetTile(tileX, tileY, newTileName, newDestTileMapName, false);
            }

            float timeTaken = Time.time - timeNow;
            Debug.Log("     Completed (took " + timeTaken + "s)");

            return true;
        }
        else
        {
            Debug.Log("     No save file present!");
            return false;
        }
    }

}