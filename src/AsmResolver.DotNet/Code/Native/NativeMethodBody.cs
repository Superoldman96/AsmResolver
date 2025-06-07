using System;
using System.Collections.Generic;
using AsmResolver.Collections;
using AsmResolver.PE.Code;
using AsmResolver.Shims;

namespace AsmResolver.DotNet.Code.Native
{
    /// <summary>
    /// Represents a method body of a method defined in a .NET assembly, implemented using machine code that runs
    /// natively on the processor.
    /// </summary>
    public class NativeMethodBody : MethodBody
    {
        /// <summary>
        /// Creates a new empty native method body.
        /// </summary>
        public NativeMethodBody()
        {
            Code = ArrayShim.Empty<byte>();
        }

        /// <summary>
        /// Creates a new native method body with the provided raw code stream.
        /// </summary>
        /// <param name="code">The raw code stream.</param>
        public NativeMethodBody(byte[] code)
        {
            Code = code;
        }

        /// <summary>
        /// Gets or sets the raw native code stream.
        /// </summary>
        public byte[] Code
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a collection of fixups that need to be applied upon writing the code to the output stream.
        /// This includes addresses to imported symbols and global fields stored in data sections.
        /// </summary>
        public IList<AddressFixup> AddressFixups
        {
            get;
        } = new List<AddressFixup>();
    }

}
