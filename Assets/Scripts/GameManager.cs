using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // 30 * 20 game board
    [Header("Game Board Settings")]
    public int rows = 30;
    public int columns = 20;
    private int[,] gameBoard;
    public Tilemap tilemap;            // Assign in Inspector
    public TileBase[] tetrominoTiles; // Your tile assets for different pieces
    public Tilemap visualEffectTilemap; // Tilemap for visual effects like clearing lines, assign in Inspector
    public TileBase landingEffectTile; // Tile for landing effect, assign in Inspector
    public Vector2Int tileOffset = new Vector2Int(0, 0); // Adjust as needed for tile placement
    private int newestCrackColumn;

    [Header("Other Managers")]
    public GuideBlocksManager guideManager; // Reference to guide blocks manager
    public AudioManager audioManager; // Reference to audio manager

    [Header("Falling Pieces")]
    // all the falling blocks use the following
    private List<Tetromino> fallingPieces = new List<Tetromino>();
    // Track the row index below the cleared rows for chain fall checking
    private int loggedClearedRowForChainFall = -1; 

    [Header("Score Management")]
    private int score = 0;
    private int highScore = 0;

    [Header("Gameplay Timers")]
    private float fallTimer = 0f;
    public float moveCooldown = 0.12f;      // seconds between moves
    private float lastMoveTime = -999f;     // last time a move was performed
    private float riseTimer = 0f;           // timer for rising new rows
    
    [Header("Game Settings")]
    // private float gameTimer = 0.0f;
    public float fallSpeed = 0.06f; // How often piece falls (seconds)
    public float riseSpeed = 0.5f; // How often new rows are added and rise (seconds)
    private int stackHeightLimit = 25; // if blocks reach this height, game over
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
    // score board
    public TextMeshProUGUI scoreText; // Assign in Inspector 
    public TextMeshProUGUI highScoreText; // Assign in Inspector 

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
        
        score = 0;
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        scoreText.text = "Score: 0";
        highScoreText.text = "High Score: " + highScore.ToString();
        
        currentState = GameState.Paused;
        // Note: SpawnNewPiece() sets playingState to PlayingState.Moving

        Debug.Log("Game loaded with guide blocks system");
    }

    private void NewGame()
    {
        // renew game board
        InitializeGameBoard();
        fallingPieces.Clear();
        score = 0;
        scoreText.text = "Score: 0";
        
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

        audioManager.SwitchBGM(0); // Switch to gameplay music
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
        for (int row = fillRows - 1; row >=0; row--)
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
            newestCrackColumn = crackCol;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == GameState.Playing)
        {
            bool land_check_done = false; // to ensure we only check for landing once per frame, even if there are multiple pieces (like in chain falling)

            // make the board generate new rows and rise
            riseTimer += Time.deltaTime;
            if (riseTimer >= riseSpeed)
            {
                Debug.Log("Rising new row...");
                land_check_done = true;
                LandAllLandablePieces(); // make all pieces that can land land before we generate the new row, to avoid potential issues of pieces rising up with the new row and not being able to land immediately
                // Generate and add a new row at the bottom
                GenerateNewRow();
                riseTimer = 0f;
                
                // // if it makes all pieces land, we switch to clearing state to check if there are rows to clear, and if not we switch back to moving state to let player move the pieces around and decide when to land
                // if (fallingPieces.Count == 0)
                // {
                //     playingState = PlayingState.Clearing;
                //     Debug.Log("All pieces landed due to rising row - switching to Clearing state");
                // }
            }
            
            // check for game over condition before spawning new piece
            if (IsGameOver())
            {
                OnGameOver();
                return;
            }

            Debug.Log("We are here at the start of Update loop, current playing state: " + playingState);

            // Handle different playing states
            switch (playingState)
            {
                case PlayingState.Moving:
                    // User can move or rotate the piece
                    HandlePlayerInput();
                    DrawLandingEffect();
                    break;
                    
                case PlayingState.Landing:
                    // Blocks fall automatically after user decides to land
                    
                    Debug.Log("Now landing......");
                    fallTimer += Time.deltaTime;
                    if (fallTimer >= fallSpeed)
                    {
                        // only land if the "land_check_done" flag is false
                        Debug.Log("Normal landing......");
                        if (!land_check_done)
                        {
                            LandAllLandablePieces();
                        }
                        // now all remaining falling pieces should be the ones that are still falling after landing
                        // if no more pieces can land, we switch to clearing state to check for line clears, and if there are no line clears we switch back to moving state to let player move the pieces around and decide when to land again
                        if (fallingPieces.Count == 0)
                        {
                            playingState = PlayingState.Clearing;
                            Debug.Log("All pieces landed - switching to Clearing state");
                        }
                        else
                        {
                            Debug.Log("Moving pieces down......");
                            MovePieceDown();
                        }
                        fallTimer = 0f;
                    }
                    break;
                    
                case PlayingState.Clearing:
                    // If cleared do the strip. move will not be processed here
                    CheckForClearing();
                    if (loggedClearedRowForChainFall >= 0)
                    {
                        Debug.Log($"Rows cleared with lowest cleared row at {loggedClearedRowForChainFall}. Preparing for landing.");
                        StripBlocksAboveClearedRow();
                        playingState = PlayingState.Landing;

                        // Play line clear sound effect
                        audioManager.PlayClearSound();
                    }
                    else
                    {
                        Debug.Log("No rows cleared. Spawning new piece.");
                        playingState = PlayingState.Spawning;
                    }
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

    private bool IsGameOver()
    {
        // Check if any blocks have reached the stack height limit
        for (int col = 0; col < columns; col++)
        {
            if (gameBoard[stackHeightLimit, col] != 0)
            {
                Debug.Log($"Game Over condition met: block at row {stackHeightLimit}, column {col}");
                return true;
            }
        }
        return false;
    }

    private void OnGameOver()
    {
        // pause game and disable player input
        currentState = GameState.GameOver;
        // Show main menu UI, disable continue button
        continueButton.interactable = false;
        pauseMenuCanvas.alpha = 1f;
        pauseMenuCanvas.interactable = true;
        pauseMenuCanvas.blocksRaycasts = true;
        Debug.Log("Game Over! Implement game over logic here.");
        // make audio manager play game over music or sound effect
        audioManager.PlayGameOverSound();
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
            OnGameOver();
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
    private void MovePieceDown()
    {
        for (int i = 0; i < fallingPieces.Count; i++)
        {
            MovePieceDown(i);
        }
    }

    private void LandAllLandablePieces()
    {
        for (int i = fallingPieces.Count - 1; i >= 0; i--)
    {
        if (!IsPositionValid(fallingPieces[i], move_offset: Vector2Int.down))
        {
            LandPiece(i);
            fallingPieces.RemoveAt(i);
            Debug.Log("Piece landed and removed.");
        }
    }
    }

    // returns true if piece successfully moved down, false if it landed (used for both normal landing and chain falling)
    private void MovePieceDown(int piece_index)
    {
        ClearCurrentPiece(piece_index);
        
        Vector2Int newPosition = fallingPieces[piece_index].position + Vector2Int.down;
        fallingPieces[piece_index].position = newPosition;
        
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
    
    // clear the full row, and log the lowest cleared row, -1 if none cleared
    private void CheckForClearing()
    {
        bool anyRowsCleared = false;
        int linesCleared = 0;
        int lowestClearedRow = -1; // Track the lowest cleared row for guide block generation
        
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
                linesCleared++;
                // record the lowest and highest cleared rows for guide block generation
                if (lowestClearedRow == -1 || row < lowestClearedRow)
                {
                    lowestClearedRow = row;
                }
                
                // Clear the row
                for (int col = 0; col < columns; col++)
                {
                    gameBoard[row, col] = 0;
                }

                Debug.Log($"Row cleared at {row}");
                
                // Start from the same row since rows shifted down
                row--;
            }
        }

        // make all blocks above the highest cleared row a falling piece
        if (anyRowsCleared)
        {
            // log loggedClearedRowForChainFall
            loggedClearedRowForChainFall = lowestClearedRow;
            IncreaseScore(linesCleared);
        }
        else
        {
            loggedClearedRowForChainFall = -1; // reset if no rows cleared to avoid unintended chain fall
        }

    }

    private void StripBlocksAboveClearedRow()
    {
        if (loggedClearedRowForChainFall < 0)
        {
            Debug.Log("No cleared row logged for chain fall checking.");
            return;
        }
        loggedClearedRowForChainFall -= 1;

        // // Find the crack columns below the cleared row
        // List<int> crackColumn = new List<int>();
        // for (int col = 0; col < columns; col++)
        // {
        //     if (gameBoard[loggedClearedRowForChainFall, col] == 0)
        //     {
        //         crackColumn.Add(col);
        //         break;
        //     }
        // }
        // if (crackColumn.Count == 0) return;

        // go up from each crack column to find falling blocks
        for(int col = 0; col < columns; col++)
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

        
    }

    // Draws a strip of tiles below the gameover line to indicate where the piece will land if the player decides to land immediately. This is called in HandlePlayerInput() to update in real time as the player moves the piece around.
    private void DrawLandingEffect()
    {
        // useful variable: stackHeightLimit
        // we clear the strip first to avoid stacking landing effect tiles when the player moves the piece around, and we only draw the landing effect for the current piece (the first one in the fallingPieces list)
        BoundsInt area = new BoundsInt(
            new Vector3Int(tileOffset.x, tileOffset.y, 0), 
            new Vector3Int(columns, stackHeightLimit, 1)
            );
        TileBase[] emptyTiles = new TileBase[area.size.x * area.size.y];
        visualEffectTilemap.SetTilesBlock(area, emptyTiles);


        if (playingState == PlayingState.Moving && fallingPieces.Count > 0)
        {
            int piece_width = fallingPieces[0].shape.GetLength(1);
            int piece_column = fallingPieces[0].position.x;

            // draw the tile
            BoundsInt effect_area = new BoundsInt(
                new Vector3Int(tileOffset.x + piece_column, tileOffset.y, 0), 
                new Vector3Int(piece_width, stackHeightLimit, 1)
                );
            TileBase[] effectTiles = new TileBase[effect_area.size.x * effect_area.size.y];
            for (int i = 0; i < effectTiles.Length; i++)
            {
                effectTiles[i] = landingEffectTile;
            }
            visualEffectTilemap.SetTilesBlock(effect_area, effectTiles);
        }
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

    private void GenerateNewRow()
    {
        // Shift all rows up
        for (int row = rows - 1; row > 0; row--)
        {
            for (int col = 0; col < columns; col++)
            {
                gameBoard[row, col] = gameBoard[row - 1, col];
            }
        }
        
        // Generate new bottom row with crack
        int crackCol = newestCrackColumn + Random.Range(-1, 2);
        crackCol = Mathf.Clamp(crackCol, 0, columns - 1);
        int[] newRow = GenerateRowWithCrack(columns, crackCol, erosionChance: 0.2f);
        for (int col = 0; col < columns; col++)
        {
            gameBoard[0, col] = newRow[col];
        }
        
        newestCrackColumn = crackCol;
    }

    private void IncreaseScore(int linesCleared)
    {
        // Simple scoring: 100 points per line, with a bonus for multiple lines
        int points = linesCleared * 100;
        if (linesCleared > 1)
        {
            points += (linesCleared - 1) * 50; // Bonus for multiple lines
        }
        score += points;
        scoreText.text = "Score: " + score.ToString();

        // Check for high score
        if (score > highScore)
        {
            highScore = score;
            highScoreText.text = "High Score: " + highScore.ToString();
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
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
        // if player pressed tab but it is new game
        if (continueButton.interactable == false)
        {
            // the button is not interactable, meaning the player has not started a new game yet, so we ignore the pause input
            Debug.Log("Pause input ignored because continue button is not interactable (no game started)");
            return;
        }


        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            Debug.Log("Game Paused");
            // Show pause menu UI
            ToggleCanvasGroup(pauseMenuCanvas, true);
            audioManager.PauseBGM(); // Pause background music when paused
        }
        else if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Debug.Log("Game Resumed");
            ToggleCanvasGroup(pauseMenuCanvas, false);
            audioManager.ResumeBGM(); // Resume background music when unpaused
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
        ToggleCanvasGroup(pauseMenuCanvas, false);
    }

    public void OnContinueButton()
    {
        if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Debug.Log("Game Resumed from Continue Button");
            ToggleCanvasGroup(pauseMenuCanvas, false);
        }
    }

    public void OnExitButton()
    {
        Debug.Log("Exit Game");
        Application.Quit();
    }

    private void ToggleCanvasGroup(CanvasGroup canvasGroup, bool show)
    {
        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
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
    Spawning
}
