using System;
using Avalonia.Input;

namespace pGameboyX
{
    
    public class IO
    {
   

        private int keyData = 0xFF;
        private int keyDataLast;

        private bool saveStateToggle = false;
        public bool FrameLimit = true;
        private bool frameLimitToggle = false;

        private const byte GB_UP = 0x40;
        private const byte GB_DOWN = 0x80;
        private const byte GB_LEFT = 0x20;
        private const byte GB_RIGHT = 0x10;
        private const byte GB_A = 0x1;
        private const byte GB_B = 0x2;
        private const byte GB_START = 0x8;
        private const byte GB_SELECT = 0x4;

        public IO()
        {
        }

        public long ElapsedTimeMS()
        {
            var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            return now;
        }

        public long ElapsedTimeMicro()
        {
            var now = DateTime.Now.Ticks / TimeSpan.TicksPerMicrosecond;
            return now;
        }
        

        public void AvaloniaKeyDown(ref Core _gb, Avalonia.Input.KeyEventArgs e)
        {
            if(_gb == null) return;
            keyDataLast = keyData;
            //keyData = 0xFF;
            if (e.Key == Key.Right)
            {
                keyData &= ~GB_RIGHT;
            }
            if (e.Key == Key.Left)
            {
                keyData &= ~GB_LEFT;
            }
            if (e.Key == Key.Up)
            {
                keyData &= ~GB_UP;
            }
            if (e.Key == Key.Down)
            {
                keyData &= ~GB_DOWN;
            }
            if (e.Key == Key.S)
            {
                keyData &= ~GB_START;
            }

            if (e.Key == Key.A)
            {
                keyData &= ~GB_SELECT;
            }

            if (e.Key == Key.X)
            {
                keyData &= ~GB_A;
            }

            if (e.Key == Key.Z)
            {
                keyData &= ~GB_B;
            }

            if (keyData != keyDataLast)
            {
                _gb.UpdatePad((byte)keyData);
            }

            if (e.Key == Key.C)
            {
                FrameLimit = !FrameLimit;
            }

            if (e.Key == Key.Q)
            {
                _gb.SelectedSavestate--;
            }

            if (e.Key == Key.W)
            {
                _gb.SelectedSavestate++;
            }

            if (e.Key == Key.R)
            {
                _gb.LoadState = true;
            }

            if (e.Key == Key.E)
            {
                _gb.SaveState = true;
            }
            

        }
        public void AvaloniaKeyUp(ref Core _gb, Avalonia.Input.KeyEventArgs e)
        {
            if(_gb == null) return;
            keyDataLast = keyData;
            //keyData = 0;
            if (e.Key == Key.Right)
            {
                keyData |= GB_RIGHT;
            }
            if (e.Key == Key.Left)
            {
                keyData |= GB_LEFT;
            }
            if (e.Key == Key.Up)
            {
                keyData |= GB_UP;
            }
            if (e.Key == Key.Down)
            {
                keyData |= GB_DOWN;
            }
            if (e.Key == Key.S)
            {
                keyData |= GB_START;
            }
            if (e.Key == Key.A)
            {
                keyData |= GB_SELECT;
            }
            if (e.Key == Key.X)
            {
                keyData |= GB_A;
            }
            if (e.Key == Key.Z)
            {
                keyData |= GB_B;
            }
            if (keyData != keyDataLast)
            {
                _gb.UpdatePad((byte)keyData);
            }
        }
      
    }
}

    

