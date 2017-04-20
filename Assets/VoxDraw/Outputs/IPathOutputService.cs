using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxDraw.Outputs
{
    interface IPathOutputService
    {
        void Process(PathDrawingContext context);
    }
}
