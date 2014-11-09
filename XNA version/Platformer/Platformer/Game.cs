#region File Description
//-----------------------------------------------------------------------------
// ValorsEndGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input.Touch;


namespace ValorsEnd
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class ValorsEndGame : Microsoft.Xna.Framework.Game
    {

        #region ChargeSwitch Durations
        private const float deadZoneChargeSwitchDuration = 2f;
        private const float exitChargeSwitchDuration = 2f;
        #endregion
        //Xbox controller stuff

        #region Input Data
        private int selectedplayer;
        private GamePadState[] gamePadStates = new GamePadState[4];
        private GamePadCapabilities[] gamePadCapabilities = new GamePadCapabilities[4];
        private KeyboardState lastKeyboardState;
        #endregion


        #region Dead Zone Data
        private GamePadDeadZone deadZone = GamePadDeadZone.IndependentAxes;
        public GamePadDeadZone DeadZone
        {
            get { return deadZone; }
            set
            {
                deadZone = value;
                deadZoneString = "(" + deadZone.ToString() + ")";
                if (hudFont != null)
                {
                    Vector2 deadZoneStringSize =
                        hudFont.MeasureString(deadZoneString);
                    deadZoneStringPosition = new Vector2(
                        (float)Math.Floor(deadZoneStringCenterPosition.X -
                            deadZoneStringSize.X / 2f),
                        (float)Math.Floor(deadZoneStringCenterPosition.Y -
                            deadZoneStringSize.Y / 2f));
                }
            }
        }
        private string deadZoneString;
        private Vector2 deadZoneStringPosition;
        private Vector2 deadZoneStringCenterPosition;
        #endregion


        #region ChargeSwitches
        private ChargeSwitchExit exitSwitch =
            new ChargeSwitchExit(exitChargeSwitchDuration);
        private ChargeSwitchDeadZone deadZoneSwitch =
            new ChargeSwitchDeadZone(deadZoneChargeSwitchDuration);
        #endregion

        //camera
        public bool initCamera = true;
        public static Camera2d cam;

        // Resources for drawing.
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        public static bool playerStoleSoul = false;
        public static bool playerSparedBoss = false;

        // Global content.
        private SpriteFont hudFont;

        private Texture2D winOverlay;
        private Texture2D loseOverlay;
        private Texture2D diedOverlay;

        private Texture2D hudStamina;
        private Texture2D hudHealth;
        private Texture2D hudBackground;

        // Meta-level game state.
        public static int levelIndex = -1;
        private Level level;
        private bool wasContinuePressed;

        //these bools control dialogue based on story decisions
        public static bool miniBossDown = false;

        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);

        // We store our input states so that we only poll once per frame, 
        // then we use the same input state wherever needed
        private GamePadState gamePadState1;
        private GamePadState gamePadState2;
        private KeyboardState keyboardState;
        private TouchCollection touchState;
        private AccelerometerState accelerometerState;

        // The number of levels in the Levels directory of our content. We assume that
        // levels in our content are 0-based and that all numbers under this constant
        // have a level file present. This allows us to not need to check for the file
        // or handle exceptions, both of which can add unnecessary time to level loading.
        private const int numberOfLevels = 5;

        public ValorsEndGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            exitSwitch.Fire += new ChargeSwitch.FireDelegate(exitSwitch_Fire);
            deadZoneSwitch.Fire += new ChargeSwitch.FireDelegate(ToggleDeadZone);

#if WINDOWS_PHONE
            graphics.IsFullScreen = true;
            TargetElapsedTime = TimeSpan.FromTicks(333333);
#endif
           
            Accelerometer.Initialize();
        }

        #region ChargeSwitch Event Handlers
        /// <summary>
        /// Handles the dead-zone ChargeSwitch fire event.  Toggles dead zone types.
        /// </summary>
        private void ToggleDeadZone()
        {
            switch (DeadZone)
            {
                case GamePadDeadZone.IndependentAxes:
                    DeadZone = GamePadDeadZone.Circular;
                    break;
                case GamePadDeadZone.Circular:
                    DeadZone = GamePadDeadZone.None;
                    break;
                case GamePadDeadZone.None:
                    DeadZone = GamePadDeadZone.IndependentAxes;
                    break;
            }
        }


        /// <summary>
        /// Handles the exit ChargeSwitch fire event.  Exits the application.
        /// </summary>
        private void exitSwitch_Fire()
        {
            this.Exit();
        }
        #endregion

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load fonts
            hudFont = Content.Load<SpriteFont>("Fonts/Hud");

            // Load overlay textures
            winOverlay = Content.Load<Texture2D>("Overlays/you_win");
            loseOverlay = Content.Load<Texture2D>("Overlays/you_lose");
            diedOverlay = Content.Load<Texture2D>("Overlays/you_died");

            hudStamina = Content.Load<Texture2D>("Sprites/player/green");
            hudHealth = Content.Load<Texture2D>("Sprites/player/red");
            hudBackground = Content.Load<Texture2D>("Sprites/player/black");

            //Known issue that you get exceptions if you use Media PLayer while connected to your PC
            //See http://social.msdn.microsoft.com/Forums/en/windowsphone7series/thread/c8a243d2-d360-46b1-96bd-62b1ef268c66
            //Which means its impossible to test this from VS.
            //So we have to catch the exception and throw it away
            try
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(Content.Load<Song>("Sounds/Music"));
            }
            catch { }

            LoadNextLevel();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Handle polling for our input and handling high-level input
            HandleInput();

            // update our level, passing down the GameTime along with all of our input states
            level.Update(gameTime, keyboardState, gamePadState1, gamePadState2, touchState,
                         accelerometerState, Window.CurrentOrientation);
            //if(!initCamera)cam.Pos = new Vector2(level.player1.position.X,level.player1.position.Y);
         

            bool setSelectedplayer = false; // give preference to earlier controllers
            for (int i = 0; i < 4; i++)
            {
                gamePadStates[i] = GamePad.GetState((PlayerIndex)i, deadZone);
                gamePadCapabilities[i] = GamePad.GetCapabilities((PlayerIndex)i);
                if (!setSelectedplayer && IsActiveGamePad(ref gamePadStates[i]))
                {
                    selectedplayer = i;
                    setSelectedplayer = true;
                }
            }
            
            deadZoneSwitch.Update(gameTime, ref gamePadStates[selectedplayer]);
            exitSwitch.Update(gameTime, ref gamePadStates[selectedplayer]);

            base.Update(gameTime);
        }

        /// <summary>
        /// Determines if the provided GamePadState is "active".
        /// </summary>
        /// <param name="gamePadState">The GamePadState that is checked.</param>
        /// <remarks>
        /// "Active" currently means that at least one of the buttons is being pressed.
        /// </remarks>
        /// <returns>True if "active".</returns>
        private static bool IsActiveGamePad(ref GamePadState gamePadState)
        {
            return (gamePadState.IsConnected &&
                ((gamePadState.Buttons.A == ButtonState.Pressed) ||
                (gamePadState.Buttons.B == ButtonState.Pressed) ||
                (gamePadState.Buttons.X == ButtonState.Pressed) ||
                (gamePadState.Buttons.Y == ButtonState.Pressed) ||
                (gamePadState.Buttons.Start == ButtonState.Pressed) ||
                (gamePadState.Buttons.Back == ButtonState.Pressed) ||
                (gamePadState.Buttons.LeftShoulder == ButtonState.Pressed) ||
                (gamePadState.Buttons.RightShoulder == ButtonState.Pressed) ||
                (gamePadState.Buttons.LeftStick == ButtonState.Pressed) ||
                (gamePadState.Buttons.RightStick == ButtonState.Pressed) ||
                (gamePadState.DPad.Up == ButtonState.Pressed) ||
                (gamePadState.DPad.Left == ButtonState.Pressed) ||
                (gamePadState.DPad.Right == ButtonState.Pressed) ||
                (gamePadState.DPad.Down == ButtonState.Pressed)));
        }

        private void HandleInput()
        {
            // get all of our input states
            keyboardState = Keyboard.GetState();
            gamePadState1 = GamePad.GetState(PlayerIndex.One);
            gamePadState2 = GamePad.GetState(PlayerIndex.Two);
            touchState = TouchPanel.GetState();
            accelerometerState = Accelerometer.GetState();

            // Exit the game when back is pressed.
            //if (gamePadState1.Buttons.Back == ButtonState.Pressed || gamePadState2.Buttons.Back ==                                        ButtonState.Pressed)
                //Exit();

            bool continuePressed =
                keyboardState.IsKeyDown(Keys.Space) ||
                gamePadState1.IsButtonDown(Buttons.A) ||
                gamePadState2.IsButtonDown(Buttons.A) ||
                touchState.AnyTouch();

            // Perform the appropriate action to advance the game and
            // to get the player back to playing.
            if (!wasContinuePressed && continuePressed)
            {
                if (!level.Player1.IsAlive)
                {
                    levelIndex--;
                    LoadNextLevel();
                    //level.StartNewLife(1);
                }
                else if (!level.Player2.IsAlive)
                {
                    levelIndex--;
                    LoadNextLevel();
                    //level.StartNewLife(2);
                }
            }

            wasContinuePressed = continuePressed;
        }

        private void LoadNextLevel()
        {
            // move to the next level
            levelIndex = (levelIndex + 1) % numberOfLevels;
            miniBossDown = false;
            
            // Unloads the content for the current level before loading the next one.
            if (level != null)
                level.Dispose();

            // Load the level.
            string levelPath = string.Format("Content/Levels/{0}.txt", levelIndex);
            using (Stream fileStream = TitleContainer.OpenStream(levelPath))
                level = new Level(Services, fileStream, levelIndex);

            if (ValorsEndGame.playerSparedBoss && ValorsEndGame.levelIndex == 4)
            {
                level.MiniAlly = new Enemy(level, 
                    new Vector2(level.player1.position.X + 170, level.player1.position.Y + 170), "MonsterB");
            }
            if (levelIndex == 0)
            {
                playerSparedBoss = false;
                playerStoleSoul = false;
                
            }
            if (levelIndex == 2)
            {
                foreach (Enemy enemy in level.enemies)
                    enemy.stuncooldown = 300;
                level.player1.stuncooldown = 300;
                level.player2.stuncooldown = 300;
            }
            if (levelIndex == 4)
            {
                foreach (Enemy enemy in level.enemies)
                    enemy.stuncooldown = 300;
                level.player1.stuncooldown = 300;
                level.player2.stuncooldown = 300;
                if (playerSparedBoss) level.MiniAlly.stuncooldown = 300;
            }
        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel();
        }

        /// <summary>
        /// Draws the game from background to foreground.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);


            if(initCamera)
            {
                cam = new Camera2d();
                cam.Pos = new Vector2(500.0f, 200.0f);
                // cam.Zoom = 2.0f // Example of Zoom in
                // cam.Zoom = 0.5f // Example of Zoom out
                initCamera = false;
            }

            
            spriteBatch.Begin(SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        null,
                        null,
                        null,
                        null,
                        cam.get_transformation(graphics.GraphicsDevice/*Send the variable that has your graphic device here*/));

            level.Draw(gameTime, spriteBatch);

            DrawHud();

            spriteBatch.End();

            base.Draw(gameTime);

            if (!initCamera)
            {
                cam.Pos = new Vector2((level.player1.position.X + level.player2.position.X) / 2, (level.player1.position.Y + level.player2.position.Y) / 2);

                double camStartX = cam.Pos.X - graphics.PreferredBackBufferWidth/2/cam.Zoom;// +graphics.PreferredBackBufferWidth / cam.Zoom;
                double camEdgeX = camStartX + graphics.PreferredBackBufferWidth / cam.Zoom;
                double camWidth = camEdgeX - camStartX;//(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width / 1.1f);//*cam.Zoom;
                double camStartY = cam.Pos.Y - graphics.PreferredBackBufferHeight / 2 / cam.Zoom;
                double camEdgeY = camStartY + graphics.PreferredBackBufferHeight / cam.Zoom;
                double camHeight = camEdgeY - camStartY;//(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height / 1.1f);// * cam.Zoom;

                if (level.player1.position.X < camStartX * 1.1 || level.player1.position.X > camEdgeX * 0.96
                    || level.player1.position.Y < camStartY * 1.1 || level.player1.position.Y > camEdgeY * 0.96
                    || level.player2.position.X < camStartX * 1.1 || level.player2.position.X > camEdgeX * 0.96
                    || level.player2.position.Y < camStartY * 1.1 || level.player2.position.Y > camEdgeY * 0.96) cam.Zoom -= 0.005f;

                else if (cam.Zoom < 1.5 && level.player1.position.X > camStartX * 1.2 && level.player1.position.X < camEdgeX * 0.9
                    && level.player1.position.Y > camStartY * 1.2 && level.player1.position.Y < camEdgeY * 0.9
                    && level.player2.position.X > camStartX * 1.2 && level.player2.position.X < camEdgeX * 0.9
                    && level.player2.position.Y > camStartY * 1.2 && level.player2.position.Y < camEdgeY * 0.9) cam.Zoom += 0.005f;

                /*if ((level.player1.position.X + level.player2.position.X) >
                    GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width*3/cam.Zoom) cam.Zoom -= 0.01f;
                if ((level.player1.position.X + level.player2.position.X) <
                    (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width*3/cam.Zoom ) ) cam.Zoom += 0.01f;*/
            }
        }

        private void DrawHud()
        {
            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            // Draw time remaining. Uses modulo division to cause blinking when the
            // player is running out of time.
            string timeString = "TIME: " + level.TimeRemaining.Minutes.ToString("00") + ":" + level.TimeRemaining.Seconds.ToString("00");
            Color timeColor;
            if (level.TimeRemaining > WarningTime ||
                level.ReachedExit ||
                (int)level.TimeRemaining.TotalSeconds % 2 == 0)
            {
                timeColor = Color.Yellow;
            }
            else
            {
                timeColor = Color.Red;
            }
            //DrawShadowedString(hudFont, timeString, hudLocation, timeColor);

            // Draw score
            float timeHeight = hudFont.MeasureString(timeString).Y;
            //DrawShadowedString(hudFont, "SCORE: " + level.Score.ToString(), hudLocation + new Vector2(0.0f, timeHeight * 1.2f), Color.Yellow);
            DrawShadowedString(hudFont, "P1 Health: " + level.player1.health.ToString(),
                new Vector2(0.0f + 10, timeHeight * 1.2f -20), Color.Red);
            DrawShadowedString(hudFont, "P1",
                new Vector2(0.0f + 10, timeHeight * 2.2f), Color.Red);
            DrawShadowedString(hudFont, "    Stamina: " + level.player1.stamina.ToString(),
                new Vector2(0.0f + 10, timeHeight * 2.2f), Color.ForestGreen);
            spriteBatch.Draw(hudBackground, new Rectangle((int)hudLocation.X + 10,
                (int)hudLocation.Y + 40, 200, 10), Color.White);
            spriteBatch.Draw(hudHealth, new Rectangle((int)hudLocation.X + 10,
                (int)hudLocation.Y + 40, level.player1.health / 2, 10), Color.White);
            spriteBatch.Draw(hudBackground, new Rectangle((int)hudLocation.X + 10,
                (int)hudLocation.Y + 80, 200, 10), Color.White);
            spriteBatch.Draw(hudStamina, new Rectangle((int)hudLocation.X +10,
                (int)hudLocation.Y + 80, (int)(level.player1.stamina * 1.666), 10), Color.White);

            DrawShadowedString(hudFont, "P2",
                hudLocation + new Vector2(0.0f + 550, timeHeight * 1.2f - 20), Color.Blue);
            DrawShadowedString(hudFont, "    Health: " + level.player2.health.ToString(),
                hudLocation + new Vector2(0.0f + 550, timeHeight * 1.2f - 20), Color.Red);
            DrawShadowedString(hudFont, "P2",
                hudLocation + new Vector2(0.0f + 550, timeHeight * 2.2f), Color.Blue);
            DrawShadowedString(hudFont, "    Stamina: " + level.player2.stamina.ToString(),
                hudLocation + new Vector2(0.0f + 550, timeHeight * 2.2f), Color.ForestGreen);
            spriteBatch.Draw(hudBackground, new Rectangle((int)hudLocation.X + 550,
                (int)hudLocation.Y + 40, 200, 10), Color.White);
            spriteBatch.Draw(hudHealth, new Rectangle((int)hudLocation.X + 550,
                (int)hudLocation.Y + 40, level.player2.health / 2, 10), Color.White);
            spriteBatch.Draw(hudBackground, new Rectangle((int)hudLocation.X + 550,
                (int)hudLocation.Y + 80, 200, 10), Color.White);
            spriteBatch.Draw(hudStamina, new Rectangle((int)hudLocation.X + 550,
                (int)hudLocation.Y + 80, (int)(level.player2.stamina * 1.666), 10), Color.White);

            // Determine the status overlay message to show.
            Texture2D status = null;

            if (level.ReachedExit)
            {
                status = winOverlay;
            }

            else if (!level.Player1.IsAlive || !level.Player2.IsAlive)
            {
                status = diedOverlay;
            }

            if (status != null)
            {
                // Draw status message.
                Vector2 statusSize = new Vector2(status.Width, status.Height);
                spriteBatch.Draw(status, center - statusSize / 2, Color.White);
            }
        }

        private void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
        {
            spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
            spriteBatch.DrawString(font, value, position, color);
        }
    }
}
