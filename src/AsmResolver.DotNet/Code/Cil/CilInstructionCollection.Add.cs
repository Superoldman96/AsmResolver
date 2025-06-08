using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet.Collections;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace AsmResolver.DotNet.Code.Cil
{
    public partial class CilInstructionCollection
    {
        /// <summary>
        /// Verifies and adds an instruction to the end of the collection.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">Occurs when the provided operation requires an operand.</exception>
        public CilInstruction Add(CilOpCode code) => Insert(Count, code);

        /// <summary>
        /// Verifies and adds a branch instruction to the end of the collection.
        /// </summary>
        /// <param name="code">The branch opcode.</param>
        /// <param name="label">The label referenced by the branch instruction.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not a branch opcode.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="label"/> is null.
        /// </exception>
        public CilInstruction Add(CilOpCode code, ICilLabel label) => Insert(Count, code, label);

        /// <summary>
        /// Verifies and adds a switch instruction to the end of the collection.
        /// </summary>
        /// <param name="code">The switch opcode.</param>
        /// <param name="labels">The labels referenced by the switch instruction.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not a branch opcode.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="labels"/> is null.
        /// </exception>
        public CilInstruction Add(CilOpCode code, params ICilLabel[] labels) =>
            Insert(Count, code, (IEnumerable<ICilLabel>) labels);

        /// <summary>
        /// Verifies and adds a switch instruction to the end of the collection.
        /// </summary>
        /// <param name="code">The switch opcode.</param>
        /// <param name="labels">The labels referenced by the switch instruction.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not a branch opcode.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="labels"/> is null.
        /// </exception>
        public CilInstruction Add(CilOpCode code, IEnumerable<ICilLabel> labels) => Insert(Count, code, labels);

        /// <summary>
        /// Verifies and adds a instruction to the end of the collection that pushes an integer constant.
        /// </summary>
        /// <param name="code">The opcode.</param>
        /// <param name="constant">The constant to push.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not an opcode referencing an integer constant.
        /// </exception>
        public CilInstruction Add(CilOpCode code, int constant) => Insert(Count, code, constant);

        /// <summary>
        /// Verifies and adds a instruction to the end of the collection that pushes a 64-bit integer constant.
        /// </summary>
        /// <param name="code">The opcode.</param>
        /// <param name="constant">The constant to push.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not an opcode referencing a 64-bit integer constant.
        /// </exception>
        public CilInstruction Add(CilOpCode code, long constant) => Insert(Count, code, constant);

        /// <summary>
        /// Verifies and adds a instruction to the end of the collection that references a float32 constant.
        /// </summary>
        /// <param name="code">The opcode.</param>
        /// <param name="constant">The constant to push.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not an opcode referencing a float32 constant.
        /// </exception>
        public CilInstruction Add(CilOpCode code, float constant) => Insert(Count, code, constant);

        /// <summary>
        /// Verifies and adds a instruction to the end of the collection that references a float64 constant.
        /// </summary>
        /// <param name="code">The opcode.</param>
        /// <param name="constant">The constant to push.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not an opcode referencing a float64 constant.
        /// </exception>
        public CilInstruction Add(CilOpCode code, double constant) => Insert(Count, code, constant);

        /// <summary>
        /// Verifies and adds a instruction to the end of the collection that pushes a string constant.
        /// </summary>
        /// <param name="code">The opcode.</param>
        /// <param name="constant">The constant to push.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not an opcode referencing a string constant.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="constant"/> is null.
        /// </exception>
        public CilInstruction Add(CilOpCode code, string constant) => Insert(Count, code, constant);

        /// <summary>
        /// Verifies and adds a instruction to the end of the collection that references a local variable.
        /// </summary>
        /// <param name="code">The opcode.</param>
        /// <param name="variable">The referenced variable.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not an opcode referencing a variable.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="variable"/> is null.
        /// </exception>
        public CilInstruction Add(CilOpCode code, CilLocalVariable variable) => Insert(Count, code, variable);

        /// <summary>
        /// Verifies and adds a instruction to the end of the collection that references a parameter.
        /// </summary>
        /// <param name="code">The opcode.</param>
        /// <param name="parameter">The referenced parameter.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not an opcode referencing a parameter.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="parameter"/> is null.
        /// </exception>
        public CilInstruction Add(CilOpCode code, Parameter parameter) => Insert(Count, code, parameter);

        /// <summary>
        /// Verifies and adds an instruction to the end of the collection that references a field.
        /// </summary>
        /// <param name="code">The field opcode.</param>
        /// <param name="field">The field referenced by the instruction.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not a field opcode.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="field"/> is null.
        /// </exception>
        public CilInstruction Add(CilOpCode code, IFieldDescriptor field) => Insert(Count, code, field);

        /// <summary>
        /// Verifies and adds an instruction to the end of the collection that references a method.
        /// </summary>
        /// <param name="code">The method opcode.</param>
        /// <param name="method">The method referenced by the instruction.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not a method opcode.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="method"/> is null.
        /// </exception>
        public CilInstruction Add(CilOpCode code, IMethodDescriptor method) => Insert(Count, code, method);

        /// <summary>
        /// Verifies and adds an instruction to the end of the collection that references a member.
        /// </summary>
        /// <param name="code">The member opcode.</param>
        /// <param name="member">The member referenced by the instruction.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation does not match with the provided member reference.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="member"/> is null.
        /// </exception>
        public CilInstruction Add(CilOpCode code, MemberReference member) => Insert(Count, code, member);

        /// <summary>
        /// Verifies and adds an instruction to the end of the collection that references a type.
        /// </summary>
        /// <param name="code">The type opcode.</param>
        /// <param name="type">The type referenced by the instruction.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not a type opcode.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="type"/> is null.
        /// </exception>
        public CilInstruction Add(CilOpCode code, ITypeDefOrRef type) => Insert(Count, code, type);

        /// <summary>
        /// Verifies and adds an instruction to the end of the collection that references a metadata member.
        /// </summary>
        /// <param name="code">The method opcode.</param>
        /// <param name="member">The member referenced by the instruction.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not a member opcode.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="member"/> is null.
        /// </exception>
        public CilInstruction Add(CilOpCode code, IMetadataMember member) => Insert(Count, code, member);

        /// <summary>
        /// Verifies and adds a instruction to the end of the collection that references a standalone signature
        /// referencing a method signature in the blob stream.
        /// </summary>
        /// <param name="code">The opcode.</param>
        /// <param name="signature">The referenced signature.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not an opcode referencing a standalone signature.
        /// </exception>
        public CilInstruction Add(CilOpCode code, StandAloneSignature signature) => Insert(Count, code, signature);

        /// <summary>
        /// Verifies and adds a instruction to the end of the collection that references a metadata member by its token.
        /// </summary>
        /// <param name="code">The opcode.</param>
        /// <param name="token">The token of the referenced member.</param>
        /// <returns>The created instruction.</returns>
        /// <exception cref="InvalidCilInstructionException">
        /// Occurs when the provided operation is not an opcode referencing a metadata member.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Occurs when the provided token is not valid in a CIL stream.
        /// </exception>
        public CilInstruction Add(CilOpCode code, MetadataToken token) => Insert(Count, code, token);
    }
}
