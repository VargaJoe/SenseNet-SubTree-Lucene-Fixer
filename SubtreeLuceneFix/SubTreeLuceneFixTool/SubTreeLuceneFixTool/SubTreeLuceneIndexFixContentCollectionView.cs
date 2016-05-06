using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.UI.WebControls;
using Lucene.Net.Index;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;
using SenseNet.Portal.Portlets;
using SenseNet.Portal.UI.Controls;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace Azrael.LuceneIndexTools
{
    public class NodeItem
    {
        public Node NodeObj { get; set; }
        public LucObject Luci { get; set; }
        public string NodePath { get; set; }
        public int NodeId { get; set; }
        public int VersionId { get; set; }
        public string NodeName { get; set; }
        public int ModifiedById { get; set; }
        public int CreatedById { get; set; }
        public bool IsLastPublic { get; set; }
        public bool IsLastDraft { get; set; }
        public bool IsMajor { get; set; }
        public bool IsPublic { get; set; }

        public bool IsInDB { get; set; }
        public bool IsInLucene { get; set; }

        public NodeItem()
        {
            NodeObj = null;
            NodePath = string.Empty;
            IsInDB = false;
            IsInLucene = false;
        }

        public NodeItem(Node n)
        {
            NodeObj = n;
            Luci = null;
            NodeName = n.Name;
            NodePath = n.Path;
            NodeId = n.Id;

            CreatedById = n.CreatedById;
            ModifiedById = n.ModifiedById;
            VersionId = n.VersionId;

            ////isLastDraft = n.IsLatestVersion; //??
            //isLastPublic = n.IsLastPublicVersion;
            ////isMajor = false; //?
            ////isPublic = false; //?

            IsInDB = false;
            IsInLucene = false;
        }

        public NodeItem(LucObject l)
        {
            NodeObj = null;
            Luci = l;
            NodeName = l.Name;
            NodePath = l.Path;
            NodeId = l.NodeId;
            CreatedById = l.CreatedById;
            ModifiedById = l.ModifiedById;
            VersionId = l.VersionId;

            //isLastDraft = l.IsLastDraft;
            //isLastPublic = l.IsLastPublic;
            //isMajor = l.IsMajor;
            //isPublic = l.IsPublic;

            IsInDB = false;
            IsInLucene = false;
        }
    }

    public class SubTreeLuceneIndexFixContentCollectionView : ContentCollectionView
    {
        private static System.Threading.Tasks.Task oneTask;
        private static CancellationTokenSource TokenSource;
        private static CancellationToken Ct;
        private static int _count;

        //System.Web.UI.WebControls.Button FixIndex;
        //System.Web.UI.WebControls.Button FixStatus;
        //System.Web.UI.WebControls.Button FixCancel;
        //System.Web.UI.WebControls.Button FixList;
        private Label StatusLabel { get; set; }
        private Repeater FixItems { get; set; }

        private string subTreePath = string.Empty;
        private string DbCacheKey {
            get { return string.Concat("fixLuceneDbItemKey-",subTreePath.Replace("/","-")); }
        }

        private string LucCacheKey
        {
            get { return string.Concat("fixLuceneLucItemKey-", subTreePath.Replace("/", "-")); }
        }

        private ConcurrentDictionary<string, NodeItem> GlobalItemList;

        public int DbNodeItemCount;
        public int LucNodeItemCount;

        //======================================================= Properties

        private List<Node> _dbNodeList;
        public List<Node> DbNodeItemList
        {
            get
            {
                var resultObj = DistributedApplication.Cache.Get(DbCacheKey);
                List<Node> result = (resultObj != null) ? (List<Node>)resultObj : null;
                if (result == null)
                {
                    if (_dbNodeList == null)
                    {
                        _dbNodeList =
                            SenseNet.ContentRepository.Storage.Search.NodeQuery.QueryNodesByPath(
                                this.Model.Content.Path,
                                false)
                                .Nodes.ToList();
                    }
                    //return _dbNodeList;
                    DistributedApplication.Cache.Insert(DbCacheKey, _dbNodeList, null, DateTime.Now.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
                    result = _dbNodeList;
                }
                return result;

            }
        }

        private List<LucObject> _lucNodeList;
        public List<LucObject> LucNodeItemList
        {
            get
            {
                var resultObj = DistributedApplication.Cache.Get(LucCacheKey);
                List<LucObject> result = (resultObj != null) ? (List<LucObject>)resultObj : null;
                if (result == null)
                {
                    if (_lucNodeList == null)
                    {
                        ///no.1
                        //_lucNodeList = SenseNet.ContentRepository.Content.All.DisableAutofilters()
                        //    .DisableLifespan()
                        //    .Where(c => c.InTree(this.Model.Content.Path) && c.Path != this.Model.Content.Path)
                        //    .ToArray()
                        //    .Select(c => c.ContentHandler)
                        //    .ToList();

                        var query = "+InTree:'" + this.Model.Content.Path + "' -Path:'" + this.Model.Content.Path + "'";
                        ///no.2
                        //SenseNet.Search.QuerySettings qs = new SenseNet.Search.QuerySettings();
                        //qs.EnableLifespanFilter = SenseNet.Search.FilterStatus.Disabled;
                        //qs.EnableAutofilters = SenseNet.Search.FilterStatus.Disabled;
                        //_lucNodeList = SenseNet.Search.ContentQuery.CreateQuery(query, qs).Execute().Nodes.ToList();

                        ///no.3
                        var q = LucQuery.Parse(query);
                        q.EnableAutofilters = FilterStatus.Disabled;
                        q.EnableLifespanFilter = FilterStatus.Disabled;
                        _lucNodeList = (from lucObject in q.Execute() select lucObject).ToList();
                    }
                    //return _lucNodeList;
                    DistributedApplication.Cache.Insert(LucCacheKey, _lucNodeList, null, DateTime.Now.AddMinutes(10),
                        System.Web.Caching.Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
                    result = _lucNodeList;
                }
                return result;
            }
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            //FixIndex = this.FindControlRecursive("FixIndex") as Button;
            //FixStatus = this.FindControlRecursive("FixStatus") as Button;
            //FixCancel = this.FindControlRecursive("FixCancel") as Button;
            //FixList = this.FindControlRecursive("FixList") as Button;
            StatusLabel = this.FindControlRecursive("StatusLabel") as Label;
            FixItems = this.FindControlRecursive("FixItems") as Repeater;
            subTreePath = this.Model.Content.Path;

            if (TokenSource == null)
            {
                TokenSource = new CancellationTokenSource();
                Ct = TokenSource.Token;
            }

            if (FixItems != null)
            {
                FixItems.DataSource = null;
                FixItems.DataBind();
            }

            InitGlobalItemList();

            if (oneTask != null && StatusLabel != null)
            {
                StatusLabel.Text = string.Format("IndexFix task is {0} and actual item count is {1}", oneTask.Status.ToString(), _count, ToString());
            }
        }

        protected void FixIndex_OnClick(object sender, EventArgs e)
        {
            if (oneTask != null && oneTask.Status != TaskStatus.Running)
            {
                oneTask.Dispose();
                oneTask = null;
            }
            if (oneTask == null)
            {
                //System.Threading.Tasks.Task t = new System.Threading.Tasks.Task(() => FixIt(), Ct);
                System.Threading.Tasks.Task t = new System.Threading.Tasks.Task(() => FixIt());
                oneTask = t;
                t.Start();
            }
        }

        protected void FixCancel_OnClick(object sender, EventArgs e)
        {
            if (TokenSource != null)
            {
                TokenSource.Cancel();
                TokenSource.Dispose();
                TokenSource = null;
            }

            if (oneTask != null && StatusLabel != null)
            {
                StatusLabel.Text = string.Format("IndexFix task is {0} and actual item count is {1}", oneTask.Status.ToString(), _count, ToString());
            }
        }


        protected void FixList_OnClick(object sender, EventArgs e)
        {
            FixItems.DataSource = GetGlobalItemList().Values.OrderBy(d => d.NodePath);
            FixItems.DataBind();
        }

        protected void FixDiffList_OnClick(object sender, EventArgs e)
        {
            FixItems.DataSource = GetGlobalItemList().Values.Where(ni => !ni.IsInLucene || !ni.IsInDB).OrderBy(d => d.NodePath);
            FixItems.DataBind();
        }

        protected void ClearCache_OnClick(object sender, EventArgs e)
        {
            if (DistributedApplication.Cache.Get(DbCacheKey) != null)
                DistributedApplication.Cache.Remove(DbCacheKey);
            if (DistributedApplication.Cache.Get(LucCacheKey) != null)
                DistributedApplication.Cache.Remove(LucCacheKey);
        }

        //======================================================== Helper methods

        private bool PushItemInDictionary(Node n, bool isLucene)
        {
            string itemId = string.Concat(n.Path.ToLower(), '-', n.Id.ToString(), '-', n.VersionId) ;
            //var item = GlobalItemList.GetOrAdd(n.Path.ToLower(), new NodeItem(n));
            var item = GlobalItemList.GetOrAdd(itemId, new NodeItem(n));
            if (isLucene)
            {
                item.IsInLucene = true;
            }
            else
            {
                item.IsInDB = true;
            }
            return true;
        }
        //ebbol a kettobol lehetne generic
        private bool PushItemInDictionary(LucObject l, bool isLucene)
        {
            string itemId = string.Concat(l.Path.ToLower(), '-', l.NodeId.ToString(), '-', l.VersionId);
            //var item = GlobalItemList.GetOrAdd(l.Path.ToLower(), new NodeItem(l));
            var item = GlobalItemList.GetOrAdd(itemId, new NodeItem(l));
            if (isLucene)
            {
                item.IsInLucene = true;
            }
            else
            {
                item.IsInDB = true;
            }
            return true;
        }

        public ConcurrentDictionary<string, NodeItem> GetGlobalItemList()
        {
            return GlobalItemList;
        }

        public void InitGlobalItemList()
        {
            if (GlobalItemList == null)
            {
                GlobalItemList = new ConcurrentDictionary<string, NodeItem>();
                DbNodeItemCount = DbNodeItemList.Count(n => PushItemInDictionary(n, false));
                LucNodeItemCount = LucNodeItemList.Count(l => PushItemInDictionary(l, true));
            }
        }

        public int GetDiffCount()
        {
            return GetGlobalItemList().Values.Count(ni => !ni.IsInLucene || !ni.IsInDB); 
        }

        private void FixItRecurzive()
        {
            DocumentPopulator dp = new DocumentPopulator();
            dp.RebuildIndex(this.Model.Content.ContentHandler, false, IndexRebuildLevel.DatabaseAndIndex);
        }

        private void FixIt()
        {

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Logger.WriteInformation(40050,
                string.Format("Fix lucene index started: {0:hh\\:mm\\:ss}) on subtree> {1}", stopwatch.Elapsed,
                    this.Model.Content.Path));
            _count = 0;
            try
            {
                //foreach (var n in SenseNet.ContentRepository.Storage.NodeEnumerator.GetNodes(this.Model.Content.Path))
                foreach (
                    var n in
                        GlobalItemList.Values.Where(
                            ni => (!ni.IsInLucene || !ni.IsInDB) && ni.NodePath != subTreePath))
                    //foreach (var n in GlobalItemList.Values)
                {
                    // Poll on this property if you have to do
                    // other cleanup before throwing.
                    if (Ct.IsCancellationRequested)
                    {
                        // Clean up here, then...
                        Ct.ThrowIfCancellationRequested();
                    }

                    _count++;
                    if (n.IsInDB && !n.IsInLucene)
                    {
                        SenseNet.ContentRepository.Content cnt =
                            SenseNet.ContentRepository.Content.LoadByIdOrPath(n.NodePath);
                        cnt.RebuildIndex(false, IndexRebuildLevel.DatabaseAndIndex);
                    }
                    else if (n.IsInLucene && !n.IsInDB)
                    {
                        //StorageContext.Search.SearchEngine.GetPopulator().DeleteForest(new[] { n.nodeId }, false);
                        StorageContext.Search.SearchEngine.GetPopulator().DeleteTree(n.NodePath, false);

                        SenseNet.ContentRepository.Content cnt =
                            SenseNet.ContentRepository.Content.LoadByIdOrPath(n.NodePath);
                        //if (Node.LoadNode(n.nodePath) != null)
                        if (cnt != null)
                        {
                            Logger.WriteInformation(40053,
                                string.Format(string.Format("Content found {0}. Rebuild index after delete...",
                                    n.NodePath)));
                            cnt.RebuildIndex(false, IndexRebuildLevel.DatabaseAndIndex);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Logger.WriteInformation(40051, string.Format("Fix lucene index throw an exception: {0})", e.Message));
            }
            Logger.WriteInformation(40052,
                string.Format("Fix lucene index ended: {0:hh\\:mm\\:ss})", stopwatch.Elapsed));
            stopwatch.Stop();

        }

        protected void FixStatus_OnClick(object sender, EventArgs e)
        {
            if (oneTask != null && StatusLabel != null)
            {
                StatusLabel.Text = string.Format("IndexFix task is {0} and actual item count is {1}", oneTask.Status.ToString(), _count, ToString());
            }
        }
    }
}