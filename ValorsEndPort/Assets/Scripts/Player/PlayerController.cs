using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{

    Animator anim;
    int runHash = Animator.StringToHash("IsMoving");
    int onGroundHash = Animator.StringToHash("OnGround");
    int airVelocityHash = Animator.StringToHash("AirVelocity");
    int startJumpHash = Animator.StringToHash("StartJump");
    int speedHash = Animator.StringToHash("Speed");
    int runStateHash = Animator.StringToHash("Base Layer.IsMoving");
    int CanSprintHash = Animator.StringToHash("CanSprint");
    int IsSprintingHash = Animator.StringToHash("IsSprinting");
    int IsCrouchingHash = Animator.StringToHash("IsCrouched");
    int StartRollHash = Animator.StringToHash("Roll");
    int StartAttack1Hash = Animator.StringToHash("Attack1");
    int StartAttack2Hash = Animator.StringToHash("Attack2");
    int StartAttack3Hash = Animator.StringToHash("Attack3");
    int StartAttack4Hash = Animator.StringToHash("Attack4");
    int EndAttackHash = Animator.StringToHash("EndAttack");
    bool facingLeft = false;
    public float walkSpeed = 2.5f;
    private float currentSpeed;
    public float maxJump = 5;
    public bool onGround = true;
    public Transform groundCheck;
    float groundRadius = 0.2f;
    public LayerMask isGround;
    float terminalVelocity = -10;
    public GameObject attack1HitBox;
    public GameObject attack2HitBox;
    public GameObject attack3HitBox;
    public GameObject attack4HitBox;
    public GameObject crouchAttack1HitBox;
    public GameObject crouchAttack2HitBox;
    public GameObject crouchAttack3HitBox;
    public GameObject crouchAttack4HitBox;
    float move;
    float jump;
    bool jumpButton = false;
    bool sprintButton = false;
    bool rollButton = false;


    bool attackButton = false;
    public bool attack1 = false;
    public bool attack2 = false;
    public bool attack3 = false;
    public bool attack4 = false;
    int attackBuffer = 0;

    bool crouchAttack = false;
    bool isCrouching = false;
    bool isRolling = false;
    public float slopeAngle;
    public bool test = false;
    public int maxHealth = 100;
    public int health = 100;
    public float maxStamina = 100;
    public float stamina = 100;
    public float staminaCoolDown = 0;
    bool wasSprinting = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        grey = new Color(0.2f, 0.2f, 0.3f, 0.9f);
        red = new Color(0.8f, 0.2f, 0.1f, 0.9f);
        green = new Color(0.2f, 0.8f, 0.2f, 0.9f);
        currentSpeed = walkSpeed;
    }

    void attackLogic(GameObject crouchAttackHitBox, GameObject attackHitBox, float start, float end){
        if (crouchAttack)
        {
            //if we're crouching, we need to stay crouched
            jump = -1;
            if (staminaCoolDown <= start && staminaCoolDown >= end)
                crouchAttackHitBox.collider2D.enabled = true;
            else
                crouchAttackHitBox.collider2D.enabled = false;
            
        } else
        {
            jump = 0;
            //if not, we don't want to crouch until the attack is done
            isCrouching = false;
            if (staminaCoolDown <= start && staminaCoolDown >= end)
                attackHitBox.collider2D.enabled = true;
            else
                attackHitBox.collider2D.enabled = false;
            
        }
    }
    
    void Update()
    {
        move = Input.GetAxis("Horizontal");
        jump = Input.GetAxis("Vertical");
        jumpButton = Input.GetButtonDown("Jump");
        rollButton = Input.GetButtonDown("Roll");
        attackButton = Input.GetButtonDown("Attack");
        sprintButton = Input.GetButton("Sprint");

        //anim.SetFloat("Speed", move);
        
        //AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        //if(Input.GetKeyDown(KeyCode.Space) && stateInfo.nameHash == runStateHash)
        //{
        //anim.SetTrigger (jumpHash);
        //}
        if (isRolling && staminaCoolDown <= 0)
            isRolling = false;
        if (isRolling)
            move = 0;

        if (staminaCoolDown < 0)
        {
           
                attack1 = false;
                attack2 = false;
                attack3 = false;
                attack4 = false;
            //anim.SetTrigger(EndAttackHash);

        }
            
        if (attack1 || attack2 || attack3 || attack4)
        {
            move = 0;
            jumpButton = false;

            if(attack1)
            {
                attackLogic(crouchAttack1HitBox,attack1HitBox, 0.45f, 0.1f);
            }
            if(attack2)
            {
                attackLogic(crouchAttack2HitBox,attack2HitBox, 0.55f, 0.1f);
            }
            if(attack3)
            {
                attackLogic(crouchAttack3HitBox,attack3HitBox, 0.45f, 0.1f);
            }
            if(attack4)
            {
                attackLogic(crouchAttack4HitBox,attack4HitBox, 0.85f, 0.1f);
            }
        } else
        {
            attack1HitBox.collider2D.enabled = false;
            crouchAttack1HitBox.collider2D.enabled = false;
            attack2HitBox.collider2D.enabled = false;
            crouchAttack2HitBox.collider2D.enabled = false;
            attack3HitBox.collider2D.enabled = false;
            crouchAttack3HitBox.collider2D.enabled = false;
            attack4HitBox.collider2D.enabled = false;
            crouchAttack4HitBox.collider2D.enabled = false;
            crouchAttack = false;
        }

        if (move > 0)
        {
            //anim.SetTrigger (jumpHash);
            anim.SetBool(runHash, true);
            facingLeft = false;
        } else if (move < 0)
        {
            anim.SetBool(runHash, true);
            facingLeft = true;

        } else
        {
        
            anim.SetBool(runHash, false);
        
        }

        if (facingLeft)
        {
            transform.localScale = new Vector3(-1, 1, 1);

        
        } else
        {
            transform.localScale = new Vector3(1, 1, 1);    
        }


        if (attackButton && onGround && !isRolling && stamina > 15)
        {
            if (!attack1 && staminaCoolDown <= 0)
            {
                startAttack1();
            }

            if(attackButton && staminaCoolDown < 0.4 && !attack4)
            {
                attackBuffer++;
            }
            
        }


        if (rollButton && onGround && !isRolling && !attack1 && stamina > 15)
        {
            anim.SetTrigger(StartRollHash);
            stamina -= 15;
            isRolling = true;
            //this.rigidbody2D.AddForce(new Vector2 (0, maxJump));
            staminaCoolDown = 0.55f;
            
        }

        if (jumpButton && onGround && !isRolling && !attack1 && stamina > 10)
        {
            anim.SetTrigger(startJumpHash);
            stamina -= 10;
            anim.SetBool(onGroundHash, false);
            onGround = false;
            //this.rigidbody2D.AddForce(new Vector2 (0, maxJump));
            this.rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, maxJump);
            
        }

        if (jump < 0 && onGround)
        {

            anim.SetBool(IsCrouchingHash, true);
            onGround = true;
            isCrouching = true;
                
        } else
        {
            anim.SetBool(IsCrouchingHash, false);
            onGround = false;
            isCrouching = false;
        
        }

        if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D) || isCrouching)
            move = 0;

        onGround = Physics2D.OverlapCircle(groundCheck.position, groundRadius, isGround);

        //Debug.DrawRay (groundCheck.transform.position, Vector3.down, Color.cyan);
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.transform.position + new Vector3(0, -0.4f, 0), -Vector2.up, 0.1f);
        if (Physics2D.Raycast(groundCheck.transform.position + new Vector3(0, -0.4f, 0), -Vector2.up, 0.1f))
        {
            test = true;        
            //slopeAngle = Vector2.Angle (transform.up, hit.normal);
            //Debug.Log(hit.normal);
            //Debug.Log(hit.collider.name);
            slopeAngle = hit.collider.transform.eulerAngles.z;
        
        } else
        {
            slopeAngle = 0; 
            test = false;       
        }
        if (staminaCoolDown > 0)
        {
            staminaCoolDown -= Time.smoothDeltaTime;
            currentSpeed = walkSpeed;
            anim.SetBool(IsSprintingHash, false);
        } else if (Mathf.Abs(move) > 0 && sprintButton && stamina > 0)
        {
            anim.SetBool(IsSprintingHash, true);
            currentSpeed = walkSpeed * 2;
            stamina -= 10 * Time.deltaTime;
        }

        if (stamina <= 0 && currentSpeed >= walkSpeed * 2)
        {
            stamina = 0;
            staminaCoolDown = 2f;
            anim.SetBool(IsSprintingHash, false);
            currentSpeed = walkSpeed;
        }

        if (wasSprinting && !sprintButton)
        {
            staminaCoolDown = 0.5f;
            currentSpeed = walkSpeed;
            anim.SetBool(IsSprintingHash, false);
        }

        if (move > -0.1 && move < 0.1 && wasSprinting)
        {
            staminaCoolDown = 0.25f;
            currentSpeed = walkSpeed;
            anim.SetBool(IsSprintingHash, false);
        }

        if (currentSpeed == walkSpeed && staminaCoolDown <= 0)
        {
            stamina += maxStamina * 0.2f * Time.deltaTime;
            if (stamina > maxStamina)
                stamina = maxStamina;       
        }

        healthBarDisplay = health / maxHealth;
        staminaBarDisplay = stamina / maxStamina;

        if (currentSpeed == walkSpeed * 2)
            wasSprinting = true;
        else 
            wasSprinting = false;

    }

    void FixedUpdate()
    {

        anim.SetBool(onGroundHash, onGround);

        if (slopeAngle != 0)
        {
            if (move < 0 && slopeAngle < 360 && slopeAngle > 180) //if we're moving right and the slope is negative
                move = move / 2;
            else if (move > 0 && slopeAngle > 0 && slopeAngle < 180) //if we're moving left and the slope is positive
                move = move / 2;
        }


        this.rigidbody2D.velocity = new Vector2(move * currentSpeed, rigidbody2D.velocity.y);

        if (isRolling)
        {
            if (slopeAngle != 0)
            {
                if (facingLeft)
                {
                    int rollSlow = 1;
                    if (slopeAngle < 360 && slopeAngle > 180)
                        rollSlow = 2;
                    this.rigidbody2D.velocity = new Vector2(-walkSpeed * 3 / rollSlow, rigidbody2D.velocity.y);
                } else
                {
                    int rollSlow = 1;
                    if (slopeAngle > 0 && slopeAngle < 180)
                        rollSlow = 2;
                    this.rigidbody2D.velocity = new Vector2(walkSpeed * 3 / rollSlow, rigidbody2D.velocity.y);
                }
                    
            } else
            {
                if (facingLeft)
                    this.rigidbody2D.velocity = new Vector2(-walkSpeed * 3, rigidbody2D.velocity.y);
                else
                    this.rigidbody2D.velocity = new Vector2(walkSpeed * 3, rigidbody2D.velocity.y);
            }       
        }


        anim.SetFloat(airVelocityHash, this.rigidbody2D.velocity.y);

        anim.SetFloat(speedHash, Mathf.Abs(this.rigidbody2D.velocity.x));


    }

    void startAttack1()
    {
        
        anim.SetTrigger(StartAttack1Hash);
        stamina -= 15;
        attack1 = true;
        //this.rigidbody2D.AddForce(new Vector2 (0, maxJump));
        staminaCoolDown = 0.55f;
        if (isCrouching)
            crouchAttack = true;
    }
    
    void startAttack2()
    {
        if (attackBuffer > 0)
        {
            anim.SetTrigger(StartAttack2Hash);
            stamina -= 15;
            attack1 = false;
            attack2 = true;
            attackBuffer = 0;
            //this.rigidbody2D.AddForce(new Vector2 (0, maxJump));
            staminaCoolDown = 0.65f;
            attack1HitBox.collider2D.enabled = false;
            crouchAttack1HitBox.collider2D.enabled = false;
        }
        else
        {
            anim.SetTrigger(EndAttackHash);
        }

    }
    
    void startAttack3()
    {
        if (attackBuffer > 0)
        {
            anim.SetTrigger(StartAttack3Hash);
            stamina -= 15;
            attack2 = false;
            attack3 = true;
            attackBuffer = 0;
            //this.rigidbody2D.AddForce(new Vector2 (0, maxJump));
            staminaCoolDown = 0.75f;
            attack2HitBox.collider2D.enabled = false;
            crouchAttack2HitBox.collider2D.enabled = false;
        }
        else
        {
            anim.SetTrigger(EndAttackHash);
        }
    }
    
    void startAttack4()
    {
        if (attackBuffer > 0)
        {
            anim.SetTrigger(StartAttack4Hash);
            stamina -= 15;
            attack3 = false;
            attack4 = true;
            attackBuffer = 0;
            //this.rigidbody2D.AddForce(new Vector2 (0, maxJump));
            staminaCoolDown = 1f;
            attack3HitBox.collider2D.enabled = false;
            crouchAttack3HitBox.collider2D.enabled = false;
        }
        else
        {
            anim.SetTrigger(EndAttackHash);
        }
    }

    void endAtack ()
    {
        anim.SetTrigger(EndAttackHash);
    }
    
    float healthBarDisplay = 0.5f;
    float staminaBarDisplay = 0.5f;
    private GUIStyle barBG = null;
    private GUIStyle barHealth = null;
    private GUIStyle barStam = null;
    Color grey = new Color(0.4f, 0.4f, 0.4f, 0.5f);
    Color red = new Color(0.8f, 0.2f, 0.1f, 0.5f);
    Color green = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    Texture2D progressBarEmpty;
    Texture2D progressBarFull;

    private void InitStyles(ref GUIStyle currentStyle, Color color)
    {
        if (currentStyle == null)
        {
            currentStyle = new GUIStyle(GUI.skin.box);
            currentStyle.normal.background = MakeTex(2, 2, color);
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix [i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    void drawHealthBar()
    {

        Vector2 pos = new Vector2(20, 40); 
        Vector2 size = new Vector2(60, 20); 


        // draw the background:
        GUI.BeginGroup(new Rect(pos.x, pos.y, size.x, size.y));
        GUI.Box(new Rect(0, 0, size.x, size.y), progressBarEmpty, barBG);
        
        // draw the filled-in part:
        GUI.BeginGroup(new Rect(0, 0, size.x * healthBarDisplay, size.y));
        GUI.Box(new Rect(0, 0, size.x, size.y), progressBarFull, barHealth);
        GUI.EndGroup();
        
        GUI.EndGroup();
    }

    void drawStaminaBar()
    {
        
        Vector2 pos = new Vector2(20, 80); 
        Vector2 size = new Vector2(60, 20); 


        
        // draw the background:
        GUI.BeginGroup(new Rect(pos.x, pos.y, size.x, size.y));
        GUI.Box(new Rect(0, 0, size.x, size.y), progressBarEmpty, barBG);
        
        // draw the filled-in part:
        GUI.BeginGroup(new Rect(0, 0, size.x * staminaBarDisplay, size.y));
        GUI.Box(new Rect(0, 0, size.x, size.y), progressBarFull, barStam);
        GUI.EndGroup();
        
        GUI.EndGroup();
    }

    void OnGUI()
    {
        InitStyles(ref barBG, grey);
        InitStyles(ref barHealth, red);
        InitStyles(ref barStam, green);
        
        drawHealthBar();
        drawStaminaBar();
        
    }

}
