using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// TODO: big overhaul. fix clearing. current situation: the chunk after clear seems good but do not fall
// TODO: try options: use lowest cleared row as a start to chop all blocks above into pieces by column

public class GameManager : MonoBehaviour
{
    // 30 * 20 game board
    [Header("Game Board Settings")]
    public int rows = 30;
    public int columns = 20;
    private int[,] gameBoard;
    public Tilemap tilemap;            // Assign in Inspector
    public TileBase[] tetrominoTiles; // Your tile assets for different pieces
    public Vector2Int tileOffset = new Vector2Int(0, 0); // Adjust as needed for tile placement

    [Header("Guide Blocks System")]
    public GuideBlocksManager guideManager; // Reference to guide blocks manager

    [Header("Falling Pieces")]
    // all the falling blocks use the following
    private List<Tetromino> fallingPieces = new List<Tetromino>();

    // Track the row index below the cleared rows for chain fall checking
    private int loggedClearedRowForChainFall = -1; 

    [Header("Gameplay Timers")]
    private float fallTimer = 0f;
    public float moveCooldown = 0.12f;      // seconds between moves
    private float lastMoveTime = -999f;     // last time a move was performed
    
    [Header("Game Settings")]
    // private float gameTimer = 0.0f;
    public float fallSpeed = 0.8f; // How often piece falls (seconds)
    private GameState currentState;
    private PlayingState playingState = PlayingState.Moving;

    [Header("Input Settings")]
    public InputActionReference move;
    public InputActionReference rotate;
    public InputActionReference land;
    public InputActionReference pause;

    [Header("Canvas References")]
    public CanvasGroup pauseMenuCanvas; // Assign in Inspector
    // Continue button reference
    public UnityEngine.UI.Button continueButton; // Assign in Inspector

    #region Unity Lifecycle
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameBoard = new int[rows, columns];

        // Initialize guide manager if not set
        if (guideManager == null)
        {
            guideManager = FindFirstObjectByType<GuideBlocksManager>();
        }

        // Check if tetrominoTiles is assigned
        if (tetrominoTiles == null || tetrominoTiles.Length == 0)
        {
            Debug.LogError("tetrominoTiles is not assigned in GameManager Inspector!");
            return;
        }

        // Check if tetrominoTiles[0] exists (for empty/background cells)
        if (tetrominoTiles[0] == null)
        {
            Debug.LogWarning("tetrominoTiles[0] is null! This might cause background tiles to disappear.");
        }

        // init guideManager - sync the tetrominoTiles
        if (guideManager != null)
        {
            guideManager.tetrominoTiles = tetrominoTiles;
        }
        else
        {
            Debug.LogError("guideManager is null!");
            return;
        }
        
        
        currentState = GameState.Paused;
        // Note: SpawnNewPiece() sets playingState to PlayingState.Moving

        Debug.Log("Game loaded with guide blocks system");
    }

    private void NewGame()
    {
        // renew game board
        InitializeGameBoard();
        fallingPieces.Clear();
        
        // clear and init guideManager things
        guideManager.ClearGuideBoard();

        Debug.Log("New Game");
        DrawGameBoard();
        // Spawn first piece
        SpawnNewPiece();
        playingState = PlayingState.Moving;
        
        // Ensure guideBoard is initialized before generating pieces
        guideManager.InitializeGuideBoard();
        // Force reinitialization of guide manager with new tiles
        guideManager.GenerateUpcomingPieces();
    }

    private void InitializeGameBoard()
    {
        // first, paint all of the board with base color
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                gameBoard[i, j] = 0;
            }
        }
        // 1. Calculate how many rows to fill from bottom
        int fillRows = rows * 3 / 5;  // 18 rows (0 to 17)
        
        // 2. Start crack at a random column near the bottom
        int crackCol = Random.Range(2, columns - 2); // avoid edges
        for (int row = 0; row < fillRows; row++)
        {
            // Generate row with current crack column
            int[] rowData = GenerateRowWithCrack(columns, crackCol, 0.2f);
            
            // Copy into board
            for (int col = 0; col < columns; col++)
            {
                gameBoard[row, col] = rowData[col];
            }
            
            // Randomly shift crack column for next row (the "meander")
            crackCol += Random.Range(-1, 2);
            crackCol = Mathf.Clamp(crackCol, 0, columns - 1);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == GameState.Playing)
        {
            // Handle different playing states
            switch (playingState)
            {
                case PlayingState.Moving:
                    // User can move or rotate the piece
                    HandlePlayerInput();
                    break;
                    
                case PlayingState.Landing:
                    // Blocks fall automatically after user decides to land
                    fallTimer += Time.deltaTime;
                    if (fallTimer >= fallSpeed)
                    {
                        bool movedDown = MovePieceDown();
                        if (!movedDown)
                        {
                            // playingState = PlayingState.Clearing;
                            fallingPieces.Clear(); // Clear the list of falling pieces as they are now part of the board
                            CheckForClearing();
                            // if no clearing, go to spawning. if cleared, go to clearing state to let blocks fall
                            if (fallingPieces.Count > 0)
                            {
                                Debug.Log("Blocks need to fall after clearing - starting clearing sequence");
                                playingState = PlayingState.Clearing;
                            }
                            else
                            {
                                Debug.Log("No blocks to fall after landing - spawning new piece");
                                playingState = PlayingState.Spawning;
                            }
                        }
                        fallTimer = 0f;
                    }
                    break;
                    
                case PlayingState.Clearing:
                    // CheckForClearing();
                    // TODO: if cleared do the strip. move will not be processed here
                    fallTimer += Time.deltaTime;
                    if (fallTimer >= fallSpeed)
                    {
                        bool movedDown = MovePieceDown();
                        Debug.Log("a move was executed in Clearing state, movedDown=" + movedDown);
                        if (!movedDown)
                        {
                            // After all blocks have fallen as much as they can, check if we need to start chain falling
                            
                            // playingState = PlayingState.Landing;
                            fallingPieces.Clear();

                            PickBlocksForChainFall();
                            // reset loggedClearedRowForChainFall after checking for chain fall to avoid infinite loop of chain fall
                            loggedClearedRowForChainFall = -1;
                            // go to landing if have blocks for chain fall, or go to spawning
                            if (fallingPieces.Count > 0)
                            {
                                Debug.Log("Chain fall detected - starting landing sequence for chain blocks");
                                playingState = PlayingState.Landing;
                            }
                            else
                            {
                                Debug.Log("No chain fall - spawning new piece");
                                playingState = PlayingState.Spawning;
                            }
                        }
                        fallTimer = 0f;
                    }
                    break;

                case PlayingState.ChainFalling:
                    // ProcessChainFall();
                    break;
                    
                case PlayingState.Spawning:
                    // Spawn new piece
                    Debug.Log("State: Spawning - getting new piece");
                    SpawnNewPiece();
                    // After spawning, go back to moving state
                    playingState = PlayingState.Moving;
                    Debug.Log("State transition: Spawning -> Moving");
                    break;
            }
            
            // Always redraw the board to ensure background tiles are preserved
            DrawGameBoard();
            for (int i = 0; i < fallingPieces.Count; i++)
            {
                DrawCurrentPiece(i);
            }
        }
    }
    #endregion

    #region Game Logic Methods
    private void DrawGameBoard()
    {
        // Loop through the game board and draw each cell
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                Vector3Int cell = new Vector3Int(j + tileOffset.x, i + tileOffset.y, 0);
                tilemap.SetTile(cell, tetrominoTiles[gameBoard[i, j]]);
            }
        }
    }
    
    private void DrawCurrentPiece(int piece_index)
    {
        if (piece_index > fallingPieces.Count) return;
        Tetromino currentPiece = fallingPieces[piece_index];
        
        // Draw the current piece
        for (int i = 0; i < currentPiece.shape.GetLength(0); i++)
        {
            for (int j = 0; j < currentPiece.shape.GetLength(1); j++)
            {
                if (currentPiece.shape[i, j] == 1)
                {
                    int row = currentPiece.position.y + i;
                    int col = currentPiece.position.x + j;
                    
                    if (row >= 0 && row < rows && col >= 0 && col < columns)
                    {
                        Vector3Int cell = new Vector3Int(col + tileOffset.x, row + tileOffset.y, 0);
                        tilemap.SetTile(cell, tetrominoTiles[currentPiece.shape[i, j]]);
                    }
                }
            }
        }
    }
    
    private void ClearCurrentPiece(int piece_index)
    {
        if (piece_index > fallingPieces.Count) return;
        Tetromino currentPiece = fallingPieces[piece_index];
        
        // Debug: log what we're clearing
        // Debug.Log($"Clearing piece at position {currentPiecePosition}");
        
        // Clear the current piece from tilemap by restoring what was underneath
        for (int i = 0; i < currentPiece.shape.GetLength(0); i++)
        {
            for (int j = 0; j < currentPiece.shape.GetLength(1); j++)
            {
                if (currentPiece.shape[i, j] == 1)
                {
                    int row = currentPiece.position.y + i;
                    int col = currentPiece.position.x + j;
                    
                    if (row >= 0 && row < rows && col >= 0 && col < columns)
                    {
                        Vector3Int cell = new Vector3Int(col + tileOffset.x, row + tileOffset.y, 0);
                        
                        // Check what should be at this position
                        int tileIndex = gameBoard[row, col];
                        
                        // Debug: log what we're restoring
                        // Debug.Log($"Restoring cell ({row},{col}): gameBoard={tileIndex}, tetrominoTiles[0]={(tetrominoTiles != null && tetrominoTiles.Length > 0 ? tetrominoTiles[0] != null ? "not null" : "null" : "N/A")}");
                        
                        // If gameBoard has a tile (index > 0), restore it
                        // If gameBoard is empty (index == 0), set to appropriate tile
                        if (tileIndex >= 0 && tileIndex < tetrominoTiles.Length)
                        {
                            tilemap.SetTile(cell, tetrominoTiles[tileIndex]);
                        }
                        else
                        {
                            // Invalid tile index - set to null
                            tilemap.SetTile(cell, null);
                        }
                    }
                }
            }
        }
    }
    
    private void SpawnNewPiece()
    {
        Tetromino newPiece;
        if (guideManager != null)
        {
            newPiece = guideManager.GetNextPiece();
        }
        else
        {
            // Fallback: create a simple piece
            newPiece = new Tetromino();
            newPiece.shape = new int[2, 2] { {1, 1}, {1, 1} };
            newPiece.tileIndex = 2; // O shape
        }
        
        // Start position: top center
        newPiece.position = new Vector2Int(
            columns / 2 - newPiece.shape.GetLength(1) / 2,
            rows - newPiece.shape.GetLength(0)
        );
        
        // Check if spawn position is valid
        if (!IsPositionValid(newPiece))
        {
            // Game over - piece can't spawn
            currentState = GameState.GameOver;
            Debug.Log("Game Over! Board is full.");
            return;
        }

        // add current piece to fallingPieces
        fallingPieces.Add(newPiece);
        
    }
    
    private bool IsPositionValid(Tetromino piece, Vector2Int? move_offset = null)
    {
        Vector2Int offset = move_offset ?? Vector2Int.zero;
        for (int i = 0; i < piece.shape.GetLength(0); i++)
        {
            for (int j = 0; j < piece.shape.GetLength(1); j++)
            {
                if (piece.shape[i, j] == 1)
                {
                    int row = piece.position.y + i + offset.y;
                    int col = piece.position.x + j + offset.x;

                    // Check bounds
                    if (row < 0 || row >= rows || col < 0 || col >= columns)
                        return false;
                    
                    // Check collision with existing blocks
                    if (gameBoard[row, col] != 0)
                        return false;
                }
            }
        }
        return true;
    }
    
    // Base method, move all fallingPieces
    private bool MovePieceDown()
    {
        bool anyMovedDown = false;
        int i = 0;
        while (i < fallingPieces.Count)
        {
            if (MovePieceDown(i))
            {
                anyMovedDown = true;
                i++; // only move to next piece if current piece successfully moved down
            }
            else
            {
                // current piece has landed and is now part of the board, so we remove it from fallingPieces list
                fallingPieces.RemoveAt(i);
                // do not increment i, as we want to check the next piece that has now shifted into the current index
            }
        }
        return anyMovedDown;
    }

    // returns true if piece successfully moved down, false if it landed (used for both normal landing and chain falling)
    private bool MovePieceDown(int piece_index)
    {
        ClearCurrentPiece(piece_index);
        
        Vector2Int newPosition = fallingPieces[piece_index].position + Vector2Int.down;
        bool canMoveDown = IsPositionValid(fallingPieces[piece_index], move_offset: Vector2Int.down);
        
        if (canMoveDown)
        {
            fallingPieces[piece_index].position = newPosition;
        }
        else
        {
            // Add piece to game board
            LandPiece(piece_index);
            // state switching to be handled in Update() at cases that includes landing pieces
        }
        return canMoveDown;
    }
    
    // this method is only used if there is only one piece (the new one) in the fallingPieces list
    private void MovePieceLeft()
    {
        ClearCurrentPiece(0);
        
        Vector2Int newPosition = fallingPieces[0].position + Vector2Int.left;
        
        if (IsPositionValid(fallingPieces[0], move_offset: Vector2Int.left))
        {
            fallingPieces[0].position = newPosition;
            // DrawCurrentPiece(0);
        }
        // else
        // {
        //     DrawCurrentPiece(0); // Restore original position
        // }
    }
    
    // this method is only used if there is only one piece (the new one) in the fallingPieces list
    private void MovePieceRight()
    {
        ClearCurrentPiece(0);
        
        Vector2Int newPosition = fallingPieces[0].position + Vector2Int.right;
        
        if (IsPositionValid(fallingPieces[0], move_offset: Vector2Int.right))
        {
            fallingPieces[0].position = newPosition;
            // DrawCurrentPiece(0);
        }
        // else
        // {
        //     DrawCurrentPiece(0); // Restore original position
        // }
    }
    
    // this method is only used if there is only one piece (the new one) in the fallingPieces list
    private void RotatePiece()
    {
        ClearCurrentPiece(0);

        int[,] rotatedShape = RotateMatrix(fallingPieces[0].shape);
        int[,] originalShape = fallingPieces[0].shape;
        Vector2Int originalPos = fallingPieces[0].position;

        // Calculate offset to keep the piece's center roughly the same
        int oldHeight = originalShape.GetLength(0);
        int oldWidth  = originalShape.GetLength(1);
        int newHeight = rotatedShape.GetLength(0);
        int newWidth  = rotatedShape.GetLength(1);

        Vector2Int offset = new Vector2Int(
            (oldWidth - newWidth) / 2,
            (oldHeight - newHeight) / 2
        );

        Vector2Int newPos = originalPos + offset;
        fallingPieces[0].shape = rotatedShape;
        fallingPieces[0].position = newPos;

        // Validate position
        if (!IsPositionValid(fallingPieces[0], move_offset: Vector2Int.zero))
        {
            // Try more wall kicks (left, right, down, up, double left/right)
            Vector2Int[] kicks = {
                Vector2Int.left, Vector2Int.right,
                Vector2Int.down, Vector2Int.up,
                Vector2Int.left * 2, Vector2Int.right * 2
            };

            bool success = false;
            foreach (var kick in kicks)
            {
                Vector2Int kickedPos = newPos + kick;
                if (IsPositionValid(fallingPieces[0], move_offset: kick))
                {
                    fallingPieces[0].position = kickedPos;
                    success = true;
                    break;
                }
            }

            if (!success)
            {
                // Revert everything
                fallingPieces[0].shape = originalShape;
                fallingPieces[0].position = originalPos;
            }
        }

        // DrawCurrentPiece(0);
    }
    
    private int[,] RotateMatrix(int[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        int[,] rotated = new int[cols, rows];
        
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                rotated[j, rows - 1 - i] = matrix[i, j];
            }
        }
        
        return rotated;
    }

    
    private void LandPiece(int piece_index)
    {
        // Add piece to game board
        for (int i = 0; i < fallingPieces[piece_index].shape.GetLength(0); i++)
        {
            for (int j = 0; j < fallingPieces[piece_index].shape.GetLength(1); j++)
            {
                if (fallingPieces[piece_index].shape[i, j] == 1)
                {
                    int row = fallingPieces[piece_index].position.y + i;
                    int col = fallingPieces[piece_index].position.x + j;
                    
                    if (row >= 0 && row < rows && col >= 0 && col < columns)
                    {
                        gameBoard[row, col] = fallingPieces[piece_index].shape[i, j]; // Use the shape value (1) to indicate filled cell, or you can use tileIndex for coloring
                    }
                }
            }
        }
        
    }
    
    // clear the full row, make all rows above a falling piece to be moved down in clearing state
    private void CheckForClearing()
    {
        bool anyRowsCleared = false;
        int lowestClearedRow = -1; // Track the lowest cleared row for guide block generation
        int highestClearedRow = -1; // Track the highest cleared row for guide block generation
        
        // Simple row clearing check
        for (int row = 0; row < rows; row++)
        {
            bool rowFull = true;
            for (int col = 0; col < columns; col++)
            {
                if (gameBoard[row, col] == 0)
                {
                    rowFull = false;
                    break;
                }
            }
            
            if (rowFull)
            {
                anyRowsCleared = true;
                // record the lowest and highest cleared rows for guide block generation
                if (lowestClearedRow == -1 || row < lowestClearedRow)
                {
                    lowestClearedRow = row;
                }
                if (highestClearedRow == -1 || row > highestClearedRow)
                {
                    highestClearedRow = row;
                }
                
                // Clear the row
                for (int col = 0; col < columns; col++)
                {
                    gameBoard[row, col] = 0;
                }
                // // Redraw board
                // DrawGameBoard();
                
                // Start from the same row since rows shifted down
                row--;
            }
        }

        // make all blocks above the highest cleared row a falling piece
        if (anyRowsCleared)
        {
            Debug.Log($"Rows cleared from {lowestClearedRow} to {highestClearedRow}");
            // get all rows above the highest cleared row and make them a falling piece
            int[,] blocks_above_cleared_rows = new int[rows - highestClearedRow - 1, columns];
            Debug.Log($"blocks_above_cleared_rows dimension {blocks_above_cleared_rows.GetLength(0)} to {blocks_above_cleared_rows.GetLength(1)}");
            for (int row = highestClearedRow + 1; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int blocks_row_index = row - (highestClearedRow + 1);
                    blocks_above_cleared_rows[blocks_row_index, col] = gameBoard[row, col];
                    gameBoard[row, col] = 0; // clear from board to prepare for falling
                    if (gameBoard[row, col] != 0)
                    {
                        Debug.Log($"Block at ({row},{col}) with tile index {gameBoard[row, col]} added to falling piece");
                    }
                }
            }

            Tetromino fallingPiece = new Tetromino();
            fallingPiece.position = new Vector2Int(
                0, highestClearedRow + 1
            );
            fallingPiece.shape = blocks_above_cleared_rows;
            Debug.Log($"new piece at ({fallingPiece.position.x}, {fallingPiece.position.y}) with tile index {fallingPiece.shape[0, 0]}");

            fallingPieces.Add(fallingPiece);

            // log loggedClearedRowForChainFall
            loggedClearedRowForChainFall = lowestClearedRow;
        }

    }

    private void PickBlocksForChainFall()
    {
        if (loggedClearedRowForChainFall <= 0)
        {
            Debug.Log("No cleared row logged for chain fall checking.");
            return;
        }
        loggedClearedRowForChainFall -= 1;

        // Find the crack columns below the cleared row
        List<int> crackColumn = new List<int>();
        for (int col = 0; col < columns; col++)
        {
            if (gameBoard[loggedClearedRowForChainFall, col] == 0)
            {
                crackColumn.Add(col);
                break;
            }
        }
        if (crackColumn.Count == 0) return;

        // go up from each crack column to find falling blocks
        foreach (int col in crackColumn)
        {
            // if several blocks are connected, they should fall together as a piece
            List<int> connected_blocks = new List<int>();
            for (int row = loggedClearedRowForChainFall + 1; row < rows; row++)
            {
                if (gameBoard[row, col] != 0)
                {
                    connected_blocks.Add(gameBoard[row, col]);
                    gameBoard[row, col] = 0; // clear from board to prepare for falling
                    // // found a block that can fall, add to fallingPieces
                    // Tetromino fallingPiece = new Tetromino();
                    // fallingPiece.position = new Vector2Int(col, row);
                    // fallingPiece.shape = new int[1, 1] { { gameBoard[row, col] } };
                    // // fallingPiece.tileIndex = gameBoard[row, col];
                    // fallingPieces.Add(fallingPiece);

                    // // clear from board to prepare for falling
                    // gameBoard[row, col] = 0;
                }
                else if (connected_blocks.Count > 0 || row == rows - 1) // if we reach an empty cell after finding connected blocks, or we reach the top of the board, we finalize the current falling piece
                {
                    // make the connected_blocks a new falling piece and add to fallingPieces
                    if (connected_blocks.Count > 0)
                    {
                        Tetromino fallingPiece = new Tetromino();
                        fallingPiece.position = new Vector2Int(col, row-connected_blocks.Count);
                        fallingPiece.shape = new int[connected_blocks.Count, 1];
                        for (int i = 0; i < connected_blocks.Count; i++)
                        {
                            fallingPiece.shape[i, 0] = connected_blocks[i];
                        }
                        fallingPieces.Add(fallingPiece);
                    }
                    connected_blocks.Clear(); // reset for next potential piece in the same column
                }
            }
        }

        // // For each column, collect blocks above cleared row within limit
        // for (int col = 0; col < columns; col++)
        // {
        //     int blockCountAbove = 0;
        //     for (int row = clearedRow + 1; row < rows; row++)
        //     {
        //         if (gameBoard[row, col] != 0)
        //             blockCountAbove++;
        //     }
        //     if (blockCountAbove > chainFallRowLimit)
        //     {
        //         Debug.Log($"Column {col} exceeds chain fall limit. Aborting chain fall.");
        //         chainBlocks.Clear();
        //         return;
        //     }

        //     // Collect each block as a ChainBlock
        //     for (int row = clearedRow + 1; row < clearedRow + 1 + chainFallRowLimit && row < rows; row++)
        //     {
        //         int tileIndex = gameBoard[row, col];
        //         if (tileIndex != 0)
        //         {
        //             chainBlocks.Add(new ChainBlock { column = col, tileIndex = tileIndex });
        //             gameBoard[row, col] = 0;   // remove from board
        //         }
        //     }
        // }

        // if (chainBlocks.Count > 0)
        // {
        //     Debug.Log($"Chain fall ready: {chainBlocks.Count} blocks will fall automatically.");
        //     DrawGameBoard();
        // }
    }
    
    private void HandlePlayerInput()
    {
        // Move left/right
        Vector2 moveInput = move.action.ReadValue<Vector2>();
        float currentTime = Time.time;
        
        // Only allow move if enough time has passed since last move
        if (currentTime - lastMoveTime >= moveCooldown)
        {
            if (moveInput.x < -0.5f)
            {
                MovePieceLeft();
                lastMoveTime = currentTime;
            }
            else if (moveInput.x > 0.5f)
            {
                MovePieceRight();
                lastMoveTime = currentTime;
            }
        }

        // Rotate with rotate input
        if (rotate != null && rotate.action.WasPressedThisFrame())
        {
            RotatePiece();
        }
        
        // Land piece with land input
        if (land != null && land.action.WasPressedThisFrame())
        {
            StartLanding();
        }
        
        // // Quick drop (optional - keep for compatibility)
        // if (moveInput.y < -0.5f)
        // {
        //     // Quick drop - immediately land
        //     while (IsPositionValid(currentPiecePosition + Vector2Int.down))
        //     {
        //         MovePieceDown();
        //     }
        //     // Trigger landing after quick drop
        //     StartLanding();
        // }
    }
    
    private void StartLanding()
    {
        if (playingState == PlayingState.Moving)
        {
            // Switch to landing state
            playingState = PlayingState.Landing;
            fallTimer = 0f; // Reset timer for first fall
            Debug.Log("Starting landing sequence");
        }
    }

    private int[] GenerateRowWithCrack(int columns, int crackColumn, float erosionChance = 0.2f)
    {
        // Clamp crack column to valid range
        crackColumn = Mathf.Clamp(crackColumn, 0, columns - 1);
        
        // Start with all filled (1)
        int[] row = new int[columns];
        for (int i = 0; i < columns; i++)
        {
            row[i] = 1;
        }
        
        // Place the crack (empty cell)
        row[crackColumn] = 0;
        
        // Optional: erode adjacent cells (left/right) with given chance
        if (erosionChance > 0)
        {
            // Left neighbor
            if (crackColumn > 0 && Random.value < erosionChance)
                row[crackColumn - 1] = 0;
            
            // Right neighbor
            if (crackColumn < columns - 1 && Random.value < erosionChance)
                row[crackColumn + 1] = 0;
        }
        return row;
    }

    #endregion


    #region InputAction Callbacks
    private void OnEnable()
    {
        // Enable all input actions
        if (move != null) move.action.Enable();
        if (rotate != null) rotate.action.Enable();
        if (land != null) land.action.Enable();
        if (pause != null)
        {
            pause.action.Enable();
            pause.action.performed += OnPause;
        }
    }

    private void OnDisable()
    {
        // Disable all input actions
        if (pause != null)
        {
            pause.action.performed -= OnPause;
            pause.action.Disable();
        }
        if (land != null) land.action.Disable();
        if (rotate != null) rotate.action.Disable();
        if (move != null) move.action.Disable();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            Debug.Log("Game Paused");
            // Show pause menu UI
            pauseMenuCanvas.alpha = 1f;
            pauseMenuCanvas.interactable = true;
            pauseMenuCanvas.blocksRaycasts = true;
        }
        else if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Debug.Log("Game Resumed");
            pauseMenuCanvas.alpha = 0f;
            pauseMenuCanvas.interactable = false;
            pauseMenuCanvas.blocksRaycasts = false;
        }
    }
    #endregion

    #region UI Button Callbacks
    public void OnNewGameButton()
    {
        Debug.Log("New Game");
        NewGame();
        // enable continue button
        continueButton.interactable = true;
        currentState = GameState.Playing;
        Debug.Log("New Game Started from New Game Button");
        pauseMenuCanvas.alpha = 0f;
        pauseMenuCanvas.interactable = false;
        pauseMenuCanvas.blocksRaycasts = false;
    }

    public void OnContinueButton()
    {
        if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Debug.Log("Game Resumed from Continue Button");
            pauseMenuCanvas.alpha = 0f;
            pauseMenuCanvas.interactable = false;
            pauseMenuCanvas.blocksRaycasts = false;
        }
    }

    public void OnExitButton()
    {
        Debug.Log("Exit Game");
        Application.Quit();
    }
    #endregion
}

public enum GameState
{
    Playing,
    Paused,
    GameOver
}


// when a chain fall ia available, loop through ChainFalling and Clearing until no more chain fall is possible, then spawn a new piece
public enum PlayingState
{
    Moving,             // time when user move or rotate the piece
    Landing,            // time when the blocks fall, happens after player decide to land the piece
    Clearing,
    ChainFalling,
    Spawning
}
