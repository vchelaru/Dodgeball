﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dodgeball.Entities;
using RenderingLibrary;

namespace Dodgeball.GumRuntimes
{
    public partial class WorldComponentRuntime
    {
        public IPositionedSizedObject PlayArea => GameArea;
        public IPositionedSizedObject LeftTeamRectangle => Team0Rectangle;
        public IPositionedSizedObject RightTeamRectangle => Team1Rectangle;

        partial void CustomInitialize()
        {
#if DEBUG
            if (DebuggingVariables.ShowDebugShapes)
            {
                GameArea.Visible = true;
                Team0Rectangle.Visible = true;
                Team1Rectangle.Visible = true;
            }
            else
            {
#endif
                GameArea.Visible = false;
                Team0Rectangle.Visible = false;
                Team1Rectangle.Visible = false;
#if DEBUG
            }
#endif
        }
    }
}
