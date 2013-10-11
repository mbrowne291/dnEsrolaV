#region File Description
//-----------------------------------------------------------------------------
// Player.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Platformer
{
    /// <summary>
    /// Our fearless adventurer!
    /// </summary>
    class Player
    {
        public bool left = true;

        Color player1color = new Color(1f, 1f, 1f);
        Color player2color = new Color(0.5f, 0.5f, 1f);

        public bool amplayer1 = false;
        public bool amplayer2 = false;

        public bool skipthislevel = false;

        public Rectangle fireSoul = new Rectangle (-30, -30, 50, 50);
        private int fireSoulVelocity = 0;

        private bool isSprinting = false;
        public bool isCrouching = false;
        public bool isMagicAttack = true;
        // Animations
        private Texture2D fireAttack;
        private Texture2D swordAnimation;
        private Texture2D shieldIdle;
        private Texture2D shieldBlock;
        private Texture2D swordHitbox;
        private Animation crouchAnimation;
        private Animation idleAnimation;
        private Animation runAnimation;
        private Animation blockAnimation;
        private Animation attackAnimation;
        private Animation jumpAnimation;
        private Animation celebrateAnimation;
        private Animation dieAnimation;
        private SpriteEffects flip = SpriteEffects.None;
        private AnimationPlayer sprite;
        // Sounds
        private SoundEffect killedSound;
        public SoundEffect jumpSound;
        private SoundEffect fallSound;
        private SoundEffect attackSound;

        public Level Level
        {
            get { return level; }
        }
        Level level;

        public bool IsAlive
        {
            get { return isAlive; }
        }
        bool isAlive;

        // Physics state
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        public Vector2 position;

        public Rectangle shieldTop = new Rectangle(0, 0, 10, 10);
        public Rectangle shieldNearTop = new Rectangle(0, 0, 10, 10);
        public Rectangle shieldMid = new Rectangle(0, 0, 10, 10);
        public Rectangle shieldNearBtm = new Rectangle(0, 0, 10, 10);
        public Rectangle shieldBtm = new Rectangle(0, 0, 10, 10);

        public Rectangle swordTip = new Rectangle(0, 0, 5, 5);
        public Rectangle swordMid = new Rectangle(0, 0, 5, 5);
        public Rectangle swordHilt = new Rectangle(0, 0, 5, 5);


        private float previousBottom;

        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        public Vector2 velocity;

        // Constants for controling horizontal movement
        private const float MoveAcceleration = 13000.0f;
        private const float MaxMoveSpeed = 150.0f;
        private const float GroundDragFactor = 0.6f;
        private const float AirDragFactor = 0.6f;

        // Constants for controlling vertical movement
        private const float MaxJumpTime = 0.35f;
        private float JumpLaunchVelocity = -3500.0f;
        private const float GravityAcceleration = 2400.0f;
        private const float MaxFallSpeed = 300.0f;
        private const float JumpControlPower = 0.11f;

        public int health = 400;
        public float stamina = 120;
        public int hitcooldown = 0;
        public int attackcooldown = 0;
        public int stuncooldown = 0;
        public int timespentblocking = 0;

        private int attackangle = 0;

        // Input configuration
        private const float MoveStickScale = 1.0f;
        private const float AccelerometerScale = 1.5f;
        private const Buttons JumpButton = Buttons.A;
        private const Buttons MagicButton1 = Buttons.Y;
        private const Buttons MagicButton2 = Buttons.RightTrigger;
        private const Buttons AttackButton1 = Buttons.X;
        private const Buttons AttackButton2 = Buttons.RightShoulder;
        private const Buttons BlockButton = Buttons.LeftShoulder;
        private const Buttons SprintButton = Buttons.B;

        /// <summary>
        /// Gets whether or not the player's feet are on the ground.
        /// </summary>
        public bool IsOnGround
        {
            get { return isOnGround; }
        }
        bool isOnGround;

        /// <summary>
        /// Current user movement input.
        /// </summary>
        private float movement;

        public bool isBlocking;
        private float blockAngle = 0;

        private bool isAttacking;
        private bool wasAttacking;
        private float attackTime;

        // Jumping state
        private bool isJumping;
        private bool wasJumping;
        private float jumpTime;

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this player in world space.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
            set
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                BoundingRectangle = new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }


        /// <summary>
        /// Constructors a new player.
        /// </summary>
        public Player(Level level, Vector2 position)
        {
            this.level = level;

            LoadContent();

            Reset(position);
        }

        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        public void LoadContent()
        {
            // Load animated textures.
            swordAnimation = Level.Content.Load<Texture2D>("Sprites/Player/Sword");
            fireAttack = Level.Content.Load<Texture2D>("Sprites/Player/SoulFire");
            swordHitbox = Level.Content.Load<Texture2D>("Sprites/Player/pink");
            shieldIdle = Level.Content.Load<Texture2D>("Sprites/shield_idle");
            shieldBlock = Level.Content.Load<Texture2D>("Sprites/shield_block");
            crouchAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Crouch"), 0.1f, true);
            idleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Idle"), 0.1f, true);
            runAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Run"), 0.2f, true);
            blockAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/block"), 0.1f, true);
            attackAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/attack"), 0.3f, false);
            jumpAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Jump"), 0.1f, false);
            celebrateAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Celebrate"), 0.1f, false);
            dieAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Die"), 0.1f, false);

            // Calculate bounds within texture size.            
            int width = (int)(idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.8);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            // Load sounds.            

            attackSound = Level.Content.Load<SoundEffect>("Sounds/throw");
            killedSound = Level.Content.Load<SoundEffect>("Sounds/PlayerKilled");
            jumpSound = Level.Content.Load<SoundEffect>("Sounds/PlayerJump");
            fallSound = Level.Content.Load<SoundEffect>("Sounds/PlayerFall");
        }



        /// <summary>
        /// Resets the player to life.
        /// </summary>
        /// <param name="position">The position to come to life at.</param>
        public void Reset(Vector2 position)
        {
            Position = position;
            Velocity = Vector2.Zero;
            isAlive = true;
            health = 400;
            sprite.PlayAnimation(idleAnimation);
        }

        /// <summary>
        /// Handles input, performs physics, and animates the player sprite.
        /// </summary>
        /// <remarks>
        /// We pass in all of the input states so that our game is only polling the hardware
        /// once per frame. We also pass the game's orientation because when using the accelerometer,
        /// we need to reverse our motion when the orientation is in the LandscapeRight orientation.
        /// </remarks>
        public void Update(
            GameTime gameTime,
            KeyboardState keyboardState,
            GamePadState gamePadState,
            TouchCollection touchState,
            AccelerometerState accelState,
            DisplayOrientation orientation)
        {
            GetInput(keyboardState, gamePadState, touchState, accelState, orientation);
            ApplyPhysics(gameTime);

            if(PlatformerGame.playerStoleSoul)fireSoul.X += fireSoulVelocity;

            if (stamina < 120 && attackcooldown <= 0)
                if (isBlocking) stamina += 0.375f;
                else stamina += 0.75f;

            if (velocity.X > MaxMoveSpeed || velocity.X < -MaxMoveSpeed && stamina > -10) stamina -= 2;

            if (isSprinting && stamina == 0) stamina -= 20;
            //shield hit box movement
            if (isBlocking)
            {
                if (left)
                {
                    shieldTop.X = (int)(Position.X
                        - 30 + (Math.Cos((blockAngle - 90) * 0.0174532925) * 40));
                    shieldTop.Y = (int)(Position.Y
                        - 20 + (Math.Sin((blockAngle - 90) * 0.0174532925) * 40));
                    shieldNearTop.X = (int)(Position.X
                        - 30 + (Math.Cos((blockAngle - 90) * 0.0174532925) * 32));
                    shieldNearTop.Y = (int)(Position.Y
                        - 20 + (Math.Sin((blockAngle - 90) * 0.0174532925) * 32));
                    shieldMid.X = (int)(Position.X
                        - 30 + (Math.Cos((blockAngle - 90) * 0.0174532925) * 24));
                    shieldMid.Y = (int)(Position.Y
                        - 20 + (Math.Sin((blockAngle - 90) * 0.0174532925) * 24));
                    shieldNearBtm.X = (int)(Position.X
                        - 30 + (Math.Cos((blockAngle - 90) * 0.0174532925) * 16));
                    shieldNearBtm.Y = (int)(Position.Y
                        - 20 + (Math.Sin((blockAngle - 90) * 0.0174532925) * 16));
                    shieldBtm.X = (int)(Position.X
                        - 30 + (Math.Cos((blockAngle - 90) * 0.0174532925) * 8));
                    shieldBtm.Y = (int)(Position.Y
                        - 20 + (Math.Sin((blockAngle - 90) * 0.0174532925) * 8));
                }
                else
                {
                    shieldTop.X = (int)(Position.X
                        + 20 + (Math.Cos((blockAngle - 90) * 0.0174532925) * 40));
                    shieldTop.Y = (int)(Position.Y
                        - 20 + (Math.Sin((blockAngle - 90) * 0.0174532925) * 40));
                    shieldNearTop.X = (int)(Position.X
                        + 20 + (Math.Cos((blockAngle - 90) * 0.0174532925) * 32));
                    shieldNearTop.Y = (int)(Position.Y
                        - 20 + (Math.Sin((blockAngle - 90) * 0.0174532925) * 32));
                    shieldMid.X = (int)(Position.X
                        + 20 + (Math.Cos((blockAngle - 90) * 0.0174532925) * 24));
                    shieldMid.Y = (int)(Position.Y
                        - 20 + (Math.Sin((blockAngle - 90) * 0.0174532925) * 24));
                    shieldNearBtm.X = (int)(Position.X
                        + 20 + (Math.Cos((blockAngle - 90) * 0.0174532925) * 16));
                    shieldNearBtm.Y = (int)(Position.Y
                        - 20 + (Math.Sin((blockAngle - 90) * 0.0174532925) * 16));
                    shieldBtm.X = (int)(Position.X
                        + 20 + (Math.Cos((blockAngle - 90) * 0.0174532925) * 8));
                    shieldBtm.Y = (int)(Position.Y
                        - 20 + (Math.Sin((blockAngle - 90) * 0.0174532925) * 8));
                }
            }

            if (isBlocking || timespentblocking > 0) timespentblocking++;
            else if (!isBlocking && timespentblocking > 15) timespentblocking = 0;
            //sword hit box movement
            if (attackcooldown >= 0)
            {
                attackcooldown--;
                if (!left)
                {
                    attackangle += 3;
                    swordTip.X = (int)(Position.X
                        + 10 + (Math.Cos((attackangle - 90) * 0.0174532925) * 52));
                    swordTip.Y = (int)(Position.Y
                        - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 52));
                    swordMid.X = (int)(Position.X
                        + 10 + (Math.Cos((attackangle - 90) * 0.0174532925) * 30));
                    swordMid.Y = (int)(Position.Y
                        - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 30));
                    swordHilt.X = (int)(Position.X
                        + 10 + (Math.Cos((attackangle - 90) * 0.0174532925) * 12));
                    swordHilt.Y = (int)(Position.Y
                        - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 12));
                    if (attackangle > 115 && attackcooldown > 0)
                    {
                        attackcooldown = 0;
                        stuncooldown = 15;
                    }
                }
                else
                {
                    attackangle -= 3;
                    swordTip.X = (int)(Position.X
                        - 10 + (Math.Cos((attackangle - 90) * 0.0174532925) * 52));
                    swordTip.Y = (int)(Position.Y
                        - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 52));
                    swordMid.X = (int)(Position.X
                        - 10 + (Math.Cos((attackangle - 90) * 0.0174532925) * 30));
                    swordMid.Y = (int)(Position.Y
                        - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 30));
                    swordHilt.X = (int)(Position.X
                        - 10 + (Math.Cos((attackangle - 90) * 0.0174532925) * 12));
                    swordHilt.Y = (int)(Position.Y
                        - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 12));
                    if (attackangle < 245 && attackcooldown > 0)
                    {
                        attackcooldown = 0;
                        stuncooldown = 15;
                    }
                }
            }
            else attackangle = 0;

            if (IsAlive && IsOnGround)
            {
                if (Math.Abs(Velocity.X) - 0.02f > 0)
                {
                    sprite.PlayAnimation(runAnimation);
                }
                else if (isCrouching)
                    sprite.PlayAnimation(crouchAnimation);
                else if (isBlocking && attackcooldown <= 0)
                    sprite.PlayAnimation(blockAnimation);
                else if (attackcooldown > 0)
                    sprite.PlayAnimation(attackAnimation);
                else
                {
                    sprite.PlayAnimation(idleAnimation);
                }
            }

            stuncooldown--;
            // Clear input.
            movement = 0.0f;
            //isJumping = false;
            isAttacking = false;
            if (velocity.X < 0) left = true;
            if (velocity.X > 0) left = false;
        }

        /// <summary>
        /// Gets player horizontal movement and jump commands from input.
        /// </summary>
        private void GetInput(
            KeyboardState keyboardState,
            GamePadState gamePadState,
            TouchCollection touchState,
            AccelerometerState accelState,
            DisplayOrientation orientation)
        {
            if(keyboardState.IsKeyDown(Keys.Delete)) skipthislevel = true;

            // Get analog horizontal movement.
            if (!isBlocking && attackcooldown <= 0 && stuncooldown <= 0) movement = gamePadState.ThumbSticks.Left.X * MoveStickScale;
            else if (isBlocking && attackcooldown <= 0 && stuncooldown <= 0) movement = (gamePadState.ThumbSticks.Left.X * MoveStickScale) / 2;



            //get block angle
            Vector2 rightStick = GamePad.GetState(PlayerIndex.One).ThumbSticks.Right;

            rightStick.Normalize();

            float angle = (float)Math.Acos(rightStick.Y);

            if (rightStick.X < 0.0f)

                angle = -angle;

            if (rightStick.Y <= -0.1 || rightStick.Y >= 0.1) blockAngle = angle;
            // Ignore small movements to prevent running in place.
            if (Math.Abs(movement) < 0.3f)
                movement = 0.0f;

            // Move the player with accelerometer
            if (Math.Abs(accelState.Acceleration.Y) > 0.10f)
            {
                // set our movement speed
                if (attackcooldown <= 0 && !isBlocking && stuncooldown <= 0) movement = MathHelper.Clamp(-accelState.Acceleration.Y * AccelerometerScale, -1f, 1f);

                // if we're in the LandscapeLeft orientation, we must reverse our movement
                if (orientation == DisplayOrientation.LandscapeRight)
                    movement = -movement;
            }

            if (amplayer1 && ((gamePadState.IsButtonDown(SprintButton)) 
                || keyboardState.IsKeyDown(Keys.CapsLock)) ) isSprinting = true;
            else if (amplayer2 &&( (gamePadState.IsButtonDown(SprintButton))
                || keyboardState.IsKeyDown(Keys.End))) isSprinting = true;
            else isSprinting = false;

            if (amplayer1 && (gamePadState.IsButtonDown(MagicButton1))
                || (gamePadState.IsButtonDown(MagicButton2))
                || keyboardState.IsKeyDown(Keys.D2)) isMagicAttack = true;
            else if (amplayer2 && ((gamePadState.IsButtonDown(MagicButton1))
                || (gamePadState.IsButtonDown(MagicButton2))
                || keyboardState.IsKeyDown(Keys.Enter)))  isMagicAttack = true;
            else isMagicAttack = false;

            if (amplayer1) isCrouching = (gamePadState.IsButtonDown(Buttons.DPadDown) ||
                 (gamePadState.ThumbSticks.Left.Y == -1f) ||
                  keyboardState.IsKeyDown(Keys.S));

            if (amplayer2) isCrouching = (gamePadState.IsButtonDown(Buttons.DPadDown) ||
                (gamePadState.ThumbSticks.Left.Y == -1f) ||
                  keyboardState.IsKeyDown(Keys.Down));


            // If any digital horizontal movement input is found, override the analog movement.
            if (amplayer1) if (gamePadState.IsButtonDown(Buttons.DPadLeft) ||
                  keyboardState.IsKeyDown(Keys.A))
                {
                    if (attackcooldown <= 0 && stuncooldown <= 0) movement = -1.0f;
                    else if (isBlocking && attackcooldown <= 0 && stuncooldown <= 0) movement = -0.5f;
                }
             else if (gamePadState.IsButtonDown(Buttons.DPadRight) ||
                         keyboardState.IsKeyDown(Keys.D))
                {
                    if (attackcooldown <= 0 && stuncooldown <= 0) movement = 1.0f;
                    else if (isBlocking && attackcooldown <= 0 && stuncooldown <= 0) movement = 0.5f;
                }

            if (amplayer2) if (gamePadState.IsButtonDown(Buttons.DPadLeft) ||
                  keyboardState.IsKeyDown(Keys.Left))
                {
                    if (attackcooldown <= 0 && stuncooldown <= 0) movement = -1.0f;
                    else if (isBlocking && attackcooldown <= 0 && stuncooldown <= 0) movement = -0.5f;
                }
             else if (gamePadState.IsButtonDown(Buttons.DPadRight) ||
                         keyboardState.IsKeyDown(Keys.Right))
                {
                    if (attackcooldown <= 0 && stuncooldown <= 0) movement = 1.0f;
                    else if (isBlocking && attackcooldown <= 0 && stuncooldown <= 0) movement = 0.5f;
                }

            //check if the player wants to block
            if (amplayer1) isBlocking =
                  gamePadState.IsButtonDown(BlockButton) ||
                  keyboardState.IsKeyDown(Keys.Tab) ||
                  keyboardState.IsKeyDown(Keys.LeftControl);

            if (amplayer2) isBlocking =
                  gamePadState.IsButtonDown(BlockButton) ||
                  keyboardState.IsKeyDown(Keys.RightShift);

            // Check if the player wants to attack.
            if (amplayer1) isAttacking =
                gamePadState.IsButtonDown(AttackButton1) ||
                gamePadState.IsButtonDown(AttackButton2) ||
                keyboardState.IsKeyDown(Keys.LeftShift);

            if (amplayer2) isAttacking =
                gamePadState.IsButtonDown(AttackButton1) ||
                gamePadState.IsButtonDown(AttackButton2) ||
                keyboardState.IsKeyDown(Keys.RightControl);

            // Check if the player wants to jump.

            if (amplayer1) isJumping =
                  gamePadState.IsButtonDown(JumpButton) ||
                  keyboardState.IsKeyDown(Keys.Space) ||
                  keyboardState.IsKeyDown(Keys.W);

            if (amplayer2) isJumping =
                  gamePadState.IsButtonDown(JumpButton) ||
                  keyboardState.IsKeyDown(Keys.Enter) ||
                  keyboardState.IsKeyDown(Keys.Up);

        }

        /// <summary>
        /// Updates the player's velocity and position based on input, gravity, etc.
        /// </summary>
        public void ApplyPhysics(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 previousPosition = Position;

            // Base velocity is a combination of horizontal movement control and
            // acceleration downward due to gravity.
            velocity.X += movement * MoveAcceleration * elapsed;
            velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);

            velocity.X = DoAttack(velocity.X, gameTime);
            velocity.X = DoMagic(velocity.X, gameTime);
            velocity.Y = DoJump(velocity.Y, gameTime);

            // Apply pseudo-drag horizontally.
            if (IsOnGround)
                velocity.X *= GroundDragFactor;
            else
                velocity.X *= AirDragFactor;

            // Prevent the player from running faster than his top speed.            
            if (isSprinting && stamina > 0) velocity.X = MathHelper.Clamp(velocity.X, -MaxMoveSpeed * 2, MaxMoveSpeed * 2);
            else velocity.X = MathHelper.Clamp(velocity.X, -MaxMoveSpeed, MaxMoveSpeed);
            // Apply velocity.
            Position += (velocity) * elapsed;
            Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

            // If the player is now colliding with the level, separate them.
            HandleCollisions();

            // If the collision stopped us from moving, reset the velocity to zero.
            if (Position.X == previousPosition.X)
                velocity.X = 0;

            if (Position.Y == previousPosition.Y)
                velocity.Y = 0;
        }


        public float GetHit(float velocityY)
        {
            jumpSound.Play();
            sprite.PlayAnimation(jumpAnimation);
            velocityY = 1500 * (1.0f - (float)Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));

            return velocityY;
        }

        public float DoMagic(float velocityX, GameTime gameTime)
        {
            // If the player wants to jump
            if (attackcooldown <= 0 && isAlive && isMagicAttack)
            {
                if (PlatformerGame.playerStoleSoul && stamina > 100)
                {
                    attackTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    attackcooldown = 150;
                    stamina -= 100;
                    if (attackTime == 0.0f)
                        attackSound.Play();
                    fireSoul.X = (int)position.X;
                    fireSoul.Y = (int)position.Y - 25;
                    if (left) fireSoulVelocity = -5;
                    else fireSoulVelocity = 5;
                    attackTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                }
            }
            return velocityX;
        }

        private float DoAttack(float velocityX, GameTime gameTime)
        {
            // If the player wants to jump
            if (isAttacking && attackcooldown <= 0)
            {
                // Begin or continue a jump
                if ((!wasAttacking && attackcooldown <= 0 && stamina > 0) || attackTime > 0.0f)
                {
                    attackcooldown = 35;
                    stamina -= 40;
                    if (!left) attackangle = 20;
                    else attackangle = 340;
                    if (attackTime == 0.0f)
                        attackSound.Play();

                    attackTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    //sprite.PlayAnimation(jumpAnimation);
                }

                // If we are in the ascent of the jump
                if (0.0f < attackTime && attackTime <= MaxJumpTime)
                {
                    // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    velocityX = velocityX * (1.0f - (float)Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));
                }
                else
                {
                    // Reached the apex of the jump
                    attackTime = 0.0f;
                }
            }
            else
            {
                // Continues not jumping or cancels a jump in progress
                attackTime = 0.0f;
            }
            wasAttacking = isAttacking;

            return velocityX;
        }


        public void DoBlock(int damagedone)
        {
            if (attackcooldown <= 0)
            {
                damagedone -= (int)(damagedone * 0.8);
                if (stamina > damagedone)
                {
                    stamina -= damagedone;
                }
                else
                {
                    stamina -= damagedone;
                    health -= damagedone;
                    stuncooldown = 80;
                }
            }
        }


        /// <summary>
        /// Calculates the Y velocity accounting for jumping and
        /// animates accordingly.
        /// </summary>
        /// <remarks>
        /// During the accent of a jump, the Y velocity is completely
        /// overridden by a power curve. During the decent, gravity takes
        /// over. The jump velocity is controlled by the jumpTime field
        /// which measures time into the accent of the current jump.
        /// </remarks>
        /// <param name="velocityY">
        /// The player's current velocity along the Y axis.
        /// </param>
        /// <returns>
        /// A new Y velocity if beginning or continuing a jump.
        /// Otherwise, the existing Y velocity.
        /// </returns>
        private float DoJump(float velocityY, GameTime gameTime)
        {
            // If the player wants to jump
            if (isJumping)
            {
                // Begin or continue a jump
                if ((!wasJumping && IsOnGround && stamina > 0) || jumpTime > 0.0f)
                {
                    if (jumpTime == 0.0f)
                    {
                        jumpSound.Play();
                        stamina -= 10;
                    }
                    if (isCrouching) JumpLaunchVelocity = -1000f;
                    else JumpLaunchVelocity = -3500f;
                    jumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    sprite.PlayAnimation(jumpAnimation);
                }

                // If we are in the ascent of the jump
                if (0.0f < jumpTime && jumpTime <= MaxJumpTime)
                {
                    // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    velocityY = JumpLaunchVelocity * (1.0f - (float)Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));
                }
                else
                {
                    // Reached the apex of the jump
                    jumpTime = 0.0f;
                }
            }
            else
            {
                // Continues not jumping or cancels a jump in progress
                jumpTime = 0.0f;
            }
            wasJumping = isJumping;

            return velocityY;
        }

        /// <summary>
        /// Detects and resolves all collisions between the player and his neighboring
        /// tiles. When a collision is detected, the player is pushed away along one
        /// axis to prevent overlapping. There is some special logic for the Y axis to
        /// handle platforms which behave differently depending on direction of movement.
        /// </summary>
        private void HandleCollisions()
        {
            // Get the player's bounding rectangle and find neighboring tiles.
            Rectangle bounds = BoundingRectangle;
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            // Reset flag to search for ground collision.
            isOnGround = false;

            // For each potentially colliding tile,
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable,
                    TileCollision collision = Level.GetCollision(x, y);
                    if (collision != TileCollision.Passable)
                    {
                        // Determine collision depth (with direction) and magnitude.
                        Rectangle tileBounds = Level.GetBounds(x, y);
                        Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                        if (depth != Vector2.Zero)
                        {
                            float absDepthX = Math.Abs(depth.X);
                            float absDepthY = Math.Abs(depth.Y);

                            // Resolve the collision along the shallow axis.
                            if (absDepthY < absDepthX || collision == TileCollision.Platform)
                            {
                                // If we crossed the top of a tile, we are on the ground.
                                if (previousBottom <= tileBounds.Top)
                                    if (!(isCrouching && isJumping))
                                    {
                                        isOnGround = true;
                                        isJumping = false;
                                    }



                                // Ignore platforms, unless we are on the ground.
                                if (collision == TileCollision.Impassable || IsOnGround)
                                {
                                    // Resolve the collision along the Y axis.
                                    Position = new Vector2(Position.X, Position.Y + depth.Y);

                                    // Perform further collisions with the new bounds.
                                    bounds = BoundingRectangle;
                                }
                            }
                            else if (collision == TileCollision.Impassable) // Ignore platforms.
                            {
                                // Resolve the collision along the X axis.
                                Position = new Vector2(Position.X + depth.X, Position.Y);

                                // Perform further collisions with the new bounds.
                                bounds = BoundingRectangle;
                            }
                        }
                    }
                }
            }

            // Save the new bounds bottom.
            previousBottom = bounds.Bottom;
        }

        /// <summary>
        /// Called when the player has been killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This parameter is null if the player was
        /// not killed by an enemy (fell into a hole).
        /// </param>
        public void OnKilled(Enemy killedBy)
        {
            isAlive = false;

            if (killedBy != null)
                killedSound.Play();
            else
                fallSound.Play();

            sprite.PlayAnimation(dieAnimation);
        }

        /// <summary>
        /// Called when this player reaches the level's exit.
        /// </summary>
        public void OnReachedExit()
        {
            sprite.PlayAnimation(celebrateAnimation);
        }

        /// <summary>
        /// Draws the animated player.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Flip the sprite to face the way we are moving.
            if (Velocity.X > 0)
            {
                flip = SpriteEffects.FlipHorizontally;
                left = false;
            }
            else if (Velocity.X < 0)
            {
                flip = SpriteEffects.None;
                left = true;
            }

            //offset position for when we're blocking
            int swordoffset = 0;

            if (isBlocking && attackcooldown <= 0)
                if (left) { attackangle = 0; swordoffset = 12; } //250
                else { attackangle = 0; swordoffset = -12; } //110

            //drawing the sword
            if (left)
                spriteBatch.Draw(swordAnimation,
                     new Vector2(Position.X - 10 + swordoffset, Position.Y - 20 - (swordoffset / 2)),
                     null, Color.White,
                     (Single)(attackangle * 0.0174532925), new Vector2(5, 69), (float)0.75, flip, 0);
            else
                spriteBatch.Draw(swordAnimation,
                   new Vector2(Position.X + 10 + swordoffset, Position.Y - 20 + (swordoffset / 2)),
                   null, Color.White,
                   (Single)(attackangle * 0.0174532925), new Vector2(5, 69), (float)0.75, flip, 0);
            ///
            Color pcolor = Color.White;
            if (amplayer1) pcolor = player1color;
            if (amplayer2) pcolor = player2color;


            if (PlatformerGame.playerStoleSoul)
                spriteBatch.Draw(fireAttack, fireSoul, pcolor);                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              

            // Draw the player.
            //first, the player needs to be drawn if dead
            if (!isAlive) sprite.Draw(gameTime, spriteBatch, Position, flip, pcolor);

            //if the player got hit, they should flash
            else if (hitcooldown <= 0) sprite.Draw(gameTime, spriteBatch, Position, flip, pcolor);
            else if (hitcooldown > 0 && hitcooldown < 5) ;
            else if (hitcooldown > 5 && hitcooldown < 10) sprite.Draw(gameTime, spriteBatch, Position, flip, pcolor);
            else if (hitcooldown > 15 && hitcooldown < 20) ;
            else if (hitcooldown > 20 && hitcooldown < 25) sprite.Draw(gameTime, spriteBatch, Position, flip, pcolor);
            else if (hitcooldown > 25 && hitcooldown < 30) ;
            else if (hitcooldown > 30 && hitcooldown < 35) sprite.Draw(gameTime, spriteBatch, Position, flip, pcolor);
            else if (hitcooldown > 35 && hitcooldown < 40) ;
            else if (hitcooldown > 40 && hitcooldown < 45) sprite.Draw(gameTime, spriteBatch, Position, flip, pcolor);
            else if (hitcooldown > 45 && hitcooldown < 50) ;
            else sprite.Draw(gameTime, spriteBatch, Position, flip, pcolor);

            //we draw the shield last as, with respect to the "camera," it is on top.
            if (left)
            {
                if (isBlocking && attackcooldown <= 0)
                    spriteBatch.Draw(shieldBlock,
                        new Vector2(Position.X - 18, Position.Y - 38),
                        null, Color.White,
                        (Single)(blockAngle /*attackangle * 0.0174532925*/),
                        new Vector2(8, 20), (float)0.6, flip, 0);
                else
                    spriteBatch.Draw(shieldIdle,
                        new Vector2(Position.X + 10 , Position.Y - 25),
                        null, Color.White,
                        (Single)(0 /*attackangle * 0.0174532925*/),
                        new Vector2(15, 20), (float)0.55, flip, 0);
            }

            else
            {
                if (isBlocking && attackcooldown <= 0)
                    spriteBatch.Draw(shieldBlock,
                        new Vector2(Position.X + 18, Position.Y - 38),
                        null, Color.White,
                        (Single)(blockAngle /*attackangle * 0.0174532925*/),
                        new Vector2(8, 20), (float)0.6, flip, 0);
                else
                    spriteBatch.Draw(shieldIdle,
                        new Vector2(Position.X - 10 , Position.Y - 25),
                        null, Color.White,
                        (Single)(0 /*attackangle * 0.0174532925*/),
                        new Vector2(15, 20), (float)0.55, flip, 0);
            }
            //spriteBatch.Draw(swordHitbox, swordTip, Color.Pink);
            //spriteBatch.Draw(swordHitbox, swordMid, Color.Pink);
            //spriteBatch.Draw(swordHitbox, swordHilt, Color.Pink);

            //spriteBatch.Draw(swordHitbox, shieldTop, Color.Green);
            //spriteBatch.Draw(swordHitbox, shieldNearTop, Color.Green);
            //spriteBatch.Draw(swordHitbox, shieldMid, Color.Green);
            //spriteBatch.Draw(swordHitbox, shieldNearBtm, Color.Green);
            //spriteBatch.Draw(swordHitbox, shieldBtm, Color.Green);
        }
    }
}
