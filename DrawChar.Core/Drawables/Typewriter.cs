using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace DrawChar.Core.Drawables {
    public class Typewriter : Drawable {
        public char? Background { get; init; }
        public string Text { get; init; }
        public int TextPosition = 0;
        public int AutoTypingDelay = -1;
        public bool LoopTyping = false;
        private ulong counter = 0;
        /// <summary>
        /// Text typewriter
        /// </summary>
        /// <param name="background">Currently unsupported</param>
        public Typewriter(string text, float x = 0, float y = 0, float w = 0, float h = 0, char? background = null) {
            Text = text;
            Position = new Vector2(x, y);
            Size = new Vector2(w, h);
            Background = background;
        }
        public string Next() {
            TextPosition++;
            if(LoopTyping && TextPosition > Text.Length)
                TextPosition = 0;
            TextPosition = Math.Min(TextPosition, Text.Length);
            var final = Text.Substring(0, TextPosition - 1);
            Buffer = final.ToCharArray().ToList();
            return final;
        }
        public override void OnDraw() {
            if(AutoTypingDelay == -1)
                return;
            counter++;
            if(counter % (ulong)AutoTypingDelay == 0) {
                this.Next();
            }
        }
    }
}
