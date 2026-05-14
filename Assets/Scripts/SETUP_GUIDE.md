# Guide Blocks System Setup Guide

## Overview
This system adds a preview of upcoming blocks across the top of the game board. It shows the next 3 pieces that will spawn.

## Setup Steps

### 1. Create Tile Assets
You need at least 8 tile assets (including empty tile):
- Tile 0: Empty/Background tile
- Tiles 1-7: Different colored blocks for tetromino pieces

### 2. Scene Setup

#### Main Game Board:
1. Create a GameObject called "GameManager"
2. Add the `GameManager` script component
3. Create a Tilemap GameObject for the main board
4. Assign the Tilemap reference to GameManager's `tilemap` field
5. Create and assign the `tetrominoTiles` array (8 tiles)

#### Guide Blocks Board:
1. Create a new GameObject called "GuideBlocksManager"
2. Add the `GuideBlocksManager` script component
3. Create a separate Tilemap GameObject for guide blocks
4. Assign this Tilemap to GuideBlocksManager's `guideTilemap` field
5. Assign the same `tetrominoTiles` array to GuideBlocksManager
6. Drag the GuideBlocksManager GameObject to GameManager's `guideManager` field

### 3. Input System Setup
The system uses Unity's new Input System:
1. Create Input Actions asset if not already
2. Set up a 2D Vector action called "Move" with gamepad/arrow keys binding
3. Assign to GameManager's `move` field

### 4. Configure Settings

#### GameManager:
- `rows`: 30 (game board height)
- `columns`: 20 (game board width)
- `fallSpeed`: 1.0 (seconds between automatic drops)
- `tileOffset`: Adjust to position board in scene

#### GuideBlocksManager:
- `previewCount`: 3 (number of upcoming pieces to show)
- `guideBoardSize`: (5, 5) - size for each preview section
- `guideOffset`: (25, 15) - position guide board to the right of main board
- `tetrominoTiles`: Same array as GameManager

### 5. Testing
1. Play the scene
2. You should see:
   - Main board with initial cracked terrain
   - Guide board on the right showing 3 upcoming pieces
   - Current piece falling from top
3. Use arrow keys/gamepad to:
   - Left/Right: Move piece
   - Up: Rotate piece
   - Down: Quick drop

## How It Works

### Piece Generation:
1. GuideBlocksManager maintains a queue of upcoming pieces
2. When GameManager needs a new piece, it calls `GetNextPiece()`
3. This returns the next piece and generates a new one to maintain the preview count

### Guide Display:
- Each preview piece is centered in its section
- Pieces are drawn on a separate tilemap
- The guide board updates whenever a piece is taken

### Game Flow:
1. Piece spawns from guide queue
2. Player controls piece movement/rotation
3. Piece lands when it can't move down
4. Full rows are cleared
5. New piece spawns from guide queue

## Customization

### Changing Preview Count:
Modify `previewCount` in GuideBlocksManager. Higher values show more upcoming pieces.

### Changing Guide Position:
Adjust `guideOffset` to move the guide board. Positive X moves right, positive Y moves up.

### Adding New Piece Shapes:
Edit the `tetrominoShapes` list in GuideBlocksManager.cs to add custom shapes.

### Changing Controls:
Modify the `HandlePlayerInput()` method in GameManager.cs to use different input mappings.

## Troubleshooting

### No Guide Blocks Showing:
- Check that GuideBlocksManager has tilemap and tiles assigned
- Verify guideOffset positions guide board within camera view
- Check console for errors

### Pieces Not Spawning:
- Ensure GameManager has GuideBlocksManager reference
- Check that tetrominoTiles array has at least 8 tiles
- Verify spawn position is valid (not colliding with existing blocks)

### Input Not Working:
- Verify Input Actions are set up correctly
- Check that move action is enabled in GameManager's OnEnable method
- Test with different input devices