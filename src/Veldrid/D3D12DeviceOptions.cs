using System;

namespace Veldrid
{
    /// <summary>
    /// A structure describing Direct3D12-specific device creation options.
    /// </summary>
    public struct D3D12DeviceOptions
    {
        /// <summary>
        /// Native pointer to an adapter.
        /// </summary>
        public IntPtr AdapterPtr;

    }
}
