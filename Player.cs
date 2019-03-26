using Godot;
using System;

//using System.Collections.Generic;	// What additional namespaces are needed

// Contains the command the user wishes upon the character
struct Inputs
{
    public float forwardMove;
    public float rightMove;
    //public float upMove;
}

public class Player : KinematicBody
{
	const float gravity = 25.0f;
	float friction = 6;
	
	// Q3: players can queue the next jump just before he hits the ground
    private bool wishJump = false;
	
	// Player commands
    private Inputs _inputs;
	
	//Camera
    public Transform playerView;            // Camera
    public float playerViewYOffset = 0.6f; // The height at which the camera is bound to
    public float xMouseSensitivity = 0.1f;
    public float yMouseSensitivity = 0.1f;

    // Camera rotations
    private float rotX = 0.0f;
    private float rotY = 90.0f;
    private Vector3 moveDirectionNorm = Vector3.Zero;
    private Vector3 playerVelocity = Vector3.Zero;
    float mouseYaw = 0.022f;     //mouse yaw/pitch. Overwatch = 0.0066, Quake 0.022
	
	// CPM / VQ3
    bool useCPM = false;                        // True = CPM, False = VQ3
    float moveSpeed = 7.0f; //7                     // Ground move speed
    float runAcceleration = 14.0f; //14         // Ground accel
    float runDeacceleration = 10.0f; //10       // Deacceleration that occurs when running on the ground
    float airAcceleration = 2.0f; //2          // Air accel
    float airDecceleration = 2.0f; //2         // Deacceleration experienced when ooposite strafing
    float airControl = 0.3f; //0.3                    // How precise air control is
    float sideStrafeAcceleration = 50.0f; //50  // How fast acceleration occurs to get up to sideStrafeSpeed when
    float sideStrafeSpeed = 1.0f; //1               // What the max speed to generate when side strafing
    float jumpSpeed = 8.0f; //8                // The speed at which the character's up axis gains when hitting jump
	
	
	// Test variables
	bool onFloor = false;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }
	
	// Mouse Controls
	public override void _Input(InputEvent @event)
	{
		// Free Cursor. Temporary. Eventually move this to Esc/ShowMenu method
		if (Input.IsActionPressed("Escape"))
		{
			Input.SetMouseMode(Input.MouseMode.Visible);
		}
		
		// Ensure that the cursor is locked into the screen
        if (Input.GetMouseMode() != Input.MouseMode.Captured)		// If not already locked
        {
            if (Input.IsActionPressed("Fire1"))
                Input.SetMouseMode(Input.MouseMode.Captured);		// Lock cursor
        }
		
		if (@event is InputEventMouseMotion mouseMotion)
	    {
	        // modify accumulated mouse rotation
	        rotX -= mouseMotion.Relative.x * xMouseSensitivity * mouseYaw;
	        rotY += mouseMotion.Relative.y * yMouseSensitivity * mouseYaw;
			
			/*
			// Clamp the Y rotation
        	if (rotY < -90)
            	rotY = -90;
        	else if (rotY > 90)
            	rotY = 90;
				*/
				
			if (rotY < 88)		// Rotations are different from Unitys 360 degrees. Edit so that looking down is -90 degrees and up is 90 degrees?
				rotY = 88;
			else if (rotY > 91)
				rotY = 91;
	
	        // reset rotation
	        Transform transform = Transform;
	        transform.basis = Basis.Identity;
	        Transform = transform;
	
			// Rotate camera
	        RotateObjectLocal(Vector3.Up, rotX);
	        RotateObjectLocal(Vector3.Right, rotY);
			//this.transform.rotation = Quaternion.Euler(0, rotY, 0); // Rotates the collider
			//playerView.rotation = Quaternion.Euler(rotX, rotY, 0); // Rotates the camera
			
			
	    }
	}


	private void DebugLog()
	{
		// Debug/Print camera rotations
		//Console.WriteLine("Camera X rotation: " +rotX +" Camera Y rotation: " +rotY);
		//Console.WriteLine(GlobalTransform.basis.GetEuler());	// Radians
		Console.WriteLine(GetNode("Camera"));	//(Rotation)	// GetNode("path"). The NodePath can be either a relative path (from the current node) or an absolute path (in the scene tree) to a node
		
		//Console.WriteLine("On floor: " +IsOnFloor());
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(float delta)
	{
		DebugLog();
		
        QueueJump();

		Vector3 test = new Vector3(0,1,0);	//Down
		//MoveAndSlide(playerVelocity * delta, test);
		if (onFloor)
        {
            GroundMove(delta);
        }
        else
        {
            AirMove(delta);
        }
		
		// Move the controller
		//MoveAndCollide(playerVelocity * delta);
		//MoveAndSlide(playerVelocity * delta, test);
		MoveAndSlide(playerVelocity, test);
		
		// Ground check		// Requires moveandslide(movement, floor normal) to work	(before it)
		if(IsOnFloor())
			onFloor = true;
		else
			onFloor = false;
	}
	
	private void SetMovementDir()
    {
		// Is there a way to do this with axis instead? Like in Unity.
        //_inputs.forwardMove = Input.GetAxisRaw("Vertical");
        //_inputs.rightMove = Input.GetAxisRaw("Horizontal");
		
		if (Input.IsActionPressed("Forward"))
		{
			_inputs.forwardMove = 1;
		}
			
		if (Input.IsActionPressed("Backward"))
		{
			_inputs.forwardMove = -1;
		}
			
		if (Input.IsActionPressed("Right"))
		{
			_inputs.rightMove = 1;
		}
			
		if (Input.IsActionPressed("Left"))
		{
			_inputs.rightMove = -1;
		}
		
		if (!Input.IsActionPressed("Forward") && !Input.IsActionPressed("Backward"))
		{
			_inputs.forwardMove = 0;
		}
		
		if (!Input.IsActionPressed("Right") && !Input.IsActionPressed("Left"))
		{
			_inputs.rightMove = 0;
		}
    }
	
	private void QueueJump()
    {
        if (Input.IsActionPressed("Jump") && !wishJump)
        {
            wishJump = true;
        }
        if (!Input.IsActionPressed("Jump"))
        {
            wishJump = false;
        }
    }
	
	private void GroundMove(float delta)
    {
		Vector3 wishdir;

        // Do not apply friction if the player is queueing up the next jump
        if (!wishJump)
            ApplyFriction(1.0f, delta);
        else
            ApplyFriction(0, delta);

        SetMovementDir();

        wishdir = new Vector3(_inputs.rightMove, 0, _inputs.forwardMove);		// Get inputs -> wishdir
        //wishdir = transform.TransformDirection(wishdir);						// Transform direction from local space to world space			//Godot conversions are to_global, to_local?
		//wishdir = wishdir.ToGlobal();
        //wishdir.Normalize();													// Normalize direction vector (keep direction, length of 1)
		wishdir = wishdir.Normalized();
        moveDirectionNorm = wishdir;

		// pointless? normalized so length is 1
		var wishspeed = 1f;
        //var wishspeed = wishdir.magnitude;										// .magnitude returns length of the vector. The length of the vector is square root of (x*x+y*y+z*z).
        wishspeed *= moveSpeed;

        Accelerate(wishdir, wishspeed, runAcceleration, delta);

        // Reset the gravity velocity           
        playerVelocity.y = -gravity * delta;

        if (wishJump)
        {
            playerVelocity.y = jumpSpeed;
            wishJump = false;
        }
    }
	
	private void AirMove(float delta)
    {
        Vector3 wishdir;
        float wishvel = airAcceleration;
        float accel;

        SetMovementDir();

        wishdir = new Vector3(_inputs.rightMove, 0, _inputs.forwardMove);			// Get player input
        //wishdir = transform.TransformDirection(wishdir);							// Transform direction from local space to world space

        //float wishspeed = wishdir.magnitude;										// Returns length of the vector
		float wishspeed = wishdir.Length();
        wishspeed *= moveSpeed;

        //wishdir.Normalize();
		wishdir = wishdir.Normalized();												// Normalize direction vector
        moveDirectionNorm = wishdir;

        // CPM: Aircontrol
        if (useCPM)
        {
            float wishspeed2 = wishspeed;
            //if (Vector3.Dot(playerVelocity, wishdir) < 0)
			if (playerVelocity.Dot(wishdir) < 0)
                accel = airDecceleration;
            else
                accel = airAcceleration;
            // If the player is ONLY strafing left or right
            if (_inputs.forwardMove == 0 && _inputs.rightMove != 0)
            {
                if (wishspeed > sideStrafeSpeed)
                    wishspeed = sideStrafeSpeed;
                accel = sideStrafeAcceleration;
            }

            Accelerate(wishdir, wishspeed, accel, delta);
            if (airControl > 0)
                AirControl(wishdir, wishspeed2, delta);
            // !CPM: Aircontrol
        }
        else // VQ3
        {
            Accelerate(wishdir, wishspeed, airAcceleration, delta);
        }

        // Apply gravity
        playerVelocity.y -= gravity * delta;
    }
	
	private void AirMoveVQ3(float delta) //Q3 PM_AirMove
    {
        Vector3 wishdir;

        SetMovementDir();

        wishdir = new Vector3(_inputs.rightMove, 0, _inputs.forwardMove);
        //wishdir = transform.TransformDirection(wishdir);

        // Changing the order here results in different acceleration!!!
        // merge both so that acceleration is the same and only difference is in air control
        // Different for VQ3 and CPM?
        // double check source codes for order of input calculations. Groundmove, Airmove CPM & VQ3
        //wishdir.Normalize();
		wishdir = wishdir.Normalized();
        moveDirectionNorm = wishdir;
        //float wishspeed = wishdir.magnitude;
		float wishspeed = wishdir.Length();
        wishspeed *= moveSpeed;
        //=============

        Accelerate(wishdir, wishspeed, airAcceleration, delta);  

        // Apply gravity
        playerVelocity.y -= gravity * delta;

    }
	
	private void AirControl(Vector3 wishdir, float wishspeed, float delta)
    {
        float zspeed;
        float speed;
        float dot;
        float k;

        // Can't control movement if not moving forward or backward
        if (Mathf.Abs(_inputs.forwardMove) < 0.001 || Mathf.Abs(wishspeed) < 0.001)
            return;
        zspeed = playerVelocity.y;
        playerVelocity.y = 0;
        /* Next two lines are equivalent to idTech's VectorNormalize() */
        //speed = playerVelocity.magnitude;
		speed = playerVelocity.Length();
        //playerVelocity.Normalize();
		playerVelocity = playerVelocity.Normalized();

        //dot = Vector3.Dot(playerVelocity, wishdir);
		dot = playerVelocity.Dot(wishdir); 
        k = 32;
        k *= airControl * dot * dot * delta;

        // Change direction while slowing down
        if (dot > 0)
        {
            playerVelocity.x = playerVelocity.x * speed + wishdir.x * k;
            playerVelocity.y = playerVelocity.y * speed + wishdir.y * k;
            playerVelocity.z = playerVelocity.z * speed + wishdir.z * k;

            //playerVelocity.Normalize();
			playerVelocity = playerVelocity.Normalized();
            moveDirectionNorm = playerVelocity;
        }

        playerVelocity.x *= speed;
        playerVelocity.y = zspeed; // Note this line
        playerVelocity.z *= speed;
    }
	
	private void ApplyFriction(float t, float delta)
    {
        Vector3 vec = playerVelocity; // Equivalent to: VectorCopy();
        float speed;
        float newspeed;
        float control;
        float drop;

        vec.y = 0.0f;
        //speed = vec.magnitude;
		speed = vec.Length();
        drop = 0.0f;

        /* Only if the player is on the ground then apply friction */
        if (IsOnFloor())
        {
            control = speed < runDeacceleration ? runDeacceleration : speed;
            drop = control * friction * delta * t;
        }

        newspeed = speed - drop;
        //playerFriction = newspeed;	// Used to display realtime friction values. Removed?
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        playerVelocity.x *= newspeed;
        playerVelocity.z *= newspeed;
    }

    private void Accelerate(Vector3 wishdir, float wishspeed, float accel, float delta)
    {
        float addspeed;
        float accelspeed;
        float currentspeed;

        //currentspeed = Vector3.Dot(playerVelocity, wishdir);
		currentspeed = playerVelocity.Dot(wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0)
            return;
        accelspeed = accel * delta * wishspeed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        playerVelocity.x += accelspeed * wishdir.x;
        playerVelocity.z += accelspeed * wishdir.z;
    }
}
