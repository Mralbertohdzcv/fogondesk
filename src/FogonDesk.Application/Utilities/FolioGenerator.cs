using System;
using System.Globalization;

namespace FogonDesk.Application.Utilities
{
    public sealed class FolioGenerator
    {
        public string Generate(string prefix, DateTime utcNow, int sequence)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("El prefijo es obligatorio.", nameof(prefix));
            }

            if (sequence < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence), "La secuencia debe ser mayor o igual a 1.");
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1:yyyyMMdd}-{2:000000}",
                prefix.Trim().ToUpperInvariant(),
                utcNow,
                sequence);
        }
    }
}
