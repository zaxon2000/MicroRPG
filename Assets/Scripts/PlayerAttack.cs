using UnityEngine;


public class PlayerAttack : MonoBehaviour
{
    [Header("Combat")]
    public float attackRange;           // range we can deal damage to an enemy
    public float attackRate;            // minimum time between attacks
    private float _lastAttackTime;       // last time we attacked
    public int damage;                  // damage we deal
    
    private HumanMovement _movement;
    private ParticleSystem _hitEffect;

    private void Awake()
    {
        _movement = GetComponent<HumanMovement>();
        if (_movement == null) _movement = gameObject.AddComponent<HumanMovement>();

        _hitEffect = gameObject.GetComponentInChildren<ParticleSystem>();
    }

    private void Update()
    {
        // when we press the attack button
        if(Input.GetKeyDown(KeyCode.Space))
        {
            // can we attack?
            if(Time.time - _lastAttackTime >= attackRate)
                Attack();
        }
    }
    
    // shoot a raycast and deal damage if we hit an enemy
    private void Attack()
    {
        _lastAttackTime = Time.time;

        // shoot a raycast in the direction of where we're facing.
        Vector2 facing = _movement != null ? _movement.FacingDirection : Vector2.down;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, facing, attackRange, 1 << 8);

        if(hit.collider != null)
        {
            hit.collider.GetComponent<Enemy>().TakeDamage(damage);

            // play hit effect
            _hitEffect.transform.position = hit.collider.transform.position;
            _hitEffect.Play();
        }
    }
    
}