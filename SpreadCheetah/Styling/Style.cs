using System;
using System.Collections.Generic;

namespace SpreadCheetah.Styling
{
    public sealed class Style : IEquatable<Style>
    {
        public Fill Fill { get; set; } = new Fill();
        public Font Font { get; set; } = new Font();

        /// <inheritdoc/>
        public bool Equals(Style? other) => other != null
            && EqualityComparer<Fill>.Default.Equals(Fill, other.Fill)
            && EqualityComparer<Font>.Default.Equals(Font, other.Font);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Style other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Fill, Font);
    }
}
