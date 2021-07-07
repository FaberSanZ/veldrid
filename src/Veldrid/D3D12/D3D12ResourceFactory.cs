using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D12;

namespace Veldrid.D3D12
{
    internal class D3D12ResourceFactory : ResourceFactory
    {
        public override GraphicsBackend BackendType => GraphicsBackend.Direct3D12;

        private readonly D3D12GraphicsDevice _gd;
        public ID3D12Device _device;
       

        public D3D12ResourceFactory(D3D12GraphicsDevice vkGraphicsDevice)
            : base(vkGraphicsDevice.Features)
        {
            _gd = vkGraphicsDevice;
            _device = vkGraphicsDevice._device;
        }

        public override CommandList CreateCommandList(ref CommandListDescription description)
        {
            throw new NotImplementedException();
        }

        public override Pipeline CreateComputePipeline(ref ComputePipelineDescription description)
        {
            throw new NotImplementedException();
        }

        public override Fence CreateFence(bool signaled)
        {
            throw new NotImplementedException();
        }

        public override Framebuffer CreateFramebuffer(ref FramebufferDescription description)
        {
            throw new NotImplementedException();
        }

        public override ResourceLayout CreateResourceLayout(ref ResourceLayoutDescription description)
        {
            throw new NotImplementedException();
        }

        public override ResourceSet CreateResourceSet(ref ResourceSetDescription description)
        {
            throw new NotImplementedException();
        }

        public override Swapchain CreateSwapchain(ref SwapchainDescription description)
        {
            throw new NotImplementedException();
        }

        protected override DeviceBuffer CreateBufferCore(ref BufferDescription description)
        {
            throw new NotImplementedException();
        }

        protected override Pipeline CreateGraphicsPipelineCore(ref GraphicsPipelineDescription description)
        {
            throw new NotImplementedException();
        }

        protected override Sampler CreateSamplerCore(ref SamplerDescription description)
        {
            throw new NotImplementedException();
        }

        protected override Shader CreateShaderCore(ref ShaderDescription description)
        {
            throw new NotImplementedException();
        }

        protected override Texture CreateTextureCore(ulong nativeTexture, ref TextureDescription description)
        {
            throw new NotImplementedException();
        }

        protected override Texture CreateTextureCore(ref TextureDescription description)
        {
            throw new NotImplementedException();
        }

        protected override TextureView CreateTextureViewCore(ref TextureViewDescription description)
        {
            throw new NotImplementedException();
        }
    }
}
