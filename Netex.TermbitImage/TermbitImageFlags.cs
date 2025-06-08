namespace Netex.TermbitImage;

using System;

[Flags]
public enum TermbitImageFlags
{
    Compressed = 1 << 0,
    ContainsMetadata = 1 << 1
}