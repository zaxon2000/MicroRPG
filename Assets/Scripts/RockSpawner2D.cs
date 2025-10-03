using System.Collections;
using UnityEngine;

public class RockSpawner2D : MonoBehaviour
{
    [Header("Debug Logging")] [SerializeField]
    private bool runDebugs;

    [Header("Setup")]
    public FallingRock rockPrefab;
    public BoxCollider2D spawnArea;

    [Header("Player")]
    [Tooltip("Player HumanMovement. Auto-found if left null.")]
    public HumanMovement player;

    [Header("Timing")]
    public Vector2 spawnIntervalRange = new Vector2(0.7f, 2.0f);
    public float initialDelay = 0.5f;

    [Header("Rocks per spawn")]
    public Vector2Int countPerWave = new(1, 2);

    [Header("Initial Push")]
    public Vector2 initialDownwardVelocityRange = new(0.0f, 1.5f);

    [Header("Variation")]
    public Vector2 uniformScaleRange = new(0.8f, 1.25f);
    public Transform container;

    private Coroutine _loop;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<HumanMovement>();
        
        // if(runDebugs) Debug.Log($"[RockSpawner2D] Awake: player={(player != null ? player.name : "null")}, rockPrefab={(rockPrefab != null ? rockPrefab.name : "null")}, spawnArea={(spawnArea != null ? spawnArea.name : "null")}");
    }

    private void OnEnable()
    {
        if (_loop == null)
            _loop = StartCoroutine(SpawnLoop());
        
        // if(runDebugs) Debug.Log($"[RockSpawner2D] OnEnable: starting loop={_loop != null}");
    }

    private void OnDisable()
    {
        if (_loop != null)
        {
            // if(runDebugs) Debug.Log("[RockSpawner2D] OnDisable: stopping loop");
            
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        if (rockPrefab == null || spawnArea == null)
        {
            Debug.LogError("[RockSpawner2D] Missing prefab or spawn area.");
            yield break;
        }

        if (initialDelay > 0f)
        {
            // if(runDebugs) Debug.Log($"[RockSpawner2D] SpawnLoop: initialDelay={initialDelay}s");
            yield return new WaitForSeconds(initialDelay);
        }

        while (true)
        {
            // Only spawn when the player is actively climbing
            if (player != null && player.isClimbing)
            {
                int count = Random.Range(countPerWave.x, countPerWave.y + 1);
                // if(runDebugs) Debug.Log($"[RockSpawner2D] SpawnLoop: player climbing, spawning count={count}");
                for (int i = 0; i < count; i++)
                    SpawnOne();

                // Wait a random interval *only after* a spawn while climbing.
                float wait = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
                // if(runDebugs) Debug.Log($"[RockSpawner2D] SpawnLoop: wait={wait:0.00}s before next spawn");
                yield return new WaitForSeconds(wait);
            }
            else
            {
                // if(runDebugs) Debug.Log($"[RockSpawner2D] SpawnLoop: idle (player null? {player==null}, isClimbing? {(player!=null && player.isClimbing)})");
                // Not climbing: idle lightly to avoid a hot loop.
                yield return null;
            }
        }
    }

    private void SpawnOne()
    {
        Vector2 spawnPosition = GetRandomPointInBox(spawnArea);

        if (container == null)
        {
            Debug.LogWarning("[RockSpawner2D] No container set. Using transform.");
        }
        
        var rock = Instantiate(rockPrefab, spawnPosition, Quaternion.identity);

        // if(runDebugs) Debug.Log($"[RockSpawner2D] SpawnOne: spawned '{rock.name}' at {spawnPosition} parent={(container!=null?container.name:"null")}");

        // Randomize scale
        if (uniformScaleRange.x > 0f && uniformScaleRange.y > 0f)
        {
            float randomScale = Random.Range(uniformScaleRange.x, uniformScaleRange.y);
            rock.transform.localScale = new Vector3(randomScale, randomScale, 1f);
            // if(runDebugs) Debug.Log($"[RockSpawner2D] SpawnOne: scale set to {randomScale:0.00}");
        }

        // Optional initial downward nudge
        var rb = rock.GetComponent<Rigidbody2D>();
        if (rb && initialDownwardVelocityRange.y > 0f)
        {
            float downwardVelocity = Random.Range(initialDownwardVelocityRange.x, initialDownwardVelocityRange.y);
            rb.velocity = new Vector2(0f, -downwardVelocity);
            // if(runDebugs) Debug.Log($"[RockSpawner2D] SpawnOne: initial downward velocity set to {-downwardVelocity:0.00} on {rock.name}");
        }
        else if(runDebugs)
        {
            Debug.Log($"[RockSpawner2D] SpawnOne: no initial velocity applied (rb? {rb!=null}, rangeY={initialDownwardVelocityRange.y})");
        }
    }

    private static Vector2 GetRandomPointInBox(BoxCollider2D box)
    {
        Bounds boxBounds = box.bounds; // world-space AABB
        float pointX = Random.Range(boxBounds.min.x, boxBounds.max.x);
        float pointY = Random.Range(boxBounds.min.y, boxBounds.max.y);
        return new Vector2(pointX, pointY);
    }
}
