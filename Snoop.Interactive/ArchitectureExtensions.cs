// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Interactive
{
    public static class ArchitectureExtensions
    {
        public static string GetSuffix(this Architecture architecture)
            => architecture switch
            {
                Architecture.x64 => "x64",
                Architecture.x86 => "x86",
                _ => string.Empty
            };
    }
}