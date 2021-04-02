/*
// <copyright>
// dotNetRDF is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------
// 
// Copyright (c) 2009-2021 dotNetRDF Project (http://dotnetrdf.org/)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
*/

using System;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF
{
    /// <summary>
    /// Interface for Nodes.
    /// </summary>
    public interface INode 
        : IComparable<INode>, IComparable<IBlankNode>, IComparable<IGraphLiteralNode>, IComparable<ILiteralNode>,
          IComparable<IUriNode>, IComparable<IVariableNode>, IComparable<IRefNode>,
          IEquatable<INode>, IEquatable<IBlankNode>, IEquatable<IGraphLiteralNode>, IEquatable<ILiteralNode>,
          IEquatable<IUriNode>, IEquatable<IVariableNode>, IEquatable<IRefNode>
    {
        /// <summary>
        /// Nodes have a Type.
        /// </summary>
        /// <remarks>Primarily provided so can do quick integer comparison to see what type of Node you have without having to do actual full blown Type comparison.</remarks>
        NodeType NodeType
        {
            get;
        }

        /// <summary>
        /// Gets the String representation of the Node.
        /// </summary>
        /// <returns></returns>
        string ToString();

        /// <summary>
        /// Gets the String representation of the Node formatted with the given Node formatter.
        /// </summary>
        /// <param name="formatter">Formatter.</param>
        /// <returns></returns>
        string ToString(INodeFormatter formatter);

        /// <summary>
        /// Gets the String representation of the Node formatted with the given Node formatter.
        /// </summary>
        /// <param name="formatter">Formatter.</param>
        /// <param name="segment">Triple Segment.</param>
        /// <returns></returns>
        string ToString(INodeFormatter formatter, TripleSegment segment);
    }
}
