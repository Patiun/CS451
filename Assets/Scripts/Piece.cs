using UnityEngine;
using System.Collections;

public class Piece : MonoBehaviour
{
	public bool isRed;
	public bool isKing;

	//Supports the rule that you have to capture if possible
	public bool IsForcedToMove(Piece[,] board, int x, int y)
	{
		if (isRed || isKing)
		{
			//Top Left
			if (x >= 2 && y <= 5)
			{
				Piece p = board[x - 1, y + 1];
				if (p != null && p.isRed != isRed)
				{
					//Check if it's possible to land after the jump
					if (board[x - 2, y + 2] == null)
					{
						return true;
					}
				}
			}
			//Top Right
			if (x <= 5 && y <= 5)
			{
				Piece p = board[x + 1, y + 1];
				if (p != null && p.isRed != isRed)
				{
					//Check if it's possible to land after the jump
					if (board[x + 2, y + 2] == null)
					{
						return true;
					}
				}
			}
		}

		if (!isRed || isKing)
		{
			//Bottom Left
			if (x >= 2 && y >= 2)
			{
				Piece p = board[x - 1, y - 1];
				if (p != null && p.isRed != isRed)
				{
					//Check if it's possible to land after the jump
					if (board[x - 2, y - 2] == null)
					{
						return true;
					}
				}
			}
			//Bottom Right
			if (x <= 5 && y >= 2)
			{
				Piece p = board[x + 1, y - 1];
				if (p != null && p.isRed != isRed)
				{
					//Check if it's possible to land after the jump
					if (board[x + 2, y - 2] == null)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public bool ValidMove(Piece[,] board, int x1, int y1, int x2, int y2)
	{
		//If you are moving on top of another piece
		if (board[x2, y2] != null)
		{
			return false;
		}

		int deltaMove = Mathf.Abs(x1 - x2);
		int deltaMoveY = y2 - y1;
		if (isRed || isKing)
		{
			if (deltaMove == 1)
			{
				if (deltaMoveY == 1)
				{
					return true;
				}
			}
			else if (deltaMove == 2)
			{
				if (deltaMoveY == 2)
				{
					Piece p = board[(x1 + x2) / 2, (y1 + y2) / 2];
					if (p != null && p.isRed != isRed)
					{
						return true;
					}
				}
			}
		}

		if (!isRed || isKing)
		{
			if (deltaMove == 1)
			{
				if (deltaMoveY == -1)
				{
					return true;
				}
			}
			else if (deltaMove == 2)
			{
				if (deltaMoveY == -2)
				{
					Piece p = board[(x1 + x2) / 2, (y1 + y2) / 2];
					if (p != null && p.isRed != isRed)
					{
						return true;
					}
				}
			}
		}
		return false;
	}
}