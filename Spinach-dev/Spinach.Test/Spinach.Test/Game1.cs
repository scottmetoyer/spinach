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

namespace Spinach.Test
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        DynamicSoundEffectInstance _instance;
        byte[] _xnaBuffer;
        float[] _workingBuffer;
        float[] _audioData;
        int samplesPerBuffer = 3000;
        int offset = 0;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            _instance = new DynamicSoundEffectInstance(44100, AudioChannels.Stereo);

            // Load in the wave data
            _audioData = Utility.LoadWaveSamples("Content\\break.wav");

            // Initialize the sound system
            _instance = new DynamicSoundEffectInstance(44100, AudioChannels.Stereo);
            _xnaBuffer = new byte[samplesPerBuffer * 2 * 2]; // There are two bytes per sample, 2 channels
            _workingBuffer = new float[samplesPerBuffer * 2];

            _instance.Play();
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            while (_instance.PendingBufferCount < 3)
            {
                this.FillWorkingBuffer();
                _instance.SubmitBuffer(_xnaBuffer);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            base.Draw(gameTime);
        }

        private void FillWorkingBuffer()
        {
            if (offset + samplesPerBuffer >= _audioData.Length)
            {
                offset = 0;
            }

            for (int i = 0; i < (samplesPerBuffer * 2); i++)
            {
                _workingBuffer[i] = _audioData[i + offset];        
            }

            offset += samplesPerBuffer * 2;

            // All done, convert the buffer
            Utility.ConvertBuffer(_workingBuffer, _xnaBuffer);
        }
    }
}
