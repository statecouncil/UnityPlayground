using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    //Components
    NavMeshAgent myAgent;
    Animator myAnimator;
    GameObject player;
    AudioSource myAudioSource;
    
    //State Tracking
    private bool isStandingAround;
    private bool isHuntingPlayer;
    private float originalSpeed;
    private Coroutine moveCoroutine;
    private bool isDead;
    
    //Settings
    [SerializeField] int healthPoints = 5;
    [SerializeField] float huntSpeedMult = 10f;
    [Tooltip("How far the next move can go")]
    [SerializeField] float moveRange = 3f;
    [SerializeField] float chanceToStandAround = 15f;
    [SerializeField] float waitUntilStopHunt = 15f;
    [SerializeField] float timeBetweenSteps = 1.8f;
    [SerializeField] float timeBetweenStepsHunt = 0.3f;
    //Sound
    [SerializeField] AudioClip[] footstepsAudio;
    [SerializeField] private AudioClip idleAudio;
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        myAgent = GetComponent<NavMeshAgent>();
        myAnimator = GetComponent<Animator>();
        myAudioSource = GetComponent<AudioSource>();
        
        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError($"{name} did not find player");
        }
        
        originalSpeed = myAgent.speed;
    }

  
    void Update()
    {
        if (!isDead)
        {
            DecideMovement();
        }
        
    }

    void DecideMovement()
    {
        if (isStandingAround) return;
        
        if (isHuntingPlayer)
        {
            //This needs to constantly be called so that the player pos is updated
            myAgent.SetDestination(player.transform.position); //Move to Player
            return;
        }
        
        //Check if it sees the player
        bool seesPlayer = CheckForPlayer();
            
        if (seesPlayer)
        {
            StartCoroutine(StartHuntPlayer());
        }
        else
        {
            RandomWalk();
            
        }
    
        

    }

    void RandomWalk()
    {
        //Return if already moving
        if (myAgent.pathPending || !myAgent.isOnNavMesh || myAgent.remainingDistance > 0.1f)
            return;

        
        
        if (Random.Range(0, 100) < chanceToStandAround)
        {
            //Stand Around
            StartCoroutine(StandAround());
            
            
        }
        else //Walk around
        {
            WalkAround();
        }
        
    }

    IEnumerator StartHuntPlayer()
    {
        myAgent.speed *= huntSpeedMult;
        myAnimator.SetBool("Hunting", true);
        isHuntingPlayer = true;
        
        //Hunt Footsteps
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(StepSounds());
        
        yield return new WaitForSeconds(waitUntilStopHunt);
        isHuntingPlayer = false;
        myAnimator.SetBool("Hunting", false);
        myAgent.speed = originalSpeed;
    }
    
      
    
    
    Vector3 GetRandomNavMeshPoint(Vector3 center, float range, int maxTries = 10)
    {
        for (int i = 0; i < maxTries; i++)
        {
            // Pick a random point in a circle around the center
            Vector2 circle = Random.insideUnitCircle * range;
            Vector3 randomPoint = center + new Vector3(circle.x, 0, circle.y);

            // Try to find the nearest NavMesh point to that location
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        // Fallback to current position if no valid point was found
        return center;
    }

    bool CheckForPlayer()
    {
        bool seesPlayer = false;
        // Calculate the direction vector from the NPC to the player
        Vector3 directionToPlayer = player.transform.position - transform.position;

        // Calculate the angle between the NPC's forward direction and the direction to the player
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        // These variables define the NPC's vision cone
        float viewDistance = 20f; // How far the NPC can see
        float viewAngle = 60f;    // How wide the NPC can see (like a flashlight cone)

        // Check if the player is within the NPC's view distance and angle
        if (directionToPlayer.magnitude < viewDistance && angleToPlayer < viewAngle / 2f)
        {
            // Cast a ray from the NPC's eyes (slightly above the ground) in the direction of the player
            Ray ray = new Ray(transform.position + Vector3.up, directionToPlayer.normalized);
        
            // Perform the raycast and store the result in 'hit'
            if (Physics.Raycast(ray, out RaycastHit hit, viewDistance))
            {
                // Check if the ray hit the player (not a wall or another object)
                if (hit.transform.CompareTag("Player"))
                {
                    // If the player is seen clearly, set the NPC's destination to the player's position

                    seesPlayer = true;
                }
           
            }
      
        }
        
        return seesPlayer;
    }

    void WalkAround()
    {
        //Get Random Point and move there
        Vector3 randomDestination = GetRandomNavMeshPoint(transform.position, moveRange);
        myAgent.SetDestination(randomDestination);
        myAnimator.SetBool("Walking", true);
            
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(StepSounds());
    }
    

    IEnumerator StandAround()
    {
        PlayAudio(idleAudio);
        
        isStandingAround = true;
        myAnimator.SetBool("Walking", false);
        
        float idleTime = Random.Range(1f, 3f);
        yield return new WaitForSeconds(idleTime);
        
        isStandingAround = false;
    }

    void PlayAudio(AudioClip clip)
    {
        if (clip == null) return;
        
        myAudioSource.clip = clip;
        myAudioSource.Play();
    }

    IEnumerator StepSounds()
    {
        if (isStandingAround) yield break;

        float waitTimeBetweenSteps = 0.1f;
        
        while (!isStandingAround && !isDead)
        {
            //Pick a random footstep sound
            PlayAudio(footstepsAudio[Random.Range(0, footstepsAudio.Length)]);
            if (isHuntingPlayer)
            {
                waitTimeBetweenSteps = timeBetweenStepsHunt;
            }
            else
            {
                waitTimeBetweenSteps = timeBetweenSteps;
            }
            yield return new WaitForSeconds(waitTimeBetweenSteps);
        }

    }

    public void GetHit()
    {
        if (healthPoints == 0)
        {
            myAnimator.SetTrigger("Death");
            myAgent.isStopped = true;
            isDead = true;
        }
        else
        {
            healthPoints--;
            Debug.Log($"Enemy {name} has {healthPoints} left");
        }
    }
}
