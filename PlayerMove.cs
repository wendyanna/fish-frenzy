using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class PlayerMove : MonoBehaviour
{
	private enum Orientation
	{
		Horizontal, Vertical
	};

	public Sprite bellyUpSprite;

	[Header("Player Properties")]
	public LayerMask safeMask; //assigned in inspector
	public int lives = 3; //play with this number maybe?
	[SerializeField] private float moveSpeed = 3f;
	public bool isPlayerTwo;

	[Header("Sound Effects")]
	public AudioSource wallBounce;
	public AudioSource hitByBlowfish;
	public AudioSource hitByShark;
	public AudioSource hitByJellyfish;
	public AudioSource bellyUp;

	private float gridSize = 1f;
	private float t;
	private float factor;
	private bool allowDiagonals = true;
	private bool correctDiagonalSpeed = true;
	private bool playerIsMoving = false;
	private bool cool = true;
	//private bool stayed = false;
	private Vector2 input;
	private Vector3 startPosition;
	private Vector3 endPosition;
	private Vector3 posCache;
	private GameManager manager;
	//private RaycastHit2D safety;
	private Animator animator;
	private Quaternion noRotation;
	private Orientation gridOrientation = Orientation.Vertical;
	private GameObject checkpoint;
	private List<GameObject> checkedPoints = new List<GameObject>();

	[HideInInspector] public bool isDead = false;
	[HideInInspector] public bool reachedEnd = false;
	[HideInInspector] public Camera mainCamera;
	[HideInInspector] public bool canMove = true;

	private void Start()
	{
		GameObject firstLevel = GameObject.FindGameObjectWithTag("First Level");
		transform.SetParent(firstLevel.transform);

		animator = GetComponent<Animator>();
		manager = GameObject.FindObjectOfType<GameManager>();
		mainCamera = Camera.main;
		//originalCameraPosition = mainCamera.transform.position;
		gridSize = manager.tileSize;
		//transform.position = new Vector3(manager.startSpot.x, manager.startSpot.y + manager.tileSize, 0);
		noRotation = transform.rotation;
	}

	public void Update()
	{
		if (manager.twoPlayerMode)
		{
			if (manager.bothReachedEnd == true && manager.fishGone)
			{
				if (!manager.gameOver)
				{
					manager.TransitionSequence();
				}
			}
		}
		else
		{
			if (manager.wasTwoPlayers)
			{
				if (manager.oneReachedEnd && manager.fishGone)
				{
					if (!manager.gameOver)
					{
						manager.TransitionSequence();
					}
				}
			}
			else
			{
				if(reachedEnd && manager.fishGone)
				{
					manager.TransitionSequence();
				}
			}
		}

		//move...
		if (lives != 0)
		{
			if (!playerIsMoving && cool && !manager.camIsMoving)
			{
				if (!isPlayerTwo)
				{
					input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
				}
				else
				{
					input = new Vector2(Input.GetAxis("P2 Horizontal"), Input.GetAxis("P2 Vertical"));
				}
				if (!allowDiagonals)
				{
					if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
					{
						input.y = 0;
					}
					else
					{
						input.x = 0;
					}
				}

				if (input != Vector2.zero)
				{
					RotateUpOrDown();

					if (input.x > 0)
					{
						if (GetComponent<SpriteRenderer>().flipX == false)
						{
							GetComponent<SpriteRenderer>().flipX = true;
							RotateUpOrDown();
						}
					}
					else if (input.x < 0)
					{
						if (GetComponent<SpriteRenderer>().flipX == true)
						{
							GetComponent<SpriteRenderer>().flipX = false;
							RotateUpOrDown();
						}
					}
					if (manager.gameStarted)
					{
						playerIsMoving = true;
						StartCoroutine(Move(transform));
					}
				}
			}

			if (input.y == 0)
			{
				UnRotateIfNeeded();
			}

		}
		//...or die
		else
		{
			if (!isDead)
			//you are dead
			{
				Die();
			}
		}

	}

	public IEnumerator Move(Transform transform)
	{
		startPosition = transform.position;
		t = 0;

		if (gridOrientation == Orientation.Horizontal)
		{
			endPosition = new Vector3(startPosition.x + System.Math.Sign(input.x) * gridSize,
				startPosition.y, startPosition.z + System.Math.Sign(input.y) * gridSize);
		}
		else
		{
			endPosition = new Vector3(startPosition.x + System.Math.Sign(input.x) * gridSize,
				startPosition.y + System.Math.Sign(input.y) * gridSize, startPosition.z);
		}

		if (allowDiagonals && correctDiagonalSpeed && input.x != 0 && input.y != 0)
		{
			factor = 0.7071f;
		}
		else
		{
			factor = 1f;
		}

		while (t < 1f && playerIsMoving)
		{
			t += Time.deltaTime * (moveSpeed / gridSize) * factor;
			transform.position = Vector3.Lerp(startPosition, endPosition, t);
			yield return null;
		}

		playerIsMoving = false;
		yield return 0;
	}

	private void Die()
	{
		bellyUp.Play();
		isDead = true;
		animator.SetTrigger("Die");
		GetComponent<SpriteRenderer>().sprite = bellyUpSprite;
		GetComponent<Collider2D>().enabled = false;
		if (manager.twoPlayerMode == true)
		{
			if (!manager.oneDied)
			{
				manager.oneDied = true;
				if (!manager.oneReachedEnd)
				{
					manager.oneReachedEnd = true;
				}
				else
				{
					manager.bothReachedEnd = true;
				}
				manager.wasTwoPlayers = true;
				//manager.twoPlayerMode = false;
			}
			else
			{
				manager.gameOver = true;
			}
		}
		else
		{
			manager.gameOver = true;
		}
	}

	void AndStayDead()
	{
		GetComponent<Animator>().enabled = false;
	}

	private void RotateUpOrDown()
	{
		UnRotateIfNeeded();
		if (input.y > 0 && input.x == 0)
		{
			if (GetComponent<SpriteRenderer>().flipX == false)
			{
				transform.Rotate(0, 0, -65);
			}
			else
			{
				transform.Rotate(0, 0, 65);
			}
		}
		else if (input.y < 0 && input.x == 0)
		{
			if (GetComponent<SpriteRenderer>().flipX == false)
			{
				transform.Rotate(0, 0, 65);
			}
			else
			{
				transform.Rotate(0, 0, -65);
			}
		}
		else if (input.y > 0)
		{
			if (GetComponent<SpriteRenderer>().flipX == false)
			{
				transform.Rotate(0, 0, -40);
			}
			else
			{
				transform.Rotate(0, 0, 40);
			}
		}
		else if (input.y < 0)
		{
			if (GetComponent<SpriteRenderer>().flipX == false)
			{
				transform.Rotate(0, 0, 40);
			}
			else
			{
				transform.Rotate(0, 0, -40);
			}
		}
	}

	public void UnRotateIfNeeded()
	{
		if (transform.rotation.z != 0)
		{
			transform.rotation = noRotation;
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.tag == "Enemy" && cool)
		{
			if(collision.gameObject.GetComponent<EnemyPatrol>().enemyType == "Blowfish")
			{
				hitByBlowfish.Play();
			}
			else if (collision.gameObject.GetComponent<EnemyPatrol>().enemyType == "Shark")
			{
				hitByShark.Play();
			}
			else if (collision.gameObject.GetComponent<JellyMove>() != null)
			{
				hitByJellyfish.Play();
			}

			//shakeAmt = collision.relativeVelocity.magnitude * .0025f;
			//InvokeRepeating("CameraShake", 0, .01f);
			//Invoke("StopShaking", 0.3f);
			if (!manager.camIsMoving)
			{
				Camera.main.GetComponent<CamShake>().shouldShake = true;
				Input.ResetInputAxes();
				cool = false;
				StartCoroutine(CoolDown());
				if (!manager.immortalityOn)
				{
					lives--;
				}
				playerIsMoving = false;
				UnRotateIfNeeded();

				//move player to last safe row
				//if (manager.levelsEntered % 2 != 0)
				//{
				//	safety = Physics2D.Raycast(transform.position, Vector2.down, 150f, safeMask);
				//}
				//else
				//{
				//	safety = Physics2D.Raycast(transform.position, Vector2.up, 150f, safeMask);
				//}
				Vector3 hitPos = transform.position;
				if (!checkpoint)
				{
					//back to home row
					RowBehavior home = manager.FindHome();
					transform.position = new Vector3(hitPos.x, home.transform.position.y, hitPos.z);
				}
				else
				{
					transform.position = new Vector3(hitPos.x, checkpoint.transform.position.y, hitPos.z);
				}
			}
		}
		else if (collision.gameObject.tag == "Coral")
		{
			if (!manager.camIsMoving)
			{
				Input.ResetInputAxes();
				playerIsMoving = false;
				transform.position = startPosition;
				wallBounce.Play();
			}
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.tag == "End Row")
		{
			reachedEnd = true;
			if (manager.oneReachedEnd == false)
			{
				manager.oneReachedEnd = true;
			}
			else
			{
				manager.bothReachedEnd = true;
			}
			if (!checkedPoints.Contains(collision.gameObject))
			{
				checkpoint = collision.gameObject;
				checkedPoints.Add(collision.gameObject);
			}
		}
		else if (collision.gameObject.tag == "Safe Row")
		{
			if (!checkedPoints.Contains(collision.gameObject))
			{
				checkpoint = collision.gameObject;
				checkedPoints.Add(collision.gameObject);
			}
		}
		else if (collision.gameObject.tag == "Home Row")
		{
			if (!checkedPoints.Contains(collision.gameObject))
			{
				checkpoint = collision.gameObject;
				checkedPoints.Add(collision.gameObject);
			}
		}
	}

	IEnumerator CoolDown()
	{
		yield return new WaitForSeconds(0.5f);
		cool = true;
	}

}

