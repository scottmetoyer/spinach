using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Spinach.Domain;
using Spinach.Domain.Abstract;
using System.Reflection;
using System.Configuration;

namespace Spinach.App
{
    public class SpinachApp : Microsoft.Xna.Framework.Game
    {
        private const int SampleRate = 44100;
        private const int SamplesPerBuffer = 3000;
        private const int ChannelsCount = 2;
        private const int DisplayWidth = 1600;
        private const int DisplayHeight = 900;
        protected GraphicsDeviceManager graphics;
        protected SpriteBatch spriteBatch;
        private SpriteFont _font;
        private Texture2D grainTexture;
        private List<IPanel> _panels;
        private DynamicSoundEffectInstance _instance;
        private bool _isDragging;
        private bool _isResizing;
        private bool _enterAttack;
        private bool _enterSustain;
        private bool _enterDecay;
        private bool _enterInterval;
        private bool _enterDensity;
        private bool _enterVolume;
        private Point _dragOffset;
        private IPanel _selectedPanel;
        protected KeyboardState newState;
        protected KeyboardState oldState;
        private bool _showHelp;
        private bool _showEnvelope;
        private string _enteredValue;
        private byte[] _xnaBuffer;
        private float[] _workingBuffer;
        private Keys[] digits =
            new Keys[] { Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5, Keys.NumPad6, Keys.NumPad7, Keys.NumPad8,
         Keys.NumPad9, Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9 };

        public SpinachApp()
        {
            graphics = new GraphicsDeviceManager(this);

            // Initialize graphics
            try
            {
                graphics.PreferredBackBufferWidth = Convert.ToInt32(ConfigurationManager.AppSettings["ScreenWidth"]);
                graphics.PreferredBackBufferHeight = Convert.ToInt32(ConfigurationManager.AppSettings["ScreenHeight"]);
                graphics.IsFullScreen = Convert.ToBoolean(ConfigurationManager.AppSettings["FullScreen"]);
            }
            catch
            {
                // Handle this by just using the defaults
            }

            Content.RootDirectory = "Content";
            _panels = new List<IPanel>();

            // Empty string for holding keyboard input
            _enteredValue = string.Empty;

            // Initialize the sound system
            _instance = new DynamicSoundEffectInstance(SampleRate, AudioChannels.Stereo);
            _xnaBuffer = new byte[SamplesPerBuffer * ChannelsCount * 2]; // There are two bytes per sample
            _workingBuffer = new float[SamplesPerBuffer * ChannelsCount];
        }

        protected override void Initialize()
        {
            this.IsMouseVisible = true;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("Arial");
            grainTexture = this.Content.Load<Texture2D>("circle");

            // Load the sound panels
            List<SoundPanel> sounds = SoundPanel.LoadFromPath(this, "Content");
            _panels.AddRange(sounds);

            // Start playing audio
            _instance.Play();
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // Grab the current mouse state
            MouseState state = Mouse.GetState();

            // Handle the keyboard
            this.HandleKeys();

            if (state.LeftButton == ButtonState.Pressed)
            {
                if (_isDragging)
                {
                    if (_selectedPanel != null)
                    {
                        // Drag or resize
                        if (_isResizing)
                        {
                            int width = state.X - _selectedPanel.Position.X + _dragOffset.X;
                            int height = state.Y - _selectedPanel.Position.Y + _dragOffset.Y;

                            if (width <= 10) width = 10;
                            if (height <= 10) height = 10;

                            _selectedPanel.Position = new Rectangle(
                                _selectedPanel.Position.X,
                                _selectedPanel.Position.Y,
                                width,
                                height);
                        }
                        else
                        {
                            _selectedPanel.Position = new Rectangle(state.X - _dragOffset.X, state.Y - _dragOffset.Y, _selectedPanel.Position.Width, _selectedPanel.Position.Height);
                        }
                    }
                }
                else
                {
                    // Deselect all the boxes
                    _panels.Select(c => { c.IsSelected = false; return c; }).ToList();

                    // Find the first intersection, select it. Try to find GrainClouds first (they get precedence)
                    var panel = _panels.FirstOrDefault(x => x.Position.Contains(new Point(state.X, state.Y)) && x is GrainCloud);
                    if (panel == null)
                    {
                        panel = _panels.FirstOrDefault(x => x.Position.Contains(new Point(state.X, state.Y)));
                    }

                    if (panel != null)
                    {
                        panel.IsSelected = true;
                        _selectedPanel = panel;

                        if (_selectedPanel.ResizeHandle.Contains(new Point(state.X, state.Y)))
                        {
                            _dragOffset = new Point(panel.Position.Width - (state.X - panel.Position.X), panel.Position.Height - (state.Y - panel.Position.Y));
                            _isResizing = true;
                        }
                        else
                        {
                            _dragOffset = new Point(state.X - panel.Position.X, state.Y - panel.Position.Y);
                        }

                        _isDragging = true;
                    }
                    else
                    {
                        _selectedPanel = null;
                    }
                }
            }

            if (state.LeftButton == ButtonState.Released)
            {
                if (_selectedPanel is SoundPanel && _isResizing)
                {
                    (_selectedPanel as SoundPanel).RenderWaveform(this.GraphicsDevice);
                }

                _isDragging = false;
                _isResizing = false;
                _dragOffset = new Point(0, 0);
            }

            // Update and submit the audio
            while (_instance.PendingBufferCount < 3)
                SubmitBuffer();

            // Set the appropriate mouse cursor
            var showDrag = _panels.Any(x => x.ResizeHandle.Contains(new Point(state.X, state.Y)));
            if (showDrag)
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNWSE;
            }
            else
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Arrow;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();

            foreach (var panel in _panels)
            {
                panel.Draw(spriteBatch);
            }

            // Draw the help if it's on
            if (_showHelp)
            {
                DrawHelp();
            }

            // Draw the selected cloud envelope if it's on
            if (_showEnvelope)
            {
                DrawEnvelope();
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        private void FillWorkingBuffer()
        {
            for (int i = 0; i < (SamplesPerBuffer * ChannelsCount); i++)
            {
                _workingBuffer[i] = 0;

                foreach (GrainCloud panel in _panels.Where(x => x is GrainCloud))
                {
                    // Mix the current grain sample with the other grains in this cloud
                    float sample = panel.CurrentSample;

                    if (panel.CurrentSample != 0)
                    {
                        // Grab the current sample value stored in the grain cloud
                        if (_workingBuffer[i] != 0)
                        {
                            _workingBuffer[i] = _workingBuffer[i] + sample - (_workingBuffer[i] * sample);
                        }
                        else
                        {
                            _workingBuffer[i] = sample;
                        }
                    }

                    // Move the grain cloud forward a single sample
                    panel.Update();
                }
            }
        }

        private void SubmitBuffer()
        {
            this.FillWorkingBuffer();
            Utility.ConvertBuffer(_workingBuffer, _xnaBuffer);
            _instance.SubmitBuffer(_xnaBuffer);
        }

        private void HandleKeys()
        {
            newState = Keyboard.GetState();

            // Check for digits if we're editing a parameter
            if (_enterAttack || _enterDecay || _enterSustain || _enterInterval || _enterDensity || _enterVolume)
            {
                foreach (Keys key in digits)
                {
                    if (KeyPressed(key))
                    {
                        AddKeyToDigits(key);
                        break;
                    }
                }
            }

            // Check for keypresses
            if (this.KeyPressed(Keys.F1))
            {
                _showHelp = _showHelp == true ? false : true;
            }

            if (this.KeyPressed(Keys.E))
            {
                _showEnvelope = _showEnvelope == true ? false : true;
            }

            if (this.KeyPressed(Keys.I))
            {
                if (_enterInterval && _enteredValue != string.Empty)
                {
                    this.SetGrainInterval();
                }
                _enterInterval = _enterInterval == true ? false : true;
            }

            if (this.KeyPressed(Keys.A))
            {
                if (_enterAttack && _enteredValue != string.Empty)
                {
                    this.SetAttackEnvelope();
                }
                _enterAttack = _enterAttack == true ? false : true;
            }

            if (this.KeyPressed(Keys.S))
            {
                if (_enterSustain && _enteredValue != string.Empty)
                {
                    this.SetSustainEnvelope();
                }
                _enterSustain = _enterSustain == true ? false : true;
            }

            if (this.KeyPressed(Keys.D))
            {
                if (_enterDecay && _enteredValue != string.Empty)
                {
                    this.SetDecayEnvelope();
                }
                _enterDecay = _enterDecay == true ? false : true;
            }

            if (this.KeyPressed(Keys.V))
            {
                if (_enterVolume && _enteredValue != string.Empty)
                {
                    this.SetPanelVolume();
                }
                _enterVolume = _enterVolume == true ? false : true;
            }

            if (this.KeyPressed(Keys.N))
            {
                if (_enterDensity && _enteredValue != string.Empty)
                {
                    this.SetGrainDensity();
                }
                _enterDensity = _enterDensity == true ? false : true;
            }

            if (this.KeyPressed(Keys.Enter))
            {
                if (_enteredValue != string.Empty)
                {
                    if (_enterAttack)
                    {
                        this.SetAttackEnvelope();
                    }

                    if (_enterDecay)
                    {
                        this.SetDecayEnvelope();
                    }

                    if (_enterSustain)
                    {
                        this.SetSustainEnvelope();
                    }

                    if (_enterInterval)
                    {
                        this.SetGrainInterval();
                    }

                    if (_enterDensity)
                    {
                        this.SetGrainDensity();
                    }

                    if (_enterVolume)
                    {
                        this.SetPanelVolume();
                    }
                }

                _enterAttack = false;
                _enterDecay = false;
                _enterSustain = false;
                _enterInterval = false;
                _enterDensity = false;
                _enterVolume = false;
            }

            if (this.KeyPressed(Keys.G))
            {
                MouseState state = Mouse.GetState();
                this.AddCloud(new Point(state.X, state.Y));
            }

            if (this.KeyPressed(Keys.Delete))
            {
                this.RemoveCloud(_selectedPanel as GrainCloud);
            }

            if (this.KeyPressed(Keys.Escape))
            {
                Exit();
            }

            oldState = newState;
        }

        private bool KeyPressed(Keys theKey)
        {
            if (newState.IsKeyUp(theKey) && oldState.IsKeyDown(theKey))
                return true;

            return false;
        }

        private void AddKeyToDigits(Keys key)
        {
            string input = string.Empty;

            // Cap input at five digits
            if (_enteredValue.Length >= 5)
                return;

            switch (key)
            {
                case Keys.D0:
                case Keys.NumPad0:
                    input += "0";
                    break;
                case Keys.D1:
                case Keys.NumPad1:
                    input += "1";
                    break;
                case Keys.D2:
                case Keys.NumPad2:
                    input += "2";
                    break;
                case Keys.D3:
                case Keys.NumPad3:
                    input += "3";
                    break;
                case Keys.D4:
                case Keys.NumPad4:
                    input += "4";
                    break;
                case Keys.D5:
                case Keys.NumPad5:
                    input += "5";
                    break;
                case Keys.D6:
                case Keys.NumPad6:
                    input += "6";
                    break;
                case Keys.D7:
                case Keys.NumPad7:
                    input += "7";
                    break;
                case Keys.D8:
                case Keys.NumPad8:
                    input += "8";
                    break;
                case Keys.D9:
                case Keys.NumPad9:
                    input += "9";
                    break;
            }

            _enteredValue += input;
        }

        private void SetAttackEnvelope()
        {
            if (_selectedPanel != null && _selectedPanel is GrainCloud)
            {
                var panel = _selectedPanel as GrainCloud;
                int value = 8;
                int.TryParse(_enteredValue, out value);
                panel.Envelope = new Envelope((int)(value * ((double)(SampleRate * ChannelsCount) / 1000.0)), panel.Envelope.Sustain, panel.Envelope.Decay);
            }

            _enteredValue = string.Empty;
        }

        private void SetSustainEnvelope()
        {
            if (_selectedPanel != null && _selectedPanel is GrainCloud)
            {
                var panel = _selectedPanel as GrainCloud;
                int value = 20;
                int.TryParse(_enteredValue, out value);
                panel.Envelope = new Envelope(panel.Envelope.Attack, (int)(value * ((double)(SampleRate * ChannelsCount) / 1000.0)), panel.Envelope.Decay);
            }

            _enteredValue = string.Empty;
        }

        private void SetDecayEnvelope()
        {
            if (_selectedPanel != null && _selectedPanel is GrainCloud)
            {
                var panel = _selectedPanel as GrainCloud;
                int value = 8;
                int.TryParse(_enteredValue, out value);
                panel.Envelope = new Envelope(panel.Envelope.Attack, panel.Envelope.Sustain, (int)(value * ((double)(SampleRate * ChannelsCount) / 1000.0)));
            }

            _enteredValue = string.Empty;
        }

        private void SetGrainInterval()
        {
            if (_selectedPanel != null && _selectedPanel is GrainCloud)
            {
                var panel = _selectedPanel as GrainCloud;
                int value = 60;
                int.TryParse(_enteredValue, out value);
                panel.Interval = (int)(value * ((double)(SampleRate * ChannelsCount) / 1000.0));
            }

            _enteredValue = string.Empty;
        }

        private void SetGrainDensity()
        {
            if (_selectedPanel != null && _selectedPanel is GrainCloud)
            {
                var panel = _selectedPanel as GrainCloud;
                int value = 3;
                int.TryParse(_enteredValue, out value);
                panel.Density = value;
            }

            _enteredValue = string.Empty;
        }

        private void SetPanelVolume()
        {
            if (_selectedPanel != null && _selectedPanel is SoundPanel)
            {
                var panel = _selectedPanel as SoundPanel;
                int value = (int)(((SoundPanel)_selectedPanel).Volume * 100);
                int.TryParse(_enteredValue, out value);

                // Normalize
                if (value > 100)
                {
                    value = 100;
                }

                if (value < 0)
                {
                    value = 0;
                }

                panel.Volume = value / 100.0f;
            }

            _enteredValue = string.Empty;
        }

        private void AddCloud(Point location)
        {
            // Deselect all the boxes
            _panels.Select(c => { c.IsSelected = false; return c; }).ToList();
            int density = 3;
            int interval = (int)(50 * ((double)(SampleRate * ChannelsCount) / 1000.0));
            int attack = (int)(5 * ((double)(SampleRate * ChannelsCount) / 1000.0));
            int sustain = (int)(40 * ((double)(SampleRate * ChannelsCount) / 1000.0));
            int decay = (int)(5 * ((double)(SampleRate * ChannelsCount) / 1000.0));
            GrainCloud cloud = new GrainCloud(
              this,
              new Rectangle(location.X - 80, location.Y - 80, 160, 160),
              density,
              interval,
              new Envelope(attack, sustain, decay),
              this._panels);
            _panels.Add(cloud);

            cloud.IsSelected = true;
            _selectedPanel = cloud;
        }

        private void RemoveCloud(GrainCloud cloud)
        {
            if (cloud != null)
            {
                _panels.Remove(cloud);
            }
        }

        private void DrawHelp()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version; 
            string versionString = v.Major + "." + v.Minor + "." + v.Build + "." + v.Revision; 

            spriteBatch.DrawString(_font, "Spinach v. " + versionString, new Vector2(10, 10), Color.DarkGray);
            spriteBatch.DrawString(_font, "v <val> Set volume level for the selected sound panel (0 to 100)", new Vector2(10, 30), Color.DarkGray);
            spriteBatch.DrawString(_font, "n <val> Set the grain density for the selected grain cloud", new Vector2(10, 40), Color.DarkGray);
            spriteBatch.DrawString(_font, "del     Delete the selected item from the canvas", new Vector2(10, 50), Color.DarkGray);
            spriteBatch.DrawString(_font, "g       Drop a grain cloud at the mouse position", new Vector2(10, 60), Color.DarkGray);
            spriteBatch.DrawString(_font, "a <val> Enter attack value in ms for selected grain cloud", new Vector2(10, 70), Color.DarkGray);
            spriteBatch.DrawString(_font, "s <val> Enter sustain value in ms for selected grain cloud", new Vector2(10, 80), Color.DarkGray);
            spriteBatch.DrawString(_font, "d <val> Enter decay value in ms for selected grain cloud", new Vector2(10, 90), Color.DarkGray);
            spriteBatch.DrawString(_font, "i <val> Set interval in ms for selected grain cloud", new Vector2(10, 100), Color.DarkGray);
            spriteBatch.DrawString(_font, "esc     Exit the program", new Vector2(10, 110), Color.DarkGray);
        }

        private void DrawEnvelope()
        {
            if (_selectedPanel != null && _selectedPanel is SoundPanel)
            {
                var panel = _selectedPanel as SoundPanel;
                spriteBatch.DrawString(_font, "V: ", new Vector2(GraphicsDevice.Viewport.Width - 80, 10), Color.DarkGray);
                if (!_enterVolume)
                {
                    spriteBatch.DrawString(_font, (panel.Volume * 100).ToString(), new Vector2(GraphicsDevice.Viewport.Width - 60, 10), Color.DarkGray);
                }
            }

            if (_selectedPanel != null && _selectedPanel is GrainCloud)
            {
                var panel = _selectedPanel as GrainCloud;

                spriteBatch.DrawString(_font, "A: ", new Vector2(GraphicsDevice.Viewport.Width - 260, 10), Color.DarkGray);
                if (!_enterAttack)
                {
                    spriteBatch.DrawString(_font, (panel.Envelope.Attack / ((double)(SampleRate * ChannelsCount) / 1000.0)).ToString(), new Vector2(GraphicsDevice.Viewport.Width - 240, 10), Color.DarkGray);
                }

                spriteBatch.DrawString(_font, "S: ", new Vector2(GraphicsDevice.Viewport.Width - 200, 10), Color.DarkGray);
                if (!_enterSustain)
                {
                    spriteBatch.DrawString(_font, (panel.Envelope.Sustain / ((double)(SampleRate * ChannelsCount) / 1000.0)).ToString(), new Vector2(GraphicsDevice.Viewport.Width - 180, 10), Color.DarkGray);
                }

                spriteBatch.DrawString(_font, "D: ", new Vector2(GraphicsDevice.Viewport.Width - 140, 10), Color.DarkGray);
                if (!_enterDecay)
                {
                    spriteBatch.DrawString(_font, (panel.Envelope.Decay / ((double)(SampleRate * ChannelsCount) / 1000.0)).ToString(), new Vector2(GraphicsDevice.Viewport.Width - 120, 10), Color.DarkGray);
                }

                spriteBatch.DrawString(_font, "I: ", new Vector2(GraphicsDevice.Viewport.Width - 80, 10), Color.DarkGray);
                if (!_enterInterval)
                {
                    spriteBatch.DrawString(_font, (panel.Interval / ((double)(SampleRate * ChannelsCount) / 1000.0)).ToString(), new Vector2(GraphicsDevice.Viewport.Width - 60, 10), Color.DarkGray);
                }

                spriteBatch.DrawString(_font, "N: ", new Vector2(GraphicsDevice.Viewport.Width - 260, 20), Color.DarkGray);
                if (!_enterDensity)
                {
                    spriteBatch.DrawString(_font, (panel.Density).ToString(), new Vector2(GraphicsDevice.Viewport.Width - 240, 20), Color.DarkGray);
                }
            }
        }
    }
}
