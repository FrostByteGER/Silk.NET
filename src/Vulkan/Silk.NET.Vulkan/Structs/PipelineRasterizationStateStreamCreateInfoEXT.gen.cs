// This file is part of Silk.NET.
// 
// You may modify and distribute Silk.NET under the terms
// of the MIT license. See the LICENSE file for details.


using System;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Ultz.SuperInvoke;

#pragma warning disable 1591

namespace Silk.NET.Vulkan
{
    public unsafe struct PipelineRasterizationStateStreamCreateInfoEXT
    {
        public PipelineRasterizationStateStreamCreateInfoEXT
        (
            StructureType sType = StructureType.PipelineRasterizationStateStreamCreateInfoExt,
            void* pNext = default,
            uint flags = default,
            uint rasterizationStream = default
        )
        {
           SType = sType;
           PNext = pNext;
           Flags = flags;
           RasterizationStream = rasterizationStream;
        }

/// <summary></summary>
        public StructureType SType;
/// <summary></summary>
        public void* PNext;
/// <summary></summary>
        public uint Flags;
/// <summary></summary>
        public uint RasterizationStream;
    }
}
