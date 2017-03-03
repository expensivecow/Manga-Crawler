using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MangaSplitter.Configuration;
using MangaSplitter.MangaProcess;

namespace MangaSplitter.MangaStructure
{
    public class MangaHolder
    {
        #region Constructors ...
        public MangaHolder()
        {
            this.chapters = new Dictionary<string, MangaChapter>(); 
        }
        #endregion Constructors ...

        #region Fields ...
        private Dictionary<string, MangaChapter> _chapters;

        public Dictionary<string, MangaChapter> chapters
        {
            get { return this._chapters; }
            set { _chapters = value; }
        }
        #endregion Fields ...

        public bool ContainsChapter(String chapterURL)
        {
            return chapters.ContainsKey(chapterURL);
        }

        public void AddToExistingChapter(String chapterURL, MangaPage mangaPage)
        {
            if (this.chapters.ContainsKey(chapterURL))
            {
                this.chapters[chapterURL].TryAddPage(mangaPage);
            }
        }

        public void TryAddChapter(MangaChapter currChapter)
        {
            this.chapters.Add(currChapter.chapterURL, currChapter);
        }
    }

    public class MangaChapter
    {
        #region Constructors ...
        public MangaChapter() 
        { 
            this.pages = new Dictionary<String, List<MangaPage>>();
        }
        public MangaChapter(string chapterURL)
        {
            this.chapterURL = chapterURL;
            this.pages = new Dictionary<String, List<MangaPage>>();
        }
        #endregion Constructors ...

        #region Fields ...
        private Dictionary<String, List<MangaPage>> _pages;
        private string _chapterURL;
        private int _relativeChapterNum;

        public Dictionary<String, List<MangaPage>> pages
        {
            get { return this._pages; }
            set { _pages = value; }
        }

        public string chapterURL
        {
            get { return this._chapterURL; }
            set { _chapterURL = value; }
        }

        public int relativeChapterNum
        {
            get { return this._relativeChapterNum; }
            set { _relativeChapterNum = value; }
        }

        #endregion Fields ...

        public bool ContainsPagesForChapter(String chapterURL) 
        {
            return pages.ContainsKey(chapterURL);
        }

        public void TryAddPage(MangaPage currPage)
        {
            if (this.ContainsPagesForChapter(currPage.chapterURL))
            {
                this.pages[currPage.chapterURL].Add(currPage);
            }
            else
            {
                this.pages.Add(currPage.chapterURL, new List<MangaPage>());
                this.pages[currPage.chapterURL].Add(currPage);
            }
        }
    }

    public class MangaPage
    {
        #region Constructors ...
        public MangaPage() { }
        public MangaPage(string img, string prevLink, string nextLink, string chapterURL, int relPageNum) 
        {
            this.imgSrc = img;
            this.nextLink = nextLink;
            this.chapterURL = chapterURL;
            this.prevLink = prevLink;
            this.relativePageNum = relPageNum;
        }
        #endregion Constructors ...

        #region Fields ...
        private string _imgSrc;
        private string _prevLink;
        private string _nextLink;
        private string _chapterURL;
        private int _relativePageNum;

        public string imgSrc
        {
            get { return this._imgSrc; }
            set { _imgSrc = value; }
        }
        public string prevLink
        {
            get { return this._prevLink; }
            set { _prevLink = value; }
        }
        public string nextLink
        {
            get { return this._nextLink; }
            set { _nextLink = value; }
        }
        public string chapterURL
        {
            get { return this._chapterURL; }
            set { _chapterURL = value; }
        }
        public int relativePageNum
        {
            get { return this._relativePageNum; }
            set { _relativePageNum = value; }
        }
        #endregion Fields ...
    }
}
