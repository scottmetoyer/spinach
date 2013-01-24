using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.IO;
using Spinach.Domain.Abstract;
using Microsoft.Xna.Framework.Graphics;
using NAudio.Wave;

namespace Spinach.Domain
{
    public class SoundPanel : IPanel
    {
        private const int DefaultWidth = 350;
        private const int DefaultHeight = 100;
        private const int SampleRate = 44100;
        private const int ChannelsCount = 2;
        private const int BlockAlignment = 8;

        private int _offset;
        private int _count;
        private float[] _audioBuffer;

        private static Random _random = new Random();
        private Texture2D _cornerTexture;
        private Texture2D _lineTexture;
        private Texture2D _boxTexture;
        private RenderTarget2D _waveform;
        private Texture2D _whitePixelTexture;
        private Game _game;

        public Color Color { get; set; }

        public Rectangle Position { get; set; }

        public bool IsSelected { get; set; }

        public int Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                // Block align this value
                _offset = (int)Math.Round((value / (double)BlockAlignment)) * BlockAlignment;
            }
        }

        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                // Block align this value
                _count = (int)Math.Round((value / (double)BlockAlignment)) * BlockAlignment; ;
            }
        }

        public float Volume
        {
            get;
            set;
        }

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

        public float[] AudioBuffer
        {
            get
            {
                return _audioBuffer;
            }
        }

        public SoundPanel(Game game, string waveFilePath)
        {
            _game = game;
            _cornerTexture = game.Content.Load<Texture2D>("corner");
            _boxTexture = game.Content.Load<Texture2D>("square");
            _whitePixelTexture = new Texture2D(game.GraphicsDevice, 1, 1);
            _whitePixelTexture.SetData(new[] { Color.White });
            _lineTexture = new Texture2D(game.GraphicsDevice, 1, 1);
            _lineTexture.SetData(new[] { Color.DarkGray });

            // Load in the wave data
            _audioBuffer = Utility.LoadWaveSamples(waveFilePath);

            // Scale to a default size and volume
            this.Position = new Rectangle(0, 0, DefaultWidth, DefaultHeight);
            this.Volume = 0.50f;

            // Render the waveform
            this.RenderWaveform(_game.GraphicsDevice);

            // Set a random color
            this.Color = new Color(_random.Next(255), _random.Next(255), _random.Next(255));

            // Set a random position
            int x = _random.Next(0, _game.GraphicsDevice.Viewport.Width - this.Position.Width);
            int y = _random.Next(0, _game.GraphicsDevice.Viewport.Height - this.Position.Height);
            this.Position = new Rectangle(x, y, this.Position.Width, this.Position.Height);
        }

        public void RenderWaveform(GraphicsDevice device)
        {
            SpriteBatch batch = new SpriteBatch(device);
            _waveform = new RenderTarget2D(
                device,
                this.Position.Width,
                this.Position.Height,
                false,
                device.PresentationParameters.BackBufferFormat, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);

            try
            {
                device.SetRenderTarget(_waveform);
                device.Clear(Color.Transparent);

                // Begin to draw. Use Additive for an interesting effect.
                batch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

                // The X-variable points out to which column of pixels to draw on.
                var x = 0;

                // Why are we dividing by 4 here?
                var sampleCount = _audioBuffer.Length / 4;

                // Calculate the samples per pixel 
                var samplesPerPixel = (int)Math.Ceiling((float)sampleCount / this.Position.Width);

                // The index of the current sample in the microphone buffer.
                var sampleIndex = 0;

                // The vertical mid point of the image (the Y-position
                // of a zero-sample and the height of the loudest sample).
                var halfHeight = this.Position.Height / 2;

                // The maximum number of a 16-bit signed integer.
                // Dividing a signed 16-bit integer (the range -32768..32767)
                // by this value will give a value in the range of -1 (inclusive) to 1 (exclusive).
                // const float SampleFactor = 32768f;

                // Iterate through the samples and render them on the image.
                for (var i = 0; i < sampleCount; i++)
                {
                    // Increment the X-coordinate each time 'samplesPerPixel' pixels
                    // has been drawn.
                    if ((i > 0) && ((i % samplesPerPixel) == 0))
                    {
                        x++;
                    }

                    // Convert the current sample (16-bit value) from the byte-array to a
                    // floating point value in the range of -1 (inclusive) to 1 (exclusive).
                    // var sampleValue = BitConverter.ToInt16(_audioBuffer, sampleIndex) / SampleFactor;
                    var sampleValue = _audioBuffer[sampleIndex];

                    // Scale the sampleValue to its corresponding height in pixels.
                    var sampleHeight = (int)Math.Abs(sampleValue * halfHeight);

                    // The top of the column of pixels.
                    // A positive sample should be drawn from the center and upwards,
                    // and a negative sample from the center and downwards.
                    // Since a rectangle is used to describe the "pixel column", the
                    // top must be modified depending on the sign of the sample (positive/negative).
                    var y = (sampleValue < 0)
                        ? halfHeight
                        : halfHeight - sampleHeight;

                    // Create the 1 pixel wide rectangle corresponding to the sample.
                    var destinationRectangle = new Rectangle(x, y, 1, sampleHeight);

                    // Draw using the white pixel (stretching it to fill the rectangle).
                    batch.Draw(
                        _whitePixelTexture,
                        destinationRectangle,
                        Color.White);

                    // Step the sample.
                    sampleIndex += 1;
                }
            }
            catch
            {
                // TODO: Handle exceptions
            }
            finally
            {
                batch.End();
                device.SetRenderTarget(null);
            }
        }

        public static List<SoundPanel> LoadFromPath(Game game, string path)
        {
            List<SoundPanel> panels = new List<SoundPanel>();

            // Grab all wav files in the specified path
            string[] files = Directory.GetFiles(path, "*.wav");

            foreach (string s in files)
            {
                SoundPanel panel = null;

                try
                {
                    panel = new SoundPanel(game, s);
                }
                catch
                {
                    // TODO: Do something with the exception other than fail to load the file
                }

                if (panel != null)
                {
                    panels.Add(panel);
                }
            }

            return panels;
        }

        public void Update() { }

        public void Draw(SpriteBatch batch)
        {
            // Draw the box outline and volume bar
            if (this.IsSelected)
            {
                int borderWidth = 1;
                batch.Draw(_lineTexture, new Rectangle(Position.Left, Position.Top, borderWidth, Position.Height), Color.DimGray);         // Left
                batch.Draw(_lineTexture, new Rectangle(Position.Right, Position.Top, borderWidth, Position.Height), Color.DimGray);        // Right
                batch.Draw(_lineTexture, new Rectangle(Position.Left, Position.Top, Position.Width, borderWidth), Color.DimGray);          // Top
                batch.Draw(_lineTexture, new Rectangle(Position.Left, Position.Bottom, Position.Width + 1, borderWidth), Color.DimGray);   // Bottom
                batch.Draw(_lineTexture, new Rectangle(Position.Left, Position.Bottom - (int)(this.Position.Height * this.Volume), Position.Width, borderWidth), Color.DimGray);         // Volume
                batch.Draw(
                    _cornerTexture,
                    new Rectangle(
                        (Position.Left + Position.Width) - _cornerTexture.Width,
                        (Position.Top + Position.Height) - _cornerTexture.Height,
                        _cornerTexture.Width,
                        _cornerTexture.Height),
                        Color.DimGray);
            }

            batch.Draw(
                _boxTexture,
                this.Position,
                this.Color * .25f);

            // Draw the waveform on top of it
            if (_waveform.IsContentLost)
            {
                this.RenderWaveform(_game.GraphicsDevice);
            }

            batch.Draw(
                _waveform,
                this.Position,
                Color.White * .25f);
        }

        public void UnloadContent()
        {
            _whitePixelTexture.Dispose();

            if (_boxTexture != null)
            {
                _boxTexture.Dispose();
            }

            if (_cornerTexture != null)
            {
                _cornerTexture.Dispose();
            }

            if (_waveform != null)
            {
                _waveform.Dispose();
            }
        }
    }
}
