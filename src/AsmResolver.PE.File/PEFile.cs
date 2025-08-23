using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using AsmResolver.IO;

namespace AsmResolver.PE.File
{
    /// <summary>
    /// Models a file using the portable executable (PE) file format. It provides access to various PE headers, as well
    /// as the raw contents of each section present in the file.
    /// </summary>
    public class PEFile : ISegmentReferenceFactory, IOffsetConverter
    {
        /// <summary>
        /// Indicates a valid NT header signature.
        /// </summary>
        public const uint ValidPESignature = 0x4550; // "PE\0\0"

        private readonly LazyVariable<PEFile, ISegment?> _extraSectionData;
        private readonly LazyVariable<PEFile, ISegment?> _eofData;
        private IList<PESection>? _sections;

        /// <summary>
        /// Creates a new empty portable executable file.
        /// </summary>
        public PEFile()
            : this(new DosHeader(), new FileHeader(), new OptionalHeader())
        {
        }

        /// <summary>
        /// Creates a new portable executable file.
        /// </summary>
        /// <param name="dosHeader">The DOS header to add.</param>
        /// <param name="fileHeader">The COFF header to add.</param>
        /// <param name="optionalHeader">The optional header to add.</param>
        public PEFile(DosHeader dosHeader, FileHeader fileHeader, OptionalHeader optionalHeader)
        {
            DosHeader = dosHeader ?? throw new ArgumentNullException(nameof(dosHeader));
            FileHeader = fileHeader ?? throw new ArgumentNullException(nameof(fileHeader));
            OptionalHeader = optionalHeader ?? throw new ArgumentNullException(nameof(optionalHeader));
            _extraSectionData = new LazyVariable<PEFile, ISegment?>(x =>x.GetExtraSectionData());
            _eofData = new LazyVariable<PEFile, ISegment?>(x =>x.GetEofData());
            MappingMode = PEMappingMode.Unmapped;
        }

        /// <summary>
        /// When this PE file was read from the disk, gets the file path to the PE file.
        /// </summary>
        public string? FilePath
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the DOS header of the PE file.
        /// </summary>
        public DosHeader DosHeader
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the COFF file header of the portable executable (PE) file.
        /// </summary>
        public FileHeader FileHeader
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the optional header of the portable executable (PE) file.
        /// </summary>
        public OptionalHeader OptionalHeader
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a collection of sections present in the portable executable (PE) file.
        /// </summary>
        public IList<PESection> Sections
        {
            get
            {
                if (_sections is null)
                    Interlocked.CompareExchange(ref _sections, GetSections(), null);
                return _sections;
            }
        }

        /// <summary>
        /// Gets a value indicating the mapping mode of the PE file. If the PE file is in its mapped form,
        /// then every offset of all segments in the PE file will be equal to the physical memory address.
        /// If the file is in its unmapped form, the offsets will be equal to the file offset.
        /// </summary>
        public PEMappingMode MappingMode
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the padding data in between the last section header and the first section.
        /// </summary>
        public ISegment? ExtraSectionData
        {
            get => _extraSectionData.GetValue(this);
            set => _extraSectionData.SetValue(value);
        }

        /// <summary>
        /// Gets or sets the data appended to the end of the file (EoF), if available.
        /// </summary>
        public ISegment? EofData
        {
            get => _eofData.GetValue(this);
            set => _eofData.SetValue(value);
        }

        /// <summary>
        /// Reads an unmapped PE file from the disk.
        /// </summary>
        /// <param name="path">The file path to the PE file.</param>
        /// <returns>The PE file that was read.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the file does not follow the PE file format.</exception>
        public static PEFile FromFile(string path) => FromFile(UncachedFileService.Instance.OpenFile(path));

        /// <summary>
        /// Reads an unmapped PE file.
        /// </summary>
        /// <param name="file">The file representing the PE.</param>
        /// <returns>The PE file that was read.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the file does not follow the PE file format.</exception>
        public static PEFile FromFile(IInputFile file)
        {
            var result = FromReader(file.CreateReader());
            result.FilePath = file.FilePath;
            return result;
        }

        /// <summary>
        /// Reads an unmapped PE file from memory.
        /// </summary>
        /// <param name="raw">The raw bytes representing the contents of the PE file to read.</param>
        /// <returns>The PE file that was read.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the file does not follow the PE file format.</exception>
        public static PEFile FromBytes(byte[] raw) => FromReader(new BinaryStreamReader(raw));

        /// <summary>
        /// Reads an unmapped PE file from memory.
        /// </summary>
        /// <param name="raw">The raw stream representing the contents of the PE file to read.</param>
        /// <returns>The PE file that was read.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the file does not follow the PE file format.</exception>
        public static PEFile FromStream(Stream raw) => FromReader(new BinaryStreamReader(raw));

        /// <summary>
        /// Reads a mapped PE file starting at the provided module base address (HINSTANCE).
        /// </summary>
        /// <param name="hInstance">The HINSTANCE or base address of the module.</param>
        /// <returns>The PE file that was read.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the file does not follow the PE file format.</exception>
        public static PEFile FromModuleBaseAddress(IntPtr hInstance) => FromModuleBaseAddress(hInstance, PEMappingMode.Mapped);

        /// <summary>
        /// Reads a PE file starting at the provided module base address (HINSTANCE).
        /// </summary>
        /// <param name="hInstance">The HINSTANCE or base address of the module.</param>
        /// <param name="mode">Indicates how the input PE file is mapped.</param>
        /// <returns>The PE file that was read.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the file does not follow the PE file format.</exception>
        public static unsafe PEFile FromModuleBaseAddress(IntPtr hInstance, PEMappingMode mode)
        {
            // Perform some minimal parsing to get the size of the image from the optional header.
            uint nextHeaderOffset = *(uint*) ((byte*) hInstance + DosHeader.NextHeaderFieldOffset);
            uint sizeOfImage = *(uint*) ((byte*) hInstance
                + nextHeaderOffset
                + sizeof(uint)
                + FileHeader.FileHeaderSize
                + OptionalHeader.OptionalHeaderSizeOfImageFieldOffset);
            return FromDataSource(new UnmanagedDataSource(hInstance, sizeOfImage), mode);
        }

        /// <summary>
        /// Reads a PE file from the provided data source.
        /// </summary>
        /// <param name="dataSource">The data source to read from.</param>
        /// <param name="mode">Indicates how the input PE file is mapped.</param>
        /// <returns>The PE file that was read.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the file does not follow the PE file format.</exception>
        public static PEFile FromDataSource(IDataSource dataSource, PEMappingMode mode = PEMappingMode.Unmapped) =>
            FromReader(new BinaryStreamReader(dataSource, dataSource.BaseAddress, 0, (uint) dataSource.Length), mode);

        /// <summary>
        /// Reads a PE file from the provided input stream.
        /// </summary>
        /// <param name="reader">The input stream to read from.</param>
        /// <param name="mode">Indicates how the input PE file is mapped.</param>
        /// <returns>The PE file that was read.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the file does not follow the PE file format.</exception>
        public static PEFile FromReader(in BinaryStreamReader reader, PEMappingMode mode = PEMappingMode.Unmapped) =>
            new SerializedPEFile(reader, mode);

        /// <inheritdoc />
        public ISegmentReference GetReferenceToRva(uint rva) => rva != 0
            ? new PESegmentReference(this, rva)
            : SegmentReference.Null;

        /// <inheritdoc />
        public uint FileOffsetToRva(ulong fileOffset) =>
            GetSectionContainingOffset(fileOffset).FileOffsetToRva(fileOffset);

        /// <inheritdoc />
        public ulong RvaToFileOffset(uint rva) => GetSectionContainingRva(rva).RvaToFileOffset(rva);

        /// <summary>
        /// Finds the section containing the provided file offset.
        /// </summary>
        /// <param name="fileOffset">The file offset.</param>
        /// <returns>The section containing the file offset.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Occurs when the file offset does not fall within any of the sections.</exception>
        public PESection GetSectionContainingOffset(ulong fileOffset)
        {
            if (!TryGetSectionContainingOffset(fileOffset, out var section))
                throw new ArgumentOutOfRangeException(nameof(fileOffset));
            return section;
        }

        /// <summary>
        /// Attempts to find the section containing the provided file offset.
        /// </summary>
        /// <param name="fileOffset">The file offset.</param>
        /// <param name="section">The section that was found.</param>
        /// <returns><c>true</c> if the section was found, <c>false</c> otherwise.</returns>
        public bool TryGetSectionContainingOffset(ulong fileOffset, [NotNullWhen(true)] out PESection? section)
        {
            var sections = Sections;

            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i].ContainsFileOffset(fileOffset))
                {
                    section = sections[i];
                    return true;
                }
            }

            section = null;
            return false;
        }

        /// <summary>
        /// Finds the section containing the provided virtual address.
        /// </summary>
        /// <param name="rva">The virtual address.</param>
        /// <returns>The section containing the virtual address.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Occurs when the virtual address does not fall within any of the sections.</exception>
        public PESection GetSectionContainingRva(uint rva)
        {
            if (!TryGetSectionContainingRva(rva, out var section))
                throw new ArgumentOutOfRangeException(nameof(rva));
            return section;
        }

        /// <summary>
        /// Attempts to find the section containing the provided virtual address.
        /// </summary>
        /// <param name="rva">The virtual address.</param>
        /// <param name="section">The section that was found.</param>
        /// <returns><c>true</c> if the section was found, <c>false</c> otherwise.</returns>
        public bool TryGetSectionContainingRva(uint rva,  [NotNullWhen(true)] out PESection? section)
        {
            var sections = Sections;

            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i].ContainsRva(rva))
                {
                    section = sections[i];
                    return true;
                }
            }

            section = null;
            return false;
        }

        /// <summary>
        /// Obtains a reader that spans the provided data directory.
        /// </summary>
        /// <param name="dataDirectory">The data directory to read.</param>
        /// <returns>The reader.</returns>
        public BinaryStreamReader CreateDataDirectoryReader(DataDirectory dataDirectory)
        {
            var section = GetSectionContainingRva(dataDirectory.VirtualAddress);
            ulong fileOffset = section.RvaToFileOffset(dataDirectory.VirtualAddress);
            return section.CreateReader(fileOffset, dataDirectory.Size);
        }

        /// <summary>
        /// Attempts to create a reader that spans the provided data directory.
        /// </summary>
        /// <param name="dataDirectory">The data directory to read.</param>
        /// <param name="reader">The reader that was created.</param>
        /// <returns><c>true</c> if the reader was created successfully, <c>false</c> otherwise.</returns>
        public bool TryCreateDataDirectoryReader(DataDirectory dataDirectory, out BinaryStreamReader reader)
        {
            if (TryGetSectionContainingRva(dataDirectory.VirtualAddress, out var section))
            {
                ulong fileOffset = section.RvaToFileOffset(dataDirectory.VirtualAddress);
                reader = section.CreateReader(fileOffset, dataDirectory.Size);
                return true;
            }

            reader = default;
            return false;
        }

        /// <summary>
        /// Creates a new reader at the provided file offset.
        /// </summary>
        /// <param name="fileOffset">The file offset to start reading at.</param>
        /// <returns>The reader.</returns>
        public BinaryStreamReader CreateReaderAtFileOffset(uint fileOffset)
        {
            return !TryCreateReaderAtFileOffset(fileOffset, out var reader)
                ? throw new ArgumentOutOfRangeException(nameof(fileOffset))
                : reader;
        }

        /// <summary>
        /// Attempts to create a new reader at the provided file offset.
        /// </summary>
        /// <param name="fileOffset">The file offset to start reading at.</param>
        /// <param name="reader">The reader that was created.</param>
        /// <returns><c>true</c> if the reader was created successfully, <c>false</c> otherwise.</returns>
        public bool TryCreateReaderAtFileOffset(uint fileOffset, out BinaryStreamReader reader)
        {
            if (TryGetSectionContainingOffset(fileOffset, out var section))
            {
                reader = section.CreateReader(fileOffset);
                return true;
            }

            if (EofData is IReadableSegment eofData
                && fileOffset >= eofData.Offset
                && fileOffset < eofData.Offset + eofData.GetPhysicalSize())
            {
                reader = eofData.CreateReader(fileOffset);
                return true;
            }

            reader = default;
            return false;
        }

        /// <summary>
        /// Creates a new reader of a chunk of data at the provided file offset.
        /// </summary>
        /// <param name="fileOffset">The file offset to start reading at.</param>
        /// <param name="size">The number of bytes in the chunk.</param>
        /// <returns>The reader.</returns>
        public BinaryStreamReader CreateReaderAtFileOffset(uint fileOffset, uint size)
        {
            var section = GetSectionContainingOffset(fileOffset);
            return section.CreateReader(fileOffset, size);
        }

        /// <summary>
        /// Attempts to create a new reader of a chunk of data at the provided file offset.
        /// </summary>
        /// <param name="fileOffset">The file offset to start reading at.</param>
        /// <param name="size">The number of bytes in the chunk.</param>
        /// <param name="reader">The reader that was created.</param>
        /// <returns><c>true</c> if the reader was created successfully, <c>false</c> otherwise.</returns>
        public bool TryCreateReaderAtFileOffset(uint fileOffset, uint size, out BinaryStreamReader reader)
        {
            if (TryGetSectionContainingOffset(fileOffset, out var section))
            {
                reader = section.CreateReader(fileOffset, size);
                return true;
            }

            if (EofData is IReadableSegment eofData
                && fileOffset >= eofData.Offset
                && fileOffset < eofData.Offset + eofData.GetPhysicalSize())
            {
                reader = eofData.CreateReader(fileOffset, size);
                return true;
            }

            reader = default;
            return false;
        }

        /// <summary>
        /// Creates a new reader at the provided virtual address.
        /// </summary>
        /// <param name="rva">The virtual address to start reading at.</param>
        /// <returns>The reader.</returns>
        public BinaryStreamReader CreateReaderAtRva(uint rva)
        {
            var section = GetSectionContainingRva(rva);
            return section.CreateReader(section.RvaToFileOffset(rva));
        }

        /// <summary>
        /// Attempts to create a new reader at the provided virtual address.
        /// </summary>
        /// <param name="rva">The virtual address to start reading at.</param>
        /// <param name="reader">The reader that was created.</param>
        /// <returns><c>true</c> if the reader was created successfully, <c>false</c> otherwise.</returns>
        public bool TryCreateReaderAtRva(uint rva, out BinaryStreamReader reader)
        {
            if (TryGetSectionContainingRva(rva, out var section))
            {
                reader = section.CreateReader(section.RvaToFileOffset(rva));
                return true;
            }

            reader = default;
            return false;
        }

        /// <summary>
        /// Creates a new reader of a chunk of data at the provided virtual address.
        /// </summary>
        /// <param name="rva">The virtual address to start reading at.</param>
        /// <param name="size">The number of bytes in the chunk.</param>
        /// <returns>The reader.</returns>
        public BinaryStreamReader CreateReaderAtRva(uint rva, uint size)
        {
            var section = GetSectionContainingRva(rva);
            return section.CreateReader(section.RvaToFileOffset(rva), size);
        }

        /// <summary>
        /// Attempts to create a new reader of a chunk of data at the provided virtual address.
        /// </summary>
        /// <param name="rva">The virtual address to start reading at.</param>
        /// <param name="size">The number of bytes in the chunk.</param>
        /// <param name="reader">The reader that was created.</param>
        /// <returns><c>true</c> if the reader was created successfully, <c>false</c> otherwise.</returns>
        public bool TryCreateReaderAtRva(uint rva, uint size, out BinaryStreamReader reader)
        {
            if (TryGetSectionContainingRva(rva, out var section))
            {
                reader = section.CreateReader(section.RvaToFileOffset(rva), size);
                return true;
            }

            reader = default;
            return false;
        }

        /// <summary>
        /// Recomputes file offsets and sizes in the file, optional and section headers.
        /// </summary>
        /// <remarks>
        /// Affected fields in the file header include:
        /// <list type="bullet">
        ///     <item>
        ///         <term>SizeOfOptionalHeader</term>
        ///     </item>
        /// </list>
        /// Affected fields in the optional header include:
        /// <list type="bullet">
        ///     <item>
        ///         <term>SizeOfHeaders</term>
        ///         <term>SizeOfImage</term>
        ///         <term>Data directories</term>
        ///     </item>
        /// </list>
        /// Affected fields in the section header include:
        /// <list type="bullet">
        ///     <item>
        ///         <term>VirtualAddress</term>
        ///         <term>VirtualSize</term>
        ///         <term>PointerToRawData</term>
        ///         <term>SizeOfRawData</term>
        ///     </item>
        /// </list>
        /// </remarks>
        public void UpdateHeaders()
        {
            var relocation = new RelocationParameters(OptionalHeader.ImageBase, 0, 0,
                OptionalHeader.Magic == OptionalHeaderMagic.PE32);

            // Update offsets of PE headers.
            FileHeader.UpdateOffsets(
                relocation.WithAdvance(DosHeader.NextHeaderOffset + sizeof(uint))
            );
            OptionalHeader.UpdateOffsets(
                relocation.WithAdvance((uint) FileHeader.Offset + FileHeader.GetPhysicalSize())
            );

            // Sync file header fields with actual observed values.
            FileHeader.NumberOfSections = (ushort) Sections.Count;
            FileHeader.SizeOfOptionalHeader = (ushort) OptionalHeader.GetPhysicalSize();

            // Compute headers size, and update offsets of extra data.
            uint peHeadersSize = (uint) OptionalHeader.Offset
                + FileHeader.SizeOfOptionalHeader
                + SectionHeader.SectionHeaderSize * (uint) Sections.Count;
            ExtraSectionData?.UpdateOffsets(relocation.WithAdvance(peHeadersSize));

            uint totalHeadersSize = peHeadersSize + (ExtraSectionData?.GetPhysicalSize() ?? 0);
            OptionalHeader.SizeOfHeaders = totalHeadersSize.Align(OptionalHeader.FileAlignment);

            // Re-align sections and directories.
            var oldSections = Sections.Select(x => x.CreateHeader()).ToList();
            AlignSections();
            AlignDataDirectoryEntries(oldSections);;

            // Determine full size of image.
            var lastSection = Sections[Sections.Count - 1];
            OptionalHeader.SizeOfImage = lastSection.Rva
                + lastSection.GetVirtualSize().Align(OptionalHeader.SectionAlignment);

            // Update EOF data offsets.
            EofData?.UpdateOffsets(relocation.WithOffsetRva(
                lastSection.Offset + lastSection.GetPhysicalSize(),
                OptionalHeader.SizeOfImage));
        }

        /// <summary>
        /// Aligns all sections according to the file and section alignment properties in the optional header.
        /// </summary>
        public void AlignSections()
        {
            var relocation = new RelocationParameters(
                OptionalHeader.ImageBase,
                OptionalHeader.SizeOfHeaders.Align(OptionalHeader.FileAlignment),
                OptionalHeader.SizeOfHeaders.Align(OptionalHeader.SectionAlignment),
                OptionalHeader.Magic == OptionalHeaderMagic.PE32);

            for (int i = 0; i < Sections.Count; i++)
            {
                var section = Sections[i];

                section.UpdateOffsets(relocation);
                relocation.Advance(
                    section.GetPhysicalSize().Align(OptionalHeader.FileAlignment),
                    section.GetVirtualSize().Align(OptionalHeader.SectionAlignment));
            }
        }

        /// <summary>
        /// Aligns all data directories' virtual address according to the section header's ones.
        /// </summary>
        public void AlignDataDirectoryEntries(IList<SectionHeader> oldHeaders)
        {
            var dataDirectoryEntries = OptionalHeader.DataDirectories;
            for (int j = 0; j < dataDirectoryEntries.Count; j++)
            {
                var dataDirectory = dataDirectoryEntries[j];
                if (dataDirectory.IsPresentInPE)
                {
                    uint virtualAddress = dataDirectory.VirtualAddress;
                    for(int i = 0; i < oldHeaders.Count; i++)
                    {
                        var header = oldHeaders[i];

                        // Locate section containing image directory.
                        if (header.VirtualAddress <= virtualAddress && header.VirtualAddress + header.SizeOfRawData > virtualAddress)
                        {
                            // Calculate the delta between the new section.rva and the old one.
                            if (Sections[i].Rva >= header.VirtualAddress)
                            {
                                uint sectionRvaDelta = Sections[i].Rva - header.VirtualAddress;
                                virtualAddress += sectionRvaDelta;
                            }
                            else
                            {
                                uint sectionRvaDelta = header.VirtualAddress - Sections[i].Rva;
                                virtualAddress -= sectionRvaDelta;
                            }

                            dataDirectoryEntries[j] = new DataDirectory(virtualAddress, dataDirectory.Size);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Writes the PE file to a file on the disk.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        public void Write(string filePath)
        {
            using var stream = System.IO.File.Create(filePath);
            Write(stream);
        }

        /// <summary>
        /// Writes the PE file to the provided output stream.
        /// </summary>
        /// <param name="stream">The output stream to write to.</param>
        public void Write(Stream stream) => Write(new BinaryStreamWriter(stream));

        /// <summary>
        /// Writes the PE file to the provided output stream.
        /// </summary>
        /// <param name="writer">The output stream to write to.</param>
        public void Write(BinaryStreamWriter writer)
        {
            UpdateHeaders();

            // Dos header.
            DosHeader.Write(writer);

            // NT headers
            writer.Offset = DosHeader.NextHeaderOffset;
            writer.WriteUInt32(ValidPESignature);
            FileHeader.Write(writer);
            OptionalHeader.Write(writer);

            // Section headers.
            writer.Offset = OptionalHeader.Offset + FileHeader.SizeOfOptionalHeader;
            for (int i = 0; i < Sections.Count; i++)
                Sections[i].CreateHeader().Write(writer);

            // Data between section headers and sections.
            ExtraSectionData?.Write(writer);

            // Sections.
            writer.Offset = OptionalHeader.SizeOfHeaders;
            for (int i = 0; i < Sections.Count; i++)
            {
                var section = Sections[i];
                writer.Offset = section.Offset;
                section.Contents?.Write(writer);
                writer.Align(OptionalHeader.FileAlignment);
            }

            // EOF Data.
            EofData?.Write(writer);
        }

        /// <summary>
        /// Obtains the sections in the portable executable file.
        /// </summary>
        /// <returns>The section.</returns>
        /// <remarks>
        /// This method is called upon the initialization of the <see cref="Sections"/> property.
        /// </remarks>
        protected virtual IList<PESection> GetSections() => new PESectionCollection(this);

        /// <summary>
        /// Obtains the padding data in between the last section header and the first section.
        /// </summary>
        /// <returns>The extra padding data.</returns>
        /// <remarks>
        /// This method is called upon the initialization of the <see cref="ExtraSectionData"/> property.
        /// </remarks>
        protected virtual ISegment? GetExtraSectionData() => null;

        /// <summary>
        /// Obtains any data appended to the end of the file (EoF).
        /// </summary>
        /// <returns>The extra data.</returns>
        /// <remarks>
        /// This method is called upon the initialization of the <see cref="EofData"/> property.
        /// </remarks>
        protected virtual ISegment? GetEofData() => null;
    }
}
