using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DrawChar.Core.Drawables {
    public abstract class Drawable : IDisposable {
        public Drawable() {
            ASCIIRenderer.Drawables.Add(this);
        }

        public Vector2 Position = Vector2.Zero;
        public Vector2 Size = Vector2.Zero;
        public List<char> Buffer = new();
        public bool isTransparent = true;
        public bool isVisible = true;
        public double zIndex = 0;
        public virtual void OnDraw() { }
        public virtual void Draw() => ASCIIRenderer.PlaceBuffer(Buffer, (int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
        public virtual void DrawUnsafe() => ASCIIRenderer.PlaceBufferUnsafe(Buffer, (int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);

        public void Dispose() {
            ASCIIRenderer.Drawables.Remove(this);
        }
    }
}
