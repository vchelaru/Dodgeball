using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenderingLibrary;

namespace Dodgeball.GumRuntimes
{
    public partial class WorldComponentRuntime
    {
        public IPositionedSizedObject LeftTeamRectangle => Team0Rectangle as IPositionedSizedObject;
        public IPositionedSizedObject RightTeamRectangle => Team1Rectangle as IPositionedSizedObject;
    }
}
