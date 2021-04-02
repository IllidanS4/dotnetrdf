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
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;
using VDS.RDF.Query.FullText.Schema;
using VDS.RDF.Query.FullText.Search.Lucene;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Query.FullText.Indexing.Lucene
{
    /// <summary>
    /// Abstract Implementation of a simple Full Text Indexer using Lucene.Net which indexes the full text of literal objects and associated a specific Node with that full text.
    /// </summary>
    public abstract class BaseSimpleLuceneIndexer
        : BaseSimpleFullTextIndexer, IConfigurationSerializable
    {
        private IndexingMode _mode;
        private Directory _indexDir;
        private Analyzer _analyzer;
        private IndexWriter _writer;
        private IFullTextIndexSchema _schema;
        private NTriplesFormatter _formatter = new NTriplesFormatter();
        private IndexReader _reader;

        /// <summary>
        /// Creates a new Simple Lucene Indexer.
        /// </summary>
        /// <param name="indexDir">Directory.</param>
        /// <param name="analyzer">Analyzer.</param>
        /// <param name="schema">Index Schema.</param>
        /// <param name="mode">Indexing Mode.</param>
        public BaseSimpleLuceneIndexer(Directory indexDir, Analyzer analyzer, IFullTextIndexSchema schema, IndexingMode mode)
        {
            if (_mode == IndexingMode.Custom) throw new ArgumentException("Cannot use IndexingMode.Custom with the BaseSimpleLuceneIndexer");
            _mode = mode;
            _indexDir = indexDir;
            _analyzer = analyzer;
            _schema = schema;
            _writer = new IndexWriter(indexDir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
        }

        /// <summary>
        /// Gets the Indexing Mode used.
        /// </summary>
        public override IndexingMode IndexingMode
        {
            get
            {
                return _mode;
            }
        }

        private void EnsureWriterOpen()
        {
            if (_writer == null)
            {
                _writer = new IndexWriter(_indexDir, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            }
        }

        private void EnsureWriterClosed()
        {
            if (_writer != null)
            {
                _writer.Commit();
                _writer.Dispose();
                _writer = null;
            }
        }

        private void EnsureReaderOpen()
        {
            if (_reader == null)
            {
                _reader = IndexReader.Open(_indexDir, false);
            }
            else if (!_reader.IsCurrent())
            {
                _reader.Dispose();
                _reader = IndexReader.Open(_indexDir, false);
            }
        }

        private void EnsureReaderClosed()
        {
            if (_reader != null)
            {
                _reader.Commit();
                _reader.Dispose();
                _reader = null;
            }
        }

        /// <summary>
        /// Indexes a Node and some full text as a Lucene document.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        /// <param name="n">Node.</param>
        /// <param name="text">Full Text.</param>
        protected override void Index(String graphUri, INode n, string text)
        {
            Document doc = CreateDocument(graphUri, n, text);
            EnsureReaderClosed();
            EnsureWriterOpen();
            _writer.AddDocument(doc);
        }

        /// <summary>
        /// Unindexes a Node and some full text.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        /// <param name="n">Node.</param>
        /// <param name="text">Full Text.</param>
        protected override void Unindex(String graphUri, INode n, string text)
        {
            var query = new TermQuery(new Term(_schema.HashField, GetHash(graphUri, n, text)));

            //Close the existing writer
            EnsureWriterClosed();

            //Create a read/write Index Reader to modify the index
            EnsureReaderOpen();
            var searcher = new IndexSearcher(_reader);
            var collector = new DocCollector();
            searcher.Search(query, collector);

            //Delete at most one instance
            if (collector.Count > 0)
            {
                _reader.DeleteDocument(collector.Documents.First().Key);
            }
        }

        /// <summary>
        /// Creates a Lucene document to add to the index.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        /// <param name="n">Node.</param>
        /// <param name="text">Full Text.</param>
        /// <returns></returns>
        /// <remarks>
        /// May be overridden by derived classes that wish to implement custom indexing behaviour.
        /// </remarks>
        protected virtual Document CreateDocument(String graphUri, INode n, String text)
        {
            var doc = new Document();

            //Create the Fields that represent the Node associated with the Indexed Text
            var nodeTypeField = new Field(_schema.NodeTypeField, n.NodeType.ToLuceneFieldValue(), Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            doc.Add(nodeTypeField);
            var nodeValueField = new Field(_schema.NodeValueField, n.ToLuceneFieldValue(), Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            doc.Add(nodeValueField);
            var meta = n.ToLuceneFieldMeta();
            if (meta != null)
            {
                var nodeMetaField = new Field(_schema.NodeMetaField, meta, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
                doc.Add(nodeMetaField);
            }

            //Add the field for the Indexed Text
            var indexField = new Field(_schema.IndexField, text, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.NO);
            doc.Add(indexField);

            //Add the field for the Graph URI if applicable
            if (!String.IsNullOrEmpty(graphUri))
            {
                var graphField = new Field(_schema.GraphField, graphUri, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
                doc.Add(graphField);
            }

            //Create the hash field which we use for Unindexing things
            var hashField = new Field(_schema.HashField, GetHash(graphUri, n, text), Field.Store.NO, Field.Index.NOT_ANALYZED, Field.TermVector.NO);
            doc.Add(hashField);

            return doc;
        }

        /// <summary>
        /// Gets the hash that should be included as part of a document so that it can be unindexed if desired.
        /// </summary>
        /// <param name="graphUri">Graph URI.</param>
        /// <param name="n">Node.</param>
        /// <param name="text">Full Text.</param>
        /// <returns></returns>
        protected String GetHash(String graphUri, INode n, String text)
        {
            var hashInput = _formatter.FormatUri(graphUri);
            hashInput += _formatter.Format(n);
            hashInput += text;
            return hashInput.GetSha256Hash();
        }

        /// <summary>
        /// Flushes any changes to the index.
        /// </summary>
        public override void Flush()
        {
            if (_writer != null)
            {
                _writer.Commit();
            }
            else if (_reader != null)
            {
                _reader.Commit();
            }
        }

        /// <summary>
        /// Lucene dispose logic that ensures changes to the index are discarded.
        /// </summary>
        protected override void DisposeInternal()
        {
            if (_writer != null)
            {
                _writer.Commit();
                if (_indexDir.isOpen_ForNUnit) _writer.Dispose();
            }
        }

        /// <summary>
        /// Serializes the Configuration of the Indexer.
        /// </summary>
        /// <param name="context">Serialization Context.</param>
        public virtual void SerializeConfiguration(ConfigurationSerializationContext context)
        {
            context.EnsureObjectFactory(typeof(FullTextObjectFactory));

            INode indexerObj = context.NextSubject;
            INode rdfType = context.Graph.CreateUriNode(UriFactory.Create(RdfSpecsHelper.RdfType));
            INode dnrType = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyType));
            INode indexerClass = context.Graph.CreateUriNode(UriFactory.Create(FullTextHelper.ClassIndexer));
            INode index = context.Graph.CreateUriNode(UriFactory.Create(FullTextHelper.PropertyIndex));
            INode schema = context.Graph.CreateUriNode(UriFactory.Create(FullTextHelper.PropertySchema));
            INode analyzer = context.Graph.CreateUriNode(UriFactory.Create(FullTextHelper.PropertyAnalyzer));

            //Basic Properties
            context.Graph.Assert(indexerObj, rdfType, indexerClass);
            context.Graph.Assert(indexerObj, dnrType, context.Graph.CreateLiteralNode(GetType().FullName + ", dotNetRDF.Query.FullText"));

            //Serialize and link the Index
            INode indexObj = context.Graph.CreateBlankNode();
            context.NextSubject = indexObj;
            _indexDir.SerializeConfiguration(context);
            context.Graph.Assert(indexerObj, index, indexObj);

            //Serialize and link the Schema
            INode schemaObj = context.Graph.CreateBlankNode();
            context.NextSubject = schemaObj;
            if (_schema is IConfigurationSerializable)
            {
                ((IConfigurationSerializable)_schema).SerializeConfiguration(context);
            }
            else
            {
                throw new DotNetRdfConfigurationException("Unable to serialize configuration for this Lucene Indexer as the IFullTextSchema used does not implement the IConfigurationSerializable interface");
            }
            context.Graph.Assert(indexerObj, schema, schemaObj);

            //Serialize and link the Analyzer
            INode analyzerObj = context.Graph.CreateBlankNode();
            context.NextSubject = analyzerObj;
            _analyzer.SerializeConfiguration(context);
            context.Graph.Assert(indexerObj, index, analyzerObj);
        }
    }
}
