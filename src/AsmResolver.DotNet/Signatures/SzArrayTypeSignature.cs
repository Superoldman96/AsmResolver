using System.Collections.Generic;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace AsmResolver.DotNet.Signatures
{
    /// <summary>
    /// Represents a type signature describing a single dimension array with 0 as a lower bound.
    /// </summary>
    public class SzArrayTypeSignature : ArrayBaseTypeSignature
    {
        private static readonly ArrayDimension[] SzDimensions = { new() };

        /// <summary>
        /// Creates a new single-dimension array signature with 0 as a lower bound.
        /// </summary>
        /// <param name="baseType">The type of the elements to store in the array.</param>
        public SzArrayTypeSignature(TypeSignature baseType)
            : base(baseType)
        {
        }

        /// <inheritdoc />
        public override ElementType ElementType => ElementType.SzArray;

        /// <inheritdoc />
        public override string Name => $"{BaseType.Name ?? NullTypeToString}[]";

        /// <inheritdoc />
        public override int Rank => 1;

        /// <inheritdoc />
        public override IEnumerable<ArrayDimension> GetDimensions() => SzDimensions;

        /// <inheritdoc />
        public override TypeSignature? GetDirectBaseClass() => ContextModule?.CorLibTypeFactory.CorLibScope
            .CreateTypeReference("System", "Array")
            .ToTypeSignature(false);


        /// <inheritdoc />
        public override TResult AcceptVisitor<TResult>(ITypeSignatureVisitor<TResult> visitor) =>
            visitor.VisitSzArrayType(this);

        /// <inheritdoc />
        public override TResult AcceptVisitor<TState, TResult>(ITypeSignatureVisitor<TState, TResult> visitor,
            TState state) =>
            visitor.VisitSzArrayType(this, state);
    }
}
