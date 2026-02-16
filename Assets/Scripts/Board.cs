using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    private readonly WaitForSeconds swapDelay = new WaitForSeconds(0.06f);

    private readonly WaitForSeconds collapseSwapDelay = new WaitForSeconds(0.01f);

    private readonly WaitForSeconds timeBetweenPiecesDelay = new WaitForSeconds(0.01f);


    bool swappingPieces = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Tiles = new Tile[width, height];
        Pieces = new Piece[width, height];

        SetupBoard();
        PositionCamera();
        StartCoroutine(SetUpPieces());
    }

    private IEnumerator SetUpPieces()
    {
        int maxIterations = 50;

        int currentIteration = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                yield return timeBetweenPiecesDelay;

                if (Pieces[x,y] == null)
                {
                    currentIteration = 0;
                    Piece newPiece = CreatePieceAt(x, y);

                    while (HasPreviousMatches(x, y) && currentIteration < maxIterations)
                    {
                        ClearPieceAt(x, y);
                        newPiece = CreatePieceAt(x, y);
                        currentIteration++;

                        if (currentIteration >= maxIterations)
                        {
                            Debug.LogWarning($"Max iterations reached at position {x}, {y}. There may be pre-existing matches on the board.");
                            break;
                        }
                    }
                }
            }
        }

        yield return null;
    }

    private void ClearPieceAt(int x, int y)
    {
        var pieceToClear = Pieces[x, y];
        pieceToClear.Remove(true);
        Pieces[x,y] = null;
    }

    private Piece CreatePieceAt(int x, int y)
    {
        var selectedPiece = avaialablePieces[UnityEngine.Random.Range(0, avaialablePieces.Length)];

        var o = Instantiate(selectedPiece, new Vector3(x, y+1, -5), Quaternion.identity);
        o.transform.parent = transform;
        Pieces[x, y] = o.GetComponent<Piece>();
        Pieces[x, y].Setup(x, y, this);
        Pieces[x, y].Move(x, y);

        return Pieces[x, y];
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
        if (swappingPieces) return;
        startTile = tile_;
    }

    public void TileOver(Tile tile_)
    {
        if (swappingPieces) return;
        endTile = tile_;
    }

    public void TileUp(Tile tile_)
    {
        if (swappingPieces) return;

        if (startTile != null && endTile != null && IsCloseTo(startTile, endTile))
        {
            StartCoroutine(SwapTiles());
        }
    }

    IEnumerator SwapTiles()
    {
        if (swappingPieces) yield break;

        swappingPieces = true;

        var StarPiece = Pieces[startTile.x, startTile.y];
        var EndPiece = Pieces[endTile.x, endTile.y];

        StarPiece.Move(endTile.x, endTile.y);
        EndPiece.Move(startTile.x, startTile.y);

        Pieces[startTile.x, startTile.y] = EndPiece;
        Pieces[endTile.x, endTile.y] = StarPiece;

        yield return swapDelay;

        var startMatches = GetMatchByPiece(startTile.x, startTile.y);
        var endMatches = GetMatchByPiece(endTile.x, endTile.y);

        var allMatches = startMatches.Union(endMatches).ToList();

        if (allMatches.Count > 0)
        {

            ClearPieces(allMatches);
        }
        else
        {
            // 4. Revertir si no hay match (con animación)
            StarPiece.Move(startTile.x, startTile.y);
            EndPiece.Move(endTile.x, endTile.y);

            Pieces[startTile.x, startTile.y] = StarPiece;
            Pieces[endTile.x, endTile.y] = EndPiece;

            yield return swapDelay; // Esperar a que vuelvan antes de liberar el control
        }

        // 5. Limpieza de referencias y estado
        startTile = null;
        endTile = null;
        swappingPieces = false;
    }

    private void ClearPieces(List<Piece> piecesToClear)
    {
        foreach (var piece in piecesToClear)
        {
            ClearPieceAt(piece.x, piece.y);
        }

        List<int> columns = GetColumns(piecesToClear);

        List<Piece> collapsedPieces = CollapseColumns(columns, 0.3f);

        FindMatchRecursively(collapsedPieces);
    }

    private void FindMatchRecursively(List<Piece> collapsedPieces)
    {
        StartCoroutine(FindMatchRecursivelyCoroutine(collapsedPieces));
    }

    IEnumerator FindMatchRecursivelyCoroutine(List<Piece> collapsedPieces)
    {
        yield return collapseSwapDelay;

        List<Piece> newMatches = new List<Piece>();

        foreach (var piece in collapsedPieces)
        {
            var matches = GetMatchByPiece(piece.x, piece.y);
            if (matches.Count > 0)
            {
                newMatches = newMatches.Union(matches).ToList();
                ClearPieces(matches);
            }
        }

        if(newMatches.Count > 0)
        {
            var newCollapsedPieces = CollapseColumns(GetColumns(newMatches), 0.3f);
            FindMatchRecursively(newCollapsedPieces);
        }
        else
        {
            yield return timeBetweenPiecesDelay;

            StartCoroutine(SetUpPieces());
        }

        yield return null;
    }

    private List<Piece> CollapseColumns(List<int> columns, float timeToColapse)
    {
        List<Piece> movingPieces = new List<Piece>();

        for (int i = 0; i < columns.Count; i++) {
            var column = columns[i];

            for (int y = 0; y < height; y++) {
                if (Pieces[column, y]==null) {
                    for (int yplus = y + 1; yplus < height; yplus++)
                    {
                        if (Pieces[column, yplus] != null) {
                            Pieces[column, yplus].Move(column, y);
                            Pieces[column, y] = Pieces[column, yplus];

                            if (!movingPieces.Contains(Pieces[column, y]))
                            {
                                movingPieces.Add(Pieces[column, y]);
                            }

                            Pieces[column, yplus] = null;
                            break;
                        }
                    }
                }
            }
        }

        return movingPieces;
    }

    private List<int> GetColumns(List<Piece> piecesToClear)
    {
        List<int> result = new List<int>();

        foreach (var piece in piecesToClear)
        {
            if (!result.Contains(piece.x))
            {
                result.Add(piece.x);
            }
        }

        return result;
    }

    public bool IsCloseTo(Tile start, Tile end)
    {
        return (Math.Abs(start.x - end.x) == 1 && start.y == end.y) ||
               (Math.Abs(start.y - end.y) == 1 && start.x == end.x);
    }

    public List<Piece> GetMatchByDirection(int xpos, int ypos, Vector2 direction, int minPieces=3)
    {
        List<Piece> matches = new List<Piece>();

        Piece startPiece = Pieces[xpos, ypos];

        matches.Add(startPiece);

        if (startPiece == null) return null;


        int nextX;
        int nextY;

        int maxVal = (width > height) ? width : height;


        for (int i = 1; i < maxVal; i++)
        {
            nextX = xpos + (int)(direction.x * i);
            nextY = ypos + (int)(direction.y * i);
            if (nextX >= 0 && nextX < width && nextY >= 0 && nextY < height)
            {

                Piece nextPiece = Pieces[nextX, nextY];

                if (nextPiece != null && nextPiece.pieceType == startPiece.pieceType)
                {
                    matches.Add(nextPiece);
                }
                else
                {
                    break;
                }
            }
        }

        if (matches.Count >= minPieces) return matches;

        return null;
    }

    public List<Piece> GetMatchByPiece(int xpos, int ypos, int minPieces = 3)
    {

        var upMatches = GetMatchByDirection(xpos, ypos, new Vector2(0,1), 2);
        var downMatches = GetMatchByDirection(xpos, ypos, new Vector2(0,-1), 2);
        var rightMatches = GetMatchByDirection(xpos, ypos, new Vector2(1,0), 2);
        var leftMatches = GetMatchByDirection(xpos, ypos, new Vector2(-1,0), 2);


        upMatches = upMatches is null ? new List<Piece>() : upMatches;
        downMatches = downMatches is null ? new List<Piece>() : downMatches;
        rightMatches = rightMatches is null ? new List<Piece>() : rightMatches;
        leftMatches = leftMatches is null ? new List<Piece>() : leftMatches;

        var verticalMatches = upMatches.Union(downMatches).ToList();
        var horizontalMatches = rightMatches.Union(leftMatches).ToList();

        var foundMatches = new List<Piece>();

        if (verticalMatches.Count >= minPieces)
        {
            foundMatches = foundMatches.Union(verticalMatches).ToList();
        }

        if (horizontalMatches.Count >= minPieces)
        {
            foundMatches = foundMatches.Union(horizontalMatches).ToList();
        }

        return foundMatches;
    }

    bool HasPreviousMatches(int posx, int posy)
    {
        var downMatches = GetMatchByDirection(posx, posy, new Vector2(0, -1), 2);
        var leftMatches = GetMatchByDirection(posx, posy, new Vector2(-1, 0), 2);

        downMatches = downMatches is null ? new List<Piece>() : downMatches;
        leftMatches = leftMatches is null ? new List<Piece>() : leftMatches;

        return downMatches.Count > 0 || leftMatches.Count > 0;
    }
}
