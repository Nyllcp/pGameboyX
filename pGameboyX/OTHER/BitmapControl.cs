using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace pGameboyX
{
    public class GameboyBitmapControl : Control
    {
        private WriteableBitmap? _bitmap;
        private uint[]? _frame; 
        
        private const int Width = 160;
        private const int Height = 144;

        public int redrawFrames;

        public GameboyBitmapControl()
        {
            this.AttachedToVisualTree += (s, e) =>
            {
                _bitmap = new WriteableBitmap(
                    new PixelSize(Width, Height),
                    new Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul
                );
                
            };
            RenderOptions.SetBitmapInterpolationMode(this,BitmapInterpolationMode.None);
        }

        public void SetBitmapFromStream(Stream stream)
        {
            _bitmap = WriteableBitmap.Decode(stream);
            InvalidateVisual();
        }

        public void InterpolationMode(BitmapInterpolationMode interpolationMode)
        {
            RenderOptions.SetBitmapInterpolationMode(this, interpolationMode);
            InvalidateVisual();
        }
        public void UpdateFrame(uint[] frame)
        {
            if (frame.Length != Width * Height)
                throw new ArgumentException("Frame size mismatch");

            _frame = frame;
            if (_bitmap == null || _frame == null)
                return;

            byte[] result = new byte[_frame.Length * sizeof(int)];
            Buffer.BlockCopy(_frame, 0, result, 0, result.Length);
            using (var fb = _bitmap.Lock())
            {
                Marshal.Copy(result, 0, fb.Address, result.Length);
            }
            InvalidateVisual(); 
            redrawFrames++;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_bitmap == null)
                return;
            
            var widthRatio = Bounds.Width / Width;
            var heightRatio = Bounds.Height / Height;
            var rat1 = 16f;
            var rat2 = 15f;
            double calcwidth = 0;
            double calcheight = 0;
            if(widthRatio > heightRatio)
            {
                calcheight = Bounds.Height;
                var ratio = calcheight / rat2;
                calcwidth = rat1 * ratio;
            }
            else
            {
                calcwidth = Bounds.Width;
                var ratio = calcwidth / rat1;
                calcheight = ratio * rat2;
            }
            var xoffset = (Bounds.Width - calcwidth) / 2;
            var yoffset = (Bounds.Height - calcheight) / 2;
            Rect rect = new Rect(xoffset, yoffset, calcwidth, calcheight);

            context.DrawImage(
                _bitmap,
                rect
            );
        }
    }
}