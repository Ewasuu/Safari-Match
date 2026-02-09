using System;
using UnityEngine;

public class Board : MonoBehaviour
{

    public int width;
    public int height;
    public GameObject tileObject;


    public float cameraSizeOffSet;
    public float cameraVerticalOffSet;

    public GameObject[] avaialablePieces;

    Tile[,] Tiles;
    Piece[,] Pieces;

    Tile startTile;
    Tile endTile;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Tiles = new Tile[width, height];
        Pieces = new Piece[width, height];

        SetupBoard();
        PositionCamera();
        SetUpPieces();
    }

    private void SetUpPieces()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var selectedPiece = avaialablePieces[UnityEngine.Random.Range(0, avaialablePieces.Length)];

                var o = Instantiate(selectedPiece, new Vector3(x, y, -5), Quaternion.identity);
                o.transform.parent = transform;
                Pieces[x, y] = o.GetComponent<Piece>();
                Pieces[x, y].Setup(x, y, this);
            }
        }
    }

    private void PositionCamera()
    {
        float newPosX = (float)width / 2f;
        float newPosY = (float)height / 2f;

        Camera.main.transform.position = new Vector3(newPosX - 0.5f, newPosY - 0.5f + cameraVerticalOffSet, -10f);

        float horizontal = width + 1;
        float vertical = (height / 2) + 1;

        Camera.main.orthographicSize = (horizontal > vertical) ? horizontal + cameraSizeOffSet: vertical + cameraSizeOffSet;
    }

    private void SetupBoard()
    {

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++) {
                var o = Instantiate(tileObject, new Vector3(x, y, -5), Quaternion.identity);
                o.transform.parent = transform;
                Tiles[x, y] = o.GetComponent<Tile>();
                Tiles[x, y].Setup(x, y, this);
            }
        }
    }

    public void TileDown(Tile tile_)

    {
        startTile = tile_;
    }

    public void TileOver(Tile tile_)
    {
        endTile = tile_;
    }

    public void TileUp(Tile tile_)
    {
        if (startTile != null && endTile != null)
        {
            SwapTiles();
        }
        startTile = null;
        endTile = null;
    }

    private void SwapTiles()
    {
        var StarPiece = Pieces[startTile.x, startTile.y];
        var EndPiece = Pieces[endTile.x, endTile.y];

        StarPiece.Move(endTile.x, endTile.y);
        EndPiece.Move(startTile.x, startTile.y);

        Pieces[startTile.x, startTile.y] = EndPiece;
        Pieces[endTile.x, endTile.y] = StarPiece;
    }
}
