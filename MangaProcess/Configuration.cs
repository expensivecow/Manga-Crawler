using System;
using System.Net;
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

namespace MangaSplitter.Configuration
{
    public static class ErrorMsg
    {
        public static string INVALID_FOLDER_PATH = "Please enter a valid folder path.";
        public static string INVALID_WEB_LINK = "Please enter a valid web link.";
        public static string INVALID_WEB_SUPPORT = "Currently the system does not support the input link. Please try a different source provider.";
        public static string MSG_NO_PAGES_FOUND = "Error. No pages found.";
        public static string INVALID_TEMP_FOLDER_PATH = "Unable to find temporary folder path";
    }

    public class SourceConfig
    {
        #region Constructors ...
        public SourceConfig() { }

        public SourceConfig(string inputLink, string folderPath) 
        {
            this.inputLink = inputLink;
            this.folderPath = folderPath;

            // assert valid paths
            validLinkAndFolderPath();
        }
        #endregion Constructors ...

        #region Fields ...
        string inputLink;
        string folderPath;
        #endregion Fields ...

        /* 
         * Defines Configuration to find certain elements from source HTML
         * ** DICTIONARY **
         * KEY - Name of website reference
         * stringlist[0] - Last Page Identifier
         * stringlist[1] - Next Page Identifier
         * stringlist[2] - Home Page Identifier
         * stringlist[3] - Image Source Identifier
         */
        private static readonly Dictionary<string, List<KeyValuePair<string, int>>[]> confSource = new Dictionary<string, List<KeyValuePair<string, int>>[]>
        {
            {
                "mangafox.me", 
                new List<KeyValuePair<string, int>>[]
                {
                    // Next Page
                    new List<KeyValuePair<string, int>> 
                    {
                        new KeyValuePair<string,int> ("//div[@class='widepage page']", 1), 
                        new KeyValuePair<string,int> ("//a[@class='btn next_page']", 1)
                    },
                    // Home Page
                    new List<KeyValuePair<string, int>> 
                    {
                        new KeyValuePair<string,int> ("//div[@class='widepage page']", 1), 
                        new KeyValuePair<string,int> ("//div[@id='tool']", 1),
                        new KeyValuePair<string,int> ("//div[@id='series']", 1), 
                        new KeyValuePair<string,int> ("//strong", 1)
                    },
                    // Image Source
                    new List<KeyValuePair<string, int>> 
                    {
                        new KeyValuePair<string,int> ("//meta[@property='og:image']", 1)
                    },
                    // Chapter Source
                    new List<KeyValuePair<string, int>> 
                    {
                        new KeyValuePair<string,int> ("//div[@id='header']", 1), 
                        new KeyValuePair<string,int> ("//div[@class='widepage']", 1), 
                        new KeyValuePair<string,int> ("//div[@class='cl']", 1), 
                        new KeyValuePair<string,int> ("//a[@id='comments']", 1)
                    },
                    // Next Chapter Configuration
                    new List<KeyValuePair<string, int>> 
                    {
                        new KeyValuePair<string,int> ("//body[@id='body']", 1), 
                        new KeyValuePair<string,int> ("//div[@class='tips']", 1), 
                        new KeyValuePair<string,int> ("//div[@style='float:left;width:410px']", 1), 
                        new KeyValuePair<string,int> ("//div[@id='chnav']", 1), 
                        new KeyValuePair<string,int> ("//p", 2), 
                        new KeyValuePair<string,int> (".//a", 1)
                    }
                }
            }
        };

        /**
         * Determines whether source is within the valid configuration list
         * @param (string) host - Full link of web source starting point
         */
        private bool IsValidSource(string host)
        {
            Uri link = new Uri(inputLink);
            return confSource.ContainsKey(link.Authority);
        }

        /** 
         * Returns the identifier used to find the next page
         * @param (List<string>) host - host of web source starting point
         */
        public List<KeyValuePair<string, int>> returnNextPageIdentifiers(string host)
        {
            List<KeyValuePair<string, int>>[] nextPageIdentifiers;
            confSource.TryGetValue(host, out nextPageIdentifiers);
            return nextPageIdentifiers[0];
        }

        /** 
         * Returns the identifier used to find the home page
         * @param (string) host - Host of web source starting point
         */
        public List<KeyValuePair<string, int>> returnHomePageIdentifiers(string host)
        {
            List<KeyValuePair<string, int>>[] homePageIdentifiers;
            confSource.TryGetValue(host, out homePageIdentifiers);
            return homePageIdentifiers[1];
        }

        /** 
         * Returns the identifier used to find the image source
         * @param (string) host - Host of web source starting point
         */
        public List<KeyValuePair<string, int>> returnImageSourceIdentifiers(string host)
        {
            List<KeyValuePair<string, int>>[] imgPageIdentifiers;
            confSource.TryGetValue(host, out imgPageIdentifiers);
            return imgPageIdentifiers[2];
        }

        /** 
         * Returns the identifier used to find the image source
         * @param (string) host - Host of web source starting point
         */
        public List<KeyValuePair<string, int>> returnChapterPageIdentifiers(string host)
        {
            List<KeyValuePair<string, int>>[] ChapterPageIdentifiers;
            confSource.TryGetValue(host, out ChapterPageIdentifiers);
            return ChapterPageIdentifiers[3];
        }

        /** 
         * Returns the identifier used to find the image source
         * @param (string) host - Host of web source starting point
         */
        public List<KeyValuePair<string, int>> returnNextChapterPageIdentifiers(string host)
        {
            List<KeyValuePair<string, int>>[] NextChapterPageIdentifiers;
            confSource.TryGetValue(host, out NextChapterPageIdentifiers);
            return NextChapterPageIdentifiers[4];
        }

        public void validLinkAndFolderPath()
        {
            WebValidator webV = new WebValidator();
            LocalValidator localV = new LocalValidator();

            string errorMsg = "";

            errorMsg = (!webV.isValidURL(inputLink) ? errorMsg + ErrorMsg.INVALID_WEB_LINK + System.Environment.NewLine : errorMsg);
            errorMsg = (!localV.isValidFolderPath(folderPath) ? errorMsg + ErrorMsg.INVALID_FOLDER_PATH + System.Environment.NewLine : errorMsg);
            errorMsg = (!IsValidSource(inputLink) ? errorMsg + ErrorMsg.INVALID_WEB_SUPPORT + System.Environment.NewLine : errorMsg);

            if (!String.IsNullOrEmpty(errorMsg))
            {
                throw new Exception(errorMsg);
            }

            return;
        }

        public string getHost()
        {
            return (new Uri(inputLink)).Host;
        }
    }

    public class WebValidator
    {
        #region Constructors ...
        public WebValidator() { }
        #endregion Constructors ...

        public bool isValidURL(string inputLink)
        {
            Uri uriResult;
            return Uri.TryCreate(inputLink, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }

    public class LocalValidator
    {
        #region Constructors ...
        public LocalValidator() { }
        #endregion Constructors ...

        public bool isValidFolderPath(string inputPath)
        {
            return Directory.Exists(inputPath);
        }
    }

    public class GzipWebClient : WebClient
    {
        #region Constructors ...
        public GzipWebClient() { }
        #endregion Constructors ...

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return request;
        }
    }
}