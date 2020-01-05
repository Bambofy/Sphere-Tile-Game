using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerMovement : Bolt.EntityBehaviour<IOpenLifePlayer>
{
    private int _positionX = 0;
    private int _positionY = 0;

    private Map _map;
    private CameraManager _cameraManager;

    public override void Attached()
    {
        // get a reference to the map to draw the player.
        _map = GameObject.FindGameObjectWithTag("MAP").GetComponent<Map>();

        // get a reference to the camera to keep it in sync with the player.
        // if this player gamemobject is the client.
        if (entity.IsOwner)
        {
            _cameraManager = GameObject.FindGameObjectWithTag("VIEW_CAMERA").GetComponent<CameraManager>();
        }


        state.AddCallback("Position_X", () =>
        {
            ClearPlayerFromTilemap();
            _positionX = state.Position_X;
            DrawPlayerOntoTilemap();
        });

        state.AddCallback("Position_Y", () =>
        {
            ClearPlayerFromTilemap();
            _positionY = state.Position_Y;
            DrawPlayerOntoTilemap();
        });

        DrawPlayerOntoTilemap();
    }

    public void ClearPlayerFromTilemap()
    {
        _map.SetTile(_positionX, _positionY, "", "players", false);
        _map.SetTile(_positionX, _positionY, "colored_0", "solids_mask", false);
    }

    public void DrawPlayerOntoTilemap()
    {
        if (entity.IsOwner)
        {
            _cameraManager.LookAtCellPosition(_positionX, _positionY);
        }

        _map.SetTile(_positionX, _positionY, "colored_25", "players", false);
        _map.SetTile(_positionX, _positionY, "colored_1", "solids_mask", false);
    }

    private bool CheckCanMove(int pCellX, int pCellY)
    {
        if (Input.GetKey(KeyCode.LeftShift)) return true;

        TileBase t = _map.GetTile(pCellX, pCellY, "solids_mask");
        if (t == null)
        {
            return true;
        }
        else if (t.name == "colored_1")
        {
            return false;
        }

        return true;
    }

    public override void SimulateOwner()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (!CheckCanMove(_positionX - 1, _positionY)) return;

            ClearPlayerFromTilemap();
            _positionX--;
            state.Position_X = _positionX;
            DrawPlayerOntoTilemap();
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            if (!CheckCanMove(_positionX + 1, _positionY)) return;

            ClearPlayerFromTilemap();
            _positionX++;
            state.Position_X = _positionX;
            DrawPlayerOntoTilemap();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            if (!CheckCanMove(_positionX , _positionY + 1)) return;

            ClearPlayerFromTilemap();
            _positionY++;
            state.Position_Y = _positionY;
            DrawPlayerOntoTilemap();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            if (!CheckCanMove(_positionX, _positionY - 1)) return;

            ClearPlayerFromTilemap();
            _positionY--;
            state.Position_Y = _positionY;
            DrawPlayerOntoTilemap();
        }
    }
}
