using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Binary
{
    /// <summary>
    /// An enumeration of ways to encode a node.
    /// </summary>
    public enum NodeEncodingType : byte
    {
        /// <summary>
        /// A table of templated nodes. The table is prefixed with a reference to the
        /// template and the table itself consists of references to other nodes.
        /// </summary>
        TemplatedNode = 0,

        /// <summary>
        /// An id node, which is encoded as an index in the symbol table.
        /// </summary>
        IdNode = 1,

        /// <summary>
        /// A string literal, which is encoded as an index in the symbol table.
        /// </summary>
        String = 2,

        /// <summary>
        /// An 8-bit signed integer literal.
        /// </summary>
        Int8 = 3,
        /// <summary>
        /// A 16-bit signed integer literal.
        /// </summary>
        Int16 = 4,
        /// <summary>
        /// A 32-bit signed integer literal.
        /// </summary>
        Int32 = 5,
        /// <summary>
        /// A 64-bit signed integer literal.
        /// </summary>
        Int64 = 6,

        /// <summary>
        /// An 8-bit unsigned integer literal.
        /// </summary>
        UInt8 = 7,
        /// <summary>
        /// A 16-bit unsigned integer literal.
        /// </summary>
        UInt16 = 8,
        /// <summary>
        /// A 32-bit unsigned integer literal.
        /// </summary>
        UInt32 = 9,
        /// <summary>
        /// A 64-bit unsigned integer literal.
        /// </summary>
        UInt64 = 10,

        /// <summary>
        /// A 32-bit single-precision IEEE floating-point literal.
        /// </summary>
        Float32 = 11,

        /// <summary>
        /// A 64-bit double-precision IEEE floating-point literal.
        /// </summary>
        Float64 = 12,

        /// <summary>
        /// A character literal.
        /// </summary>
        Char = 13,

        /// <summary>
        /// A boolean literal.
        /// </summary>
        Boolean = 14,

        /// <summary>
        /// The void singleton value.
        /// </summary>
        Void = 15,

        /// <summary>
        /// The null singleton value.
        /// </summary>
        Null = 16,

        /// <summary>
        /// A decimal literal
        /// </summary>
        Decimal = 17,

        /// <summary>
        /// A BigInteger literal.
        /// </summary>
        BigInteger = 18,

        /// <summary>
        /// A table of templated nodes where each node specifies its own
        /// template, followed by a list of references to nodes.
        /// </summary>
        VariablyTemplatedNode = 19,
    }

    /// <summary>
    /// Describes the encoding of (a table of) nodes.
    /// </summary>
    public struct NodeEncoding : IEquatable<NodeEncoding>
    {
        /// <summary>
        /// Creates a node encoding from the encoding type.
        /// </summary>
        /// <param name="kind">The kind of encoding.</param>
        public NodeEncoding(NodeEncodingType kind)
        {
            this.Kind = kind;
            this.TemplateIndex = 0;
        }

        /// <summary>
        /// Creates a node encoding from the given template index.
        /// </summary>
        /// <param name="templateIndex">An index into the template table.</param>
        public NodeEncoding(int templateIndex)
        {
            this.Kind = NodeEncodingType.TemplatedNode;
            this.TemplateIndex = templateIndex;
        }

        /// <summary>
        /// Creates a node encoding from the encoding type and template index.
        /// </summary>
        /// <param name="kind">The kind of encoding.</param>
        /// <param name="templateIndex">An index into the template table.</param>
        public NodeEncoding(NodeEncodingType kind, int templateIndex)
        {
            this.Kind = kind;
            this.TemplateIndex = templateIndex;
        }

        /// <summary>
        /// Gets the kind of encoding.
        /// </summary>
        /// <returns>The kind of encoding.</returns>
        public NodeEncodingType Kind { get; private set; }

        /// <summary>
        /// Tells if this encoding relies on a template.
        /// </summary>
        public bool HasTemplate => IsTemplateEncoding(Kind);

        /// <summary>
        /// Gets the index of this encoding in the template table.
        /// </summary>
        /// <returns>The index in the template table.</returns>
        public int TemplateIndex { get; private set; }

        /// <summary>
        /// Tests if this node encoding is equal to the given node encoding.
        /// </summary>
        /// <param name="other">The encoding to test for equality.</param>
        /// <returns><c>true</c> if the encodings are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(NodeEncoding other)
        {
            return Kind == other.Kind
                && TemplateIndex == other.TemplateIndex;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is NodeEncoding && Equals((NodeEncoding)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (int)Kind ^ TemplateIndex;
        }

        /// <summary>
        /// Tells if the given encoding type uses a template.
        /// </summary>
        /// <param name="kind">The encoding type.</param>
        /// <returns><c>true</c> if the encoding type uses a template; otherwise, <c>false</c>.</returns>
        public static bool IsTemplateEncoding(NodeEncodingType kind)
        {
            return kind == NodeEncodingType.TemplatedNode;
        }
    }
}
