# Guide Blocks System Implementation Summary

## What Was Implemented

### 1. GuideBlocksManager.cs
- **Purpose**: Manages preview of upcoming blocks
- **Features**:
  - Shows configurable number of upcoming pieces (default: 3)
  - Generates random tetromino pieces (7 standard shapes)
  - Draws preview on separate tilemap
  - Centers each piece in its preview section
  - Maintains queue of upcoming pieces

### 2. Enhanced GameManager.cs
- **New Features**:
  - Integrated with guide system
  - Falling piece mechanics with gravity
  - Player controls (move left/right, rotate, quick drop)
  - Collision detection
  - Piece landing and board updating
  - Row clearing mechanics
  - Game state management

### 3. Enhanced Tetromino.cs
- Added Clone() method for proper copying
- Made class serializable

### 4. Support Files
- **SETUP_GUIDE.md**: Complete setup instructions
- **TestGuideSystem.cs**: Testing/debugging script
- **Editor/GuideSystemSetupHelper.cs**: Unity editor helper window

## How It Works

### Guide System Flow:
1. GuideBlocksManager generates 3 random pieces on start
2. Pieces are displayed in guide area (top/right of main board)
3. When GameManager needs a new piece, it calls `GetNextPiece()`
4. GuideBlocksManager returns first piece and generates new one
5. Guide display updates automatically

### Game Flow:
1. Piece spawns from guide queue at top center
2. Piece falls automatically (configurable speed)
3. Player can move/rotate piece
4. Piece lands when it hits bottom or other blocks
5. Full rows are cleared
6. New piece spawns from guide queue

## Key Configuration Options

### GuideBlocksManager:
- `previewCount`: How many pieces to show (default: 3)
- `guideBoardSize`: Size of each preview section (default: 5x5)
- `guideOffset`: Position of guide board (default: 25,15)

### GameManager:
- `fallSpeed`: Seconds between automatic drops (default: 1.0)
- `rows`/`columns`: Game board size (default: 30x20)

## Setup Requirements

1. **Tile Assets**: Need 8 tile assets (1 empty + 7 colored blocks)
2. **Tilemaps**: Two tilemaps needed (main board + guide board)
3. **Input System**: Unity's new Input System with 2D Vector action
4. **Scene Objects**: GameManager and GuideBlocksManager GameObjects

## Testing Commands

In Play Mode:
- **Arrow Keys**: Move piece (Left/Right/Up=Rotate/Down=Quick Drop)
- **T Key**: Debug guide system (shows upcoming pieces in console)
- **G Key**: Manually get next piece (for testing)

## Benefits

1. **Player Preview**: Players can see upcoming pieces and plan ahead
2. **Modular Design**: Guide system is separate from core game logic
3. **Configurable**: Easy to adjust preview count, position, etc.
4. **Extensible**: Easy to add new piece shapes or modify behavior

## Next Steps (For You)

1. Create tile assets (8 different tiles)
2. Set up tilemaps in Unity scene
3. Configure Input System actions
4. Adjust positions/sizes for your specific game layout
5. Test and tweak gameplay balance (fall speed, piece generation, etc.)

The system is designed to be flexible - you can easily modify piece shapes, colors, preview count, or guide position to match your game's aesthetic and gameplay needs.