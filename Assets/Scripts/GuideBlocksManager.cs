using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GuideBlocksManager : MonoBehaviour
{
    [Header("Guide Settings")]
    public int previewCount = 3; // Number of upcoming pieces to show
    public Vector2Int guideBoardSize = new Vector2Int(5, 5); // Size for each preview
    public Vector2Int guideOffset = new Vector2Int(10, 0); // Position of guide board relative to main board
    
    [Header("References")]
    public Tilemap guideTilemap; // Separate tilemap for guide blocks
    public TileBase[] tetrominoTiles; // Same tiles as main game
    
    private List<Tetromino> upcomingPieces = new List<Tetromino>();
    private int[,] guideBoard; // Combined guide board for all previews
    
    // Common Tetromino shapes (I, O, T, S, Z, J, L)
    private readonly List<int[,]> tetrominoShapes = new List<int[,]>
    {
        // I shape (4x1)
        new int[4, 1] { {1}, {1}, {1}, {1} },
        
        // O shape (2x2)
        new int[2, 2] { {1, 1}, {1, 1} },
        
        // T shape (3x2) - Fixed: 3 rows, 2 columns for proper T shape
        new int[3, 2] { {0, 1}, {1, 1}, {0, 1} },
        
        // S shape (2x3)
        new int[2, 3] { {0, 1, 1}, {1, 1, 0} },
        
        // Z shape (2x3)
        new int[2, 3] { {1, 1, 0}, {0, 1, 1} },
        
        // J shape (3x2)
        new int[3, 2] { {1, 0}, {1, 0}, {1, 1} },
        
        // L shape (3x2)
        new int[3, 2] { {0, 1}, {0, 1}, {1, 1} }
    };
    
    void Start()
    {
        InitializeGuideBoard();
        GenerateUpcomingPieces();
        DrawGuideBlocks();
    }
    
    public void InitializeGuideBoard()
    {
        if (guideBoard != null) return; // Already initialized
        
        // Validate settings
        if (previewCount <= 0)
        {
            // DebugogWarning($"previewCount must be > 0, but is {previewCount}. Using default 3.");
            previewCount = 3;
        }
        
        if (guideBoardSize.y <= 0 || guideBoardSize.x <= 0)
        {
            // DebugogWarning($"guideBoardSize must be > 0, but is {guideBoardSize}. Using default 5x5.");
            guideBoardSize = new Vector2Int(5, 5);
        }
        
        // Initialize guide board (previewCount * guideBoardSize.y rows, guideBoardSize.x columns)
        int totalRows = previewCount * guideBoardSize.y;
        guideBoard = new int[totalRows, guideBoardSize.x];
        // Debugog($"Initialized guideBoard: {totalRows}x{guideBoardSize.x}");
    }
    
    public void GenerateUpcomingPieces()
    {
        // Debugog("GenerateUpcomingPieces called");
        
        if (upcomingPieces == null)
        {
            upcomingPieces = new List<Tetromino>();
        }
        
        upcomingPieces.Clear();
        
        for (int i = 0; i < previewCount; i++)
        {
            Tetromino piece = GenerateRandomTetromino();
            if (piece != null && piece.shape != null)
            {
                upcomingPieces.Add(piece);
                // Debugog($"Generated piece {i}: {piece.shape.GetLength(0)}x{piece.shape.GetLength(1)}");
            }
            // else
            // {
            //     // DebugogError($"Failed to generate piece {i}");
            //     // Add a default piece
            //     Tetromino defaultPiece = new Tetromino();
            //     defaultPiece.shape = new int[2, 2] { {1, 1}, {1, 1} };
            //     defaultPiece.tileIndex = 1;
            //     defaultPiece.PaintTetromino();
            //     defaultPiece.position = Vector2Int.zero;
            //     upcomingPieces.Add(defaultPiece);
            // }
        }
        
        // Debugog($"Calling UpdateGuideBoard with {upcomingPieces.Count} pieces");
        UpdateGuideBoard();
    }
    
    private Tetromino GenerateRandomTetromino()
    {
        Tetromino tetromino = new Tetromino();
        
        // Check if tetrominoShapes has elements
        if (tetrominoShapes == null || tetrominoShapes.Count == 0)
        {
            // DebugogError("tetrominoShapes is null or empty!");
            // Return a default shape
            tetromino.shape = new int[2, 2] { {1, 1}, {1, 1} };
            tetromino.tileIndex = 1;
            tetromino.position = Vector2Int.zero;
            return tetromino;
        }
        
        // Random shape
        int shapeIndex = Random.Range(0, tetrominoShapes.Count);
        tetromino.shape = tetrominoShapes[shapeIndex];
        
        // // Use shape index + 1 for tile index (assuming 0 is empty)
        // tetromino.tileIndex = shapeIndex + 1;
        
        // // Ensure tileIndex is valid for tetrominoTiles array
        // if (tetrominoTiles != null && tetromino.tileIndex >= tetrominoTiles.Length)
        // {
        //     // DebugogWarning($"Tile index {tetromino.tileIndex} out of bounds (0-{tetrominoTiles.Length - 1}). Using 1.");
        //     tetromino.tileIndex = 1;
        // }

        // TODO: all tileIndex use 1 for now, changelater
        tetromino.tileIndex = 1;

        // Paint the tetromino shape with its tile index for easier drawing in guide
        Debug.Log($"Generated Tetromino: Shape {tetromino.shape.GetLength(0)}x{tetromino.shape.GetLength(1)}, TileIndex {tetromino.tileIndex}");
        tetromino.PaintTetromino();
        
        // Position will be set when drawing in guide
        tetromino.position = Vector2Int.zero;
        
        return tetromino;
    }
    
    public Tetromino GetNextPiece()
    {
        if (upcomingPieces == null || upcomingPieces.Count == 0)
        {
            // DebugogWarning("upcomingPieces is empty, generating new pieces");
            GenerateUpcomingPieces();
        }
        
        if (upcomingPieces.Count == 0)
        {
            // DebugogError("upcomingPieces is still empty after GenerateUpcomingPieces!");
            return GenerateRandomTetromino();
        }
        
        Tetromino nextPiece = upcomingPieces[0];
        upcomingPieces.RemoveAt(0);
        
        // Add a new piece to the end
        upcomingPieces.Add(GenerateRandomTetromino());
        
        UpdateGuideBoard();
        DrawGuideBlocks();
        
        return nextPiece;
    }
    
    private void UpdateGuideBoard()
    {
        try
        {
            // Debugog("UpdateGuideBoard called");
            
            // Check if guideBoard is initialized
            if (guideBoard == null)
            {
                // DebugogError("guideBoard is null in UpdateGuideBoard! Initializing now...");
                InitializeGuideBoard();
            }
            
            // Check if guideBoard is valid (not 0x0)
            if (guideBoard.GetLength(0) == 0 || guideBoard.GetLength(1) == 0)
            {
                // DebugogError($"guideBoard has invalid dimensions: {guideBoard.GetLength(0)}x{guideBoard.GetLength(1)}");
                InitializeGuideBoard();
            }
            
            // Debugog($"guideBoard dimensions: {guideBoard.GetLength(0)}x{guideBoard.GetLength(1)}");
            // Debugog($"upcomingPieces count: {upcomingPieces?.Count ?? 0}");
            // Debugog($"guideBoardSize: {guideBoardSize}");
            
            // Clear guide board
            for (int i = 0; i < guideBoard.GetLength(0); i++)
            {
                for (int j = 0; j < guideBoard.GetLength(1); j++)
                {
                    guideBoard[i, j] = 0;
                }
            }
            
            // Draw each upcoming piece in its section
            if (upcomingPieces != null)
            {
                for (int pieceIndex = 0; pieceIndex < upcomingPieces.Count; pieceIndex++)
                {
                    // Debugog($"Processing piece index {pieceIndex}");
                    
                    Tetromino piece = upcomingPieces[pieceIndex];
                    
                    // Check if piece or shape is null
                    if (piece == null)
                    {
                        // DebugogError($"Piece at index {pieceIndex} is null!");
                        continue;
                    }
                    
                    if (piece.shape == null)
                    {
                        // DebugogError($"Piece.shape at index {pieceIndex} is null!");
                        continue;
                    }
                    
                    // Debugog($"Piece shape dimensions: {piece.shape.GetLength(0)}x{piece.shape.GetLength(1)}");
                    
                    int startRow = pieceIndex * guideBoardSize.y;
                    // Debugog($"startRow: {startRow} (pieceIndex={pieceIndex} * guideBoardSize.y={guideBoardSize.y})");
                    
                    // Center the piece horizontally in its section
                    int shapeWidth = piece.shape.GetLength(1);
                    int startCol = (guideBoardSize.x - shapeWidth) / 2;
                    // Debugog($"startCol: {startCol} (guideBoardSize.x={guideBoardSize.x} - shapeWidth={shapeWidth}) / 2");
                    
                    // Ensure startCol is not negative
                    if (startCol < 0)
                    {
                        // DebugogWarning($"startCol is negative ({startCol}), using 0");
                        startCol = 0;
                    }
                    
                    // Draw the piece
                    for (int i = 0; i < piece.shape.GetLength(0); i++)
                    {
                        for (int j = 0; j < piece.shape.GetLength(1); j++)
                        {
                            if (piece.shape[i, j] == 1)
                            {
                                int row = startRow + i;
                                int col = startCol + j;
                                
                                if (row >= 0 && row < guideBoard.GetLength(0) && 
                                    col >= 0 && col < guideBoard.GetLength(1))
                                {
                                    guideBoard[row, col] = piece.tileIndex;
                                }
                                else
                                {
                                    // DebugogWarning($"Position out of bounds: row={row}, col={col}, board={guideBoard.GetLength(0)}x{guideBoard.GetLength(1)}");
                                }
                            }
                        }
                    }
                }
            }
            
            // Debugog("UpdateGuideBoard completed successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in UpdateGuideBoard: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }
    
    private void DrawGuideBlocks()
    {
        try
        {
            // Debugog("DrawGuideBlocks called");
            
            // Clear the guide tilemap
            if (guideTilemap == null)
            {
                // DebugogError("guideTilemap is null in DrawGuideBlocks!");
                return;
            }
            
            guideTilemap.ClearAllTiles();
            
            // Check if tetrominoTiles is valid
            if (tetrominoTiles == null || tetrominoTiles.Length == 0)
            {
                // DebugogError("tetrominoTiles is null or empty in DrawGuideBlocks!");
                return;
            }
            
            // Check if guideBoard is valid
            if (guideBoard == null)
            {
                // DebugogError("guideBoard is null in DrawGuideBlocks!");
                return;
            }
            
            // Draw the guide board
            for (int i = 0; i < guideBoard.GetLength(0); i++)
            {
                for (int j = 0; j < guideBoard.GetLength(1); j++)
                {
                    int tileIndex = guideBoard[i, j];
                    if (tileIndex > 0)
                    {
                        // Ensure tileIndex is within bounds
                        if (tileIndex >= tetrominoTiles.Length)
                        {
                            // DebugogWarning($"Tile index {tileIndex} out of bounds (0-{tetrominoTiles.Length - 1}). Using index 0.");
                            tileIndex = 0;
                        }
                        
                        Vector3Int cell = new Vector3Int(
                            j + guideOffset.x, 
                            i + guideOffset.y, 
                            0
                        );
                        
                        TileBase tile = tetrominoTiles[tileIndex];
                        if (tile != null)
                        {
                            guideTilemap.SetTile(cell, tile);
                        }
                        else
                        {
                            // DebugogWarning($"tetrominoTiles[{tileIndex}] is null!");
                        }
                    }
                }
            }
            
            // Debugog("DrawGuideBlocks completed successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in DrawGuideBlocks: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }
    
    // Debug method to see upcoming pieces in console
    public void DebugUpcomingPieces()
    {
        // Debugog($"Upcoming pieces ({upcomingPieces?.Count ?? 0}):");
        if (upcomingPieces != null)
        {
            for (int i = 0; i < upcomingPieces.Count; i++)
            {
                var piece = upcomingPieces[i];
                if (piece != null && piece.shape != null)
                {
                    Debug.Log($"Piece {i + 1}: TileIndex {piece.tileIndex}, Shape {piece.shape.GetLength(0)}x{piece.shape.GetLength(1)}");
                }
                else
                {
                    Debug.LogError($"Piece {i + 1}: NULL");
                }
            }
        }
    }

}