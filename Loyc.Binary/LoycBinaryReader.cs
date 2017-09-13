using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Loyc.Binary
{
    /// <summary>
    /// A type that reads binary encoded loyc trees.
    /// </summary>
    public class LoycBinaryReader : IDisposable
    {
        #region Constructors

        /// <summary>
        /// Creates a new loyc binary reader from the given 
        /// binary reader and set of decoders and template parsers.
        /// </summary>
        /// <param name="reader">The binary reader that reads raw data.</param>
        /// <param name="literalEncoding">A dictionary of possible literal encodings.</param>
        /// <param name="templateParsers">A dictionary of possible template encodings.</param>
        public LoycBinaryReader(BinaryReader reader,
            IReadOnlyDictionary<NodeEncodingType, Func<LoycBinaryReader, ReaderState, LNode>> literalEncoding,
            IReadOnlyDictionary<NodeTemplateType, Func<LoycBinaryReader, NodeTemplate>> templateParsers)
        {
            Reader = reader;
            LiteralEncodings = literalEncoding;
            TemplateParsers = templateParsers;
        }

        /// <summary>
        /// Creates a new loyc binary reader from the given 
        /// input stream and set of decoders and template parsers.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="literalEncoding">A dictionary of possible literal encodings.</param>
        /// <param name="templateParsers">A dictionary of possible template encodings.</param>
        public LoycBinaryReader(Stream inputStream,
            IReadOnlyDictionary<NodeEncodingType, Func<LoycBinaryReader, ReaderState, LNode>> literalEncoding,
            IReadOnlyDictionary<NodeTemplateType, Func<LoycBinaryReader, NodeTemplate>> templateParsers)
            : this(new BinaryReader(inputStream), literalEncoding, templateParsers)
        {
        }

        /// <summary>
        /// Creates a new loyc binary reader from the given binary reader.
        /// The default set of decoders and template parsers are used.
        /// </summary>
        /// <param name="reader"></param>
        public LoycBinaryReader(BinaryReader reader)
            : this(reader, DefaultEncodings, DefaultTemplateParsers)
        {
        }

        /// <summary>
        /// Creates a new loyc binary reader from the given input stream.
        /// The default set of decoders and template parsers are used.
        /// </summary>
        /// <param name="inputStream"></param>
        public LoycBinaryReader(Stream inputStream)
            : this(new BinaryReader(inputStream))
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the inner binary reader.
        /// </summary>
        public BinaryReader Reader { get; private set; }

        /// <summary>
        /// Gets the mapping of literal encodings to decoders that this binary reader uses.
        /// Templated nodes and id nodes are treated as special cases, and are not part of this dictionary.
        /// </summary>
        public IReadOnlyDictionary<NodeEncodingType, Func<LoycBinaryReader, ReaderState, LNode>> LiteralEncodings { get; private set; }

        /// <summary>
        /// Gets the mapping of node template types to node template parsers that this binary reader uses.
        /// </summary>
        public IReadOnlyDictionary<NodeTemplateType, Func<LoycBinaryReader, NodeTemplate>> TemplateParsers { get; private set; }

        #endregion

        #region Static

        /// <summary>
        /// Gets the default decoder dictionary.
        /// </summary>
        public static IReadOnlyDictionary<NodeEncodingType, Func<LoycBinaryReader, ReaderState, LNode>> DefaultEncodings
        {
            get
            {
                return new Dictionary<NodeEncodingType, Func<LoycBinaryReader, ReaderState, LNode>>()
                {
                    { NodeEncodingType.String, (reader, state) => state.NodeFactory.Literal(reader.ReadStringReference(state)) },
                    { NodeEncodingType.BigInteger, (reader, state) => state.NodeFactory.Literal(reader.ReadBigInteger()) },
                    { NodeEncodingType.Int8, CreateLiteralNodeReader(reader => reader.ReadSByte()) },
                    { NodeEncodingType.Int16, CreateLiteralNodeReader(reader => reader.ReadInt16()) },
                    { NodeEncodingType.Int32, CreateLiteralNodeReader(reader => reader.ReadInt32()) },
                    { NodeEncodingType.Int64, CreateLiteralNodeReader(reader => reader.ReadInt64()) },
                    { NodeEncodingType.UInt8, CreateLiteralNodeReader(reader => reader.ReadByte()) },
                    { NodeEncodingType.UInt16, CreateLiteralNodeReader(reader => reader.ReadUInt16()) },
                    { NodeEncodingType.UInt32, CreateLiteralNodeReader(reader => reader.ReadUInt32()) },
                    { NodeEncodingType.UInt64, CreateLiteralNodeReader(reader => reader.ReadUInt64()) },
                    { NodeEncodingType.Float32, CreateLiteralNodeReader(reader => reader.ReadSingle()) },
                    { NodeEncodingType.Float64, CreateLiteralNodeReader(reader => reader.ReadDouble()) },
                    { NodeEncodingType.Decimal, CreateLiteralNodeReader(reader => reader.ReadDecimal()) },
                    { NodeEncodingType.Boolean, CreateLiteralNodeReader(reader => reader.ReadBoolean()) },
                    { NodeEncodingType.Char, CreateLiteralNodeReader(reader => reader.ReadChar()) },
                    { NodeEncodingType.Void, CreateLiteralNodeReader(reader => @void.Value) },
                    { NodeEncodingType.Null, CreateLiteralNodeReader<object>(reader => null) }
                };
            }
        }

        /// <summary>
        /// Creates a decoder that creates a literal node based on a literal reader.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ReadLiteral"></param>
        /// <returns></returns>
        public static Func<LoycBinaryReader, ReaderState, LNode> CreateLiteralNodeReader<T>(Func<BinaryReader, T> ReadLiteral)
        {
            return (reader, state) => state.NodeFactory.Literal(ReadLiteral(reader.Reader));
        }

        /// <summary>
        /// Gets the default template parser dictionary.
        /// </summary>
        public static IReadOnlyDictionary<NodeTemplateType, Func<LoycBinaryReader, NodeTemplate>> DefaultTemplateParsers
        {
            get
            {
                return new Dictionary<NodeTemplateType, Func<LoycBinaryReader, NodeTemplate>>()
                {
                    { NodeTemplateType.CallNode, CallNodeTemplate.Read },
                    { NodeTemplateType.CallIdNode, CallIdNodeTemplate.Read },
                    { NodeTemplateType.AttributeNode, AttributeNodeTemplate.Read }
                };
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Reads a LEB128 variable-length unsigned integer from the input stream.
        /// </summary>
        public uint ReadULeb128()
        {
            // C# translation of code borrowed from Wikipedia article:
            // https://en.wikipedia.org/wiki/LEB128
            uint result = 0;
            int shift = 0;
            while (true) 
            {
                byte b = Reader.ReadByte();
                result |= (uint)((b & 0x7F) << shift);
                if ((b & 0x80) == 0)
                    break;
                shift += 7;
            }
            return result;
        }

        /// <summary>
        /// Reads an encoding type from the stream.
        /// </summary>
        /// <returns></returns>
        public NodeEncodingType ReadEncodingType()
        {
            return (NodeEncodingType)Reader.ReadByte();
        }

        /// <summary>
        /// Reads a length-prefixed list of items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ReadItem"></param>
        /// <returns></returns>
        public IReadOnlyList<T> ReadList<T>(Func<T> ReadItem)
        {
            int count = (int)ReadULeb128();
            return ReadListContents(ReadItem, count);
        }

        /// <summary>
        /// Reads an unprefixed list of items of the given length.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ReadItem"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        public IReadOnlyList<T> ReadListContents<T>(Func<T> ReadItem, int Length)
        {
            var results = new T[Length];
            for (int i = 0; i < Length; i++)
            {
                results[i] = ReadItem();
            }
            return results;
        }

        #endregion

        #region Header Parsing

        /// <summary>
        /// Reads a symbol as defined in the symbol table.
        /// </summary>
        /// <returns></returns>
        private Symbol ReadSymbol(SymbolPool Pool)
        {
            int length = (int)ReadULeb128();
            byte[] data = Reader.ReadBytes(length);
            return Pool.GetGlobalOrCreateHere(UTF8Encoding.UTF8.GetString(data));
        }

        /// <summary>
        /// Reads the symbol table.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<Symbol> ReadSymbolTable()
        {
            var pool = new SymbolPool();
            int length = (int)ReadULeb128();
            var table = new Symbol[length];
            for (int i = 0; i < length; i++)
            {
                table[i] = ReadSymbol(pool);
            }
            return table;
        }

        /// <summary>
        /// Reads the template definition table.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<NodeTemplate> ReadTemplateTable()
        {
            int length = (int)ReadULeb128();
            var table = new NodeTemplate[length];
            for (int i = 0; i < length; i++)
            {
                table[i] = ReadTemplateDefinition();
            }
            return table;
        }

        /// <summary>
        /// Reads a single template definition.
        /// </summary>
        /// <returns></returns>
        public NodeTemplate ReadTemplateDefinition()
        {
            NodeTemplateType type = (NodeTemplateType)Reader.ReadByte();
            if (!TemplateParsers.ContainsKey(type))
            {
                throw new InvalidDataException("Unknown node template type.");
            }
            return TemplateParsers[type](this);
        }

        /// <summary>
        /// Reads a binary encoded loyc file's header.
        /// </summary>
        /// <param name="Identifier">A string that identifies the binary tree's source. This is typically the file name.</param>
        /// <returns></returns>
        public ReaderState ReadHeader(string Identifier)
        {
            var symbolTable = ReadSymbolTable();
            var templateTable = ReadTemplateTable();
            return new ReaderState(new LNodeFactory(new EmptySourceFile(Identifier)), symbolTable, templateTable);
        }

        #endregion

        #region Body Parsing

        /// <summary>
        /// Reads a reference to a symbol.
        /// </summary>
        /// <param name="State"></param>
        /// <returns></returns>
        public Symbol ReadSymbolReference(ReaderState State)
        {
            int index = (int)ReadULeb128();

            if (index >= State.SymbolTable.Count)
            {
                throw new InvalidDataException("Symbol index out of bounds.");
            }

            return State.SymbolTable[index];
        }

        /// <summary>
        /// Reads a reference to a string in the symbol table.
        /// </summary>
        /// <param name="State"></param>
        /// <returns></returns>
        public string ReadStringReference(ReaderState State)
        {
            return ReadSymbolReference(State).Name;
        }

        /// <summary>
        /// Reads a BigInteger value.
        /// </summary>
        /// <returns>The big integer.</returns>
        public BigInteger ReadBigInteger()
        {
            // BigInteger values are encoded as a length prefix 
            // (ULEB128), followed by a byte array.
            int length = (int)ReadULeb128();
            return new BigInteger(Reader.ReadBytes(length));
        }

        /// <summary>
        /// Reads a reference to a node template.
        /// </summary>
        /// <param name="State"></param>
        /// <returns></returns>
        public NodeTemplate ReadTemplateReference(ReaderState State)
        {
            int index = (int)ReadULeb128();

            if (index >= State.TemplateTable.Count)
            {
                throw new InvalidDataException("Template index out of bounds.");
            }

            return State.TemplateTable[index];
        }

        /// <summary>
        /// Reads the node table as a flat list of nodes.
        /// </summary>
        /// <param name="State">The reader state.</param>
        /// <returns>The node table.</returns>
        public IReadOnlyList<LNode> ReadNodeTable(ReaderState State)
        {
            var nodes = new List<LNode>();

            // The node table is a length-prefixed list of (length, encoding)-prefixed
            // lists of nodes. It forms a flat index space.
            int tableCount = (int)ReadULeb128();
            for (int i = 0; i < tableCount; i++)
            {
                var encoding = ReadEncodingType();
                if (encoding == NodeEncodingType.TemplatedNode)
                {
                    var template = ReadTemplateReference(State);
                    int tableSize = (int)ReadULeb128();
                    ReadTemplatedNodes(State, nodes, tableSize, template);
                }
                else
                {
                    int tableSize = (int)ReadULeb128();
                    ReadNodes(State, encoding, nodes, tableSize);
                }
            }
            return nodes;
        }

        /// <summary>
        /// Reads a reference to a node in the node table.
        /// </summary>
        /// <param name="NodeTable">The node table.</param>
        /// <returns>The referenced node.</returns>
        private LNode ReadNodeReference(IReadOnlyList<LNode> NodeTable)
        {
            return NodeTable[(int)ReadULeb128()];
        }

        /// <summary>
        /// Reads a list of nodes with the given encoding.
        /// </summary>
        private void ReadNodes(
            ReaderState State,
            NodeEncodingType Encoding,
            List<LNode> NodeTable,
            int NumberOfNodesToRead)
        {
            if (Encoding == NodeEncodingType.VariablyTemplatedNode)
            {
                for (int i = 0; i < NumberOfNodesToRead; i++)
                {
                    var template = ReadTemplateReference(State);
                    ReadTemplatedNodes(State, NodeTable, 1, template);
                }
            }
            else if (Encoding == NodeEncodingType.IdNode)
            {
                for (int i = 0; i < NumberOfNodesToRead; i++)
                {
                    NodeTable.Add(State.NodeFactory.Id(ReadSymbolReference(State)));
                }
            }
            else
            {
                Func<LoycBinaryReader, ReaderState, LNode> parser;

                if (LiteralEncodings.TryGetValue(Encoding, out parser))
                {
                    for (int i = 0; i < NumberOfNodesToRead; i++)
                    {
                        NodeTable.Add(parser(this, State));
                    }
                }
                else
                {
                    throw new InvalidDataException("Unknown node encoding: '" + Encoding + "'.");
                }
            }
        }

        private void ReadTemplatedNodes(
            ReaderState State,
            List<LNode> NodeTable,
            int NumberOfNodesToRead,
            NodeTemplate Template)
        {
            int argCount = Template.ArgumentCount;
            for (int i = 0; i < NumberOfNodesToRead; i++)
            {
                var args = new LNode[argCount];
                for (int j = 0; j < args.Length; j++)
                {
                    args[j] = ReadNodeReference(NodeTable);
                }
                NodeTable.Add(Template.Instantiate(State, args));
            }
        }

        #endregion

        #region File Parsing

        /// <summary>
        /// Reads the file's magic string, and returns a boolean value
        /// that tells if it matched the loyc binary tree format's magic string.
        /// </summary>
        /// <returns></returns>
        public bool CheckMagic()
        {
            return Reader.ReadBytes(LoycBinaryHelpers.Magic.Length).Select(Convert.ToChar).SequenceEqual(LoycBinaryHelpers.Magic);
        }

        /// <summary>
        /// Reads the BLT file's version number, as a single integer.
        /// </summary>
        /// <returns>The BLT file's version number.</returns>
        public int ReadVersion()
        {
            return Reader.ReadInt32();
        }

        /// <summary>
        /// Reads a file encoded in the loyc binary tree format.
        /// This checks the magic number first, and then parses the 
        /// file's contents.
        /// </summary>
        /// <param name="Identifier">A string that identifies the binary tree's source. This is typically the file name.</param>
        /// <returns></returns>
        public IReadOnlyList<LNode> ReadFile(string Identifier)
        {
            if (!CheckMagic())
            {
                throw new InvalidDataException(
                    "The given stream's magic number did not read '" +
                    LoycBinaryHelpers.Magic +
                    "', which is the loyc binary tree format's magic string.");
            }

            var versionNumber = ReadVersion();

            if (versionNumber > LoycBinaryHelpers.VersionNumber)
            {
                int majorVersionNumber = versionNumber >> 16;
                int minorVersionNumber = versionNumber & 0x0000FFFF;
                throw new InvalidDataException(
                    "The given stream's version number '" +
                    majorVersionNumber + "." + minorVersionNumber +
                    "' is not supported. Max supported version number: '" + 
                    LoycBinaryHelpers.MajorVersionNumber + "." +
                    LoycBinaryHelpers.MinorVersionNumber + "'.");
            }

            return ReadFileContents(Identifier);
        }

        /// <summary>
        /// Reads the contents of a file encoded in the loyc binary tree format.
        /// </summary>
        /// <param name="Identifier">A string that identifies the binary tree's source. This is typically the file name.</param>
        /// <returns></returns>
        public IReadOnlyList<LNode> ReadFileContents(string Identifier)
        {
            var header = ReadHeader(Identifier);
            var nodes = ReadNodeTable(header);
            return ReadList(() => ReadNodeReference(nodes));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resource used by the <see cref="Loyc.Binary.LoycBinaryReader"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Loyc.Binary.LoycBinaryReader"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="Loyc.Binary.LoycBinaryReader"/> in an unusable state.
        /// After calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="Loyc.Binary.LoycBinaryReader"/> so the garbage collector can reclaim the memory that the
        /// <see cref="Loyc.Binary.LoycBinaryReader"/> was occupying.</remarks>
        public void Dispose()
        {
            Reader.Dispose();
        }

        #endregion
    }
}
