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
using VDS.Common.Tries;

namespace VDS.RDF
{
    /// <summary>
    /// A static helper class for interning URIs to reduce memory usage.
    /// </summary>
    public static class UriFactory
    {
        private static readonly ITrie<string, char, Uri> _uris = new SparseStringTrie<Uri>();

        /// <summary>
        /// Get / set the flag that controls the caching of Uri instances constructed by this factory.
        /// </summary>
        /// <remarks>When <see cref="InternUris"/> is set to true, the factory will cache each constructed URI against the original string value used for construction and return a cached Uri where available in preference to calling the Uri constructor.</remarks>
        public static bool InternUris { get; set; }

        /// <summary>
        /// Creates a URI interning it if interning is enabled via the <see cref="InternUris"/> property.
        /// </summary>
        /// <param name="uri">String URI.</param>
        /// <returns></returns>
        /// <remarks>
        /// When URI interning is disabled this is equivalent to just invoking the constructor of the <see cref="Uri">Uri</see> class.
        /// </remarks>
        public static Uri Create(string uri)
        {
            if (InternUris)
            {
                ITrieNode<char, Uri> node = _uris.MoveToNode(uri);
                if (node.HasValue)
                {
                    return node.Value;
                }
                var u = new Uri(uri);
                node.Value = u;
                return node.Value;
            }
            return new Uri(uri);
        }

        /// <summary>
        /// Clears all interned URIs.
        /// </summary>
        public static void Clear()
        {
            _uris.Clear();
        }
    }
}
