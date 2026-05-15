using UnityEngine;

[System.Serializable]
public class Tetromino
{
    public Vector2Int position;
    public int[,] shape;            // The shape contain index of the tiles for coloring
    public int tileIndex;       // The tile index for the current tetromino, used for mass coloring
    
    public Tetromino Clone()
    {
        Tetromino clone = new Tetromino();
        clone.position = this.position;
        clone.tileIndex = this.tileIndex;
        
        // Deep copy the shape array
        if (this.shape != null)
        {
            int rows = this.shape.GetLength(0);
            int cols = this.shape.GetLength(1);
            clone.shape = new int[rows, cols];
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    clone.shape[i, j] = this.shape[i, j];
                }
            }
        }
        
        return clone;
    }

    public void PaintTetromino()
    {
        if (shape == null) return;

        for (int i = 0; i < shape.GetLength(0); i++)
        {
            for (int j = 0; j < shape.GetLength(1); j++)
            {
                if (shape[i, j] != 0) // Assuming non-zero means a block is present
                {
                    shape[i, j] = tileIndex;
                }
            }
        }
    }
}