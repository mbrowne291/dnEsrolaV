#region File Description
//-----------------------------------------------------------------------------
// Enemy.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Platformer
{

    /// <summary>
    /// Facing direction along the X axis.
    /// </summary>
    enum FaceDirection
    {
        Left = -1,
        Right = 1,
    }


    /// <summary>
    /// A monster who is impeding the progress of our fearless adventurer.
    /// </summary>
    class Enemy
    {
        private Texture2D hudHealth;
        private Texture2D hudBackground;

        private Rectangle hudHealthRect;
        private Rectangle hudBackgroundRect;

        public bool left = true;
        public bool hasSword = true;
        public bool isWizard = false;
        public bool isMiniBoss = false;
        public bool isFinalBoss = false;
        public bool isAlive = true;
        public int health = 80;
        public int deathtimer = 0;
        public Level Level
        {
            get { return level; }
        }
        Level level;

        public SoundEffect monsterKilled;
        private SoundEffect attackSound;
        public int hitcooldown = 0;
        public int stuncooldown = 0;


        /// <summary>
        /// Position in world space of the bottom center of this enemy.
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
        }
        public Vector2 position;

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this enemy in world space.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        private bool isAttacking;
        private bool wasAttacking;
        private float attackTime;

        // Animations
        private Texture2D swordAnimation;
        private Texture2D swordHitbox;

        private Texture2D fireball;
        public Rectangle fireballHitbox = new Rectangle(-30,-30,15,15);

        public Rectangle triplefireHitbox1 = new Rectangle(-30, -30, 15, 15);
        public Rectangle triplefireHitbox2 = new Rectangle(-30, -30, 15, 15);
        public Rectangle triplefireHitbox3 = new Rectangle(-30, -30, 15, 15);

        private int fireballVelocity = 0;
     
        private Animation runAnimation;
        private Animation idleAnimation;
        private Animation deathAnimation;
        private Animation attackAnimation;
        private Animation stunAnimation;
        private AnimationPlayer sprite;

        /// <summary>
        /// The direction this enemy is facing and moving along the X axis.
        /// </summary>
        private FaceDirection direction = FaceDirection.Left;

        /// <summary>
        /// How long this enemy has been waiting before turning around.
        /// </summary>
        private float waitTime;

        /// <summary>
        /// How long to wait before turning around.
        /// </summary>
        private const float MaxWaitTime = 0.5f;

        /// <summary>
        /// The speed at which this enemy moves along the X axis.
        /// </summary>
        private const float MoveSpeed = 30.0f;

        public Rectangle swordTip = new Rectangle(0, 0, 5, 5);
        public Rectangle swordMid = new Rectangle(0, 0, 5, 5);
        public Rectangle swordHilt = new Rectangle(0, 0, 5, 5);


        public int attackcooldown = 0;
        private int attackangle = 0;

        /// <summary>
        /// Constructs a new Enemy.
        /// </summary>
        public Enemy(Level level, Vector2 position, string spriteSet)
        {
            this.level = level;
            this.position = position;

            LoadContent(spriteSet);
        }

        /// <summary>
        /// Loads a particular enemy sprite sheet and sounds.
        /// </summary>
        public void LoadContent(string spriteSet)
        {
            // Load animations.
            if (spriteSet == "MonsterA")
            {
                hasSword = false;
                isWizard = true;
                health = 60;
            }
            if (spriteSet == "MonsterB")
            {
                hasSword = false;
                isMiniBoss = true;
                health = 450;
            }
            if (spriteSet == "MonsterD")
            {
                hasSword = false;
                isFinalBoss = true;
                health = 800;
            }

            hudHealth = Level.Content.Load<Texture2D>("Sprites/player/red");
            hudBackground = Level.Content.Load<Texture2D>("Sprites/player/black");
            fireball = Level.Content.Load<Texture2D>("Sprites/fireball");
            if (isFinalBoss)
            {
                Level.Content.Load<Texture2D>("Sprites/Lightning");
                fireballHitbox = new Rectangle(-100, -100, 20, 200);
            }
            spriteSet = "Sprites/" + spriteSet + "/";
            swordAnimation = Level.Content.Load<Texture2D>("Sprites/Player/gladius");
            swordHitbox = Level.Content.Load<Texture2D>("Sprites/Player/pink");

            runAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Run"), 0.1f, true);
            idleAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Idle"), 0.15f, true);
            deathAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Death"), 0.3f, false);
            attackAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Attack"), 0.25f, false);
            stunAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Stun"), 0.3f, false);
            sprite.PlayAnimation(idleAnimation);

            monsterKilled = Level.Content.Load<SoundEffect>("Sounds/MonsterKilled");
            attackSound = Level.Content.Load<SoundEffect>("Sounds/throw");

            // Calculate bounds within texture size.
            int width = (int)(idleAnimation.FrameWidth * 0.35);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.7);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            hudBackgroundRect = new Rectangle( (int)position.X, (int)position.Y + height + 5, width, 3 );
            hudHealthRect = new Rectangle((int)position.X, (int)position.Y + height + 5, width, 3);
        }


        /// <summary>
        /// Paces back and forth along a platform, waiting at either end.
        /// </summary>
        public void Update(GameTime gameTime, Player player1, Player player2)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Calculate tile position based on the side we are walking towards.
            float posX = 0;
            posX = Position.X + localBounds.Width / 2 * (int)direction;
            int tileX = (int)Math.Floor(posX / Tile.Width) - (int)direction;
            int tileY = (int)Math.Floor(Position.Y / Tile.Height);
            if ((int)direction == -1) left = true;
            else left = false;

            if (!left && (hasSword || isFinalBoss))
            {
                swordTip.X = (int)(Position.X  + (Math.Cos((attackangle - 90) * 0.0174532925) * 40));
                swordTip.Y = (int)(Position.Y - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 40));
                swordMid.X = (int)(Position.X  + (Math.Cos((attackangle - 90) * 0.0174532925) * 25));
                swordMid.Y = (int)(Position.Y - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 25));
                swordHilt.X = (int)(Position.X + (Math.Cos((attackangle - 90) * 0.0174532925) * 12));
                swordHilt.Y = (int)(Position.Y - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 12));
            }

            else if (left && (hasSword || isFinalBoss))
            {
                swordTip.X = (int)(Position.X - 10 + (Math.Cos((attackangle - 90) * 0.0174532925) * 40));
                swordTip.Y = (int)(Position.Y - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 40));
                swordMid.X = (int)(Position.X - 10 + (Math.Cos((attackangle - 90) * 0.0174532925) * 25));
                swordMid.Y = (int)(Position.Y - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 25));
                swordHilt.X = (int)(Position.X - 10 + (Math.Cos((attackangle - 90) * 0.0174532925) * 12));
                swordHilt.Y = (int)(Position.Y - 25 + (Math.Sin((attackangle - 90) * 0.0174532925) * 12));
            }

            if (isAlive && attackcooldown >= 0 && stuncooldown <= 0)
            {
                attackcooldown--;
                if (!left && attackangle < 110 && hasSword)
                {
                    attackangle += 2;
                }
                else if (left && attackangle > 250 && hasSword)
                {
                    attackangle -= 2;
                }
                if (attackcooldown == 0) stuncooldown = 15;
            }
            else if (!isAlive && stuncooldown > 0 && hasSword)
            {
                if (!left) attackangle = 330;
                else attackangle = 30;

            }
            else attackangle = 0;

            if (isWizard)
            {
                fireballHitbox.X += fireballVelocity;
            }

            if (isMiniBoss)
            {
                triplefireHitbox1.X += fireballVelocity;
                triplefireHitbox1.Y += fireballVelocity;
                triplefireHitbox2.X += fireballVelocity;
                triplefireHitbox3.X += fireballVelocity;
                triplefireHitbox3.Y -= fireballVelocity;

                if (!left)
                {
                    swordTip.X = (int)(Position.X + 40);
                    swordTip.Y = (int)(Position.Y - 25);
                    swordMid.X = (int)(Position.X + 25);
                    swordMid.Y = (int)(Position.Y - 25);
                    swordHilt.X = (int)(Position.X + 12);
                    swordHilt.Y = (int)(Position.Y - 25);
                }
                else
                {
                    swordTip.X = (int)(Position.X - 40 );
                    swordTip.Y = (int)(Position.Y - 25);
                    swordMid.X = (int)(Position.X - 25 );
                    swordMid.Y = (int)(Position.Y - 25);
                    swordHilt.X = (int)(Position.X - 12 );
                    swordHilt.Y = (int)(Position.Y - 25);
                }


            }


            if (isAlive && waitTime > 0 && attackcooldown <= 0 && stuncooldown <= 0)
            {
                // Wait for some amount of time.
                waitTime = Math.Max(0.0f, waitTime - (float)gameTime.ElapsedGameTime.TotalSeconds);
                if (waitTime <= 0.0f)
                {
                    // Then turn around.
                    direction = (FaceDirection)(-(int)direction);
                }
            }
            else
            {
                // If we are about to run into a wall or off a cliff, start waiting.
                if (isAlive && 
                    Level.GetCollision(tileX + (int)direction, tileY - 1) == TileCollision.Impassable ||                                  Level.GetCollision(tileX + (int)direction, tileY) == TileCollision.Passable ||
                    ((Position.X - player1.Position.X < 35
                        && Position.X - player1.Position.X > 0
                        && Position.Y - player1.Position.Y > -50 
                        && Position.Y - player1.Position.Y < 50)
                        ||
                        (Position.X - player1.Position.X > -35
                        && Position.X - player1.Position.X < 0)
                        && Position.Y - player1.Position.Y > -50 
                        && Position.Y - player1.Position.Y < 50)
                        ||
                    ((Position.X - player2.Position.X < 35
                        && Position.X - player2.Position.X > 0
                        && (Position.Y - player2.Position.Y > -50 
                        && Position.Y - player2.Position.Y < 50))
                        ||
                        (Position.X - player2.Position.X > -35
                        && Position.X - player2.Position.X < 0
                        &&Position.Y - player2.Position.Y > -50 
                        && Position.Y - player2.Position.Y < 50)) )
                {
                    waitTime = MaxWaitTime;
                }
                //if the player is nearby, walk over to them
                else if (isAlive && (Position.Y - player1.Position.Y > -50 && Position.Y - player1.Position.Y < 50)
                    || (Position.Y - player2.Position.Y > -50 && Position.Y - player2.Position.Y < 50))
                {
                    if ((Position.X - player1.Position.X > -400
                        && Position.X - player1.Position.X < -35)
                        ||
                        (Position.X - player2.Position.X > -400
                        && Position.X - player2.Position.X < -35))
                    {
                        direction = FaceDirection.Right;
                        Vector2 velocity = new Vector2((int)direction * MoveSpeed * elapsed, 0.0f);
                        if (isAlive && attackTime <= 0 && stuncooldown <= 0) position = position + velocity;
                    }
                    else if ((Position.X - player1.Position.X < 400
                        && Position.X - player1.Position.X > 35)
                        ||
                        (Position.X - player2.Position.X < 400
                        && Position.X - player2.Position.X > 35))
                    {
                        direction = FaceDirection.Left;
                        Vector2 velocity = new Vector2((int)direction * MoveSpeed * elapsed, 0.0f);
                        if (isAlive && attackTime <= 0 && stuncooldown <= 0) position = position + velocity;
                    }
                }
                else if (isAlive )
                {
                    // Move in the current direction.
                    Vector2 velocity = new Vector2((int)direction * MoveSpeed * elapsed, 0.0f);
                    if (isAlive && attackTime <= 0 && stuncooldown <= 0) position = position + velocity;
                }
            }
            if (stuncooldown > 0) stuncooldown--;
            hudBackgroundRect.X = BoundingRectangle.X;
            hudBackgroundRect.Y = BoundingRectangle.Y - BoundingRectangle.Height + 5;
            hudHealthRect.X = BoundingRectangle.X;
            hudHealthRect.Y = BoundingRectangle.Y - BoundingRectangle.Height + 5;
            if (hasSword) hudHealthRect.Width = (int)Math.Round((((double)health / 80.0)) * BoundingRectangle.Width);
            if (isWizard) hudHealthRect.Width = (int)Math.Round((((double)health / 60.0)) * BoundingRectangle.Width);
            if (isMiniBoss) hudHealthRect.Width = 
                (int)Math.Round((((double)health / 450.0)) * BoundingRectangle.Width);
            if (isFinalBoss) hudHealthRect.Width = 
                (int)Math.Round((((double)health / 800.0)) * BoundingRectangle.Width);
                //= new Rectangle((int)position.X, (int)position.Y + height + 5, width, 5);
        }

        public void OnKilled()
        {
            isAlive = false;
        }

        public float DoAttack(float velocityX, GameTime gameTime)
        {
            // If the player wants to jump
            if (attackcooldown <= 0 && isAlive)
            {
                if(!isWizard)
                {
                // Begin or continue a jump
                    if ((!wasAttacking && attackcooldown <= 0))
                    {
                        attackcooldown = 60;
                        if (!left) attackangle = 0;
                        else attackangle = 0;
                        if (attackTime == 0.0f)
                            attackSound.Play();

                        attackTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                        //sprite.PlayAnimation(jumpAnimation);
                        if (isFinalBoss)
                        {
                            attackcooldown = 100;
                            if (fireballHitbox.X < 0) fireballHitbox.X = (int)Position.X - 10;
                            else fireballHitbox.X -= 20;
                            fireballHitbox.Y = (int)Position.Y;
                        }
                        if (isMiniBoss)
                        {
                            attackcooldown = 80;
                            triplefireHitbox1.X = (int)swordTip.X;
                            triplefireHitbox1.Y = (int)swordTip.Y;
                            triplefireHitbox2.X = (int)swordTip.X;
                            triplefireHitbox2.Y = (int)swordTip.Y;
                            triplefireHitbox3.X = (int)swordTip.X;
                            triplefireHitbox3.Y = (int)swordTip.Y;
                            if (left) fireballVelocity = -5;
                            else fireballVelocity = 5;
                            if (PlatformerGame.playerSparedBoss && PlatformerGame.levelIndex == 4)
                            {
                                left = false;
                                
                            }
                        }

                    }
                }
                else if (isWizard)
                {
                    attackcooldown = 150;
                    if (attackTime == 0.0f)
                        attackSound.Play();
                    fireballHitbox.X = (int)Position.X ;
                    fireballHitbox.Y = (int)Position.Y - 45;
                    if (left) fireballVelocity = -5;
                    else fireballVelocity = 5;
                    attackTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
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

        /// <summary>
        /// Draws the animated enemy.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Stop running when the game is paused or before turning around.
            if (isAlive && attackcooldown <= 0 && stuncooldown <= 0
                &&( !Level.player1.IsAlive || !Level.Player2.IsAlive ||
                Level.ReachedExit ||
                Level.TimeRemaining == TimeSpan.Zero ||
                waitTime > 0) )
            {
                sprite.PlayAnimation(idleAnimation);
            }
            else if (!isAlive)
            {
                sprite.PlayAnimation(deathAnimation);
            }
            else if(isAlive && attackcooldown > 0 && stuncooldown <= 0)
            {
                sprite.PlayAnimation(attackAnimation);
            }
            else if (stuncooldown > 0 && isAlive)
            {
                sprite.PlayAnimation(stunAnimation);
            }
            else
            {
                sprite.PlayAnimation(runAnimation);
            }


            // Draw facing the way the enemy is moving.
            SpriteEffects flip = direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            sprite.Draw(gameTime, spriteBatch, Position, flip, new Color(1f - (deathtimer / 255), 1f - (deathtimer / 255), 1f - (deathtimer / 255), 1f - (deathtimer / 255)));


            if (hasSword && (double)(health/80) < 0.9)
            {
                spriteBatch.Draw(hudBackground, hudBackgroundRect, Color.White);
                spriteBatch.Draw(hudHealth, hudHealthRect, Color.White);
            }

            else if (isWizard && (double)(health / 60) < 0.9)
            {
                spriteBatch.Draw(hudBackground, hudBackgroundRect, Color.White);
                spriteBatch.Draw(hudHealth, hudHealthRect, Color.White);
            }
            else if (isMiniBoss && (double)(health / 450) < 0.9)
            {
                spriteBatch.Draw(hudBackground, hudBackgroundRect, Color.White);
                spriteBatch.Draw(hudHealth, hudHealthRect, Color.White);
            }
            else if (isFinalBoss && (double)(health / 800) < 0.9)
            {
                spriteBatch.Draw(hudBackground, hudBackgroundRect, Color.White);
                spriteBatch.Draw(hudHealth, hudHealthRect, Color.White);
            }


            if (left && hasSword)
            {
               /* if(isAlive)spriteBatch.Draw(swordAnimation,
                new Vector2(Position.X - 10, Position.Y - 25),
                null, Color.White,
                (Single)(attackangle * 0.0174532925), new Vector2(5, 69), (float)0.75, flip, 0);*/
                //spriteBatch.Draw(swordHitbox, swordTip, Color.Pink);
                //spriteBatch.Draw(swordHitbox, swordMid, Color.Pink);
                //spriteBatch.Draw(swordHitbox, swordHilt, Color.Pink);
            }

            else if (!left && hasSword)
            {
                /*if(isAlive)spriteBatch.Draw(swordAnimation,
                   new Vector2(Position.X + 10, Position.Y - 25),
                   null, Color.White,
                   (Single)(attackangle * 0.0174532925), new Vector2(5, 69), (float)0.75, flip, 0);*/
                //spriteBatch.Draw(swordHitbox, swordTip, Color.Pink);
                //spriteBatch.Draw(swordHitbox, swordMid, Color.Pink);
                //spriteBatch.Draw(swordHitbox, swordHilt, Color.Pink);
            }
            if (isWizard)
            {
                spriteBatch.Draw(fireball,
                   new Vector2(fireballHitbox.X, fireballHitbox.Y),
                   null, Color.White,
                   (Single)(attackangle * 0.0174532925), new Vector2(15, 15), (float)0.5, flip, 0);
                attackangle+=12;
            }
            if (isMiniBoss)
            {
                spriteBatch.Draw(fireball,
                   new Vector2(triplefireHitbox1.X, triplefireHitbox1.Y),
                   null, Color.White,
                   (Single)(attackangle * 0.0174532925), new Vector2(15, 15), (float)0.5, flip, 0);
                spriteBatch.Draw(fireball,
                    new Vector2(triplefireHitbox2.X, triplefireHitbox2.Y),
                    null, Color.White,
                    (Single)(attackangle * 0.0174532925), new Vector2(15, 15), (float)0.5, flip, 0);
                spriteBatch.Draw(fireball,
                   new Vector2(triplefireHitbox3.X, triplefireHitbox3.Y),
                   null, Color.White,
                   (Single)(attackangle * 0.0174532925), new Vector2(15, 15), (float)0.5, flip, 0);
                //spriteBatch.Draw(swordHitbox, swordTip, Color.Pink);
                //spriteBatch.Draw(swordHitbox, swordMid, Color.Pink);
                //spriteBatch.Draw(swordHitbox, swordHilt, Color.Pink);
                attackangle += 12;
            }
            if (isFinalBoss)
            {
                spriteBatch.Draw(fireball,
                   new Vector2(fireballHitbox.X, fireballHitbox.Y),
                   null, Color.White,
                   (Single)(attackangle * 0.0174532925), new Vector2(15, 15), (float)0.5, flip, 0);
                //spriteBatch.Draw(swordHitbox, swordTip, Color.Pink);
                //spriteBatch.Draw(swordHitbox, swordMid, Color.Pink);
                //spriteBatch.Draw(swordHitbox, swordHilt, Color.Pink);
                attackangle += 12;
            }

        }
    }
}
