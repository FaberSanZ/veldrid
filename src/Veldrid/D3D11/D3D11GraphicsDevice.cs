﻿using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Veldrid.D3D11
{
    internal class D3D11GraphicsDevice : GraphicsDevice
    {
        private readonly SharpDX.Direct3D11.Device _device;
        private readonly DeviceContext _immediateContext;
        private readonly SwapChain _swapChain;
        private D3D11Framebuffer _swapChainFramebuffer;
        private readonly bool _supportsConcurrentResources;
        private readonly bool _supportsCommandLists;
        private readonly object _immediateContextLock = new object();

        public override GraphicsBackend BackendType => GraphicsBackend.Direct3D11;

        public override ResourceFactory ResourceFactory { get; }

        public override Framebuffer SwapchainFramebuffer => _swapChainFramebuffer;

        public SharpDX.Direct3D11.Device Device => _device;

        public bool SupportsConcurrentResources => _supportsConcurrentResources;

        public bool SupportsCommandLists => _supportsCommandLists;

        public List<D3D11CommandList> CommandListsReferencingSwapchain { get; internal set; } = new List<D3D11CommandList>();

        public D3D11GraphicsDevice(IntPtr hwnd, int width, int height)
        {
            SwapChainDescription swapChainDescription = new SwapChainDescription()
            {
                BufferCount = 1,
                IsWindowed = true,
                ModeDescription = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                OutputHandle = hwnd,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };
#if DEBUG
            DeviceCreationFlags creationFlags = DeviceCreationFlags.Debug;
#else
            DeviceCreationFlags creationFlags = DeviceCreationFlags.None;
#endif 
            SharpDX.Direct3D11.Device4.CreateWithSwapChain(
                SharpDX.Direct3D.DriverType.Hardware,
                creationFlags,
                swapChainDescription,
                out _device,
                out _swapChain);
            _immediateContext = _device.ImmediateContext;
            _device.CheckThreadingSupport(out _supportsConcurrentResources, out _supportsCommandLists);

            Factory factory = _swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(hwnd, WindowAssociationFlags.IgnoreAll);

            ResourceFactory = new D3D11ResourceFactory(this);
            RecreateSwapchainFramebuffer(width, height);

            PostDeviceCreated();
        }

        public override void ResizeMainWindow(uint width, uint height)
        {
            RecreateSwapchainFramebuffer((int)width, (int)height);
        }

        private void RecreateSwapchainFramebuffer(int width, int height)
        {
            // NOTE: Perhaps this should be deferred until all CommandLists naturally remove their references to the swapchain.
            // The actual resize could be done in ExecuteCommands() when it is found that this list is empty.
            foreach (D3D11CommandList d3dCL in CommandListsReferencingSwapchain)
            {
                d3dCL.Reset();
            }

            _swapChainFramebuffer?.Dispose();

            _swapChain.ResizeBuffers(2, width, height, Format.B8G8R8A8_UNorm, SwapChainFlags.None);

            // Get the backbuffer from the swapchain
            using (Texture2D backBufferTexture = _swapChain.GetBackBuffer<Texture2D>(0))
            using (Texture2D depthBufferTexture = new Texture2D(
                _device,
                new Texture2DDescription()
                {
                    Format = Format.D16_UNorm,
                    ArraySize = 1,
                    MipLevels = 1,
                    Width = backBufferTexture.Description.Width,
                    Height = backBufferTexture.Description.Height,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                }))
            {
                D3D11Texture backBufferVdTexture = new D3D11Texture(backBufferTexture);
                D3D11Texture depthVdTexture = new D3D11Texture(depthBufferTexture);
                FramebufferDescription desc = new FramebufferDescription(depthVdTexture, backBufferVdTexture);
                _swapChainFramebuffer = new D3D11Framebuffer(_device, ref desc);
                _swapChainFramebuffer.IsSwapchainFramebuffer = true;
            }
        }

        public override void ExecuteCommands(CommandList cl)
        {
            D3D11CommandList d3d11CL = Util.AssertSubtype<CommandList, D3D11CommandList>(cl);
            lock (_immediateContextLock)
            {
                _immediateContext.ExecuteCommandList(d3d11CL.DeviceCommandList, false);
            }
            d3d11CL.DeviceCommandList.Dispose();
            d3d11CL.DeviceCommandList = null;
            CommandListsReferencingSwapchain.Remove(d3d11CL);
        }

        public override void SwapBuffers()
        {
            _swapChain.Present(0, PresentFlags.None);
        }

        public override void SetResourceName(DeviceResource resource, string name)
        {
            switch (resource)
            {
                case D3D11Buffer buffer:
                    buffer.Buffer.DebugName = name;
                    break;
                case D3D11CommandList commandList:
                    commandList.DeviceContext.DebugName = name;
                    break;
                case D3D11Framebuffer framebuffer:
                    for (int i = 0; i < framebuffer.RenderTargetViews.Length; i++)
                    {
                        framebuffer.RenderTargetViews[i].DebugName = string.Format("{0}_RTV{1}", name, i);
                    }
                    if (framebuffer.DepthStencilView != null)
                    {
                        framebuffer.DepthStencilView.DebugName = string.Format("{0}_DSV", name);
                    }
                    break;
                case D3D11Sampler sampler:
                    sampler.DeviceSampler.DebugName = name;
                    break;
                case D3D11Shader shader:
                    shader.DeviceShader.DebugName = name;
                    break;
                case D3D11Texture tex:
                    tex.DeviceTexture.DebugName = name;
                    break;
                case D3D11TextureView texView:
                    texView.ShaderResourceView.DebugName = name;
                    break;
            }
        }

        public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat)
        {
            Format dxgiFormat = D3D11Formats.ToDxgiFormat(format, depthFormat);
            if (CheckFormat(dxgiFormat, 32))
            {
                return TextureSampleCount.Count32;
            }
            else if (CheckFormat(dxgiFormat, 16))
            {
                return TextureSampleCount.Count16;
            }
            else if (CheckFormat(dxgiFormat, 8))
            {
                return TextureSampleCount.Count8;
            }
            else if (CheckFormat(dxgiFormat, 4))
            {
                return TextureSampleCount.Count4;
            }
            else if (CheckFormat(dxgiFormat, 2))
            {
                return TextureSampleCount.Count2;
            }

            return TextureSampleCount.Count1;
        }

        private bool CheckFormat(Format format, int sampleCount)
        {
            return _device.CheckMultisampleQualityLevels(format, sampleCount) != 0;
        }

        protected override MappedResource MapCore(MappableResource resource, MapMode mode, uint subresource)
        {
            if (resource is D3D11Buffer buffer)
            {
                DataBox db = _immediateContext.MapSubresource(
                    buffer.Buffer,
                    0,
                    D3D11Formats.VdToD3D11MapMode(mode),
                    SharpDX.Direct3D11.MapFlags.None,
                    out DataStream ds);

                return new MappedResource(resource, mode, db.DataPointer, buffer.SizeInBytes);
            }
            else throw new NotImplementedException();
        }

        protected override void UnmapCore(MappableResource resource, uint subresource)
        {
            lock (_immediateContextLock)
            {
                if (resource is D3D11Buffer buffer)
                {
                    _immediateContext.UnmapSubresource(buffer.Buffer, 0);
                }
            }
        }

        public unsafe override void UpdateBuffer(Buffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            D3D11Buffer d3dBuffer = Util.AssertSubtype<Buffer, D3D11Buffer>(buffer);
            if (sizeInBytes == 0)
            {
                return;
            }

            bool useMap = (buffer.Usage & BufferUsage.Dynamic) == BufferUsage.Dynamic;

            if (useMap)
            {
                if (bufferOffsetInBytes != 0)
                {
                    throw new NotImplementedException("bufferOffsetInBytes must be 0 for Dynamic Buffers.");
                }
                MappedResource mr = MapCore(buffer, MapMode.Write, 0);
                if (sizeInBytes < 1024)
                {
                    Unsafe.CopyBlock(mr.Data.ToPointer(), source.ToPointer(), sizeInBytes);
                }
                else
                {
                    System.Buffer.MemoryCopy(source.ToPointer(), mr.Data.ToPointer(), buffer.SizeInBytes, sizeInBytes);
                }
                UnmapCore(buffer, 0);
            }
            else
            {
                ResourceRegion? subregion = null;
                if ((d3dBuffer.Buffer.Description.BindFlags & BindFlags.ConstantBuffer) != BindFlags.ConstantBuffer)
                {
                    // For a shader-constant buffer; set pDstBox to null. It is not possible to use
                    // this method to partially update a shader-constant buffer

                    subregion = new ResourceRegion()
                    {
                        Left = (int)bufferOffsetInBytes,
                        Right = (int)(sizeInBytes + bufferOffsetInBytes),
                        Bottom = 1,
                        Back = 1
                    };
                }
                lock (_immediateContextLock)
                {
                    _immediateContext.UpdateSubresource(d3dBuffer.Buffer, 0, subregion, source, 0, 0);
                }
            }
        }

        public override void UpdateTexture(
            Texture texture,
            IntPtr source,
            uint sizeInBytes,
            uint x,
            uint y,
            uint z,
            uint width,
            uint height,
            uint depth,
            uint mipLevel,
            uint arrayLayer)
        {
            Texture2D deviceTexture = Util.AssertSubtype<Texture, D3D11Texture>(texture).DeviceTexture;
            int subresource = D3D11Util.ComputeSubresource(mipLevel, texture.MipLevels, arrayLayer);
            ResourceRegion resourceRegion = new ResourceRegion(
                left: (int)x,
                right: (int)(x + width),
                top: (int)y,
                front: (int)z,
                bottom: (int)(y + height),
                back: (int)(z + depth));
            uint srcRowPitch = FormatHelpers.GetSizeInBytes(texture.Format) * width;
            lock (_immediateContextLock)
            {
                _immediateContext.UpdateSubresource(deviceTexture, subresource, resourceRegion, source, (int)srcRowPitch, 0);
            }
        }

        protected override void PlatformDispose()
        {
            DeviceDebug deviceDebug = _device.QueryInterfaceOrNull<DeviceDebug>();
            if (deviceDebug != null)
            {
                deviceDebug.ReportLiveDeviceObjects(ReportingLevel.Summary);
                deviceDebug.ReportLiveDeviceObjects(ReportingLevel.Detail);
            }
        }

        public override void WaitForIdle()
        {
        }
    }
}
