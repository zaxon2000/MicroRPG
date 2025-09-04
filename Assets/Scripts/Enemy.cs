using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int curHp;                   // our current health
    public int maxHp;                   // our maximum health
    public float moveSpeed;             // how fast we move
    public int xpToGive;                // xp we give when we die

    [Header("Target")]
    public float chaseRange;            // range at which we move towards the player
    public float attackRange;           // range at which we attack the player
    private Player player;

    [Header("Attack")]
    public int damage;                  // damage we deal to the player
    public float attackRate;            // minimum time between attacks
    private float lastAttackTime;       // last time we attacked the player

    // components
    private Rigidbody2D rig;

    void Awake ()
    {
        // get the player target
        player = FindObjectOfType<Player>();

        // get the rigidbody component
        rig = GetComponent<Rigidbody2D>();
    }

    void Update ()
    {
        // calculate the distance between us and the player
        float playerDist = Vector2.Distance(transform.position, player.transform.position);

        // if we're in attack range, try and attack the player
        if(playerDist <= attackRange)
        {
            if(Time.time - lastAttackTime >= attackRate)
                Attack();

            rig.velocity = Vector2.zero;
        }
        // if we're in the chase range, chase after the player
        else if(playerDist <= chaseRange)
            Chase();
        else
            rig.velocity = Vector2.zero;
    }

    // move towards the player
    void Chase ()
    {
        // calculate direction between us and the player
        Vector2 dir = (player.transform.position - transform.position).normalized;

        rig.velocity = dir * moveSpeed;
    }

    // damage the player
    void Attack ()
    {
        lastAttackTime = Time.time;

        player.TakeDamage(damage);
    }

    // called when the player attacks us
    public void TakeDamage (int damageTaken)
    {
        curHp -= damageTaken;

        if(curHp <= 0)
            Die();
    }

    // called when out hp reaches 0
    void Die ()
    {
        player.AddXp(xpToGive);
        Destroy(gameObject);
    }
}