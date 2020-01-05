using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraManager : MonoBehaviour
{
    private Tilemap _groundTileMap;

    // Start is called before the first frame update
    void Start()
    {
        _groundTileMap = GameObject.FindGameObjectWithTag("TILESET_GROUND").GetComponent<Tilemap>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LookAtCellPosition(int pX, int pY)
    {
        Vector3 worldPosition = _groundTileMap.CellToWorld(new Vector3Int(pX, pY, 0));
        worldPosition.z = -10.0f;

        this.transform.position = worldPosition;
    }
}
