using p4gpc.custompartypanel.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static p4gpc.tinyadditions.P4GEnums;
using static Reloaded.Hooks.Definitions.X86.FunctionAttribute;

namespace p4gpc.custompartypanel
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod
    {
        private IAsmHook _inBtlFgHook;
        private IAsmHook _inBtlBgHook;
        private IAsmHook _inBtlHpBarHook;
        private IAsmHook _commandCircleHook;
        private IAsmHook _commandCircleTextHook;
        private IAsmHook _dungeonHook;

        private string _setCommandCircleColourCall;

        private IReverseWrapper<SetFgColourFunction> _setFgColourReverseWrapper;
        private IReverseWrapper<SetBgColourFunction> _setBgColourReverseWrapper;
        private IReverseWrapper<SetHpBgColourFunction> _setHpBgColourReverseWrapper;
        private IReverseWrapper<SetCommandCircleColourFunction> _setCommandCircleColourReverseWrapper;
        private IReverseWrapper<SetInDungeonColoursFunction> _setInDungeonColoursReverseWrapper;

        private Config _configuration;
        private IMemory _memory;
        private Utils _utils;
        private Reloaded.Hooks.Definitions.IReloadedHooks _hooks;

        private Colour[] _fgColours;
        private Colour[] _bgColours;
        private Colour[] _ogFgColours;
        private Colour[] _ogBgColours;
        private PartyMember _currentMember;

        // A pointer to the party address as the address doesn't get loaded until the game loads
        private IntPtr _partyPtr;

        // Start address where all party information is
        private IntPtr _partyAddress;

        public Mod(Utils utils, Config configuration, IMemory memory, Reloaded.Hooks.Definitions.IReloadedHooks hooks)
        {
            _configuration = configuration;
            _memory = memory;
            _utils = utils;
            _hooks = hooks;

            InitPartyLocation();
            InitColourArrays();
            InitInBtlHook();
            InitCommandCircleHook();
            //InitCommandCircleTextHook();
            InitDungeonHook();
        }

        // Initialises the arrays that store the party panel colours (so we don't do reflection 100s of times a second which presumably would be badish)
        private void InitColourArrays()
        {
            List<Colour> fgColours = new List<Colour>();
            List<Colour> bgColours = new List<Colour>();
            foreach (PartyMember member in (PartyMember[])Enum.GetValues(typeof(PartyMember)))
            {
                if (member == PartyMember.Rise)
                {
                    // Rise doesn't have a party panel so no need for a colour
                    fgColours.Add(null);
                    bgColours.Add(null);
                }
                else
                {
                    fgColours.Add((Colour)_configuration.GetType().GetProperty($"{member}FgColour").GetValue(_configuration));
                    bgColours.Add((Colour)_configuration.GetType().GetProperty($"{member}BgColour").GetValue(_configuration));
                }
            }

            // The og colours are used for rgb garbage since we need to keep a reference of the original value so it's nice and smooth
            _fgColours = fgColours.ToArray();
            _bgColours = bgColours.ToArray();

            // Create 2 new arrays of colours (has to be done like this otherwise the new array would still reference the old colour objects)
            fgColours.Clear();
            bgColours.Clear();
            for (int i = 0; i < _fgColours.Length; i++)
            {
                Colour fgColour = _fgColours[i];
                if (fgColour == null)
                {
                    // Rise's "colour" is null
                    fgColours.Add(null);
                    bgColours.Add(null);
                    continue;
                }
                fgColours.Add(new Colour(fgColour.R, fgColour.G, fgColour.B));
                Colour bgColour = _bgColours[i];
                bgColours.Add(new Colour(bgColour.R, bgColour.G, bgColour.B));
            }
            _ogBgColours = bgColours.ToArray();
            _ogFgColours = fgColours.ToArray();
        }

        // Initialises the hook for changing colours while in battle
        private void InitInBtlHook()
        {
            long address = _utils.SigScan("E8 ?? ?? ?? ?? F3 0F 10 94 24 ?? ?? ?? ?? 83 C4 04 F3 0F 10 8C 24 ?? ?? ?? ??", "in battle colour");
            string[] fgFunction =
            {
                "use32",
                // Save xmm0 (will be unintentionally altered)
                $"sub esp, 16", // allocate space on stack
                $"movdqu dqword [esp], xmm0",
                // Save xmm3
                $"sub esp, 16", // allocate space on stack
                $"movdqu dqword [esp], xmm3",
                $"{_hooks.Utilities.PushCdeclCallerSavedRegisters()}",
                $"{_hooks.Utilities.GetAbsoluteCallMnemonics(SetFgColour, out _setFgColourReverseWrapper)}",
                $"{_hooks.Utilities.PopCdeclCallerSavedRegisters()}",
                //Pop back the value from stack to xmm3
                $"movdqu xmm3, dqword [esp]",
                $"add esp, 16", // re-align the stack
                //Pop back the value from stack to xmm0
                $"movdqu xmm0, dqword [esp]",
                $"add esp, 16", // re-align the stack
            };
            _inBtlFgHook = _hooks.CreateAsmHook(fgFunction, address - 10, AsmHookBehaviour.ExecuteAfter).Activate();

            string[] bgFunction =
            {
                "use32",
                // Save xmm0 (will be unintentionally altered)
                $"sub esp, 16", // allocate space on stack
                $"movdqu dqword [esp], xmm0",
                // Save xmm3
                $"sub esp, 16", // allocate space on stack
                $"movdqu dqword [esp], xmm3",
                $"{_hooks.Utilities.PushCdeclCallerSavedRegisters()}",
                $"{_hooks.Utilities.GetAbsoluteCallMnemonics(SetBgColour, out _setBgColourReverseWrapper)}",
                $"{_hooks.Utilities.PopCdeclCallerSavedRegisters()}",
                //Pop back the value from stack to xmm3
                $"movdqu xmm3, dqword [esp]",
                $"add esp, 16", // re-align the stack
                //Pop back the value from stack to xmm0
                $"movdqu xmm0, dqword [esp]",
                $"add esp, 16", // re-align the stack
            };
            _inBtlBgHook = _hooks.CreateAsmHook(bgFunction, address - 0x96, AsmHookBehaviour.ExecuteAfter).Activate();

            string[] hpBgFunction =
            {
                "use32",
                // Save xmm0 (will be unintentionally altered)
                $"sub esp, 16", // allocate space on stack
                $"movdqu dqword [esp], xmm0",
                // Save xmm3
                $"sub esp, 16", // allocate space on stack
                $"movdqu dqword [esp], xmm3",
                $"{_hooks.Utilities.PushCdeclCallerSavedRegisters()}",
                $"{_hooks.Utilities.GetAbsoluteCallMnemonics(SetHpOutlineColour, out _setHpBgColourReverseWrapper)}",
                $"{_hooks.Utilities.PopCdeclCallerSavedRegisters()}",
                //Pop back the value from stack to xmm3
                $"movdqu xmm3, dqword [esp]",
                $"add esp, 16", // re-align the stack
                //Pop back the value from stack to xmm0
                $"movdqu xmm0, dqword [esp]",
                $"add esp, 16", // re-align the stack
            };
            _inBtlHpBarHook = _hooks.CreateAsmHook(hpBgFunction, address + 0x324, AsmHookBehaviour.ExecuteAfter).Activate();
        }

        // Initialise the hook for changing the colour of the "command" circle that displays next to the active character in battle
        private void InitCommandCircleHook()
        {
            _setCommandCircleColourCall = _hooks.Utilities.GetAbsoluteCallMnemonics(SetCommandCircleColour, out _setCommandCircleColourReverseWrapper);

            long address = _utils.SigScan("E8 ?? ?? ?? ?? 83 C4 08 66 C7 87 ?? ?? ?? ?? FF FF C6 87 ?? ?? ?? ?? FF C6 47 ?? 00 5F 5E 5B", "command circle");

            string[] function =
            {
                "use32",
                // Save xmm0 (will be unintentionally altered)
                $"sub esp, 16", // allocate space on stack
                $"movdqu dqword [esp], xmm0",
                // Save xmm3
                $"sub esp, 16", // allocate space on stack
                $"movdqu dqword [esp], xmm3",
                $"{_hooks.Utilities.PushCdeclCallerSavedRegisters()}",
                $"{_setCommandCircleColourCall}",
                $"{_hooks.Utilities.PopCdeclCallerSavedRegisters()}",
                //Pop back the value from stack to xmm3
                $"movdqu xmm3, dqword [esp]",
                $"add esp, 16", // re-align the stack
                //Pop back the value from stack to xmm0
                $"movdqu xmm0, dqword [esp]",
                $"add esp, 16", // re-align the stack
            };
            _commandCircleHook = _hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate();
        }

        // Initialise the hook for changing the colour of the text around the command circle
        private void InitCommandCircleTextHook()
        {
            long address = _utils.SigScan("E8 ?? ?? ?? ?? 83 C4 08 66 C7 87 ?? ?? ?? ?? FF FF 33 C9 C6 87 ?? ?? ?? ?? FF C6 47 ?? 00 E8 ?? ?? ?? ?? 33 C0", "command circle text");

            string[] function =
            {
                "use32",
                $"{_hooks.Utilities.PushCdeclCallerSavedRegisters()}",
                $"{_setCommandCircleColourCall}",
                $"{_hooks.Utilities.PopCdeclCallerSavedRegisters()}",
            };
            _commandCircleTextHook = _hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate();
        }

        // Initialises the hook that changes stuff for the in dungeon party panel
        private void InitDungeonHook()
        {
            long address = _utils.SigScan("E8 ?? ?? ?? ?? 8D 8E ?? ?? ?? ?? E8 ?? ?? ?? ?? 8D 8E ?? ?? ?? ?? E8 ?? ?? ?? ?? 8B 83 ?? ?? ?? ??", "dungeon party panel");

            string[] function =
            {
                "use32",
                $"{_hooks.Utilities.PushCdeclCallerSavedRegisters()}",
                $"{_hooks.Utilities.GetAbsoluteCallMnemonics(SetInDungeonColours, out _setInDungeonColoursReverseWrapper)}",
                $"{_hooks.Utilities.PopCdeclCallerSavedRegisters()}",
            };
            _dungeonHook = _hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate();
        }

        private void InitPartyLocation()
        {
            long address = _utils.SigScan("8B 0D ?? ?? ?? ?? B8 01 00 00 00 66 89 06 ?? ?? 0F B7 41 ??", "in party pointer");
            if (address == -1)
            {
                Suspend();
                return;
            }
            _memory.SafeRead((IntPtr)(address + 2), out _partyPtr);
        }

        private void SetFgColour(PartyMember member, IntPtr colourAddress)
        {
            Colour colour = _fgColours[(int)member];
            DoRgbTransition(colour, _ogFgColours[(int)member]);
            byte[] colourBytes = { colour.R, colour.G, colour.B };
            _memory.SafeWrite(colourAddress + 0x84, colourBytes);
        }

        private void SetBgColour(PartyMember member, IntPtr colourAddress)
        {
            _currentMember = member;
            Colour colour = _bgColours[(int)member];
            DoRgbTransition(colour, _ogBgColours[(int)member]);
            byte[] colourBytes = { colour.R, colour.G, colour.B };
            _memory.SafeWrite(colourAddress + 0x84, colourBytes);
        }

        private void SetHpOutlineColour(IntPtr colourAddress)
        {
            Colour colour = _fgColours[(int)_currentMember];
            byte[] colourBytes = { colour.R, colour.G, colour.B };
            _memory.SafeWrite(colourAddress + 0x84, colourBytes);
        }

        private void SetCommandCircleColour(IntPtr colourAddress, int activeMemberPos)
        {
            PartyMember member;
            if (activeMemberPos == 0)
                member = PartyMember.Protagonist;
            else
                member = (PartyMember)GetInParty()[activeMemberPos-1];
            
            Colour colour = _bgColours[(int)member - 1];
            byte[] colourBytes = { colour.R, colour.G, colour.B };
            _memory.SafeWrite(colourAddress + 0x84, colourBytes);
        }

        private void SetInDungeonColours(IntPtr colourAddress, int activeMemberPos)
        {
            // Get the in party address (has to be done after the game has initialised)
            if (_partyAddress == IntPtr.Zero)
            {
                _memory.SafeRead(_partyPtr, out _partyAddress);
                _partyAddress += 4;
                _utils.LogDebug($"The in party info starts at 0x{_partyAddress:X}");
            }

            PartyMember member;
            if (activeMemberPos == 0)
                member = PartyMember.Protagonist;
            else
                member = (PartyMember)GetInParty()[activeMemberPos - 1];

            Colour bgColour = _bgColours[(int)member - 1];
            DoRgbTransition(bgColour, _ogBgColours[(int)member - 1]);
            byte[] bgColourBytes = { bgColour.R, bgColour.G, bgColour.B, 255 };

            Colour fgColour = _fgColours[(int)member - 1];
            DoRgbTransition(fgColour, _ogFgColours[(int)member - 1]);
            byte[] fgColourBytes = { fgColour.R, fgColour.G,fgColour.B, 255 };

            // Write the changed colour for the bg
            IntPtr bgColourAddress = colourAddress + 0x274;
            bgColourAddress += 16;
            // There are 4 seperate instances of the colour that have to be written to change it fully (it's done in a gradient, so all have to be the same for a solid colour)
            for(int i = 0; i< 4; i++)
            {
                _memory.SafeWrite(bgColourAddress, bgColourBytes);
                bgColourAddress += 24; 
            }

            // Write the changed colours for the fg
            IntPtr fgColourAddress = colourAddress + 0x51C;
            fgColourAddress += 16;
            // There are 4 seperate instances of the colour that have to be written to change it fully (it's done in a gradient, so all have to be the same for a solid colour)
            for (int i = 0; i < 4; i++)
            {
                _memory.SafeWrite(fgColourAddress, fgColourBytes);
                fgColourAddress += 24;
            }
        }

        private short[] GetInParty()
        {
            StructArray.FromPtr(_partyAddress, out short[] inParty, 3);
            return inParty;
        }

        // Makes the epic rgb transition rainbow thing happen
        private void DoRgbTransition(Colour colour, Colour transitionColour)
        {
            if (!_configuration.RgbMode)
                return;
            if (colour.Equals(transitionColour))
            {
                byte r = transitionColour.R;
                transitionColour.R = transitionColour.G;
                transitionColour.G = r;
                transitionColour.B = r;
            }

            if (colour.G < transitionColour.G) colour.G++;
            else if (colour.R > transitionColour.R) colour.R--;
            else if (colour.B < transitionColour.B) colour.B++;
            else if (colour.G > transitionColour.G) colour.G--;
            else if (colour.R < transitionColour.R) colour.R++;
            else if (colour.B > transitionColour.B) colour.B--;
        }

        public void Resume()
        {
            _inBtlBgHook?.Enable();
            _inBtlFgHook?.Enable();
            _inBtlHpBarHook?.Enable();
            _commandCircleHook?.Enable();
        }

        public void Suspend()
        {
            _inBtlBgHook?.Disable();
            _inBtlFgHook?.Disable();
            _inBtlHpBarHook?.Disable();
            _commandCircleHook?.Disable();
        }

        public void UpdateConfiguration(Config configuration)
        {
            _configuration = configuration;
            InitColourArrays(); // Reload the colour arrays in case they were changed
        }

        // Function delegates
        [Function(new Register[] { Register.edx, Register.edi }, Register.eax, StackCleanup.Callee)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetFgColourFunction(PartyMember member, IntPtr colourAddress);

        [Function(new Register[] { Register.edx, Register.edi }, Register.eax, StackCleanup.Callee)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetBgColourFunction(PartyMember member, IntPtr colourAddress);

        [Function(Register.edi, Register.eax, StackCleanup.Callee)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetHpBgColourFunction(IntPtr colourAddress);

        [Function(new Register[] { Register.edi, Register.ebx }, Register.eax, StackCleanup.Callee)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetCommandCircleColourFunction(IntPtr colourAddress, int activeMemberPos);

        [Function(new Register[] { Register.esi, Register.eax }, Register.eax, StackCleanup.Callee)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetInDungeonColoursFunction(IntPtr colourAddress, int activeMemberPos);
    }
}