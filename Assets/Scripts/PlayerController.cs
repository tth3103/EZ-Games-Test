using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] float moveSpeed;
    [SerializeField] float sprintSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] float maxHP;
    [SerializeField] float maxStamina;
    [SerializeField] float attackInterval;
    [SerializeField] float staminaRegenRate = 0.5f;
    [Header("Control Settings")]
    [SerializeField] float tapThreshold = 100f;
    [SerializeField] float touchSensitivity = 1f;
    [SerializeField] float maxDragDistance = 150f;
    [Header("Components")]
    [SerializeField] Rigidbody rb;
    [SerializeField] Animator anim;
    [SerializeField] BoxCollider attackHitBox;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Transform cameraTransform;
    [SerializeField] LevelManager levelManager;
    [Header("Character Boolean State")]
    [SerializeField] bool isGrounded = false;
    [SerializeField] bool isAttacking = false;
    [SerializeField] bool isJumping = false;
    [SerializeField] bool isDead = false;
    [SerializeField] bool isSprinting = false;
    [SerializeField] bool isUnderAttack = false;
    [Header("Player Input Boolean")]
    [SerializeField] bool jumpPressed = false;
    [SerializeField] bool sprintPressed = false;
    [SerializeField] bool attackPressed = false;
    [SerializeField] bool kickPressed = false;
    float currentHP;
    float currentStamina;
    private Vector2 touchStartPos;
    bool isTouching = false;
    bool isDragging = false;
    Vector3 moveDirection;
    float lastAttackTimer = 0f;
    [SerializeField] int comboCurrentIndex = 0;
    [SerializeField] float resetTimer = 0f;
    float resetTime = 0.5f;
    List<string> attackCombos = new List<string>(new string[] {"attack1","attack2","attack3","attack4"});
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        cameraTransform = Camera.main.transform;
        currentHP = maxHP;
        currentStamina = maxStamina;
        lastAttackTimer = -attackInterval;
        SetUpAttackHitBox();
    }

    // Update is called once per frame
    void Update()
    {
        HandleTouchInput();
        if (!isUnderAttack && attackPressed && Time.time >= attackInterval + lastAttackTimer)
        {
            Attack();
        }
        if (currentStamina < maxStamina)
        {
            RegenStamina();
        }
        if (currentStamina < 0) currentStamina = 0;
        if(currentHP < 0)
        {
            currentHP = 0;
            Defeat();
        }
        if(comboCurrentIndex < 0)
        {
            resetTimer += Time.deltaTime;
            if(resetTimer >= resetTime)
            {
                //Debug.Log("Resetting Combo");
                ResetCombo();
            }
        }
    }
    private void FixedUpdate()
    {
        isGrounded = CheckGround();
        if (moveDirection != Vector3.zero && !isAttacking && !isJumping && !isDead && !isUnderAttack)
        {
            
            Move(moveDirection, moveSpeed);
            ResetCombo();
        }
        else
        {
            anim.SetBool("isWalking", false);
        }
    }
    private void Move(Vector3 direction, float speed)
    {
        rb.MovePosition(transform.position + direction * speed * Time.fixedDeltaTime);

        //Rotate the player towards the direction of movement
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        
        anim.SetBool("isWalking", true);
         
    }
    bool CheckGround()
    {
        return Physics.CheckSphere(transform.position+ Vector3.down* 0.1f, 0.3f, groundLayer);
    }
    void Attack()
    {
        isAttacking = true;
        currentStamina -= 30;
        //Debug.Log("Attacking");
        lastAttackTimer = Time.time;
        anim.Play(attackCombos[comboCurrentIndex]);
        resetTimer = 0f;
        comboCurrentIndex++;
        if (comboCurrentIndex >= attackCombos.Count)
        {
            comboCurrentIndex = 0;
        }
    }
    void Defeat()
    {
        isDead = true;
        anim.Play("Defeat");
        levelManager.PlayerDefeated();
    }
    public void OnAttackAnimationComplete()
    {
        isAttacking = false;
    }
    public void OnJumpingAnimationComplete()
    {
        isJumping = false;
    }
    public void OnHurtAnimationComplete()
    {
        isUnderAttack = false;
        isAttacking = false;
        ResetCombo();
    }
    public void OnDefeatedAnimationComplete()
    {
        //Do something
        //Debug.Log("Player Defeated");
    }
    public void RegenStamina()
    {
        currentStamina+=staminaRegenRate * Time.deltaTime;
    }
    public void TakeDamage(float damage, AttackType type)
    {
        if (isDead) return;
        isUnderAttack = true;
        switch (type)
        {
            case AttackType.Head:
                anim.Play("Head Hit");
                break;
            case AttackType.Body:
                anim.Play("Body Hit");
                break;
            case AttackType.Kidney:
                anim.Play("Kidney Hit");
                break;
        }
        currentHP -= damage;
    }
    public void EnableAttackCollider()
    {
        attackHitBox.enabled = true;
    }
    public void DisableAttackCollider()
    {
        attackHitBox.enabled = false;
    }
    public void ResetCombo()
    {
        resetTime = 0f;
        comboCurrentIndex = 0;
    }
    public bool IsDead()
    {
        return isDead;
    }
    public void SetUpAttackHitBox()
    {
        DealDamage hitBox = attackHitBox.GetComponent<DealDamage>();
        if (hitBox != null)
        {
           hitBox.SetOwner(gameObject);
           hitBox.SetTargetTag("Enemy");
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
    void HandleTouchInput()
    {
        // Mouse input for testing in editor
        if (Application.isEditor)
        {
            HandleMouseInput();
            return;
        }
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchStart(touch.position);
                    break;

                case TouchPhase.Moved:
                    OnTouchDrag(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnTouchEnd(touch.position);
                    break;
            }
        }
        else
        {
            if (isTouching)
            {
                OnTouchEnd(Vector2.zero);
            }
        }
    }
    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnTouchStart(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isTouching)
        {
            OnTouchDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnTouchEnd(Input.mousePosition);
        }
    }
    void OnTouchStart(Vector2 screenPosition)
    {
        touchStartPos = screenPosition;
        isTouching = true;
        isDragging = false;
    }

    void OnTouchDrag(Vector2 screenPosition)
    {
        if (!isTouching) return;

        Vector2 touchDelta = screenPosition - touchStartPos;

        // Check movement drag
        if (touchDelta.magnitude > tapThreshold)
        {
            isDragging = true;

            // Calculate movement direction
            Vector2 clampedDelta = Vector2.ClampMagnitude(touchDelta, maxDragDistance);
            Vector2 normalizedDirection = clampedDelta / maxDragDistance;

            // Apply touch sensitivity
            normalizedDirection *= touchSensitivity;

            // Convert screen space to world space movement
            SetMoveDirection(normalizedDirection.x, normalizedDirection.y);
        }
        else if (isDragging)
        {
            SetMoveDirection(0, 0);
            isDragging = false;
        }
    }

    void OnTouchEnd(Vector2 screenPosition)
    {
        if (!isDragging && isTouching)
        {
            // Tap Handle
            Vector2 touchDelta = screenPosition - touchStartPos;
            if (touchDelta.magnitude <= tapThreshold)
            {
                OnTap();
            }
        }

        // Reset touch state
        isTouching = false;
        isDragging = false;
        SetMoveDirection(0, 0);
    }

    void OnTap()
    {
        // Tap Attack
        attackPressed = true;
        //Debug.Log("Tap Attack Pressed");
        Invoke(nameof(ResetAttackInput), 0.1f);
    }

    void SetMoveDirection(float horizontal, float vertical)
    {
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        moveDirection = (vertical * camForward + camRight * horizontal).normalized;
    }
    void ResetAttackInput()
    {
        attackPressed = false;
    }
}