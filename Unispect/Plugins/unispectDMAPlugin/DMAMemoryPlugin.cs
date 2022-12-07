using System;
using System.IO;
using Unispect;
using vmmsharp;

namespace unispectDMAPlugin
{
    [UnispectPlugin]
    public sealed class DMAMemoryPlugin : MemoryProxy
    {
        private const string MemMapPath = @"mmap.txt";
        // Using a static field since Unispect does not Dispose the plugin after each operation.
        // However it will attempt to re-run the constructor each time (leading to an FPGA Init error).
        // Using a static field will save the plugin state between operations.
        // Uses a lock to prevent possible (but unlikely) race conditions.
        private static readonly object _sync = new();
        private static Vmm _vmm;
        private static bool _loaded = false;
        private uint _pid;

        public DMAMemoryPlugin()
        {
            lock (_sync)
            {
                try
                {
                    if (_loaded) return;
                    Log.Add("[DMA] Plugin Starting...");
                    if (File.Exists(MemMapPath))
                    {
                        Log.Add("[DMA] Memory Map Found!");
                        _vmm = new Vmm("-printf", "-v", "-device", "FPGA", "-memmap", MemMapPath, "-waitinitialize");
                    }
                    else
                        _vmm = new Vmm("-printf", "-v", "-device", "FPGA", "-waitinitialize");
                    _loaded = true;
                    Log.Add("[DMA] Plugin Loaded!");
                }
                catch (Exception ex)
                {
                    throw new DMAMemoryPluginException("[DMA] ERROR Initializing FPGA", ex);
                }
            }
        }

        public override ModuleProxy GetModule(string moduleName)
        {
            try
            {
                Log.Add($"[DMA] Module Search: '{moduleName}'");
                var module = _vmm.Map_GetModuleFromName(_pid, moduleName);
                Log.Add($"[DMA] Module Found: '{module.wszText}' | Base: 0x{module.vaBase.ToString("X")} | Size: {module.cbImageSize}");
                return new ModuleProxy(moduleName, module.vaBase, (int)module.cbImageSize);
            }
            catch (Exception ex)
            {
                throw new DMAMemoryPluginException($"[DMA] ERROR retrieving module '{moduleName}'", ex);
            }
        }

        public override bool AttachToProcess(string handle)
        {
            try
            {
                Log.Add($"[DMA] Attaching to process '{handle}'");
                // Slightly differs from Unispect's default Memory Plugin.
                // Use 'ProcessName.exe' instead of 'ProcessName'.
                _vmm.PidGetFromName(handle, out _pid);
                if (_pid != 0)
                    return true;
                throw new Exception("Process not found!");
            }
            catch (Exception ex)
            {
                throw new DMAMemoryPluginException($"[DMA] ERROR attaching to process '{handle}'", ex);
            }
        }

        public override byte[] Read(ulong address, int length)
        {
            try
            {
                // Fixed partial reads bug from original
                return _vmm.MemRead(_pid, address, (uint)length);
            }
            catch (Exception ex)
            {
                throw new DMAMemoryPluginException($"[DMA] ERROR Reading {length} bytes at 0x{address.ToString("X")}", ex);
            }
        }

        public override void Dispose()
        {
            lock (_sync)
            {
                Log.Add("[DMA] Dispose");
                _vmm?.Dispose(); // Close FPGA Connection and Release Resources
                _vmm = null;
                _loaded = false;
            }
        }
    }
}