using Indexer.Models;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Indexer
{
    public static class LuceneEngine
    {
        private static Analyzer _Analyzer;
        private static Lucene.Net.Store.Directory _Directory;
        private static IndexWriter _IndexWriter;
        private static IndexSearcher _IndexSearcher;
        private static QueryParser _QueryParser;
        private static Query _Query;
        private static string _IndexPath = @"C:\LuceneIndex";
        private static ConcurrentBag<FileItem> fileList;

        static LuceneEngine()
        {
            _Analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

            _Directory = FSDirectory.Open(_IndexPath);
            fileList = new ConcurrentBag<FileItem>();
        }

        public static void AddToIndex(IEnumerable<FileItem> values)
        {
            using (_IndexWriter = new IndexWriter(_Directory, _Analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                _IndexWriter.DeleteAll();
                var i = 0;
                foreach (var loopEntity in values)
                {
                    var doc = new Document();

                    doc.Add(new Field("Path", loopEntity.Path, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("Name", loopEntity.Name, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("Hash", CreateMD5(loopEntity.Path), Field.Store.YES, Field.Index.ANALYZED));

                    _IndexWriter.AddDocument(doc);

                    i++;
                }
                _IndexWriter.Commit();
                _IndexWriter.Optimize();
            }
        }

        public static List<FileItem> Search(string field, string keyword)
        {
            // Üzerinde arama yapmak istediğimiz field için bir query oluşturuyoruz.
            _QueryParser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, field, _Analyzer)
            {
                DefaultOperator = QueryParser.AND_OPERATOR,
                PhraseSlop = 2,
                AllowLeadingWildcard = true
            };
            _Query = _QueryParser.Parse(keyword);

            using (_IndexSearcher = new IndexSearcher(_Directory, true))
            {
                var result = _IndexSearcher.Search(_Query, 100).ScoreDocs.AsEnumerable();

                return _mapLuceneToDataList(result, _IndexSearcher).ToList();
            }
        }

        private static FileItem _mapLuceneDocumentToData(Document doc)
        {
            return new FileItem
            {
                Path = doc.Get("Path"),
                Name = doc.Get("Name"),
                Hash = doc.Get("Hash")
            };
        }

        private static IEnumerable<FileItem> _mapLuceneToDataList(IEnumerable<ScoreDoc> hits,
            IndexSearcher searcher)
        {
            return hits.Select(hit => _mapLuceneDocumentToData(searcher.Doc(hit.Doc))).ToList();
        }

        public static void DirSearch(string sDir)
        {
            try
            {
                GetFilesInDir(sDir);

                var dirs = System.IO.Directory.GetDirectories(sDir);

                Parallel.ForEach(dirs, (d) =>
                {
                    DirectoryInfo info = new DirectoryInfo(d);
                    if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden ||
                        info.Attributes == FileAttributes.System || !CheckWritePermissionOnDir(d))
                        return;

                    GetFilesInDir(d);

                    DirSearch(d);
                });
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        private static void GetFilesInDir(string d)
        {
            var files = System.IO.Directory.GetFiles(d);
            foreach (string f in files)
            {
                fileList.Add(new FileItem() { Path = f, Name = Path.GetFileName(f) });
            }
        }

        public static bool CheckWritePermissionOnDir(string path)
        {
            try
            {
                System.IO.Directory.GetFiles(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task IndexerAsync()
        {
            var paths = System.Configuration.ConfigurationManager.AppSettings["IndexPath"].Split(',').ToList();
            foreach (var item in paths)
            {
                DirSearch(item);
            }
            AddToIndex(fileList);
            ClearBag(fileList);
        }

        public static void DeleteOldFiles()
        {
            var path = $"{ConfigurationManager.AppSettings["IndexPath"].Split(',')[0]}temp/";
            System.IO.DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }

        public static void ClearBag<T>(ConcurrentBag<T> bag)
        {
            while (!bag.IsEmpty)
            {
                bag.TryTake(out _);
            }
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                foreach (var t in hashBytes)
                {
                    sb.Append(t.ToString("X2"));
                }
                return sb.ToString();
            }
        }

    }
}