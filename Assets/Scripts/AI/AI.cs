using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public enum AttackType
{
    Head,
    Body,
    Kidney
}
public partial class AI : MonoBehaviour,IPoolable
{
    protected enum AIState
    {
        Find_Target,
        Chase,
        Attack,
        Defeat
    }
    [Header("AI Properties")]
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float maxHP;
    [SerializeField] protected float currentHP;
    [SerializeField] protected float attack;
    [SerializeField] protected float attackInterval;
    [SerializeField] protected float attackRange;
    [SerializeField] protected float actionInterval;
    [SerializeField] protected string myTeamTag;
    [SerializeField] protected string enemyTeamTag;
    [Header("AI State")]
    [SerializeField] protected AIState currentState;
    [Header("AI Boolean State")]
    [SerializeField] protected bool isTargetInRange = false;
    [SerializeField] protected bool isAttacking = false;
    [SerializeField] protected bool isDead = false;
    [SerializeField] protected bool isUnderAttack = false;
    [Header("Components")]
    [SerializeField] protected Animator anim;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] protected GameObject target;
    [SerializeField] protected BoxCollider attackHitBox;
    [SerializeField] protected HealthBarUI healthBar;
    //Event
    public event Action<GameObject> OnEnemyDeath;
    public LevelManager levelManager;
    public Transform enemyTeam;
    protected List<GameObject> potentialTargets = new List<GameObject>();
    protected float attackTimer = 0f;
    //Initial state
    protected float initialHealth;
    protected float initialDamage;
    protected float initialSpeed;
    protected Vector3 initialPosition;
    protected Quaternion initialRotation;
    protected bool hasStoredInitialValues = false;

    protected virtual void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        healthBar.targetHP = gameObject;
        currentHP = maxHP;

        SetupAgent();
        SetupAttackHitBox();
        
        currentState = AIState.Find_Target;
        attackTimer = -attackInterval;
        StoreInitialValues();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTargetList();
        if (target != null)
        {
            if (target.activeInHierarchy)
            {
                isTargetInRange = Vector3.Distance(transform.position, target.transform.position) <= attackRange;
            }
            else
            {
                target = null;
                currentState = AIState.Find_Target;
            }
        }
        else
        {
            isTargetInRange = false;
        }
        if (currentHP <= 0)
        {
            currentHP = 0;
            isDead = true;
            agent.isStopped = true;
            currentState = AIState.Defeat;
        }
        switch (currentState)
        {
            case AIState.Find_Target:
                FindTarget();
                break;
            case AIState.Chase:
                Chase(target.transform);
                break;
            case AIState.Attack:
                if (!isUnderAttack && Time.time > attackInterval + attackTimer) 
                {
                    Attack();
                }
                break;
            case AIState.Defeat:
                Defeat();
                break; 
        }
    }
    virtual protected void FindTarget()
    {
        Debug.Log("Searching a target");
        UpdateTargetList();
        if (potentialTargets.Count == 0)
        {
            UpdateTargetList();
        }
        if(potentialTargets.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, potentialTargets.Count);
            target = potentialTargets[randomIndex];
            currentState = AIState.Chase;
        }
    }
    protected void UpdateTargetList()
    {
        potentialTargets.Clear();
        foreach (Transform child in enemyTeam)
        {
            if (child.gameObject.activeInHierarchy)
            {
                bool isAlive = true;
                AI targetAI = child.GetComponent<AI>();
                PlayerController player = child.GetComponent<PlayerController>();
                if (targetAI != null && targetAI.IsDead())
                {
                    isAlive = false;
                }
                if (player != null && player.IsDead())
                {
                    isAlive = false;
                }
                if (isAlive) potentialTargets.Add(child.gameObject);
            }
        }
    }
    protected void Chase(Transform target)
    {
        if (isDead) return;
        anim.SetBool("isWalking",true);
        agent.SetDestination(target.position);
        if (isTargetInRange)
        {
            anim.SetBool("isWalking", false);
            currentState = AIState.Attack;
        }
    }
    protected void Attack()
    {
        if (isDead) return;
        if (!isTargetInRange)
        {
            currentState = AIState.Chase;
            isAttacking = false;
            return;
        }
        Vector3 direction = (target.transform.position - transform.position);
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 20f);
        }
        isAttacking = true;
        attackTimer = Time.time;
        agent.isStopped = true;
        anim.SetTrigger("Punch");
    }
    public void OnAttackAnimationComplete()
    {
        agent.isStopped = false;
        isAttacking = false;
    }
    public void OnHurtAnimationComplete()
    {
        isUnderAttack = false;
        isAttacking = false;
    }
    protected void SetupAgent() 
    {
        agent.speed = moveSpeed;
        agent.autoBraking = true;
    }
    public void TakeDamage(float damage,AttackType type)
    {
        if (isDead) return;
        switch (type)
        {
            case AttackType.Head:
                anim.Play("Head Hit");
                break;
            case AttackType.Body:
                anim.Play("Stomach Hit");
                break;
            case AttackType.Kidney:
                anim.Play("Kidney Hit");
                break;
        }
        currentHP -= damage;
        healthBar.SetHealth(currentHP);
        isUnderAttack = true;   
    }
    protected void Defeat()
    {
        healthBar.gameObject.SetActive(false);
        anim.Play("Defeat");
    }
    protected virtual void OnDefeatedAnimationComplete()
    {
        //Debug.Log("Disable target");
        levelManager.DefeatEnemy();
        HandleDeath();
    }
    public void EnableAttackCollider()
    {
        attackHitBox.enabled = true;
    }
    public void DisableAttackCollider()
    {
        attackHitBox.enabled = false;
    }
    public void ScaleStat(float multiplier)
    {
        maxHP *= multiplier;
        attack *= multiplier;
    }
    public bool IsDead()
    {
        return isDead;
    }
    public void SetupAttackHitBox()
    {
        if(attackHitBox != null)
        {
            DealDamage hitbox = attackHitBox.GetComponent<DealDamage>();
            if(hitbox != null)
            {
                hitbox.SetOwner(gameObject);
                hitbox.SetTargetTag(enemyTeamTag);
                hitbox.damage = attack;
            }
        }
    }
    public float GetMaxHP()
    {
        return maxHP;
    }
    public float GetCurrentHP()
    {
        return currentHP;
    }
    protected void StoreInitialValues()
    {
        if (!hasStoredInitialValues)
        {
            initialHealth = maxHP; 
            initialDamage = attack;   
            initialSpeed = moveSpeed; 
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            hasStoredInitialValues = true;
        }
    }
    public void ResetToInitialState()
    {
        if (!hasStoredInitialValues)
        {
            StoreInitialValues();
        }

        // Reset all to initial values
        currentHP = initialHealth; 
        attack = initialDamage; 
        moveSpeed = initialSpeed; 

        // Reset position and rotation
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // Reset flags
        isDead = false; 
        isAttacking = false; 

        ResetAnimations();

        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true;

        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
    }
    public void OnSpawnedFromPool()
    {
        gameObject.SetActive(true);

        enabled = true;
        StartAIBehavior();
    }
    public void OnReturnedToPool()
    {
        StopAllCoroutines();
        enabled = false;
        ClearTargets();

        gameObject.SetActive(false);
    }
    protected void HandleDeath()
    {
        isDead = true;
        OnEnemyDeath?.Invoke(gameObject);
    }
    private void ResetAnimations()
    {
        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }
    private void StartAIBehavior()
    {
        currentState = AIState.Find_Target;
    }
    private void ClearTargets()
    {
        target = null; 
    }
}
