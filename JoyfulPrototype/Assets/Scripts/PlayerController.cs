﻿using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

    //Psuedo-Constants
    private Vector3 RIGHT_SPRITE = new Vector3(1f, 1f, 1f);
    private Vector3 LEFT_SPRITE = new Vector3(-1f, 1f, 1f);

    //basic physics fields
    public float moveSpeed;
	public float jumpVel = 10f;

    //more in depth physics fields
    public float baseGravity = .75f;
    public AnimationCurve timedGravityScale;
    public float secondsToReachMaxGravity = 1.0f;
    public float jumpGravityReduction = .5f;
    public float highGravityThresholdVelocity = 3f;
    public float terminalVelocity;
    public float aerialDragModifier = .05f;
    public float aerialDriftModifier = .1f;
    public float maxAerialDrift = 5f;
    

    //fields for dealing with detecting if the player is grounded or not
    public Transform groundCheck;
	public float groundCheckRadius;
	public LayerMask whatIsGround;
	

    //fields dealing with player projectiles
	public Transform firePoint;
	public GameObject ninjaStar;
	public float shotDelay;

    //fields dealing with knockback
	public float knockback;
	public float knockbackLength;
	public float knockbackCount;
	public bool knockFromRight;
    public bool onLadder;
    public float climbSpeed;

    //Private Members
    private float _moveVelocity;
    private float _climbVelocity;
    private float _gravityStore;
    private bool _jumping;
    private bool _falling;
    private Vector3 _mouseV;
    private Vector3 _mouseWP;
    private Vector3 _mouseTargeting;
    private Rigidbody2D _rigidbody;
    private bool _firingProjectile;
    private float _verticalMovement;
    private bool _grounded;
    private Animator _anim;
    private float _shotDelayCounter;
    private float _timeFalling;

    // Use this for initialization
    private void Start () {
        _Init_Player();
    }

    void _Init_Player()
    {
        try {
            _anim = this.gameObject.GetComponent<Animator>();
            _gravityStore = this.gameObject.GetComponent<Rigidbody2D>().gravityScale;
            _rigidbody = this.gameObject.GetComponent<Rigidbody2D>();
        }
        catch
        {
            Debug.Log("Player didn't initialize correctly");
        }
    }

    //Update is called once per frame
    //Takes care of player input and physics
    private void Update()
    {
        _Animation();
        _Navigation();
        _Behavior();
    }

    void _Animation()
    {
        //setting properties for animation
        _anim.SetBool("isGrounded", _grounded);
        _anim.SetFloat("VelocityX", Mathf.Abs(_rigidbody.velocity.x));
        _anim.SetFloat("VelocityY", _rigidbody.velocity.y);

    }

    void _Navigation()
    {
        _jumping = Input.GetButton("Jump");
        _firingProjectile = Input.GetButton("Fire1");
        _verticalMovement = Input.GetAxisRaw("Vertical");
    }

    void _Behavior()
    {
        _Sense();
        _Think();
        _Act();
    }

    void _Sense()
    {
        this._grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        //shows a vector between the player and where the mouse is
        _mouseV = Input.mousePosition;
        _mouseV.z = 10f;
        _mouseWP = Camera.main.ScreenToWorldPoint(_mouseV);
        _mouseTargeting = (_mouseWP - transform.position);
        Debug.DrawRay(transform.position, _mouseTargeting);

        //determine if the player is falling, and other cleanup
        _falling = false;
        if (_rigidbody.velocity.y <= highGravityThresholdVelocity)
        {
            _falling = true;
        }

        if (_falling)
        {
            _timeFalling += Time.deltaTime;
        }
        else
        {
            _timeFalling = 0;
        }
    }

    void _Think()
    {
        _shotDelayCounter -= Time.deltaTime;
    }

    void _Act()
    {
        if (_jumping && _grounded)
        {
            Jump();
        }
        Movement();
        KnockBack();
        Shooting();
        LadderMovement();
    }


    void Shooting()
    {
        //detect input for firing projectiles and using the sword
        if (_firingProjectile)
        {
            if (_shotDelayCounter <= 0)
            {
                _shotDelayCounter = shotDelay;
                GameObject starInstance = (GameObject)Instantiate(ninjaStar, firePoint.position, firePoint.rotation);
                starInstance.GetComponent<Rigidbody2D>().velocity = _mouseTargeting;
                ProjectileChargeCounter.decreaseProjectile();
            }
        }
    }

    void LadderMovement()
    {
        if (onLadder)
        {
            _rigidbody.gravityScale = 0f;

            _climbVelocity = climbSpeed * _verticalMovement;
            //Reduce x movement while on ladder
            if (_verticalMovement > 0)
            {
                _rigidbody.velocity = new Vector2(_rigidbody.velocity.x / 1.5f, _climbVelocity);
            }
        }

        if (!onLadder)
        {
            _rigidbody.gravityScale = _gravityStore;
        }
    }

    void Movement()
    {
        if (_rigidbody.velocity.x > 0)
        {
            transform.localScale = RIGHT_SPRITE;
        }
        else if (_rigidbody.velocity.x < 0)
        {
            transform.localScale = LEFT_SPRITE;
        }
    }

    void KnockBack()
    {
        //major physics evaluation: knockback, gravity, player input, velocity changes, etc.
        if (knockbackCount <= 0)
        {

            //evaluluate the gravity value at the current time of falling, using the base value and the timed scale
            float gravityValue = baseGravity * timedGravityScale.Evaluate(_timeFalling / secondsToReachMaxGravity);

            //if holding the jump button while moving upwards in a jump, gravity is reduced by some factor
            if (_jumping && !_falling)
            {
                gravityValue *= jumpGravityReduction;
            }

            //calculate the new y velocity after gravity, capping negative y vels at the terminal velocity
            float yVelAfterGravity = _rigidbody.velocity.y - gravityValue;
            if (yVelAfterGravity < terminalVelocity)
                yVelAfterGravity = terminalVelocity;

            //determine the new x velocity
            float xVel = _rigidbody.velocity.x;
            float xVelAfterModifiers;

            if (!_grounded)
            {
                if (Mathf.Abs(xVel) > Mathf.Abs(maxAerialDrift))
                {
                    //if in the air and going faster than the max aerial horizontal velocity, slow down per the drag modifier
                    xVelAfterModifiers = Mathf.Lerp(xVel, maxAerialDrift * (xVel > 0 ? 1 : -1), aerialDragModifier);
                }
                else
                {
                    //if in the air and going slower than the max aerial horizontal velocity, increase velocity in the direction the player is inputting per the drift modifier
                    xVelAfterModifiers = Mathf.Lerp(xVel, maxAerialDrift * Input.GetAxisRaw("Horizontal"), aerialDriftModifier);
                }
            }
            else
            {
                //if on the ground, move as fast as the ground speed in the direction the player is inputting
                xVelAfterModifiers = moveSpeed * Input.GetAxisRaw("Horizontal");
            }

            _rigidbody.velocity = new Vector2(xVelAfterModifiers, yVelAfterGravity);
        }
        else
        {
            if (knockFromRight)
            {
                _rigidbody.velocity = new Vector2(-knockback, knockback);
            }
            if (!knockFromRight)
            {
                _rigidbody.velocity = new Vector2(knockback, knockback);
            }
            knockbackCount -= Time.deltaTime;
        }
    }

    //the player jumps, by setting a new velocity for the rigid body with the y value changed to the jumpVel field
	private void Jump() {
        _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, jumpVel);
	}
}
