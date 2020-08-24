// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Interactive
{
    using System;
    using System.Diagnostics;
    using Snoop.Infrastructure;

    public static class ProcessExtensions
    {
        public static Architecture GetArchitecture(this Process process)
            => NativeMethods.IsProcess64BitWithoutException(process ?? throw new ArgumentNullException(nameof(process)))
                ? Architecture.x64
                : Architecture.x86;

        public static bool IsElevated(this Process process)
            => NativeMethods.IsProcessElevated(process ?? throw new ArgumentNullException(nameof(process)));
    }
}