using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.Syntax;

namespace Loyc.Binary
{
    /// <summary>
    /// An LNode comparer that uses equivalence classes to decide equality.
    /// </summary>
    public sealed class ClassifyingLNodeComparer : EqualityComparer<LNode>
    {
        /// <summary>
        /// Creates a classifying node comparer.
        /// </summary>
        public ClassifyingLNodeComparer()
        {
            this.equivClasses = new Dictionary<LNode, DisjointSetNode>(
                ObjectReferenceEqualityComparer<LNode>.Default);
            this.hashCodes = new Dictionary<LNode, int>(
                ObjectReferenceEqualityComparer<LNode>.Default);
        }

        private Dictionary<LNode, DisjointSetNode> equivClasses;
        private Dictionary<LNode, int> hashCodes;

        /// <summary>
        /// Gets the given node's equivalence class.
        /// </summary>
        /// <param name="node">The node to find a class for.</param>
        /// <returns>The equivalence class.</returns>
        private DisjointSetNode GetEquivalenceClass(LNode node)
        {
            DisjointSetNode equivClass;
            if (equivClasses.TryGetValue(node, out equivClass))
            {
                equivClass = equivClass.GetRoot();
                equivClasses[node] = equivClass;
                return equivClass;
            }
            else
            {
                equivClass = new DisjointSetNode();
                equivClasses[node] = equivClass;
                return equivClass;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(LNode x, LNode y)
        {
            // The idea behind this equality implementation is to take advantage of
            // previously computed information. For example, if the hash codes don't
            // match, then we know two nodes can't be equal. This covers most inequalities.
            // Furthermore, equal nodes are kept in equivalence classes, so equality
            // between any two nodes is established at most once.

            var xEquivClass = GetEquivalenceClass(x);
            var yEquivClass = GetEquivalenceClass(y);
            if (xEquivClass == yEquivClass)
            {
                return true;
            }

            var xHash = GetHashCode(x);
            var yHash = GetHashCode(y);
            if (xHash != yHash)
            {
                return false;
            }

            if (x.IsId)
            {
                if (!y.IsId || x.Name != y.Name)
                {
                    return false;
                }
            }
            else if (x.IsLiteral)
            {
                if (!y.IsLiteral || !object.Equals(x.Value, y.Value))
                {
                    return false;
                }
            }
            else
            {
                if (!y.IsCall
                    || !Equals(x.Target, y.Target)
                    || !x.Args.SequenceEqual(y.Args, this))
                {
                    return false;
                }
            }

            if (x.AttrCount != y.AttrCount
                || !x.Attrs.SequenceEqual(y.Attrs, this))
            {
                return false;
            }

            xEquivClass.Union(yEquivClass);
            return true;
        }

        private static int MergeHashCodes(int x, int y)
        {
            return ((x << 1) + x) ^ y;
        }

        /// <inheritdoc/>
        public override int GetHashCode(LNode obj)
        {
            int code;
            if (!hashCodes.TryGetValue(obj, out code))
            {
                if (obj.IsId)
                {
                    code = obj.Name.GetHashCode();
                }
                else if (obj.IsLiteral)
                {
                    code = obj.Value == null ? 0 : obj.Value.GetHashCode();
                }
                else
                {
                    code = GetHashCode(obj.Target);
                    foreach (var arg in obj.Args)
                    {
                        code = MergeHashCodes(code, GetHashCode(arg));
                    }
                }

                if (obj.HasAttrs)
                {
                    foreach (var attr in obj.Attrs)
                    {
                        code = MergeHashCodes(code, GetHashCode(attr));
                    }
                }
                hashCodes[obj] = code;
            }
            return code;
        }
    }

    /// <summary>
    /// A node in a disjoint set forest.
    /// </summary>
    internal sealed class DisjointSetNode
    {
        public DisjointSetNode()
        {
            this.Parent = this;
            this.Rank = Rank;
        }

        public DisjointSetNode Parent { get; set; }
        public int Rank { get; set; }

        public DisjointSetNode GetRoot()
        {
            if (Parent == this)
            {
                return this;
            }
            else
            {
                return Parent = Parent.GetRoot();
            }
        }

        public void Union(DisjointSetNode other)
        {
            var thisRoot = this.GetRoot();
            var otherRoot = other.GetRoot();
            if (thisRoot == otherRoot)
            {
                return;
            }

            if (thisRoot.Rank < otherRoot.Rank)
            {
                thisRoot.Parent = otherRoot;
            }
            else if (thisRoot.Rank > otherRoot.Rank)
            {
                otherRoot.Parent = thisRoot;
            }
            else
            {
                otherRoot.Parent = thisRoot;
                thisRoot.Rank = thisRoot.Rank + 1;
            }
        }
    }
}

