using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckerBaord : MonoBehaviour
{
    public static CheckerBaord Instance { set; get; }

    public Piece[,] pieces = new Piece[8, 8];
    public GameObject redPiecePrefab;
    public GameObject blackPiecePrefab;

    private Vector3 boardOffset = new Vector3(-4.0f, 0, -4.0f);
    private Vector3 pieceOffset = new Vector3(0.5f, 0, 0.5f);

    public bool isRed;
    private bool isRedTurn;
    private bool hasKilled;

    private Piece selectedPiece;
    private List<Piece> forcedPieces;

    private Vector2 mouseOver;
    private Vector2 startDrag;
    private Vector2 endDrag;

    private Client client;

	private void Start(){
        Instance = this;
        client = FindObjectOfType<Client>();
        isRed = client.isHost;

        forcedPieces = new List<Piece>();
        isRedTurn = true;
        GenerateBoard();
    }

    private void Update(){
        UpdateMouseOver();
        if ((isRed) ? isRedTurn : !isRedTurn){
            int x = (int)mouseOver.x;
            int y = (int)mouseOver.y;

            if(selectedPiece != null){
                UpdatePieceDrag(selectedPiece);
            }

            if(Input.GetMouseButtonDown(0)){
                SelectPiece(x, y);
            }

            if(Input.GetMouseButtonUp(0)){
                TryMove((int)startDrag.x, (int)startDrag.y, x, y);
            }
        }
    }

    private void UpdateMouseOver(){
        if(!Camera.main){
            Debug.Log("Unable to find main camera");
            return;
        }

        RaycastHit hit;
        if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board"))){
            mouseOver.x = (int)(hit.point.x - boardOffset.x);
            mouseOver.y = (int)(hit.point.z - boardOffset.z) ;
        }
        else{
            mouseOver.x = -1;
            mouseOver.y = -1;
        }
    }

	private void UpdatePieceDrag(Piece p)
	{
		RaycastHit hit;
		if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
		{
			p.transform.position = hit.point + Vector3.up;
		}
	}

	private void SelectPiece(int x, int y)
	{
		//Out of Bounds
		if (x < 0 || x >= 8 || y < 0 || y >= 8)
		{
			return;
		}

		Piece p = pieces[x, y];
		if (p != null && p.isRed == isRed)
		{
            if (forcedPieces.Count == 0)
			{
				selectedPiece = p;
				startDrag = mouseOver;
			}
			else
			{
				//Look for the piece under our forced pieces list
				if (forcedPieces.Find(fp => fp == p) == null)
				{
					return;
				}
				selectedPiece = p;
				startDrag = mouseOver;
			}
		}
	}

	public void TryMove(int x1, int y1, int x2, int y2)
	{
        forcedPieces = ScanForPossibleMove();

		startDrag = new Vector2(x1, y1);
		endDrag = new Vector2(x2, y2);
		selectedPiece = pieces[x1, y1];

		//Out of Bounds
		if (x2 < 0 || x2 > 8 || y2 < 0 || y2 >= 8)
		{
			if (selectedPiece != null)
			{
				MovePiece(selectedPiece, x1, y1);
			}
			startDrag = Vector2.zero;
			selectedPiece = null;
			return;
		}

		if (selectedPiece != null)
		{
			//If it hasn't been moved
			if (endDrag == startDrag)
			{
				MovePiece(selectedPiece, x1, y1);
				startDrag = Vector2.zero;
				selectedPiece = null;
				return;
			}

			//Check if Valid Move
			if (selectedPiece.ValidMove(pieces, x1, y1, x2, y2))
			{
				//Did we kill anything
				//If this this is a jump
				if (Mathf.Abs(x2 - x1) == 2)
				{
					Piece p = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
					if (p != null)
					{
						pieces[(x1 + x2) / 2, (y1 + y2) / 2] = null;
						DestroyImmediate(p.gameObject);
                        hasKilled = true;
					}
				}

				//Were we supposed to kill anything?
                if (forcedPieces.Count != 0 && !hasKilled)
				{
					MovePiece(selectedPiece, x1, y1);
					startDrag = Vector2.zero;
					selectedPiece = null;
                    return;
				}

				pieces[x2, y2] = selectedPiece;
				pieces[x1, y1] = null;
				MovePiece(selectedPiece, x2, y2);

				EndTurn();
			}
			else
			{
				//If the move isn't valid, set the piece to the original position
				MovePiece(selectedPiece, x1, y1);
				startDrag = Vector2.zero;
				selectedPiece = null;
				return;
			}
		}
	}


	private void EndTurn(){
		int x = (int)endDrag.x;
		int y = (int)endDrag.y;

		//Promotion
		if (selectedPiece != null)
		{
			if (selectedPiece.isRed && !selectedPiece.isKing && y == 7)
			{
				selectedPiece.isKing = true;

				//'King me' effect is the backside of the checker piece (flips 180)
				selectedPiece.transform.Rotate(Vector3.right * 180);
			}
			else if (!selectedPiece.isRed && !selectedPiece.isKing && y == 0)
			{
				selectedPiece.isKing = true;

				//'King me' effect is the backside of the checker piece (flips 180)
				selectedPiece.transform.Rotate(Vector3.right * 180);
			}
		}

		//Client Move
		string msg = "CMOV|";
		msg += startDrag.x.ToString() + "|";
		msg += startDrag.y.ToString() + "|";
		msg += endDrag.x.ToString() + "|";
		msg += endDrag.y.ToString();

		client.Send(msg);

		selectedPiece = null;
		startDrag = Vector2.zero;

		if (ScanForPossibleMove(selectedPiece, x, y).Count != 0 && hasKilled)
		{
			return;
		}

		isRedTurn = !isRedTurn;
        hasKilled = false;
		CheckVictory();
    }

    private void CheckVictory(){
		var ps = FindObjectsOfType<Piece>();
		bool hasRed = false, hasBlack = false;
		for (int i = 0; i < ps.Length; i++)
		{
			if (ps[i].isRed)
			{
				hasRed = true;
			}
			else
			{
				hasBlack = true;
			}
		}

		if (!hasRed)
		{
			Victory(false);
		}
		if (!hasBlack)
		{
			Victory(true);
		}
    }

    private void Victory(bool isRed){
        if(isRed){
            Debug.Log("Red Wins");
        }
        else{
            Debug.Log("Black Wins");
        }
    }

	//Overloaded ScanForPossibleMove Function
	private List<Piece> ScanForPossibleMove(Piece p, int x, int y)
	{
        forcedPieces = new List<Piece>();
		if (pieces[x, y].IsForcedToMove(pieces, x, y))
		{
            forcedPieces.Add(pieces[x, y]);
		}
        return forcedPieces;
	}

	private List<Piece> ScanForPossibleMove()
	{
		forcedPieces = new List<Piece>();

		//Check all the pieces
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				if (pieces[i, j] != null && pieces[i, j].isRed == isRedTurn)
				{
                    if (pieces[i, j].IsForcedToMove(pieces, i, j))
					{
						forcedPieces.Add(pieces[i, j]);
					}
				}
			}
		}
		return forcedPieces;
	}

	public void GenerateBoard()
	{
		//Generate Red Team
		for (int y = 0; y < 3; y++)
		{
			bool oddRow = (y % 2 == 0);
			for (int x = 0; x < 8; x += 2)
			{
				GeneratePiece((oddRow) ? x : x + 1, y);   //Iff odd row, send x,y otherwise x+1,y
			}
		}

		//Generate Black Team
		for (int y = 7; y > 4; y--)
		{
			bool oddRow = (y % 2 == 0);
			for (int x = 0; x < 8; x += 2)
			{
				GeneratePiece((oddRow) ? x : x + 1, y);   //Iff odd row, send x,y otherwise x+1,y
			}
		}
	}

	public void GeneratePiece(int x, int y)
	{
		bool isPieceRed = (y > 3) ? false : true;       //If y > 3 the piece is black
		GameObject go = Instantiate((isPieceRed) ? redPiecePrefab : blackPiecePrefab) as GameObject;
		go.transform.SetParent(transform);
		Piece p = go.GetComponent<Piece>();
		pieces[x, y] = p;
		MovePiece(p, x, y);
	}

	private void MovePiece(Piece p, int x, int y)
	{
		p.transform.position = (Vector3.right * x) + (Vector3.forward * y) + boardOffset + pieceOffset;
	}
}
