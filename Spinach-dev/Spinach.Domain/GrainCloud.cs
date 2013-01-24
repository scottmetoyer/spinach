using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Spinach.Domain.Abstract;

namespace Spinach.Domain
{
    public class GrainCloud : IPanel
    {
        private Texture2D _grainTexture;
        private Texture2D _lineTexture;
        private Texture2D _cornerTexture;
        private int _elapsed;
        private Envelope _envelope;
        private List<Grain> _grains;
        private Random _random;
        private float _currentSample;
        private List<IPanel> _playfield;

        public bool IsSelected { get; set; }

        public int Density { get; set; }

        public int Interval { get; set; }

        public Rectangle ResizeHandle
        {
            get
            {
                return new Rectangle(
                    this.Position.X + this.Position.Width - _cornerTexture.Width,
                    this.Position.Y + this.Position.Height - _cornerTexture.Height,
                    _cornerTexture.Width,
                    _cornerTexture.Height);
            }
        }

        public Envelope Envelope
        {
            get { return _envelope; }
            set
            {
                _envelope = value;
                foreach (var grain in _grains)
                {
                    grain.Envelope = new Envelope(value);
                }
            }
        }

        public Rectangle Position { get; set; }

        public List<Grain> Grains
        {
            get
            {
                return _grains;
            }
        }

        public float CurrentSample
        {
            get
            {
                return this._currentSample;
            }
        }

        public List<IPanel> Playfield
        {
            get { return this._playfield; }
        }

        public GrainCloud(Game game, Rectangle position, int density, int interval, Envelope envelope, List<IPanel> playfield)
        {
            this.Position = position;
            _grains = new List<Grain>();
            _random = new Random();
            _currentSample = 0;
            this.Density = density;
            this.Interval = interval;
            _playfield = playfield;
            _envelope = envelope;
            _elapsed = 0;
            _lineTexture = new Texture2D(game.GraphicsDevice, 1, 1);
            _lineTexture.SetData(new[] { Color.DarkGray });
            _cornerTexture = game.Content.Load<Texture2D>("corner");
            _grainTexture = game.Content.Load<Texture2D>("circle");
        }

        public void Update()
        {
            // Add or remove grains as indicated by the current density
            if (_grains.Count > this.Density)
            {
                _grains = new List<Grain>(_grains.Take(this.Density));
            }

            if (_grains.Count < this.Density)
            {
                while (_grains.Count < this.Density)
                {
                    _grains.Add(new Grain(new Envelope(_envelope)));
                }
            }

            // Increment the sample counter by a single sample
            _elapsed += 1;

            if (_elapsed >= this.Interval)
            {
                // Reset the elapsed counter
                _elapsed = 0;

                // Trigger the grains
                foreach (var grain in _grains)
                {
                    // Clear the grain buffer
                    grain.Buffer = new float[0];

                    // Trigger the grain
                    grain.Trigger(
                        new Point(
                            _random.Next(this.Position.X, this.Position.X + this.Position.Width - _grainTexture.Width),
                            _random.Next(this.Position.Y, this.Position.Y + this.Position.Height - _grainTexture.Height)));
    
                    // Temporary buffer to hold the mixed panel data
                    float[] buffer = new float[grain.Length];
                    grain.Buffer = new float[grain.Length];

                    for (int i = 0; i < _playfield.Count; i++)
                    {
                        if (_playfield[i] is SoundPanel && _playfield[i].Position.Contains(grain.Position))
                        {
                            var panel = _playfield[i] as SoundPanel;

                            // Scale the offset into the audiobuffer based on the screen coordinates
                            long offset = (((grain.Position.X - panel.Position.X) * (panel.AudioBuffer.LongLength - 0))
                                / ((panel.Position.X + panel.Position.Width) - panel.Position.X)) + 0;

                            // Block align (we're assuming the source is block aligned to begin with, not sure if this is gonna work long term)
                            while (offset % 8 != 0) { offset++; }

                            int count = grain.Length;
                            if (offset + count >= panel.AudioBuffer.Length)
                            {
                                // TODO: Make sure we don't have an off by one here
                                count = (int)(panel.AudioBuffer.LongLength - offset);
                            }

                            // Copy to the temp buffer
                            Buffer.BlockCopy(panel.AudioBuffer, (int)offset, buffer, 0, count * sizeof(float));

                            // Mix into the grain buffer
                            float y = 1.0f * (float)((panel.Position.Height - (grain.Position.Y - panel.Position.Y))) / panel.Position.Height;
                            float volume = panel.Volume * y;

                            for (int x = 0; x < count; x++)
                            {
                                grain.Buffer[x] = grain.Buffer[x] + (buffer[x] * volume) - (grain.Buffer[x] * (panel.AudioBuffer[x] * volume));
                            }
                        }
                    }
                }
            } 

            // Update the currently running grains
            _currentSample = 0;

            foreach (var grain in _grains)
            {
                if (grain.IsActive)
                {
                    // Mix the current grain sample with the other grains in this cloud
                    float sample = grain.Index < grain.Buffer.Length ? grain.Buffer[grain.Index] * grain.Envelope.CurrentAmplitude : 0.0f;

                    if (_currentSample != 0)
                    {
                        _currentSample = _currentSample + sample - (_currentSample * sample);
                    }
                    else
                    {
                        _currentSample = sample;
                    }
                }

                grain.Update();
            }
        }

        public void Draw(SpriteBatch batch)
        {
            // Draw the box outline
            if (this.IsSelected)
            {
                int borderWidth = 1;
                batch.Draw(_lineTexture, new Rectangle(Position.Left, Position.Top, borderWidth, Position.Height), Color.DimGray);     // Left
                batch.Draw(_lineTexture, new Rectangle(Position.Right, Position.Top, borderWidth, Position.Height), Color.DimGray);    // Right
                batch.Draw(_lineTexture, new Rectangle(Position.Left, Position.Top, Position.Width, borderWidth), Color.DimGray);      // Top
                batch.Draw(_lineTexture, new Rectangle(Position.Left, Position.Bottom, Position.Width + 1, borderWidth), Color.DimGray);   // Bottom
                batch.Draw(
                    _cornerTexture,
                    new Rectangle(
                        (Position.Left + Position.Width) - _cornerTexture.Width,
                        (Position.Top + Position.Height) - _cornerTexture.Height,
                        _cornerTexture.Width,
                        _cornerTexture.Height),
                        Color.DimGray);
            }

            foreach (var grain in _grains)
            {
                if (grain.IsActive)
                {
                    batch.Draw(
                        _grainTexture,
                        new Vector2(grain.Position.X, grain.Position.Y),
                        Color.OrangeRed * grain.Envelope.CurrentAmplitude);
                }
            }
        }
    }
}
