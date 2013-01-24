using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Spinach.Domain
{
    public class Grain
    {
        private float[] _buffer;

        public Envelope Envelope { get; set; }

        public int Length
        {
            get
            {
                return this.Envelope.Attack + this.Envelope.Sustain + this.Envelope.Decay;
            }
        }

        public int Index
        {
            get
            {
                return this.Envelope.TotalElapsed;
            }
        }

        public Point Position { get; set; }

        public bool IsActive { get { return this.Envelope.IsRunning; } }

        public float[] Buffer
        {
            get { return _buffer; }
            set { _buffer = value; }
        }

        public Grain(Envelope envelope)
        {
            this.Envelope = envelope;
            _buffer = new float[0];
        }

        public void Trigger(Point position)
        {
            this.Position = position;
            this.Envelope.Trigger();
        }

        public void Update()
        {
            this.Envelope.Update();

            if (!this.Envelope.IsRunning)
            {
                _buffer = new float[0];
            }
        }
    }
}
