using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LevelGenerator : MonoBehaviour
{

    [Header("Level Generation Specifics")]
    public bool firstLevel; //keep public
    [SerializeField] private int thisLevel;

    [Header("Safe Zone Sprite")]
    [SerializeField] private Sprite safeSprite;

	[Header("Debug Sprites")]
    [SerializeField] private Sprite cellSprite;
	[SerializeField] private Sprite homeSprite;
	[SerializeField] private Sprite endSprite;
	[SerializeField] private Sprite dangerSprite;

	private GameManager manager;
    private Vector2 cellSize;
    private Vector2 cellScale;
    private GameObject cO;
    private List<int[]> possibleLevels = new List<int[]>();
    private RowBehavior.RowType lastRow;
    private Vector2 gridSize;
    private Vector2 gridOffset;
    private int r;
    private int[] chosenSetup;
	private bool oneCameBefore;
	private enum GenRow { home, end, safe, danger, dangerWithJelly };
    private GenRow[] chosenRows;

    [HideInInspector] public bool levelStarted = false;
    [HideInInspector] public int rows; //manager.tileAmount

	
    void Start()
    {
        manager = GameObject.FindObjectOfType<GameManager>();
        //tileSize = manager.tileSize;
        rows = manager.tileAmount;

        thisLevel = manager.levelsGenerated + 1;
        gridSize = new Vector2(manager.tileSize * manager.rowWidth, manager.tileSize * rows);

        SetUpLevels();
        InitCells(); //Initialize all cells
    }

    void SetUpLevels()
    {
        //"whitelist a catalog of known good content"
		//I would like to randomly generate these as well but for now this will do
        chosenRows = new GenRow[manager.tileAmount];
        int added = 0;

        int[] setup1 = new int[4] { 5, 1, 1, 5 };
        possibleLevels.Add(setup1);

        int[] setup2 = new int[5] { 4, 1, 3, 1, 3 };
        possibleLevels.Add(setup2);

        int[] setup3 = new int[5] { 2, 1, 5, 1, 3 };
        possibleLevels.Add(setup3);

        int[] setup4 = new int[5] { 4, 1, 2, 1, 4 };
        possibleLevels.Add(setup4);

        int[] setup5 = new int[7] { 2, 1, 3, 1, 2, 1, 2 };
        possibleLevels.Add(setup5);

        //chose a random setup from the list
        r = Random.Range(0, possibleLevels.Count);
        //Debug.Log("chosen setup: " + r);
        chosenSetup = possibleLevels[r];

        for (int i = 0; i < chosenSetup.Length; i++)
        {
            if (chosenSetup[i] > 1)
            {
                int n = chosenSetup[i];
                for (int j = 0; j < n; j++)
                {
                    if (j % 2 == 0)
                    {
                        chosenRows[j + added] = GenRow.danger;
                    }
                    else //jelly spawn frequency
                    {
                        int coinFlip = Random.Range(0, 2);
                        if (coinFlip == 1)
                        {
                            chosenRows[j + added] = GenRow.dangerWithJelly;
                        }
                        else
                        {
                            chosenRows[j + added] = GenRow.danger;
                        }
                    }
                }
                added = added + n;
            }
            else
            {
				chosenRows[added] = GenRow.safe;
                added++;
            }
        }
    }

    void InitCells()
    {

        //creates an empty object and adds a sprite renderer component -> set the sprite to cellSprite
        GameObject cellObject = new GameObject("Row");
        cellObject.AddComponent<SpriteRenderer>().sprite = cellSprite;

        //cache the size of the sprite
        cellSize = cellSprite.bounds.size;

        //get the new cell size -> adjust the size of the cells to fit the size of the grid
        Vector2 newTileSize = new Vector2(gridSize.x, gridSize.y / (float)rows);

        //Get the scales so you can scale the cells and change their size to fit the grid
        cellScale.x = newTileSize.x / cellSize.x;
        cellScale.y = newTileSize.y / cellSize.y;

        //replace the size with the newly calculated size
        cellSize = newTileSize;
        cellObject.transform.localScale = new Vector2(cellScale.x, cellScale.y);

        //fix the cells to the grid by getting the half of the grid and cells add and minus experiment
        gridOffset.x = -(gridSize.x / 2) + cellSize.x / 2;
        gridOffset.y = -(gridSize.y / 2) + cellSize.y / 2;

        //fill the grid with cells by using Instantiate
        //TODO: clean this up!
        for (int row = 0; row < rows; row++)
        {
            lastRow = RowBehavior.RowType.unknown;

            //add the cell size so that no two cells will have the same x and y position
            Vector2 pos = new Vector2(gridOffset.x + transform.position.x, row * cellSize.y + gridOffset.y + transform.position.y);

			////because quaternions...
			//Quaternion myQuat = Quaternion.identity;
			//Quaternion quatRotation = Quaternion.Euler(myQuat.eulerAngles.x - 25f, myQuat.eulerAngles.y, myQuat.eulerAngles.z);

            cO = Instantiate(cellObject, pos, Quaternion.identity) as GameObject;
            cO.AddComponent<BoxCollider2D>();

            if (row == 0) //first row
            {
				//always home row for odd levels
                if ((manager.levelsGenerated + 1) % 2 != 0)
                {
                    lastRow = MakeHomeRow();
                } 
                else //or end row for even levels
                {
                    lastRow = MakeEndRow();
                }
            }

            else if (row == (rows - 1)) //last row
            {
                //always end row for odd levels
                if ((manager.levelsGenerated + 1) % 2 != 0)
                {
                    lastRow = MakeEndRow();
                } 
                else //or home row for even levels
                {
                    lastRow = MakeHomeRow();
                }
            }

            else //all the rows in between home and end
            {
                if (chosenRows[row - 1] == GenRow.safe)
                {
                    lastRow = MakeSafeRow();
                }
                else if (chosenRows[row - 1] == GenRow.danger || chosenRows[row - 1] == GenRow.dangerWithJelly)
                {
                    lastRow = MakeDangerRow(row);
					cO.tag = "Row";
                }
            }

            //set the parent of the cell to GRID so you can move the cells together with the grid;
            cO.transform.parent = transform;

            cO.GetComponent<RowBehavior>().myLevel = manager.levelsGenerated + 1;
			//cO.GetComponent<SpriteRenderer>().sprite = null;

		}

		//destroy the object used to instantiate the cells
		Destroy(cellObject);

        manager.levelsGenerated++;
        manager.generatedLevels.Add(this);

    }

	//jellies or no jellies decided here
	//progressive dificulty is handled here
    private RowBehavior.RowType MakeDangerRow(int row)
    {
		cO.GetComponent<SpriteRenderer>().sprite = dangerSprite;
		cO.AddComponent<RowBehavior>().myRowType = RowBehavior.RowType.danger;


		if (thisLevel == 1)
		{
			//blowfish every other row
			if (!oneCameBefore)
			{
				cO.GetComponent<RowBehavior>().freeRow = true;
				oneCameBefore = true;
			}
			else
			{
				cO.GetComponent<RowBehavior>().freeRow = false;
				oneCameBefore = false;
			}
		}

		//int chance = FindObjectOfType<GameManager>().freeRowChance;
		//int pickle = Random.Range(1, 101);
		//if (pickle > chance)
		//{
		//	cO.GetComponent<RowBehavior>().freeRow = true;
		//}

		if (chosenRows[row - 1] == GenRow.dangerWithJelly && thisLevel >= 5)
        {
            cO.GetComponent<RowBehavior>().jellies = true;
        }

        cO.layer = 8;
		cO.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
		lastRow = RowBehavior.RowType.danger;
        return lastRow;
    }

    private RowBehavior.RowType MakeSafeRow()
    {
		oneCameBefore = true;
        cO.GetComponent<SpriteRenderer>().sprite = safeSprite;
        cO.AddComponent<RowBehavior>().myRowType = RowBehavior.RowType.safe;
		cO.GetComponent<BoxCollider2D>().isTrigger = true;
		cO.layer = 11;
		cO.tag = "Safe Row";
        lastRow = RowBehavior.RowType.safe;
		return lastRow;
    }

    private RowBehavior.RowType MakeEndRow()
    {
        cO.GetComponent<SpriteRenderer>().sprite = endSprite;
        cO.AddComponent<RowBehavior>().myRowType = RowBehavior.RowType.end;
        cO.GetComponent<BoxCollider2D>().isTrigger = true;
        cO.layer = 15;
        cO.tag = "End Row";
        lastRow = RowBehavior.RowType.end;
		cO.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
		return lastRow;
    }

    private RowBehavior.RowType MakeHomeRow()
    {
		oneCameBefore = true;
        cO.GetComponent<SpriteRenderer>().sprite = homeSprite;
        cO.AddComponent<RowBehavior>().myRowType = RowBehavior.RowType.home;
		cO.GetComponent<BoxCollider2D>().isTrigger = true;
		cO.layer = 18;
        cO.tag = "Home Row";
        lastRow = RowBehavior.RowType.home;
		cO.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
		return lastRow;
    }

    //private void OnBecameInvisible()
    //{
    //    Destroy(gameObject, 3f);
    //}

    private void OnTriggerExit2D(Collider2D collision)
    {
      if(collision.tag == "Offscreen Checker")
        {
            Destroy(gameObject, 0);
        }   
    }


}
