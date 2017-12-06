using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
	//set in inspector
	public GameObject player1;
	public GameObject player2;
	public List<GameObject> wallColliders = new List<GameObject>();

	[Header("Menu Config")]
	public bool thisIsTheMainMenu;
	public GameObject deathMenu;
	public GameObject escapeMenu;
	public Text ready;
	public Text go;
	public GameObject rsgbg;

	[Header("Sound Clips")]
	public AudioSource nextLvlBubble;
	public AudioSource goSound;
	public AudioSource loseSound;
	public AudioSource p1Chomp;
	public AudioSource p2Chomp;
	public AudioSource buttonMove;
	public AudioSource buttonSelect;
	public AudioSource menuOpen;
	public AudioSource p2AddedSound;


	//grid size settings
	[HideInInspector] public int tileAmount = 14; //must be EVEN
	[HideInInspector] public int rowWidth = 11; //must be ODD

	[HideInInspector] public int levelsGenerated = 0;
	[HideInInspector] public int levelsEntered = 1;
	[HideInInspector] public int score1 = 0;
	[HideInInspector] public int score2 = 0;
	[HideInInspector] public int freeRowChance = 10;
	[HideInInspector] public float oneScreenUnit;
	[HideInInspector] public float tileSize;
	[HideInInspector] public float lvlGenStart;
	[HideInInspector] public float cameraPan;
	[HideInInspector] public bool fishGone = false;
	[HideInInspector] public bool immortalityOn = false;
	[HideInInspector] public bool gameOver = false;
	[HideInInspector] public bool isPaused = false;
	[HideInInspector] public bool camIsMoving = false;
    [HideInInspector] public bool twoPlayerMode = false;
	[HideInInspector] public bool wasTwoPlayers = false;
	[HideInInspector] public bool gameStarted = false;

	[Header("Debug")]
	public bool oneReachedEnd = false;
	public bool bothReachedEnd = false;
	public bool oneDied = false;

	[HideInInspector] public Text lvlCounterText;
	[HideInInspector] public Text scoreText;
	[HideInInspector] public Text timerText;
	[HideInInspector] public Vector3 startSpot;
	[HideInInspector] public List<LevelGenerator> generatedLevels = new List<LevelGenerator>();

	private int screenWidth;
	private int lvlsThisUpdate;
	private int levelsGeneratedCount = 1;
	private float startTime;
	private bool noLvlTwo = true;
	private bool noOffscreenEast = true;
	private bool gotReady = false;
	private GameObject firstLevel;
	private GameObject mostRecentLvl;
	private GameObject myCoral;
	private Vector3 lvlTwoPos;
	private Vector3 camTarget;
	private Vector3 playerTarget1;
	private Vector3 playerTarget2;
	private Vector3 offscreenLvlDiff;
	private Vector3 coralDist = new Vector3(14.73f, 0.91f, 0);
	private Vector3 coralDistAdj = new Vector3(14.73f, -0.91f, 0);
	private LevelGenerator newLevel;
	private RowBehavior newHome;
	private Camera cam;


	void Start()
	{

		//start the timer
		startTime = Time.time;

		//convert pixels to stupid unity units
		cam = Camera.main;
		var p1 = cam.ScreenToWorldPoint(Vector2.zero);
		var p2 = cam.ScreenToWorldPoint(Vector2.right);
		p1.z = 0;
		p2.z = 0;
		oneScreenUnit = Vector2.Distance(p1, p2);

		//the size of each "tile" based on the tile amount number given in the inspector
		if (!thisIsTheMainMenu)
		{
			tileSize = oneScreenUnit * (Screen.height / tileAmount);
		}

		//move self to bottom of level generator, level 1
		//because player gets its start location from here (fix this eventually?)
		firstLevel = GameObject.FindGameObjectWithTag("First Level");
		transform.SetParent(firstLevel.transform);
		float movedY = transform.parent.position.y - (tileSize * ((tileAmount + 2) / 2));
		movedY = movedY + tileSize / 2;
		transform.position = new Vector3(transform.parent.position.x, movedY, 0);
		startSpot = transform.position;

		//camera nonsense
		screenWidth = Screen.width;
		cameraPan = (screenWidth / 2) * oneScreenUnit;
	}

	private void Update()
	{
		if (!gotReady)
		{
			StartCoroutine(ReadySetGo());
		}

		////restart with R
		////DISABLE FOR FINAL RELEASE!!!!!
		//if (Input.GetKeyDown(KeyCode.R))
		//{
		//	SceneManager.LoadScene(1);
		//}

		////immortality on/off with I
		////DISABLE FOR FINAL RELEASE!!!!!
		//if (Input.GetKeyDown(KeyCode.I))
		//{
		//	immortalityOn = !immortalityOn;
		//}

		//if level 1 is generated but not level 2
		if (levelsGenerated == 1 && noLvlTwo)
		{
			lvlTwoPos = new Vector3((screenWidth * oneScreenUnit) / 2, firstLevel.transform.position.y);
			mostRecentLvl = Instantiate(Resources.Load<GameObject>("Prefabs/Level Grid"), lvlTwoPos, Quaternion.identity);
			noLvlTwo = false;
			offscreenLvlDiff = mostRecentLvl.transform.position - firstLevel.transform.position;

			Vector3 myCoralPos = lvlTwoPos + coralDistAdj;
			myCoral = Instantiate(Resources.Load<GameObject>("Prefabs/Coral Tower"), myCoralPos, Quaternion.AngleAxis(180, Vector3.left));
			myCoral.transform.parent = mostRecentLvl.transform;
		}

		//this part puts the "procedural" in procedural generation
		//detect if another level needs to be generated
		if (lvlsThisUpdate < levelsGenerated && noOffscreenEast)
		{
			//GameObject lastMostRecent = mostRecentLvl;
			mostRecentLvl = Instantiate(Resources.Load<GameObject>("Prefabs/Level Grid"), (mostRecentLvl.transform.position + offscreenLvlDiff), Quaternion.identity);
			noOffscreenEast = false;

			if ((levelsGenerated % 2 != 0) && levelsGenerated > 1)
			{
				Vector3 myCoralPos = mostRecentLvl.transform.position + coralDistAdj;
				myCoral = Instantiate(Resources.Load<GameObject>("Prefabs/Coral Tower"), myCoralPos, Quaternion.AngleAxis(180, Vector3.left));
				myCoral.transform.parent = mostRecentLvl.transform;
			}
			else
			{
				Vector3 myCoralPos = mostRecentLvl.transform.position + coralDist;
				myCoral = Instantiate(Resources.Load<GameObject>("Prefabs/Coral Tower"), myCoralPos, Quaternion.identity);
				myCoral.transform.parent = mostRecentLvl.transform;
			}
		}

		//update UI and check for quitters
		if (!gameOver && gameStarted)
		{
			if (timerText != null)
			{
				//timer
				float t = Time.time - startTime;
				string minutes = Mathf.Floor(t / 60).ToString("00");
				string seconds = Mathf.Floor(t % 60).ToString("00");
				timerText.text = minutes + ":" + seconds;
			}
			if (scoreText != null)
			{
				//score
				scoreText.text = score1.ToString("d4");
			}
			if (lvlCounterText != null)
			{
				//level counter
				lvlCounterText.text = "Level: " + levelsEntered.ToString();
			}

			if (Input.GetButtonDown("Escape") && escapeMenu.activeInHierarchy == false)
			{
				escapeMenu.SetActive(true);
				menuOpen.Play();
				EventSystem es = GameObject.Find("EventSystem").GetComponent<EventSystem>();
				es.SetSelectedGameObject(null);
				es.SetSelectedGameObject(GameObject.FindGameObjectWithTag("EscResume"));
				PauseGame();
			}
		}

		if (gameOver && deathMenu.activeInHierarchy == false)
		{
			deathMenu.SetActive(true);
			loseSound.Play();
		}

		//cache how many levels have been generated since last update
		//this tells it if it needs to make a new one
		lvlsThisUpdate = levelsGeneratedCount;

	}

	//when a player eats all the prize fish,
	//TODO: or if they get to the end row and there are none
	public void TransitionSequence()
	{
		player1.GetComponent<PlayerMove>().UnRotateIfNeeded();
		player2.GetComponent<PlayerMove>().UnRotateIfNeeded();

		player1.GetComponent<SpriteRenderer>().flipX = true;
		player2.GetComponent<SpriteRenderer>().flipX = true;

		player1.GetComponent<PlayerMove>().reachedEnd = false;
		player2.GetComponent<PlayerMove>().reachedEnd = false;

		player1.GetComponent<Collider2D>().enabled = false;
		player2.GetComponent<Collider2D>().enabled = false;

		//TODO: level bonus points
		levelsEntered++;
		nextLvlBubble.Play();

		//reset fishGone bool
		fishGone = false;

		//find the new level and set it as parent of this + players
		newLevel = generatedLevels[levelsEntered - 1];
		player1.transform.parent = newLevel.transform;
		player2.transform.parent = newLevel.transform;
		transform.parent = newLevel.transform;

		//disable walls
		foreach (GameObject wall in wallColliders)
		{
			wall.gameObject.SetActive(false);
		}

		//pan camera and move player
		Transform cam = Camera.main.transform;
		camTarget = new Vector3(cam.position.x + (cameraPan), cam.position.y, cam.position.z);
		newHome = FindHome();
		playerTarget1 = newHome.transform.position + new Vector3(tileSize * -2, 0, 0); //subtract 3 tiles
		playerTarget2 = newHome.transform.position + new Vector3(tileSize * 2, 0, 0); //add 3 tiles
		PanCam();

		//activate new prize fish
		newLevel.levelStarted = true;

		//generate new level
		noOffscreenEast = true;
		levelsGeneratedCount++;

		if (twoPlayerMode && oneDied)
		{
			twoPlayerMode = false;
		}
	}

	public RowBehavior FindHome()
	{
		//gets an array of all the rows in this level and returns the home row (since there's only one)
		RowBehavior[] testingRows = transform.parent.transform.GetComponentsInChildren<RowBehavior>();
		for (int i = 0; i < testingRows.Length; i++)
		{
			if (testingRows[i].myRowType == RowBehavior.RowType.home)
			{
				return testingRows[i];
			}
		}
		return null;
	}

	public void PanCam()
	{
		Transform cam = Camera.main.transform;
		Transform you1 = player1.transform;
		Transform you2 = player2.transform;
		camIsMoving = true;
		StartCoroutine(LerpToPosition(3f, cam.position, you1.position, you2.position));

		//while this is happening the enemies should be frozen in place
		//AND spawners should pause production!
	}

	//pan camera and player at the same time
	IEnumerator LerpToPosition(float lerpSpeed, Vector3 camStartingPosition, Vector3 pSP1, Vector3 pSP2)
	{
		float t = 0.0f;
		while (t < 1.0f)
		{
			t += Time.deltaTime * (Time.timeScale / lerpSpeed);

			Camera.main.transform.position = Vector3.Lerp(camStartingPosition, camTarget, t);
			if (!player1.GetComponent<PlayerMove>().isDead)
			{
				player1.transform.position = Vector3.Lerp(pSP1, playerTarget1, t);
			}
			if (!player2.GetComponent<PlayerMove>().isDead)
			{
				player2.transform.position = Vector3.Lerp(pSP2, playerTarget2, t);
			}

			if (t > 1.0f && camIsMoving)
			{
				camIsMoving = false;
				Camera.main.GetComponent<CamShake>().MoveStartPosition();

				//re-enable walls
				foreach (GameObject wall in wallColliders)
				{
					wall.gameObject.SetActive(true);
				}

				player1.GetComponent<Collider2D>().enabled = true;
				player2.GetComponent<Collider2D>().enabled = true;

				oneReachedEnd = false;
				bothReachedEnd = false;
				player1.GetComponent<PlayerMove>().reachedEnd = false;
				player2.GetComponent<PlayerMove>().reachedEnd = false;

			}

			yield return 0;
		}

	}

	void PauseGame()
	{
		isPaused = true;
		Time.timeScale = 0;
		player1.GetComponent<PlayerMove>().canMove = false;
		player2.GetComponent<PlayerMove>().canMove = false;
	}

	public void UnpauseGame()
	{
		isPaused = false;
		Time.timeScale = 1;
		player1.GetComponent<PlayerMove>().canMove = true;
		player2.GetComponent<PlayerMove>().canMove = true;
	}

	IEnumerator Example()
	{
		yield return StartCoroutine("ReadySetGo");
		print("Also after 2 seconds");
		print("This is after the Do coroutine has finished execution");
	}

	IEnumerator ReadySetGo()
	{
		gotReady = true;
		p2AddedSound.Play();
		yield return new WaitForSeconds(2f);
		ready.gameObject.SetActive(false);
		go.gameObject.SetActive(true);
		goSound.Play();
		yield return new WaitForSeconds(1f);
		rsgbg.gameObject.SetActive(false);
		gameStarted = true;
		startTime = Time.time;
		yield return null;
	}

}
