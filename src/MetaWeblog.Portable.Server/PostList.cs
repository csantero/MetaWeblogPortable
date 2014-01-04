﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Isam.Esent.Collections.Generic;

namespace MetaWeblog.Portable.Server
{
    public class PostList: IEnumerable<PostInfo> 
    {
        [Serializable]
        public struct PostInfoRecord
        {
            public string Title;
            public string Link;
            public DateTime? DateCreated;
            public string PostId;
            public string UserId;
            public int CommentCount;
            public string PostStatus;
            public string Permalink;
            public string Description;
            public string Categories;

            public PostInfoRecord(PostInfo p)
            {
                this.Title = p.Title;
                this.Link = p.Link;
                this.DateCreated = p.DateCreated;
                this.PostId = p.PostId;
                this.UserId = p.UserId;
                this.CommentCount = p.CommentCount;
                this.PostStatus = p.PostStatus;
                this.Permalink = p.Permalink;
                this.Description = p.Description;
                this.Categories = string.Join(";",p.Categories.Select(s=>s.Trim()));
            }

            public PostInfo ToPostInfo()
            {
                var p = new PostInfo();
                p.Title = this.Title;
                p.Link = this.Link;
                p.DateCreated = this.DateCreated;
                p.PostId = this.PostId;
                p.UserId = this.UserId;
                p.CommentCount = this.CommentCount;
                p.PostStatus = this.PostStatus;
                p.Permalink = this.Permalink;
                p.Description = this.Description;
                var cats = this.Categories.Split(new char[ ] {';'});
                foreach (string cat in cats)
                {
                    p.Categories.Add(cat.Trim());
                }

                return p;
            }
        }

        private readonly PersistentDictionary<string, PostInfoRecord> pdic;
        
        public PostList()
        {
            string mydocs = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string folder = System.IO.Path.Combine(mydocs, typeof (BlogServer).Name + "/" + "PostsDB");
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            this.pdic = new PersistentDictionary<string, PostInfoRecord>(folder);            
        }

        ~PostList()
        {
            if (this.pdic != null)
            {
                this.pdic.Dispose();
            }
        }

        public IEnumerator<PostInfo> GetEnumerator()
        {
            return this.pdic.Values.Select(p=>p.ToPostInfo()).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(PostInfo p)
        {
            this.pdic[p.PostId] = new PostInfoRecord(p);
            this.pdic.Flush();
        }

        public PostInfo Add(DateTime? created, string title, string desc, IList<string> cats, bool publish)
        {
            var p = new PostInfo();
            p.DateCreated = created != null ? created.Value : System.DateTime.Now;

            p.Title = title;
            p.Description = desc;
            p.PostId = System.DateTime.Now.Ticks.ToString();
            p.Link = this.TitleToPostId(p.Title);
            p.Permalink = p.Link;
            p.PostStatus = "published";

            if (cats != null)
            {
                p.Categories.AddRange(cats);
            }


            this.Add(p);

            return p;
        }

        private string TitleToPostId(string t)
        {
            t = StringUtils.CollapseWhiteSpace(t);
            var sb = new System.Text.StringBuilder(t.Length);
            foreach (char c in t)
            {
                if (Char.IsWhiteSpace(c))
                {
                    sb.Append("-");
                }
                else if (c == '?' || c == '.' || c == '!' || c == '!' || c == '$' || c == '@')
                {
                    // don't include these
                }
                else if (c == '&')
                {
                    sb.Append("-and-");
                }
                else if (c == '<')
                {
                    sb.Append("-lt-");
                }
                else if (c == '>')
                {
                    sb.Append("-gt-");
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public PostInfo TryGetPostById(string id)
        {
            if (pdic.ContainsKey(id))
            {
                return pdic[id].ToPostInfo();
            }
            return null;
        }

        public PostInfo TryGetPostByLink(string link)
        {
            var pair = this.pdic.FirstOrDefault(i => i.Value.Link == link);
            if (pair.Value.PostId != null)
            {
                return pair.Value.ToPostInfo();
            }
            return null;
        }

        public int Count
        {
            get
            {
                return this.pdic.Count;
            }
        }

        public HashSet<string> GetCategories()
        {
            var hs = new HashSet<string>();
            foreach (var post in this)
            {
                foreach (var cat in post.Categories)
                {
                    hs.Add(cat);
                }
            }
            return hs;
        }

        public Dictionary<string,List<PostInfo>> GetPostsByCategory()
        {
            var dic = new Dictionary<string,List<PostInfo>>();
            foreach (var post in this)
            {
                foreach (var cat in post.Categories)
                {
                    if (!dic.ContainsKey(cat))
                    {
                        dic[cat]= new List<PostInfo>();
                    }
                    var list = dic[cat];
                    list.Add(post);
                }
            }
            return dic;
        }

    }
}