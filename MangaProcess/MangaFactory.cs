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
using System.Net;
using HtmlAgilityPack;
using MangaSplitter.MangaStructure;
using MangaSplitter.Configuration;
using Ionic.Zip;

namespace MangaSplitter.MangaProcess
{
    public class MangaFactory
    {
        #region Constructors ...
        public MangaFactory() { }
        public MangaFactory(string link, string path) 
        {
            inputLink = link;
            folderPath = path;
        }
        #endregion Constructors ...

        #region Fields ...
        private string inputLink;
        private string folderPath;
        private Queue<KeyValuePair<string, int>> nextPageKey;
        private Queue<KeyValuePair<string, int>> homePageKey;
        private Queue<KeyValuePair<string, int>> imgPageKey;
        private Queue<KeyValuePair<string, int>> chapterPageKey;
        private Queue<KeyValuePair<string, int>> nextChapterPageKey;
        #endregion Fields ...
 
        private void loadFromSourceConfig()
        {
            SourceConfig sourceConf = new SourceConfig(inputLink, folderPath);
            this.nextPageKey = new Queue<KeyValuePair<string, int>>(sourceConf.returnNextPageIdentifiers(sourceConf.getHost()));
            this.homePageKey = new Queue<KeyValuePair<string, int>>(sourceConf.returnHomePageIdentifiers(sourceConf.getHost()));
            this.imgPageKey = new Queue<KeyValuePair<string, int>>(sourceConf.returnImageSourceIdentifiers(sourceConf.getHost()));
            this.chapterPageKey = new Queue<KeyValuePair<string, int>>(sourceConf.returnChapterPageIdentifiers(sourceConf.getHost()));
            this.nextChapterPageKey = new Queue<KeyValuePair<string, int>>(sourceConf.returnNextChapterPageIdentifiers(sourceConf.getHost()));
            return;
        }
        private string getChapterURL(string inputLink)
        {
            Uri uri = new Uri(inputLink);
            string result = string.Format("{0}://{1}", uri.Scheme, uri.Authority);

            for (int i = 0; i < uri.Segments.Length - 1; i++)
            {
                result += uri.Segments[i];
            }

            result = result.Trim("/".ToCharArray()); // remove trailing `/`

            return result;
        }
        private string getElementFromKey(HtmlNode doc, Queue<KeyValuePair<string, int>> confKeys, string keyElement)
        {
            HtmlNode test = tailRecursiveSearch(doc, new Queue<KeyValuePair<string, int>>(confKeys));

            if (test == null)
            {
                return null;
            }
            else 
            {
                string urlFromPage = test.Attributes[keyElement].Value;
                
                if (String.IsNullOrEmpty(urlFromPage))
                {
                    return null;
                }
                else
                {
                    return urlFromPage;
                }
            }
        }
        private HtmlNode tailRecursiveSearch(HtmlNode doc, Queue<KeyValuePair<string, int>> confKeys)
        {
            // Base case
            if (confKeys.Count == 0)
            {
                return doc;
            }
            else
            {
                KeyValuePair<string, int> currentConf = confKeys.Dequeue();
                string key = currentConf.Key;
                int val = currentConf.Value;

                HtmlNodeCollection collection = doc.SelectNodes(key);
                HtmlNode result = null;

                Queue<KeyValuePair<string, int>> tailQueue = new Queue<KeyValuePair<string, int>>(confKeys);

                if (collection != null && collection.Count() >= val)
                {
                    HtmlNode tempDoc = null;
                    for (int i = 0; i < val; i++)
                    {
                        tempDoc = collection[i];
                    }

                    result = tailRecursiveSearch(tempDoc, tailQueue);
                }
                else
                {
                    return null;
                }
                
                return result;
            }
        }
        private HtmlDocument getPageSource(string inputLink)
        {
            // Websites can block us and redirect to some trolling image, and we dont want that...
            System.Threading.Thread.Sleep(200);

            string html;

            using (GzipWebClient wc = new GzipWebClient())
                html = wc.DownloadString(inputLink);

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            return htmlDoc;
        }
        private string getURL(string inputTag, string currPage)
        {
            // Absolute
            if((new WebValidator()).isValidURL(inputTag))
            {
                return inputTag;    
            }
            else
            {
                Uri host = new Uri(currPage);

                // Relative to host
                if (!String.IsNullOrEmpty(inputTag))
                {
                    if (inputTag[0] == '/')
                    {
                        return host.Host + inputTag;
                    }
                    // Relative to current
                    else if (inputTag[0] == '.')
                    {
                        return currPage + inputTag;
                    }
                    else
                    {
                        return getChapterURL(currPage) + "/" + inputTag;
                    }
                }
                // nothing found or next chapter is void
                else if (!String.IsNullOrEmpty(inputTag) && inputTag.Equals("javascript:void(0);"))
                {
                    return null;
                }
                // Default is current page/../[path here]
                else
                {
                    return null;
                }
            }
        }

        public void Process()
        {
            loadFromSourceConfig();

            // make sure web source is available
            WebValidator webVal = new WebValidator();

            // ensure link is valid
            if (!webVal.isValidURL(inputLink))
                throw new Exception(ErrorMsg.INVALID_WEB_LINK);
            
            // create process tree
            string link = inputLink;
            int currChapterCount = 1;
            int currPageNum = 1;

            MangaHolder currManga = new MangaHolder();
            MangaChapter currChapter = new MangaChapter();
            while(!String.IsNullOrEmpty(link))
            {
                HtmlDocument currentDoc = getPageSource(link);
                
                string lastPageURL = link;
                string nextPageElement = getElementFromKey(currentDoc.DocumentNode, new Queue<KeyValuePair<string, int>>(nextPageKey), "href");

                string nextPageURL = getURL(getElementFromKey(currentDoc.DocumentNode, new Queue<KeyValuePair<string, int>>(nextPageKey), "href"), link);
                string chapterPageURL = getURL(getElementFromKey(currentDoc.DocumentNode, new Queue<KeyValuePair<string, int>>(chapterPageKey), "href"), link);
                string imageSrcURL = getURL(getElementFromKey(currentDoc.DocumentNode, new Queue<KeyValuePair<string, int>>(imgPageKey), "content"), link);

                // If theres a chapter configuration make there we didnt miss any chapters
                if (nextChapterPageKey.Count > 0 && nextPageElement != null && nextPageElement.Equals("javascript:void(0);"))
                {
                    nextPageURL = getURL(getElementFromKey(currentDoc.DocumentNode, new Queue<KeyValuePair<string, int>>(nextChapterPageKey), "href"), link);
                }
                if (
                        !String.IsNullOrEmpty(lastPageURL) &&
                        !String.IsNullOrEmpty(nextPageURL) && 
                        !String.IsNullOrEmpty(imageSrcURL) && 
                        !String.IsNullOrEmpty(chapterPageURL)
                    )
                {
                    
                    // Base Case
                    if (!currManga.ContainsChapter(chapterPageURL))
                    {
                        currPageNum = 1;

                        MangaChapter chapter = new MangaChapter(chapterPageURL);
                        chapter.TryAddPage(new MangaPage(imageSrcURL, lastPageURL, nextPageURL, chapterPageURL, currPageNum));
                        chapter.relativeChapterNum = currChapterCount;

                        currChapterCount++;
                        //chapter.AddPage(new MangaPage(imageSrcURL, lastPageURL, nextPageURL, chapterPageURL, currPageNum));
                        currManga.TryAddChapter(chapter);
                    }
                    else
                    {
                        currManga.AddToExistingChapter(chapterPageURL, new MangaPage(imageSrcURL, lastPageURL, nextPageURL, chapterPageURL, currPageNum));
                    }

                    currPageNum++;
                }

                link = nextPageURL;
            }

            //Create folder in temp
            string tempPath = Path.GetTempPath();
            if (String.IsNullOrEmpty(tempPath))
            {
                throw new Exception(ErrorMsg.INVALID_TEMP_FOLDER_PATH);
            }

            tempPath = tempPath + DateTime.Now.Ticks;
            // DateTime.Now.Ticks will make the folder path unique af
            Directory.CreateDirectory(tempPath);
            //

            Console.WriteLine("Temp Path: " + tempPath);
            string mangaPath = tempPath;
            string chapterFolderPath;
            string fileName;
            foreach (KeyValuePair<String, MangaChapter> chapter in currManga.chapters)
            {
                chapterFolderPath = tempPath + "\\" + chapter.Value.relativeChapterNum.ToString();
                Directory.CreateDirectory(chapterFolderPath);
                foreach (List<MangaPage> pages in chapter.Value.pages.Values)
                {
                    foreach (MangaPage page in pages)
                    {
                        fileName = chapterFolderPath + "\\" + page.relativePageNum + ".jpg";

                        Console.WriteLine("Downloading src: " + page.imgSrc);
                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(page.imgSrc, fileName);
                        }
                    }
                }
            }

            string zipName = folderPath + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip";
            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(tempPath);
                zip.Save(zipName);
            }

            System.IO.DirectoryInfo di = new DirectoryInfo(tempPath);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }

            //test(getPageSource(inputLink), inputLink);
            System.Windows.Forms.MessageBox.Show("Finished downloading images. Please locate to " + folderPath + " for the manga ripped.");
            
            return;
        }


        private void test(HtmlDocument currentDoc, string inputLink) 
        {
            string nextChapterURL = getURL(getElementFromKey(currentDoc.DocumentNode, new Queue<KeyValuePair<string, int>>(nextChapterPageKey), "href"), inputLink);

            Console.WriteLine("Next Chapter Page: " + nextChapterURL + System.Environment.NewLine);
        }
    }
}
