using System;
using System.Collections.Generic;
using System.Text;

namespace Veldrid.D3D12
{
    public class D3D12Util
    {
        public static readonly int gpu_resource_heap_uav_heap = 8;
        public static readonly int gpu_sampler_heap_count = 8 * 8;
        public static readonly int gpu_resource_heap_cbv_heap = 12;
        public static readonly int gpu_resource_heap_srv_heap = 16 * 16;

        public static readonly int shader_component_mapping4 = 5768;

        public static ulong AlignUp(uint alignment, ulong size_in_bytes)
        {
            ulong num = alignment - 1U;

            return (ulong)((long)size_in_bytes + (long)num & ~(long)num);
        }

    }
}
