using System;
using FogonDesk.Application.Contracts;

namespace FogonDesk.Infrastructure.Platform
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }
    }
}
