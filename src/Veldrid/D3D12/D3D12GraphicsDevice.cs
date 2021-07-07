using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.DXGI;
using static Vortice.Direct3D12.D3D12;
using static Vortice.DXGI.DXGI;

namespace Veldrid.D3D12
{
    internal class DX12UploadResource : IDisposable
    {
        public ID3D12Resource handle;
        public ulong begin;
        public ulong end;
        public ulong current;
        public ulong count;
        public uint align;
        private D3D12GraphicsDevice _device;

        public DX12UploadResource(D3D12GraphicsDevice device, ulong size, uint align = 512)
        {
            _device = device;
            handle = _device._device.CreateCommittedResource<ID3D12Resource>(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(size), ResourceStates.GenericRead);
            current = begin = (ulong)(long)handle.Map(0);
            end = begin + size;
            align = align;
        }

        public ulong Allocate(ulong dataSize)
        {
            ulong num = 0;
            lock (handle)
            {
                if (align > 0U)
                    current = D3D12Util.AlignUp(align, current);
                num = current;
                current += dataSize;
            }
            count++;
            return num;
        }

        public void Clear()
        {
            lock (handle)
            {
                current = begin;
                count = 0UL;
            }
        }

        public ulong CalculateOffset(ulong address) => address - begin;

        public void Dispose()
        {
            handle?.Unmap(0);
            handle?.Dispose();
        }


    }
    internal class D3D12GraphicsDevice : GraphicsDevice
    {

        public ID3D12Device _device;
        public IDXGIFactory4 _factory;
        internal D3D12DescriptorAllocator RenderTargetViewAllocator;
        internal D3D12DescriptorAllocator DepthStencilViewAllocator;
        internal D3D12DescriptorAllocator ShaderResourceViewAllocator;
        internal D3D12DescriptorAllocator SamplerAllocator;
        internal CpuDescriptorHandle[] NullDescriptors;
        internal ID3D12RootSignature DefaultGraphicsSignature;
        internal ID3D12RootSignature DefaultComputeSignature;
        internal DX12UploadResource BufferUploader;
        internal DX12UploadResource TextureUploader;
        internal ID3D12CommandSignature DispatchIndirectCommandSignature;
        internal ID3D12CommandSignature DrawInstancedIndirectCommandSignature;
        internal ID3D12CommandSignature DrawIndexedInstancedIndirectCommandSignature;
        internal ID3D12CommandQueue CopyCommandQueue;
        internal ID3D12CommandAllocator CopyCommandAlloc;
        internal ID3D12GraphicsCommandList CopyCommandList;
        internal D3D12Fence CopyFence;
        internal AutoResetEvent CopyFenceEvent;
        internal ulong copy_fence_value;


        public D3D12GraphicsDevice(GraphicsDeviceOptions options, D3D12DeviceOptions d3D12DeviceOptions, SwapchainDescription? swapchainDesc)
           : this(MergeOptions(d3D12DeviceOptions, options), swapchainDesc)
        {
        }

        public D3D12GraphicsDevice(D3D12DeviceOptions options, SwapchainDescription? swapchainDesc)
        {
            bool validation_layer = false;
#if DEBUG
            validation_layer = true;
#endif


            _device?.Dispose();
            ID3D12Debug debug_interface;
            if (validation_layer && D3D12GetDebugInterface(out debug_interface).Success)
                debug_interface.EnableDebugLayer();

            CreateDXGIFactory1(out _factory);

            D3D12CreateDevice(null, FeatureLevel.Level_12_1, out _device);

            if (validation_layer)
            {
                ID3D12DebugDevice debug_device = _device.QueryInterfaceOrNull<ID3D12DebugDevice>();

                if (debug_device != null)
                {
                    ID3D12InfoQueue info_queue = debug_device.QueryInterfaceOrNull<ID3D12InfoQueue>();
                    if (info_queue != null)
                    {
                        MessageId[] message_id = new MessageId[5]
                        {
                            MessageId.ClearDepthStencilViewMismatchingClearValue,
                            MessageId.ClearRenderTargetViewMismatchingClearValue,
                            MessageId.InvalidDescriptorHandle,
                            MessageId.MapInvalidNullRange,
                            MessageId.UnmapInvalidNullRange
                        };
                        InfoQueueFilter filter = new InfoQueueFilter()
                        {
                            DenyList = new InfoQueueFilterDescription()
                            {
                                Ids = message_id
                            },
                            AllowList = new InfoQueueFilterDescription()
                        };
                        info_queue.AddStorageFilterEntries(filter);
                    }
                    info_queue.Dispose();
                }
                debug_device.Dispose();
            }
        }


        public override string DeviceName => _factory.DebugName;

        public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

        public override bool IsUvOriginTopLeft => true;

        public override bool IsDepthRangeZeroToOne => true;

        public override bool IsClipSpaceYInverted => true;

        public override ResourceFactory ResourceFactory => null;

        public override Swapchain MainSwapchain => null;

        public override GraphicsDeviceFeatures Features => null;


        private static D3D12DeviceOptions MergeOptions(D3D12DeviceOptions d3D11DeviceOptions, GraphicsDeviceOptions options)
        {
            //if (options.Debug)
            //{
            //    d3D11DeviceOptions.DeviceCreationFlags |= (uint)DeviceCreationFlags.Debug;
            //}

            return d3D11DeviceOptions;
        }

        public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat)
        {
            return TextureSampleCount.Count4;
        }

        public override void ResetFence(Fence fence)
        {
            
        }

        public override bool WaitForFence(Fence fence, ulong nanosecondTimeout)
        {
            return true;

        }

        public override bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout)
        {
            return true;
        }

        protected override MappedResource MapCore(MappableResource resource, MapMode mode, uint subresource)
        {
            return new MappedResource();
        }

        protected override void PlatformDispose()
        {
        }

        protected override void UnmapCore(MappableResource resource, uint subresource)
        {
        }

        internal override uint GetStructuredBufferMinOffsetAlignmentCore()
        {
            return 1;
        }

        internal override uint GetUniformBufferMinOffsetAlignmentCore()
        {
            return 1;
        }

        private protected override bool GetPixelFormatSupportCore(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties)
        {
            properties = new PixelFormatProperties();
            return true;
        }

        private protected override void SubmitCommandsCore(CommandList commandList, Fence fence)
        {
        }

        private protected override void SwapBuffersCore(Swapchain swapchain)
        {
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
        }

        private protected override void UpdateTextureCore(Texture texture, IntPtr source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer)
        {
        }

        private protected override void WaitForIdleCore()
        {
        }
    }
}
