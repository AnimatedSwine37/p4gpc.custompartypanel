using p4gpc.custompartypanel.Configuration;
using Reloaded.Memory.Sigscan;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace p4gpc.custompartypanel
{
    public class Utils
    {
        public Config Configuration;
        private ILogger _logger;
        private int _baseAddress;
        private IMemory _memory;
        private IntPtr _flagLocation;
        private IntPtr _eventLocation;
        private IntPtr _inMenuLocation;
        private IntPtr _itemLocation;

        public Utils(Config configuration, ILogger logger, IMemory memory)
        {
            using var thisProcess = Process.GetCurrentProcess();
            _baseAddress = thisProcess.MainModule.BaseAddress.ToInt32();

            // Initialise fields
            Configuration = configuration;
            _logger = logger;
            _memory = memory;
        }

        public void LogDebug(string message)
        {
            if (Configuration.DebugEnabled)
                _logger.WriteLine($"[CustomPartyPanel] {message}");
        }

        public void Log(string message)
        {
            _logger.WriteLine($"[CustomPartyPanel] {message}");
        }

        public void LogError(string message, Exception e)
        {
            _logger.WriteLine($"[CustomPartyPanel] {message}: {e.Message}", System.Drawing.Color.Red);
        }

        public void LogError(string message)
        {
            _logger.WriteLine($"[CustomPartyPanel] {message}", System.Drawing.Color.Red);
        }

        // Signature Scans for a location in memory, returning -1 if the scan fails otherwise the address
        public long SigScan(string pattern, string functionName)
        {
            try
            {
                using var thisProcess = Process.GetCurrentProcess();
                using var scanner = new Scanner(thisProcess, thisProcess.MainModule);
                long functionAddress = scanner.CompiledFindPattern(pattern).Offset;
                if (functionAddress < 0) throw new Exception($"Unable to find bytes with pattern {pattern}");
                functionAddress += _baseAddress;
                LogDebug($"Found the {functionName} address at 0x{functionAddress:X}");
                return functionAddress;
            }
            catch (Exception exception)
            {
                LogError($"An error occured trying to find the {functionName} function address. Not initializing. Please report this with information on the version of P4G you are running", exception);
                return -1;
            }
        }
    }
}
