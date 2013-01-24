using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Spinach.Domain
{
    enum EnvelopeState
    {
        Attack,
        Sustain,
        Decay
    }

    public class Envelope
    {
        private bool _running;
        private EnvelopeState _state;
        private int _elapsed;
        private int _totalElapsed;
        private float _currentAmplitude;

        public int Attack { get; set; }

        public int Sustain { get; set; }

        public int Decay { get; set; }

        public int TotalElapsed
        {
            get { return this._totalElapsed; }
        }

        public bool IsRunning { get { return this._running; } }

        public float CurrentAmplitude { get { return this._currentAmplitude; } }

        public Envelope(Envelope envelope) :
            this(envelope.Attack, envelope.Sustain, envelope.Decay)
        {
        }

        public Envelope(int attack, int sustain, int decay)
        {
            this.Attack = attack;
            this.Decay = decay;
            this.Sustain = sustain;
            _elapsed = 0;
            _totalElapsed = 0;
            _currentAmplitude = 0.0f;
            _state = EnvelopeState.Attack;
            _running = false;
        }

        public void Trigger()
        {
            this._elapsed = 0;
            this._totalElapsed = 0;
            _state = EnvelopeState.Attack;
            this._running = true;
        }

        public void Update()
        {
            if (_running)
            {
                // Increment total elapsed and elapsed by a single sample
                _totalElapsed += 1;
                _elapsed += 1;

                if (this._state == EnvelopeState.Attack)
                {
                    if (_elapsed >= this.Attack)
                    {
                        this._state = EnvelopeState.Sustain;
                        _elapsed = 0;
                    }
                    else
                    {
                        _currentAmplitude = (float)_elapsed / this.Attack;
                    }
                }

                if (this._state == EnvelopeState.Sustain)
                {
                    if (_elapsed >= this.Sustain)
                    {
                        this._state = EnvelopeState.Decay;
                        _elapsed = 0;
                    }
                    else
                    {
                        _currentAmplitude = 1.0f;
                    }
                }

                if (this._state == EnvelopeState.Decay)
                {
                    // The envelope is done
                    if (_elapsed >= this.Decay)
                    {
                        this._running = false;
                    }
                    else
                    {
                        _currentAmplitude = 1.0f + (float)_elapsed / this.Decay * -1.0f;
                    }
                }
            }
        }
    }
}
