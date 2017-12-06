using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrol : MonoBehaviour
{
	[Header("Debug Stats")]
    public float enemySpeed;
	public float respawnRate;
    [HideInInspector] public string enemyType;

	private bool isPuffed = false;
	private bool puffVirgin = true;
    private bool hasBeenVisible = false;
    private int ignoreOnce = 0;
	private float deceleration = 0.2f;
	private float minSpeed = 0.8f;
    private Transform _transform;
    private GameManager manager;
	private SpriteRenderer sproite;
	private Vector3 velocity;

    public void Start()
    {
        manager = GameObject.FindObjectOfType<GameManager>();
        _transform = GetComponent<Transform>();
		sproite = gameObject.GetComponent<SpriteRenderer>();

		//TODO: randomize blowfish puff up rate
		//puffUpRate = Random.Range(3, 8);

        if (enemyType == "Blowfish")
        {
            velocity = new Vector3(enemySpeed, 0, 0);

            //InvokeRepeating("PuffUp", 1f, puffUpRate);
        }
        else if (enemyType == "Shark")
        {
            velocity = new Vector3(enemySpeed, 0, 0);
        }
    }

    void Update()
    {
        if (!manager.camIsMoving)
        {

            if (sproite.flipX)
            {
                //right
                _transform.Translate(velocity.x * Time.deltaTime, 0, 0);
            }
            else
            {
                //left
                _transform.Translate(-velocity.x * Time.deltaTime, 0, 0);
            }
        }

		//if you are a blowfish and puffed up is true
		//decelerate
		if(enemyType == "Blowfish")
		{
			if(isPuffed == true)
			{
				velocity.x -= deceleration;
				if(velocity.x < minSpeed)
				{
					velocity.x = minSpeed;
				}
			}
		//if you are a blowfish and puffed up is false
		//and you are not a puff virgin
		//accelerate back up to original speed
			else if (isPuffed == false)
			{
				if(puffVirgin == false)
				{
					velocity.x += deceleration;
					if(velocity.x > enemySpeed)
					{
						velocity.x = enemySpeed;
					}
				}
			}
		}

    }

	void PuffCollider()
	{
		if (!manager.camIsMoving)
		{
			if (puffVirgin == true)
			{
				puffVirgin = false;
			}
			//GetComponent<CircleCollider2D>().enabled = true;
			gameObject.tag = "Enemy";
			gameObject.layer = 9;
			isPuffed = true;
		}
	}

	void DisableCollider()
	{
		if (!manager.camIsMoving)
		{
			//GetComponent<CircleCollider2D>().enabled = false;
			gameObject.tag = "Untagged";
			gameObject.layer = 0;
			isPuffed = false;
		}
	}

    private void OnTriggerExit2D(Collider2D collision)
    {
        ignoreOnce++;
        if (ignoreOnce == 2) { Destroy(gameObject); }
    }

	private void OnTriggerStay2D(Collider2D collision)
	{
		if(collision.gameObject.tag == "Walls")
		{
			Destroy(gameObject, 4f);
		}
	}

	private void OnBecameVisible()
	{
		if (!hasBeenVisible)
		{
			hasBeenVisible = true;
		}
	}

	private void OnBecameInvisible()
    {
		if (hasBeenVisible)
		{
			Destroy(gameObject);
		}
    }
}
