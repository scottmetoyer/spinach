using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Spinach.Domain.Abstract
{
    public interface IPanel
    {
        bool IsSelected { get; set; }

        Rectangle Position { get; set; }

        Rectangle ResizeHandle { get; }

        void Update();

        void Draw(SpriteBatch batch);
    }
}
