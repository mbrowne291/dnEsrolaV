#region File Description
//-----------------------------------------------------------------------------
// Level.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Input;

namespace ValorsEnd
{
    /// <summary>
    /// A uniform grid of tiles with collections of gems and enemies.
    /// The level owns the player and controls the game's win and lose
    /// conditions as well as scoring.
    /// </summary>
    class Level : IDisposable
    {
        // Physical structure of the level.
        private Tile[,] tiles;
        private Texture2D[] layers;
        // The layer which entities are drawn on top of.
        private const int EntityLayer = 2;
        

        // Entities in the level.
        public Player Player1
        {
            get { return player1; }
        }
        public Player player1;

		public Player Player2
        {
            get { return player2; }
        }
        public Player player2;

        public Enemy MiniAlly;
		
        private List<Gem> gems = new List<Gem>();
        public List<Enemy> enemies = new List<Enemy>();

        // Key locations in the level.        
        private Vector2 start1;
		private Vector2 start2;
        private Point exit = InvalidPosition;
        private static readonly Point InvalidPosition = new Point(-1, -1);

        // Level game state.
        private Random random = new Random(354668); // Arbitrary, but constant seed

        public int Score
        {
            get { return score; }
        }
        int score;

        public bool ReachedExit
        {
            get { return reachedExit; }
        }
        bool reachedExit;

        public TimeSpan TimeRemaining
        {
            get { return timeRemaining; }
        }
        TimeSpan timeRemaining;

        private const int PointsPerSecond = 5;

        // Level content.        
        public ContentManager Content
        {
            get { return content; }
        }
        ContentManager content;

        private SoundEffect exitReachedSound;

        #region Loading

        /// <summary>
        /// Constructs a new level.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider that will be used to construct a ContentManager.
        /// </param>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        public Level(IServiceProvider serviceProvider, Stream fileStream, int levelIndex)
        {
            // Create a new content manager to load content used just by this level.
            content = new ContentManager(serviceProvider, "Content");

            timeRemaining = TimeSpan.FromMinutes(2.0);

            LoadTiles(fileStream);

            // Load background layer textures. For now, all levels must
            // use the same backgrounds and only use the left-most part of them.
            layers = new Texture2D[3];
            for (int i = 0; i < layers.Length; ++i)
            {
                // Choose a random segment if each background layer for level variety.
                int segmentIndex = levelIndex;
                layers[i] = Content.Load<Texture2D>("Backgrounds/Layer" + i + "_" + segmentIndex);
            }

            // Load sounds.
            exitReachedSound = Content.Load<SoundEffect>("Sounds/ExitReached");
        }

        /// <summary>
        /// Iterates over every tile in the structure file and loads its
        /// appearance and behavior. This method also validates that the
        /// file is well-formed with a player start point, exit, etc.
        /// </summary>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        private void LoadTiles(Stream fileStream)
        {
            // Load the level and ensure all of the lines are the same length.
            int width;
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line = reader.ReadLine();
                width = line.Length;
                while (line != null)
                {
                    lines.Add(line);
                    if (line.Length != width)
                        throw new Exception(String.Format("The length of line {0} is different from all preceeding lines.", lines.Count));
                    line = reader.ReadLine();
                }
            }

            // Allocate the tile grid.
            tiles = new Tile[width, lines.Count];

            // Loop over every tile position,
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    // to load each tile.
                    char tileType = lines[y][x];
                    tiles[x, y] = LoadTile(tileType, x, y);
                }
            }

            // Verify that the level has a beginning and an end.
            if (Player1 == null)
                throw new NotSupportedException("A level must have a starting point.");
            if (exit == InvalidPosition)
                throw new NotSupportedException("A level must have an exit.");

        }

        /// <summary>
        /// Loads an individual tile's appearance and behavior.
        /// </summary>
        /// <param name="tileType">
        /// The character loaded from the structure file which
        /// indicates what should be loaded.
        /// </param>
        /// <param name="x">
        /// The X location of this tile in tile space.
        /// </param>
        /// <param name="y">
        /// The Y location of this tile in tile space.
        /// </param>
        /// <returns>The loaded tile.</returns>
        private Tile LoadTile(char tileType, int x, int y)
        {
            switch (tileType)
            {
                // Blank space
                case '.':
                    return new Tile(null, TileCollision.Passable);

                // Exit
                case 'X':
                    return LoadExitTile(x, y);

                // Gem
                case 'G':
                    return LoadGemTile(x, y);

                // Floating platform
                case '-':
                    return LoadTile("Platform", TileCollision.Platform);

                // Various enemies
                case 'A':
                    return LoadEnemyTile(x, y, "MonsterA");
                case 'B':
                    return LoadEnemyTile(x, y, "MonsterB");
                case 'C':
                    return LoadEnemyTile(x, y, "MonsterC");
                case 'D':
                    return LoadEnemyTile(x, y, "MonsterD");

                // Platform block
                case '~':
                    return LoadVarietyTile("BlockB", 2, TileCollision.Platform);

                // Passable block
                case ':':
                    return LoadVarietyTile("BlockB", 2, TileCollision.Passable);

                // Player 1 start point
                case '1':
                    return LoadStartTile(x, y, 1);

                // Player 2 start2 point
                case '2':
                    return LoadStartTile(x, y, 2);

                // Impassable block
                case '#':
                    return LoadVarietyTile("BlockA", 7, TileCollision.Impassable);

                // Unknown tile type character
                default:
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
            }
        }

        /// <summary>
        /// Creates a new tile. The other tile loading methods typically chain to this
        /// method after performing their special logic.
        /// </summary>
        /// <param name="name">
        /// Path to a tile texture relative to the Content/Tiles directory.
        /// </param>
        /// <param name="collision">
        /// The tile collision type for the new tile.
        /// </param>
        /// <returns>The new tile.</returns>
        private Tile LoadTile(string name, TileCollision collision)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
        }


        /// <summary>
        /// Loads a tile with a random appearance.
        /// </summary>
        /// <param name="baseName">
        /// The content name prefix for this group of tile variations. Tile groups are
        /// name LikeThis0.png and LikeThis1.png and LikeThis2.png.
        /// </param>
        /// <param name="variationCount">
        /// The number of variations in this group.
        /// </param>
        private Tile LoadVarietyTile(string baseName, int variationCount, TileCollision collision)
        {
            int index = random.Next(variationCount);
            return LoadTile(baseName + index, collision);
        }


        /// <summary>
        /// Instantiates a player1, puts him in the level, and remembers where to put him when he is resurrected.
        /// </summary>
        private Tile LoadStartTile(int x, int y, int playerNumber)
        {
            // commented out since we want to allow multiple players
            /*if (Player != null)
                throw new NotSupportedException("A level may only have one starting point.");*/

            if (playerNumber == 1)
            {
                start1 = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
                player1 = new Player(this, start1);
            }
            else if (playerNumber == 2)
            {
                start2 = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
                player2 = new Player(this, start2);
            }

            return new Tile(null, TileCollision.Passable);
            
        }

        /// <summary>
        /// Remembers the location of the level's exit.
        /// </summary>
        private Tile LoadExitTile(int x, int y)
        {
            if (exit != InvalidPosition)
                throw new NotSupportedException("A level may only have one exit.");

            exit = GetBounds(x, y).Center;

            return LoadTile("Exit", TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates an enemy and puts him in the level.
        /// </summary>
        private Tile LoadEnemyTile(int x, int y, string spriteSet)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            enemies.Add(new Enemy(this, position, spriteSet));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a gem and puts it in the level.
        /// </summary>
        private Tile LoadGemTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            gems.Add(new Gem(this, new Vector2(position.X, position.Y)));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Unloads the level content.
        /// </summary>
        public void Dispose()
        {
            Content.Unload();
        }

        #endregion

        #region Bounds and collision

        /// <summary>
        /// Gets the collision mode of the tile at a particular location.
        /// This method handles tiles outside of the levels boundries by making it
        /// impossible to escape past the left or right edges, but allowing things
        /// to jump beyond the top of the level and fall off the bottom.
        /// </summary>
        public TileCollision GetCollision(int x, int y)
        {
            // Prevent escaping past the level ends.
            if (x < 0 || x >= Width)
                return TileCollision.Impassable;
            // Allow jumping past the level top and falling through the bottom.
            if (y < 0 || y >= Height)
                return TileCollision.Passable;

            return tiles[x, y].Collision;
        }

        /// <summary>
        /// Gets the bounding rectangle of a tile in world space.
        /// </summary>        
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        /// <summary>
        /// Width of level measured in tiles.
        /// </summary>
        public int Width
        {
            get { return tiles.GetLength(0); }
        }

        /// <summary>
        /// Height of the level measured in tiles.
        /// </summary>
        public int Height
        {
            get { return tiles.GetLength(1); }
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates all objects in the world, performs collision between them,
        /// and handles the time limit with scoring.
        /// </summary>
        public void Update(
            GameTime gameTime, 
            KeyboardState keyboardState, 
            GamePadState gamePadState1,
            GamePadState gamePadState2,  
            TouchCollection touchState, 
            AccelerometerState accelState,
            DisplayOrientation orientation)
        {
            player1.amplayer1 = true;
            player2.amplayer2 = true;

            if (ValorsEndGame.playerSparedBoss && ValorsEndGame.levelIndex == 4)
            {
                MiniAlly.Update(gameTime, player1, player2);
                MiniAlly.DoAttack(5, gameTime);
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies.Count == 0) break;
                if (enemies[i].isAlive == false)
                {
                    enemies[i].deathtimer--;
                    if (enemies[i].deathtimer <= 0)
                    {
                        enemies.RemoveAt(i);
                        if (i != 0) i--;
                        else if (i == 0) break;
                    }
                }

            }
            // Pause while a player is dead
            if (!Player1.IsAlive || !Player2.IsAlive)
            {
                // Still want to perform physics on the player1.
                Player1.ApplyPhysics(gameTime);
                Player2.ApplyPhysics(gameTime);
            }
            else
            {
                timeRemaining -= gameTime.ElapsedGameTime;
                Player1.Update(gameTime, keyboardState, gamePadState1, touchState, accelState, orientation);
                Player2.Update(gameTime, keyboardState, gamePadState2, touchState, accelState, orientation);
                UpdateGems(gameTime);

         // Falling off the bottom of the level kills the player1.
                if (Player1.BoundingRectangle.Top >= Height * Tile.Height)
                    OnPlayerKilled(null, 1);
                if (Player2.BoundingRectangle.Top >= Height * Tile.Height)
                    OnPlayerKilled(null, 2);

                UpdateEnemies(gameTime);

                // The player1 has reached the exit if they are standing on the ground and
                // his bounding rectangle contains the center of the exit tile. They can only
                // exit when they have collected all of the gems.
                if (player1.skipthislevel || player2.skipthislevel)
                {
                    OnExitReached();
                }
                else if ((Player1.IsAlive &&
                    Player1.IsOnGround &&
                    Player1.BoundingRectangle.Contains(exit)) ||
                    (Player2.IsAlive &&
                    Player2.IsOnGround &&
                    Player2.BoundingRectangle.Contains(exit)))
                {
                    OnExitReached();
                }
            }

        }

        /// <summary>
        /// Animates each gem and checks to allows the player1 to collect them.
        /// </summary>
        private void UpdateGems(GameTime gameTime)
        {
            for (int i = 0; i < gems.Count; ++i)
            {
                Gem gem = gems[i];

                gem.Update(gameTime);

                if (gem.BoundingCircle.Intersects(Player1.BoundingRectangle))
                {
                    gems.RemoveAt(i--);
                    OnGemCollected(gem, Player1);
                    player1.health += 40;
                }
                else if (gem.BoundingCircle.Intersects(Player2.BoundingRectangle))
                {
                    gems.RemoveAt(i--);
                    OnGemCollected(gem, Player2);
                    player2.health += 40;
                }
            }
        }

        /// <summary>
        /// Animates each enemy and allow them to kill the player.
        /// </summary>
        private void UpdateEnemies(GameTime gameTime)
        {

            foreach (Enemy enemy in enemies)
            {
                enemy.Update(gameTime, player1, player2);

                if (ValorsEndGame.playerSparedBoss && ValorsEndGame.levelIndex == 4 && enemy.isFinalBoss)
                {
                    if ( (MiniAlly.triplefireHitbox1.Intersects(enemy.BoundingRectangle))
                        || (MiniAlly.triplefireHitbox3.Intersects(enemy.BoundingRectangle))
                        || (MiniAlly.triplefireHitbox3.Intersects(enemy.BoundingRectangle))
                        || MiniAlly.swordTip.Intersects(enemy.BoundingRectangle) )
                        enemy.health -= 50;
                    enemy.hitcooldown = 10;

                    if (enemy.fireballHitbox.Intersects(MiniAlly.BoundingRectangle))
                        MiniAlly.health -= 50;
                    MiniAlly.hitcooldown = 15;
                    if (enemy.swordTip.Intersects(MiniAlly.BoundingRectangle) ||
                        (enemy.swordMid.Intersects(MiniAlly.BoundingRectangle)) ||
                        (enemy.swordHilt.Intersects(MiniAlly.BoundingRectangle))
                        )
                        MiniAlly.health -= 100;
                    MiniAlly.hitcooldown = 15;
                }

                //enemy magic attack right
                if (enemy.isAlive && !enemy.left && enemy.stuncooldown <= 0 && !enemy.hasSword &&
                    (((enemy.Position.X - player1.Position.X > -400
                    && enemy.Position.X - player1.Position.X < 0) &&
                    ((enemy.Position.Y - player1.Position.Y > -50 &&
                    enemy.Position.Y - player1.Position.Y < 50))) ||
                    ((enemy.Position.X - player2.Position.X > -400
                    && enemy.Position.X - player2.Position.X < 0) &&
                    ((enemy.Position.Y - player2.Position.Y > -50 &&
                    enemy.Position.Y - player2.Position.Y < 50))))) 
                    enemy.DoAttack(0, gameTime);
                //enemy magic attack left
                else if (enemy.isAlive && enemy.left && enemy.stuncooldown <= 0 && !enemy.hasSword &&
                    (((enemy.Position.X - player1.Position.X < 400
                    && enemy.Position.X - player1.Position.X > 0) &&
                    ((enemy.Position.Y - player1.Position.Y > -50 &&
                    enemy.Position.Y - player1.Position.Y < 50))) ||
                    ((enemy.Position.X - player2.Position.X < 400
                    && enemy.Position.X - player2.Position.X > 0) &&
                    ((enemy.Position.Y - player2.Position.Y > -50 &&
                    enemy.Position.Y - player2.Position.Y < 50))))) 
                    enemy.DoAttack(0, gameTime);


                //enemy sword attack right
                if (enemy.isAlive && !enemy.left && enemy.stuncooldown <= 0 && !enemy.isWizard &&
                    ( ((enemy.Position.X - player1.Position.X > -70 
                    && enemy.Position.X - player1.Position.X < 0) &&
                    ((enemy.Position.Y - player1.Position.Y > -50 && 
                    enemy.Position.Y - player1.Position.Y < 50) )) ||
                    ((enemy.Position.X - player2.Position.X > -70
                    && enemy.Position.X - player2.Position.X < 0) &&
                    ((enemy.Position.Y - player2.Position.Y > -50 &&
                    enemy.Position.Y - player2.Position.Y < 50))) ) ) 
                    enemy.DoAttack(0, gameTime);
                //enemy sword attack left
                else if (enemy.isAlive && enemy.left && enemy.stuncooldown <= 0 && !enemy.isWizard &&
                    (((enemy.Position.X - player1.Position.X < 70
                    && enemy.Position.X - player1.Position.X > 0) &&
                    ((enemy.Position.Y - player1.Position.Y > -50 &&
                    enemy.Position.Y - player1.Position.Y < 50))) ||
                    ((enemy.Position.X - player2.Position.X < 70
                    && enemy.Position.X - player2.Position.X > 0) &&
                    ((enemy.Position.Y - player2.Position.Y > -50 &&
                    enemy.Position.Y - player2.Position.Y < 50))))) 
                    enemy.DoAttack(0, gameTime);

                // Hit detection for enemy attacks landed on shield
                if (player1.isBlocking && player1.stuncooldown <= 0 
                    && enemy.stuncooldown <= 0 && player1.stamina > -15 && !enemy.isWizard)
                if (enemy.attackcooldown > 0 && (
                    (enemy.swordTip.Intersects(player1.shieldTop)) ||
                    (enemy.swordMid.Intersects(player1.shieldTop)) ||
                    (enemy.swordHilt.Intersects(player1.shieldTop)) ||
                    (enemy.swordTip.Intersects(player1.shieldNearTop)) ||
                    (enemy.swordMid.Intersects(player1.shieldNearTop)) ||
                    (enemy.swordHilt.Intersects(player1.shieldNearTop)) ||
                    (enemy.swordTip.Intersects(player1.shieldMid)) ||
                    (enemy.swordMid.Intersects(player1.shieldMid)) ||
                    (enemy.swordHilt.Intersects(player1.shieldMid)) ||
                    (enemy.swordTip.Intersects(player1.shieldNearBtm)) ||
                    (enemy.swordMid.Intersects(player1.shieldNearBtm)) ||
                    (enemy.swordHilt.Intersects(player1.shieldNearBtm)) ||
                    (enemy.swordTip.Intersects(player1.shieldBtm)) ||
                    (enemy.swordMid.Intersects(player1.shieldBtm)) ||
                    (enemy.swordHilt.Intersects(player1.shieldBtm)) 
                     && enemy.isAlive) )
                {
                    player1.DoBlock(80);
                    //enemy.attackcooldown = 0;
                    if (player1.timespentblocking < 40)
                    {
                        enemy.stuncooldown = 80;
                    }
                    else
                    {
                        enemy.stuncooldown = 20;
                    }
                }

                // Hit detection for enemy attacks landed on shield
                if (player1.isBlocking && player1.stuncooldown <= 0
                    && enemy.stuncooldown <= 0 && player1.stamina > -15 && !enemy.hasSword)
                    if (enemy.attackcooldown > 0 && (
                        (enemy.fireballHitbox.Intersects(player1.shieldTop)) ||
                        (enemy.fireballHitbox.Intersects(player1.shieldNearTop)) ||
                        (enemy.fireballHitbox.Intersects(player1.shieldMid)) ||
                        (enemy.fireballHitbox.Intersects(player1.shieldNearBtm)) ||
                        (enemy.fireballHitbox.Intersects(player1.shieldBtm))
                        ||
                        (enemy.triplefireHitbox1.Intersects(player1.shieldTop)) ||
                        (enemy.triplefireHitbox1.Intersects(player1.shieldNearTop)) ||
                        (enemy.triplefireHitbox1.Intersects(player1.shieldMid)) ||
                        (enemy.triplefireHitbox1.Intersects(player1.shieldNearBtm)) ||
                        (enemy.triplefireHitbox1.Intersects(player1.shieldBtm))
                        ||
                        (enemy.triplefireHitbox2.Intersects(player1.shieldTop)) ||
                        (enemy.triplefireHitbox2.Intersects(player1.shieldNearTop)) ||
                        (enemy.triplefireHitbox2.Intersects(player1.shieldMid)) ||
                        (enemy.triplefireHitbox2.Intersects(player1.shieldNearBtm)) ||
                        (enemy.triplefireHitbox2.Intersects(player1.shieldBtm))
                        ||
                        (enemy.triplefireHitbox3.Intersects(player1.shieldTop)) ||
                        (enemy.triplefireHitbox3.Intersects(player1.shieldNearTop)) ||
                        (enemy.triplefireHitbox3.Intersects(player1.shieldMid)) ||
                        (enemy.triplefireHitbox3.Intersects(player1.shieldNearBtm)) ||
                        (enemy.triplefireHitbox3.Intersects(player1.shieldBtm))
                         && enemy.isAlive))
                    {
                        player1.DoBlock(60);
                        player1.health -= 20;
                        enemy.fireballHitbox.X = -100;
                        enemy.fireballHitbox.Y = -400;
                        //enemy.attackcooldown = 0;
                    }

                if (player2.isBlocking && player2.stuncooldown <= 0
                    && enemy.stuncooldown <= 0 && player2.stamina > -15 && !enemy.isWizard)
                    if (enemy.attackcooldown > 0 && (
                        (enemy.swordTip.Intersects(player2.shieldTop)) ||
                        (enemy.swordMid.Intersects(player2.shieldTop)) ||
                        (enemy.swordHilt.Intersects(player2.shieldTop)) ||
                        (enemy.swordTip.Intersects(player2.shieldNearTop)) ||
                        (enemy.swordMid.Intersects(player2.shieldNearTop)) ||
                        (enemy.swordHilt.Intersects(player2.shieldNearTop)) ||
                        (enemy.swordTip.Intersects(player2.shieldMid)) ||
                        (enemy.swordMid.Intersects(player2.shieldMid)) ||
                        (enemy.swordHilt.Intersects(player2.shieldMid)) ||
                        (enemy.swordTip.Intersects(player2.shieldNearBtm)) ||
                        (enemy.swordMid.Intersects(player2.shieldNearBtm)) ||
                        (enemy.swordHilt.Intersects(player2.shieldNearBtm)) ||
                        (enemy.swordTip.Intersects(player2.shieldBtm)) ||
                        (enemy.swordMid.Intersects(player2.shieldBtm)) ||
                        (enemy.swordHilt.Intersects(player2.shieldBtm))
                        
                         && enemy.isAlive))
                    {
                        player2.DoBlock(80);
                        //enemy.attackcooldown = 0;
                        if (player2.timespentblocking < 40)
                        {
                            enemy.stuncooldown = 80;
                        }
                        else
                        {
                            enemy.stuncooldown = 20;
                        }
                    }

                // Hit detection for enemy attacks landed on shield
                if (player2.isBlocking && player2.stuncooldown <= 0
                    && enemy.stuncooldown <= 0 && player2.stamina > -15 && !enemy.hasSword)
                    if (enemy.attackcooldown > 0 && (
                        (enemy.fireballHitbox.Intersects(player2.shieldTop)) ||
                        (enemy.fireballHitbox.Intersects(player2.shieldNearTop)) ||
                        (enemy.fireballHitbox.Intersects(player2.shieldMid)) ||
                        (enemy.fireballHitbox.Intersects(player2.shieldNearBtm)) ||
                        (enemy.fireballHitbox.Intersects(player2.shieldBtm))
                        ||
                        (enemy.triplefireHitbox1.Intersects(player2.shieldTop)) ||
                        (enemy.triplefireHitbox1.Intersects(player2.shieldNearTop)) ||
                        (enemy.triplefireHitbox1.Intersects(player2.shieldMid)) ||
                        (enemy.triplefireHitbox1.Intersects(player2.shieldNearBtm)) ||
                        (enemy.triplefireHitbox1.Intersects(player2.shieldBtm))
                        ||
                        (enemy.triplefireHitbox2.Intersects(player2.shieldTop)) ||
                        (enemy.triplefireHitbox2.Intersects(player2.shieldNearTop)) ||
                        (enemy.triplefireHitbox2.Intersects(player2.shieldMid)) ||
                        (enemy.triplefireHitbox2.Intersects(player2.shieldNearBtm)) ||
                        (enemy.triplefireHitbox2.Intersects(player2.shieldBtm))
                        ||
                        (enemy.triplefireHitbox3.Intersects(player2.shieldTop)) ||
                        (enemy.triplefireHitbox3.Intersects(player2.shieldNearTop)) ||
                        (enemy.triplefireHitbox3.Intersects(player2.shieldMid)) ||
                        (enemy.triplefireHitbox3.Intersects(player2.shieldNearBtm)) ||
                        (enemy.triplefireHitbox3.Intersects(player2.shieldBtm))
                         && enemy.isAlive))
                    {
                        player2.DoBlock(60);
                        player2.health -= 20;
                        enemy.fireballHitbox.X = -100;
                        enemy.fireballHitbox.Y = -400;
                        //enemy.attackcooldown = 0;
                    }

                // Hit detection for enemy sword attacks
                if (enemy.attackcooldown > 0 && enemy.stuncooldown <= 0) 
                    if((player1.BoundingRectangle.Intersects(enemy.swordTip)) ||
                    (player1.BoundingRectangle.Intersects(enemy.swordMid)) ||
                    (player1.BoundingRectangle.Intersects(enemy.swordHilt))
                     && enemy.isAlive)
                    {
                        if (player1.hitcooldown <= 0 )
                        {
                            if (enemy.hasSword)
                            {
                                player1.health -= 80;
                                player1.hitcooldown = 50;
                                player1.stuncooldown = 20;
                            }
                            else if (enemy.isMiniBoss)
                            {
                                player1.health -= 110;
                                player1.hitcooldown = 50;
                                player1.stuncooldown = 20;
                            }
                            else if (enemy.isFinalBoss)
                            {
                                player1.health -= 200;
                                player1.hitcooldown = 50;
                                player1.stuncooldown = 20;
                            }
                        if (player1.health < 1) OnPlayerKilled(enemy, 1);
                        else player1.GetHit(player1.velocity.Y);
                        }
                    }

                if (enemy.attackcooldown > 0 && enemy.stuncooldown <= 0)
                    if ((player2.BoundingRectangle.Intersects(enemy.swordTip)) ||
                    (player2.BoundingRectangle.Intersects(enemy.swordMid)) ||
                    (player2.BoundingRectangle.Intersects(enemy.swordHilt))
                     && enemy.isAlive)
                    {
                        if (player2.hitcooldown <= 0)
                        {
                            if (enemy.hasSword)
                            {
                                player2.health -= 80;
                                player2.hitcooldown = 50;
                                player2.stuncooldown = 20;
                            }
                            else if (enemy.isMiniBoss)
                            {
                                player2.health -= 110;
                                player2.hitcooldown = 50;
                                player2.stuncooldown = 20;
                            }
                            else if (enemy.isFinalBoss)
                            {
                                player2.health -= 200;
                                player2.hitcooldown = 50;
                                player2.stuncooldown = 20;
                            }

                            if (player2.health < 1) OnPlayerKilled(enemy, 2);
                            else player2.GetHit(player2.velocity.Y);
                        }
                    }

                // Hit detection for enemy magic attacks
                if (enemy.attackcooldown > 0 && enemy.stuncooldown <= 0)
                    if ((player1.BoundingRectangle.Intersects(enemy.fireballHitbox)) 
                        ||(player1.BoundingRectangle.Intersects(enemy.triplefireHitbox1))
                        ||(player1.BoundingRectangle.Intersects(enemy.triplefireHitbox2))
                        ||(player1.BoundingRectangle.Intersects(enemy.triplefireHitbox3))
                     && enemy.isAlive)
                    {
                        if (player1.hitcooldown <= 0)
                        {
                            if (enemy.isWizard)
                            {
                                player1.health -= 80;
                                player1.hitcooldown = 50;
                                player1.stuncooldown = 20;
                            }
                            else if (enemy.isMiniBoss)
                            {
                                player1.health -= 100;
                                player1.hitcooldown = 50;
                                player1.stuncooldown = 20;
                            }
                            else if (enemy.isFinalBoss)
                            {
                                player1.health -= 150;
                                player1.hitcooldown = 50;
                                player1.stuncooldown = 20;
                            }

                            if (player1.health < 1) OnPlayerKilled(enemy, 1);
                            else player1.GetHit(player1.velocity.Y);
                        }
                    }

                if (enemy.attackcooldown > 0 && enemy.stuncooldown <= 0)
                    if ((player2.BoundingRectangle.Intersects(enemy.fireballHitbox))
                        || (player2.BoundingRectangle.Intersects(enemy.triplefireHitbox1))
                        || (player2.BoundingRectangle.Intersects(enemy.triplefireHitbox2))
                        || (player2.BoundingRectangle.Intersects(enemy.triplefireHitbox3))
                     && enemy.isAlive)
                    {
                        if (player2.hitcooldown <= 0)
                        {
                            if (enemy.isWizard)
                            {
                                player2.health -= 80;
                                player2.hitcooldown = 50;
                                player2.stuncooldown = 20;
                            }
                            else if (enemy.isMiniBoss)
                            {
                                player2.health -= 100;
                                player2.hitcooldown = 50;
                                player2.stuncooldown = 20;
                            }
                            else if (enemy.isFinalBoss)
                            {
                                player2.health -= 150;
                                player2.hitcooldown = 50;
                                player2.stuncooldown = 20;
                            }

                            if (player2.health < 1) OnPlayerKilled(enemy, 2);
                            else player2.GetHit(player1.velocity.Y);
                        }
                    }
                
                //[player hit enemy collision detection
                if ( enemy.isAlive && (player1.attackcooldown > 0 && 
                    ((enemy.BoundingRectangle.Intersects(player1.swordTip)) ||
                    (enemy.BoundingRectangle.Intersects(player1.swordMid)) ||
                    (enemy.BoundingRectangle.Intersects(player1.swordHilt)) ))
                    ||
                    (player2.attackcooldown > 0 && (
                    (enemy.BoundingRectangle.Intersects(player2.swordTip)) ||
                    (enemy.BoundingRectangle.Intersects(player2.swordMid)) ||
                    (enemy.BoundingRectangle.Intersects(player2.swordHilt)) )
                    ))  
                {
                    if (enemy.hitcooldown <= 0)
                    {
                        enemy.health -= 20;
                        enemy.hitcooldown = 20;
                        enemy.stuncooldown = 15;
                        enemy.monsterKilled.Play();
                    }
                    
                }
                if (enemy.isAlive && (
                    (enemy.BoundingRectangle.Intersects(player1.fireSoul)))
                    ||
                    ( (
                    (enemy.BoundingRectangle.Intersects(player2.fireSoul))
                    )))
                {
                    if (enemy.hitcooldown <= 0)
                    {
                        enemy.health -= 100;
                        enemy.hitcooldown = 60;
                        enemy.stuncooldown = 30;
                        
                    }

                }
                if (enemy.isMiniBoss && enemy.health < 400)
                {
                    enemy.stuncooldown = 999;
                    ValorsEndGame.miniBossDown = true;

                    if (ValorsEndGame.levelIndex == 4) enemy.health = 399;
                }
                if (enemy.hitcooldown >= 0) enemy.hitcooldown--;
                if (enemy.health < 1 && enemy.isAlive)
                {
                    enemy.monsterKilled.Play();
                    enemy.deathtimer = 130;
                    enemy.isAlive = false;
                    if (enemy.isMiniBoss && ValorsEndGame.levelIndex == 2 && !enemy.isAlive)
                        ValorsEndGame.playerStoleSoul = true;
                }
            }
            if (player1.hitcooldown >= 0) player1.hitcooldown--;
            if (player2.hitcooldown >= 0) player2.hitcooldown--;
            
        }

        /// <summary>
        /// Called when a gem is collected.
        /// </summary>
        /// <param name="gem">The gem that was collected.</param>
        /// <param name="collectedBy">The player who collected this gem.</param>
        private void OnGemCollected(Gem gem, Player collectedBy)
        {
            score += Gem.PointValue;

            gem.OnCollected(collectedBy);
        }

        /// <summary>
        /// Called when the player1 is killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player1. This is null if the player1 was not killed by an
        /// enemy, such as when a player1 falls into a hole.
        /// </param>
        private void OnPlayerKilled(Enemy killedBy, int playerNumber)
        {
            if (playerNumber == 1)
            {
                Player1.OnKilled(killedBy);
            }
            else if (playerNumber == 2)
            {
                Player2.OnKilled(killedBy);
            }
            
        }

        private void OnEnemyKilled(Enemy dead)
        {
            dead.OnKilled();
        }

        /// <summary>
        /// Called when the player reaches the level's exit.
        /// </summary>
        private void OnExitReached()
        {
            Player1.OnReachedExit();
            Player2.OnReachedExit();
            exitReachedSound.Play();
            reachedExit = true;
            if (ValorsEndGame.levelIndex == 2 
                && !ValorsEndGame.playerStoleSoul) ValorsEndGame.playerSparedBoss = true;
            
        }

        /// <summary>
        /// Restores the player to the starting point to try the level again.
        /// </summary>
        public void StartNewLife(int playerNumber)
        {
            if (playerNumber == 1)
            { 
                Player1.Reset(start1);
                Player2.Reset(start2);
            }
            else if (playerNumber == 2)
            {
                Player1.Reset(start1);
                Player2.Reset(start2);
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw everything in the level from background to foreground.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            for (int i = 0; i <= EntityLayer; ++i)
                spriteBatch.Draw(layers[i], Vector2.Zero, Color.White);

            DrawTiles(spriteBatch);

            foreach (Gem gem in gems)
                gem.Draw(gameTime, spriteBatch);

            Player1.Draw(gameTime, spriteBatch);
			Player2.Draw(gameTime, spriteBatch);
            if (ValorsEndGame.playerSparedBoss && ValorsEndGame.levelIndex == 4) MiniAlly.Draw(gameTime, spriteBatch);

            foreach (Enemy enemy in enemies)
                enemy.Draw(gameTime, spriteBatch);

            for (int i = EntityLayer + 1; i < layers.Length; ++i)
                spriteBatch.Draw(layers[i], Vector2.Zero, Color.White);
        }

        /// <summary>
        /// Draws each tile in the level.
        /// </summary>
        private void DrawTiles(SpriteBatch spriteBatch)
        {
            // For each tile position
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    // If there is a visible tile in that position
                    Texture2D texture = tiles[x, y].Texture;
                    if (texture != null)
                    {
                        // Draw it in screen space.
                        Vector2 position = new Vector2(x, y) * Tile.Size;
                        spriteBatch.Draw(texture, position, Color.White);
                    }
                }
            }
        }

        #endregion
    }
}
